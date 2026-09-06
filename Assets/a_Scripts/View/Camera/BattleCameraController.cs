using System.Collections;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗摄像机控制器 — 回合制战斗镜头调度中心
    ///
    /// 监听 BattleEventCenter 事件，通过 Cinemachine 优先级系统驱动镜头切换：
    /// - 入场序列（BattleStart）: 入場1 → 入場2 → 广角待机
    /// - 回合开始（TurnStart）: 切换到当前行动角色侧前方特写
    /// - 攻击运镜（UnitAttack）: 攻击者侧面特写 → [Blend] → 受击目标特写（同步震屏）→ 还原角色镜头
    /// - 受击反应（UnitHit）: 外部触发的震屏 + FOV 冲击
    /// - 战斗结束（BattleEnd）: 回到广角待机
    ///
    /// 挂载在场景中的 BattleCameraController GameObject 上。
    /// 需要 Inspector 中拖入所有 CinemachineVirtualCamera 和 CinemachineBrain 引用。
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class BattleCameraController : MonoBehaviour
    {
        // ── 常量：摄像机优先级 ──
        private const int PRIORITY_OFF = 0;
        private const int PRIORITY_IDLE = 5;
        private const int PRIORITY_CHARACTER = 15;
        private const int PRIORITY_ATTACK = 25;
        private const int PRIORITY_HIT = 25;       // 与 ATTACK 同级，Brain Blend 自动过渡
        private const int PRIORITY_ENTRANCE = 30;

        // ── 常量：时间参数 ──
        private const float ENTRANCE_HOLD_DURATION = 0.5f;
        private const float ATTACK_WINDUP_DURATION = 1.5f;  // 攻击者起手动作时间
        private const float HIT_HOLD_DURATION = 5f;        // 受击镜头停留时间
        private const float FOV_PUNCH_STRENGTH = 5f;
        private const float FOV_PUNCH_DURATION = 0.15f;
        private const float HIT_SHAKE_INTENSITY = 0.8f;   // 受击震屏强度（Noise 幅度）
        private const float HIT_SHAKE_DURATION = 0.25f;   // 受击震屏时长（秒）
        private const float HIT_SHAKE_FREQUENCY = 15f;    // 受击震屏噪声频率

        // ── Inspector：Cinemachine 核心 ──
        [Header("Cinemachine 核心")]
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        // ── Inspector：全局镜头（场景预设，不需要动态 Follow/LookAt）──
        [Header("全局镜头（场景预设）")]
        [SerializeField] private CinemachineVirtualCamera _vcamEntrance1;
        [SerializeField] private CinemachineVirtualCamera _vcamEntrance2;
        [SerializeField] private CinemachineVirtualCamera _vcamIdleWide;

        // ── Inspector：动态镜头（运行时设置 Follow/LookAt）──
        [Header("动态镜头（运行时赋值 Follow/LookAt）")]
        [SerializeField] private CinemachineVirtualCamera _vcamCharacterFocus;
        [SerializeField] private CinemachineVirtualCamera _vcamAttack;
        [SerializeField] private CinemachineVirtualCamera _vcamHit;

        // ── Inspector：攻击构图 ──
        [Header("攻击构图")]
        [SerializeField] private CinemachineTargetGroup _attackTargetGroup;

        // ── 内部状态 ──
        private Camera _mainCamera;
        private float _defaultFov;
        private Coroutine _attackSequenceCoroutine;
        private Coroutine _shakeCoroutine;

        #region 生命周期

        private void Awake()
        {
            // 缓存摄像机引用（URP 规范：避免使用 Camera.main）
            _mainCamera = _brain != null ? _brain.OutputCamera : null;

            // 回退：若 CinemachineBrain.OutputCamera 为空，直接从 Brain 所在 GameObject 获取 Camera
            if (_mainCamera == null && _brain != null)
                _mainCamera = _brain.GetComponent<Camera>();

            if (_mainCamera == null)
            {
                Debug.LogError("[BattleCameraController] CinemachineBrain 或 OutputCamera 未找到。" +
                               "请在 Inspector 中为 BattleCameraController 拖入 CinemachineBrain 引用，" +
                               "并确保 Brain 所在 GameObject 上有 Camera 组件。");
                return;
            }
            _defaultFov = _mainCamera.fieldOfView;

            // 初始状态：仅广角镜头激活，其他休眠
            ResetAllPriorities();
            if (_vcamIdleWide != null)
                _vcamIdleWide.Priority = PRIORITY_IDLE;
        }

        private void OnEnable()
        {
            BattleEventCenter.OnBattleStart += OnBattleStart;
            BattleEventCenter.OnTurnStart += OnTurnStart;
            BattleEventCenter.OnUnitAttack += OnUnitAttack;
            BattleEventCenter.OnUnitHit += OnUnitHit;
            BattleEventCenter.OnCameraShake += OnCameraShake;
            BattleEventCenter.OnBattleEnd += OnBattleEnd;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleStart -= OnBattleStart;
            BattleEventCenter.OnTurnStart -= OnTurnStart;
            BattleEventCenter.OnUnitAttack -= OnUnitAttack;
            BattleEventCenter.OnUnitHit -= OnUnitHit;
            BattleEventCenter.OnCameraShake -= OnCameraShake;
            BattleEventCenter.OnBattleEnd -= OnBattleEnd;
        }

        private void OnDestroy()
        {
            // 终止所有 DOTween 动画，防止访问已销毁对象
            if (_mainCamera != null)
                _mainCamera.DOKill();
        }

        #endregion

        #region 入场序列

        /// <summary>
        /// 入场序列协程：
        /// 1. 入場1 高优先级激活 → 停留 ENTRANCE_HOLD_DURATION
        /// 2. Blend 到 入場2 → 等待 Blend 完成 + 停留
        /// 3. Blend 到广角待机 → 入场结束，等待首回合
        /// </summary>
        private void OnBattleStart()
        {
            StartCoroutine(EntranceSequence());
        }

        private IEnumerator EntranceSequence()
        {
            ResetAllPriorities();

            if (_vcamEntrance1 != null)
            {
                // 第一步：入場1
                _vcamEntrance1.Priority = PRIORITY_ENTRANCE;
                yield return new WaitForSeconds(ENTRANCE_HOLD_DURATION);

                // 第二步：切换到入場2
                _vcamEntrance1.Priority = PRIORITY_OFF;
                if (_vcamEntrance2 != null)
                {
                    _vcamEntrance2.Priority = PRIORITY_ENTRANCE;
                    float blendTime = _brain != null ? _brain.m_DefaultBlend.m_Time : 0.8f;
                    yield return new WaitForSeconds(blendTime);
                    yield return new WaitForSeconds(ENTRANCE_HOLD_DURATION);
                    _vcamEntrance2.Priority = PRIORITY_OFF;
                }
            }

            // 第三步：回到广角待机
            if (_vcamIdleWide != null)
                _vcamIdleWide.Priority = PRIORITY_IDLE;
        }

        #endregion

        #region 回合镜头

        /// <summary>
        /// 轮到某角色行动时，摄像机平滑过渡到该角色侧前方特写
        /// </summary>
        private void OnTurnStart(BattleEntityData entity)
        {
            if (entity == null || _vcamCharacterFocus == null) return;

            Transform targetTransform = GetCharacterTransform(entity);
            if (targetTransform == null)
            {
                Debug.LogWarning($"[BattleCameraController] 未找到角色 {entity.heroName} 的 Transform，无法切换回合镜头");
                return;
            }

            // 动态设置 Follow 和 LookAt
            _vcamCharacterFocus.m_Follow = targetTransform;
            _vcamCharacterFocus.m_LookAt = targetTransform;
            if (entity.team == BattleTeam.Enemy)
            {
                // 敌人回合时，镜头稍微拉远一点
                _vcamCharacterFocus.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(1.9f, 1.5f, -4f);
            }
            else
            {
                // 玩家回合时，镜头稍微拉近一点
                _vcamCharacterFocus.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(1.91f, 1.13f, -1.99f);
            }


            // 激活角色镜头（CinemachineBrain 自动 Blend）
            _vcamCharacterFocus.Priority = PRIORITY_CHARACTER;
            if (_vcamIdleWide != null)
                _vcamIdleWide.Priority = PRIORITY_IDLE;
        }

        #endregion

        #region 攻击运镜（三段式：攻击者 → 受击目标 → 还原）

        /// <summary>
        /// 攻击运镜序列：
        /// 1. 切到 Shot_Attack（侧面拍攻击者），停留 ATTACK_WINDUP_DURATION
        /// 2. Brain Blend 平滑过渡到 Shot_Hit（正面拍受击目标），同步触发震屏 + FOV 冲击
        /// 3. 停留 HIT_HOLD_DURATION 后还原到角色回合镜头
        /// </summary>
        private void OnUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (source == null) return;

            // 终止之前的攻击序列（防止多次攻击重叠）
            if (_attackSequenceCoroutine != null)
            {
                StopCoroutine(_attackSequenceCoroutine);
                _attackSequenceCoroutine = null;
            }

            _attackSequenceCoroutine = StartCoroutine(AttackSequence(source, target));
        }

        private IEnumerator AttackSequence(BattleEntityData source, BattleEntityData target)
        {
            Transform sourceTransform = GetCharacterTransform(source);
            Transform targetTransform = GetCharacterTransform(target);

            if (sourceTransform == null)
            {
                Debug.LogWarning($"[BattleCameraController] 攻击方 {source.heroName} 无 Transform，跳过攻击运镜");
                _attackSequenceCoroutine = null;
                yield break;
            }

            // ── 阶段一：攻击者镜头（硬切，不平滑过渡）──
            if (_vcamAttack != null)
            {
                // Follow = 攻击者
                _vcamAttack.m_Follow = sourceTransform;

                _vcamAttack.m_LookAt = sourceTransform;

                if (source.team == BattleTeam.Enemy)
                {
                    // 敌人攻击时，镜头稍微拉远一点
                    _vcamAttack.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(-0.12f, 0.8f, 2.18f);
                }
                else
                {
                    // 玩家攻击时，镜头稍微拉近一点
                    _vcamAttack.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(-0.12f, 1.54f, 2.18f);
                }

                // 保存当前混合设置，临时切换为硬切（无过渡）
                CinemachineBlendDefinition savedBlend = _brain.m_DefaultBlend;
                _brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);


                // 激活攻击镜头，关闭角色镜头
                _vcamAttack.Priority = PRIORITY_ATTACK;

                // 等待一帧让硬切生效，然后恢复默认混合供后续阶段使用
                yield return null;
                _brain.m_DefaultBlend = savedBlend;
            }

            if (_vcamCharacterFocus != null)
                _vcamCharacterFocus.Priority = PRIORITY_OFF;

            // 等待攻击起手动作
            yield return new WaitForSeconds(ATTACK_WINDUP_DURATION);

            // ── 阶段二：受击目标镜头（Brain 自动 Blend 从 Attack → Hit）──
            if (_vcamHit != null && targetTransform != null)
            {
                // 设置受击镜头：Follow + LookAt 都指向目标
                _vcamHit.m_Follow = targetTransform;
                _vcamHit.m_LookAt = targetTransform;
                if (target.team == BattleTeam.Enemy)
                {
                    // 敌人受击时，镜头稍微拉远一点
                    _vcamHit.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(-1.2f, 1f, 4f);
                }
                else
                {
                    // 玩家受击时，镜头稍微拉近一点
                    _vcamHit.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(-1.29f, 2.08f, 3.47f);
                }
                // Attack 降级，Hit 升级（同优先级，Brain 自动 Blend）
                _vcamHit.Priority = PRIORITY_HIT;
            }

            if (_vcamAttack != null)
                _vcamAttack.Priority = PRIORITY_OFF;

            // 命中时刻：FOV 冲击 + 震屏
            PlayFovPunch();
            TriggerShake(HIT_SHAKE_INTENSITY, HIT_SHAKE_DURATION);

            // 等待受击镜头停留
            yield return new WaitForSeconds(HIT_HOLD_DURATION);

            // ── 阶段三：还原到角色回合镜头 ──
            if (_vcamHit != null)
            {
                _vcamHit.Priority = PRIORITY_OFF;
                _vcamHit.m_Follow = null;
                _vcamHit.m_LookAt = null;
            }

            if (_vcamCharacterFocus != null)
                _vcamCharacterFocus.Priority = PRIORITY_CHARACTER;

            _attackSequenceCoroutine = null;
        }

        #endregion

        #region 受击反应

        /// <summary>
        /// 单位受击时触发 FOV 冲击（震屏由动画关键帧事件通过 OnCameraShake 触发）
        /// </summary>
        private void OnUnitHit(BattleEntityData entity)
        {
            PlayFovPunch();
        }

        /// <summary>
        /// 响应外部震屏事件（技能等系统可触发自定义参数的震屏）
        /// </summary>
        private void OnCameraShake(float intensity, float duration)
        {
            TriggerShake(intensity, duration);
        }

        /// <summary>
        /// 用协程驱动受击镜头 VCam 的 Noise 组件幅度实现震屏。
        /// Noise 在 Cinemachine 渲染管线内部应用，不会被 Brain 覆盖；
        /// 每帧递减 m_AmplitudeGain 直到衰减回 0，产生"命中冲击"的抖动。
        /// </summary>
        private void TriggerShake(float intensity, float duration)
        {
            if (_vcamHit == null) return;

            CinemachineBasicMultiChannelPerlin noise = _vcamHit.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null)
                noise = _vcamHit.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();

            // 终止之前的震屏协程，避免多次震屏幅度叠加
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine(noise, intensity, duration));
        }

        /// <summary>
        /// 震屏协程：设置噪声频率，m_AmplitudeGain 从 intensity 线性衰减回 0。
        /// </summary>
        private IEnumerator ShakeRoutine(CinemachineBasicMultiChannelPerlin noise, float intensity, float duration)
        {
            noise.m_FrequencyGain = HIT_SHAKE_FREQUENCY;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                noise.m_AmplitudeGain = intensity * (1f - t);
                yield return null;
            }

            noise.m_AmplitudeGain = 0f;
            _shakeCoroutine = null;
        }

        /// <summary>
        /// DOTween FOV 冲击：瞬间缩小再弹性恢复到默认值
        /// </summary>
        private void PlayFovPunch()
        {
            if (_mainCamera == null) return;

            _mainCamera.DOKill(true);
            float targetFov = _defaultFov - FOV_PUNCH_STRENGTH;
            _mainCamera.fieldOfView = targetFov;
            _mainCamera.DOFieldOfView(_defaultFov, FOV_PUNCH_DURATION)
                .SetEase(Ease.OutBack);
        }

        #endregion

        #region 战斗结束

        /// <summary>
        /// 战斗结束时清理所有动态镜头状态，回到广角待机
        /// </summary>
        private void OnBattleEnd(BattleTeam winner)
        {
            if (_attackSequenceCoroutine != null)
            {
                StopCoroutine(_attackSequenceCoroutine);
                _attackSequenceCoroutine = null;
            }

            // 清理动态镜头引用
            if (_vcamCharacterFocus != null)
            {
                _vcamCharacterFocus.m_Follow = null;
                _vcamCharacterFocus.m_LookAt = null;
            }
            if (_vcamAttack != null)
            {
                _vcamAttack.m_Follow = null;
                _vcamAttack.m_LookAt = null;
            }
            if (_vcamHit != null)
            {
                _vcamHit.m_Follow = null;
                _vcamHit.m_LookAt = null;
            }


            // 回到广角
            ResetAllPriorities();
            if (_vcamIdleWide != null)
                _vcamIdleWide.Priority = PRIORITY_IDLE;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 通过 BattleManager 的 entity→Transform 映射获取角色的 Transform
        /// </summary>
        private Transform GetCharacterTransform(BattleEntityData entity)
        {
            if (entity == null) return null;
            if (BattleManager.Instance == null) return null;

            if (BattleManager.Instance.TryGetEntityTransform(entity.heroID, out Transform t))
                return t;

            return null;
        }

        /// <summary>
        /// 重置所有受管镜头优先级为 OFF
        /// </summary>
        private void ResetAllPriorities()
        {
            if (_vcamEntrance1 != null) _vcamEntrance1.Priority = PRIORITY_OFF;
            if (_vcamEntrance2 != null) _vcamEntrance2.Priority = PRIORITY_OFF;
            if (_vcamIdleWide != null) _vcamIdleWide.Priority = PRIORITY_OFF;
            if (_vcamCharacterFocus != null) _vcamCharacterFocus.Priority = PRIORITY_OFF;
            if (_vcamAttack != null) _vcamAttack.Priority = PRIORITY_OFF;
            if (_vcamHit != null) _vcamHit.Priority = PRIORITY_OFF;
        }

        #endregion
    }
}
