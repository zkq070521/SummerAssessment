using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 敌人战斗动画播放器 — 订阅 BattleEventCenter 事件，按 EnemyAnimationConfig 配置播放动画。
    ///
    /// 与 AttackEffectPlayer / BattleCameraController / DamageNumberSpawner 同属表现层，
    /// 通过事件订阅与逻辑层解耦：逻辑层只广播 OnUnitAttack / OnDamageDealt / OnUnitDeath，
    /// 本组件据此在对应敌人身上播放 攻击 / 受击 / 死亡 动画，默认状态播放待机动画。
    ///
    /// 播放机制：基于 Animator 现有控制器创建 AnimatorOverrideController，
    /// 用配置的 AnimationClip 覆盖对应状态，再 CrossFade 到该状态，从而支持自由更换动画资产。
    /// 状态名常量需与目标 Animator 控制器（当前为 Slime.controller）中的状态名一致。
    /// </summary>
    public class EnemyBattleAnimator : MonoBehaviour
    {
        // ── Animator 状态名（须与 Slime.controller 状态名一致）──
        private const string STATE_IDLE = "IdleNormal";
        private const string STATE_ATTACK = "Attack01";
        private const string STATE_HIT = "GetHit";
        private const string STATE_DIE = "Die";

        /// <summary>CrossFade 过渡时长（秒）</summary>
        private const float CROSS_FADE_TIME = 0.1f;

        private Animator _animator;
        private AnimatorOverrideController _overrideController;
        private string _heroID;

        // ── 初始化 ──

        /// <summary>
        /// 绑定到战斗实体并应用动画配置。由 BattleManager.InitializeEnemyAgents() 调用。
        /// </summary>
        /// <param name="entity">此敌人对应的运行时 BattleEntityData</param>
        /// <param name="config">动画资产配置；为 null 时保持 Animator 原始控制器</param>
        public void Initialize(BattleEntityData entity, EnemyAnimationConfig config)
        {
            _heroID = entity != null ? entity.heroID : null;
            _animator = GetComponent<Animator>();

            BuildOverrideController(config);
        }

        /// <summary>基于当前 Animator 控制器创建 override，并注入配置的动画片段</summary>
        private void BuildOverrideController(EnemyAnimationConfig config)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[EnemyBattleAnimator] {gameObject.name} 缺少 Animator，无法播放动画");
                return;
            }

            if (config == null)
                return; // 无配置则保持原始控制器，动画退化为控制器默认

            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;

            ApplyOverride(STATE_IDLE, config.idleAnimation);
            ApplyOverride(STATE_ATTACK, config.attackAnimation);
            ApplyOverride(STATE_HIT, config.hitAnimation);
            ApplyOverride(STATE_DIE, config.dieAnimation);
        }

        /// <summary>用指定动画片段覆盖某个状态（clip 为空则保留控制器默认片段）</summary>
        private void ApplyOverride(string stateName, AnimationClip clip)
        {
            if (clip == null) return;
            _overrideController[stateName] = clip;
        }

        // ── 事件订阅 ──

        private void OnEnable()
        {
            BattleEventCenter.OnUnitAttack += OnUnitAttack;
            BattleEventCenter.OnDamageDealt += OnDamageDealt;
            BattleEventCenter.OnUnitDeath += OnUnitDeath;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnUnitAttack -= OnUnitAttack;
            BattleEventCenter.OnDamageDealt -= OnDamageDealt;
            BattleEventCenter.OnUnitDeath -= OnUnitDeath;
        }

        // ── 事件回调 ──

        /// <summary>本敌人发动攻击 → 播放攻击动画</summary>
        private void OnUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (IsSelf(source))
                CrossFade(STATE_ATTACK);
        }

        /// <summary>本敌人受到伤害 → 播放受击动画</summary>
        private void OnDamageDealt(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
        {
            if (IsSelf(target))
                CrossFade(STATE_HIT);
        }

        /// <summary>本敌人阵亡 → 播放死亡动画</summary>
        private void OnUnitDeath(BattleEntityData entity)
        {
            if (IsSelf(entity))
                CrossFade(STATE_DIE);
        }

        // ── 辅助 ──

        /// <summary>判断事件实体是否为自身（按 heroID 过滤）</summary>
        private bool IsSelf(BattleEntityData entity)
        {
            return entity != null && !string.IsNullOrEmpty(_heroID) && entity.heroID == _heroID;
        }

        /// <summary>平滑过渡到指定动画状态</summary>
        private void CrossFade(string stateName)
        {
            if (_animator == null) return;
            _animator.CrossFade(stateName, CROSS_FADE_TIME);
        }
    }
}
