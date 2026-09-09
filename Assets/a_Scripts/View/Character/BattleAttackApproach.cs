using System.Collections;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 单攻冲撞 — 玩家角色执行单体攻击时，快速移动到目标敌人面前，攻击结束后返回出生点。
    ///
    /// 挂载在玩家战斗预制体上（与 AttackEffectPlayer 同层），订阅 OnUnitAttack（单攻事件）。
    /// 群攻走 OnAoeAttack，不会触发本组件。只响应 Player 阵营的攻击方，敌人攻击不触发。
    ///
    /// 通过 BattleManager 的实体→Transform 映射绑定自身身份：只有「当前行动者」对应的那一个模型会移动，
    /// 队伍里重复放置同一角色（多个模型共享 heroID）时，其余模型保持静止。
    ///
    /// 移动采用 SmoothStep 缓动（先加速后减速），Awake 记录出生点与朝向，冲撞结束后还原。
    /// 到达敌人面前后播放一次攻击动画（可拖拽 AnimationClip 覆盖，未拖入则播放控制器自带攻击）。
    /// </summary>
    public class BattleAttackApproach : MonoBehaviour
    {
        [Header("冲撞")]
        [SerializeField] private BattleTeam _sourceTeam = BattleTeam.Player; // 仅响应此阵营的攻击方
        [SerializeField] [Min(0f)] private float _dashDuration = 0.25f;     // 冲到敌人面前的耗时（秒）
        [SerializeField] [Min(0f)] private float _stopDistance = 1.5f;      // 停止点与敌人之间的距离（在敌人面前）
        [SerializeField] [Min(0f)] private float _holdDuration = 1.5f;      // 到达敌人面前后停留的时长（秒）
        [SerializeField] [Min(0f)] private float _returnDuration = 0.35f;   // 返回出生点的耗时（秒）
        [SerializeField] [Min(0f)] private float _minDistanceToDash = 0.01f; // 距离过近时不冲撞，直接面向敌人

        [Header("攻击动画")]
        [SerializeField] private AnimationClip _attackClip;                // 拖拽攻击动画资产（可选；留空则播放控制器自带攻击）
        [SerializeField] private string _attackStateName = "DHattack";     // 攻击动画状态名（Baie / DanHeng 控制器中为 "DHattack"）
        [SerializeField] [Min(0f)] private float _attackCrossFade = 0.1f;  // 攻击动画过渡时长（秒）

        // ── 内部状态 ──

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Coroutine _approachCoroutine;
        private Animator _animator;

        #region 生命周期

        private void Awake()
        {
            // Instantiate 到出生点时 Awake 立即执行，此时 transform 即出生点
            _homePosition = transform.position;
            _homeRotation = transform.rotation;

            _animator = GetComponent<Animator>();
            ApplyAttackClipOverride();
        }

        /// <summary>
        /// 若拖入了攻击动画资产，则用 AnimatorOverrideController 覆盖攻击状态，
        /// 使 CrossFade 到攻击状态时播放自定义动画；未拖入则播放控制器自带攻击。
        /// </summary>
        private void ApplyAttackClipOverride()
        {
            if (_animator == null || _attackClip == null || _animator.runtimeAnimatorController == null)
                return;

            AnimatorOverrideController overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            overrideController[_attackStateName] = _attackClip;
            _animator.runtimeAnimatorController = overrideController;
        }

        private void OnEnable()
        {
            BattleEventCenter.OnUnitAttack += OnUnitAttack;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnUnitAttack -= OnUnitAttack;
            StopApproach();
        }

        #endregion

        #region 事件回调

        private void OnUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (!MatchesFilter(source)) return;

            Transform targetTransform = ResolveEntityTransform(target);
            if (targetTransform == null) return;

            // 终止上一次冲撞，防止多次冲撞重叠
            StopApproach();
            _approachCoroutine = StartCoroutine(ApproachRoutine(targetTransform));
        }

        #endregion

        #region 冲撞协程

        /// <summary>
        /// 冲撞流程：面向敌人 → 冲到停止点 → 停留 → 返回出生点并还原朝向。
        /// </summary>
        private IEnumerator ApproachRoutine(Transform targetTransform)
        {
            Vector3 targetPos = targetTransform.position;

            // 停止点 = 敌人位置 + 从敌人指向我方的方向 × 停止距离（即"敌人面前"）
            Vector3 back = transform.position - targetPos;
            back.y = 0f;
            if (back.sqrMagnitude < _minDistanceToDash * _minDistanceToDash)
                back = -targetTransform.forward; // 距离过近时退而求其次：取敌人面朝的反方向
            Vector3 stopPos = targetPos + back.normalized * _stopDistance;

            // 面向敌人
            FaceTarget(targetPos);

            // 冲到停止点
            yield return MoveTo(stopPos, _dashDuration);

            // 到达敌人面前后播放一次攻击动画
            if (_animator != null)
                _animator.CrossFade(_attackStateName, _attackCrossFade);

            // 停留，等待命中表现
            if (_holdDuration > 0f)
                yield return new WaitForSeconds(_holdDuration);

            // 返回出生点并还原朝向
            yield return MoveTo(_homePosition, _returnDuration);
            transform.rotation = _homeRotation;

            _approachCoroutine = null;
        }

        /// <summary>让角色水平面向目标位置（y 轴归零，避免仰俯）</summary>
        private void FaceTarget(Vector3 targetPos)
        {
            Vector3 look = targetPos - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look.normalized);
        }

        /// <summary>用 SmoothStep 缓动在 duration 秒内把角色移动到目标点</summary>
        private IEnumerator MoveTo(Vector3 destination, float duration)
        {
            if (duration <= 0f)
            {
                transform.position = destination;
                yield break;
            }

            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t); // SmoothStep 缓入缓出
                transform.position = Vector3.Lerp(start, destination, eased);
                yield return null;
            }
            transform.position = destination;
        }

        private void StopApproach()
        {
            if (_approachCoroutine != null)
            {
                StopCoroutine(_approachCoroutine);
                _approachCoroutine = null;
            }
        }

        #endregion

        #region 过滤与解析

        private bool MatchesFilter(BattleEntityData source)
        {
            if (source == null || BattleManager.Instance == null) return false;
            if (source.team != _sourceTeam) return false;

            // 只有「当前行动者」对应的那一个模型才移动：
            // 用 BattleManager 的实体→Transform 映射判断自身是否为 source 对应的模型。
            // 当队伍里重复放置同一角色（多个模型共享 heroID）时，只有映射到的那一个模型会动。
            return BattleManager.Instance.TryGetEntityTransform(source.heroID, out Transform mapped)
                && mapped == transform;
        }

        /// <summary>通过 BattleManager 解析实体的 Transform（与镜头 / 特效脚本同一套映射）</summary>
        private static Transform ResolveEntityTransform(BattleEntityData entity)
        {
            if (entity == null || BattleManager.Instance == null) return null;
            return BattleManager.Instance.TryGetEntityTransform(entity.heroID, out Transform t) ? t : null;
        }

        #endregion
    }
}
