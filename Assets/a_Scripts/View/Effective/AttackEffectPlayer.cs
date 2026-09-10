using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 攻击特效序列播放器 — 订阅 BattleEventCenter 的单攻/群攻事件，
    /// 分别按「单攻特效组」「群攻特效组」的数组顺序，以各自间隔依次实例化并播放特效预制体。
    ///
    /// 特效预制体无需预先放进场景：本组件在运行时 Instantiate 到指定锚点（攻击者 / 目标 / 自定义挂点），
    /// 播放结束后自动销毁。不同角色在 Inspector 中拖入不同数量、不同锚点、不同间隔的特效数组即可，
    /// 无需改动代码。
    ///
    /// 单攻触发 OnUnitAttack（播放单攻特效组），群攻触发 OnAoeAttack（一次群攻只广播一次，播放群攻特效组）。
    /// 群攻特效组中 Target 锚点解析到第一个存活目标，Source / Custom 锚点行为不变。
    ///
    /// 与 BattleCameraController（镜头运镜）、DamageNumberSpawner（伤害跳字）同属表现层，
    /// 通过事件订阅与逻辑层（BattleManager）解耦。
    ///
    /// 注意：特效预制体的 ParticleSystem 建议勾选 Play On Awake；本组件只显式播放根节点粒子，
    /// 子粒子（子发射器）由父粒子驱动。粒子寿命按「发射时长 + 单粒子最大寿命」估算，适用于一次性爆点特效。
    /// </summary>
    public class AttackEffectPlayer : MonoBehaviour
    {
        /// <summary>
        /// 特效预制体 + 生成配置的配对单元。
        /// 每个数组元素在 Inspector 里独立设置：预制体、生成锚点、偏移、播放间隔。
        /// </summary>
        [System.Serializable]
        private struct ParticleSequenceEntry
        {
            [SerializeField] private GameObject _prefab;      // 特效预制体（可含多个子 ParticleSystem）
            [SerializeField] private EffectAnchor _anchor;    // 生成锚点
            [SerializeField] private Transform _customAnchor; // _anchor == Custom 时使用的挂点
            [SerializeField] private Vector3 _offset;         // 相对锚点的本地偏移（微调生成位置）
            [SerializeField][Min(0f)] private float _delay;  // 生成并播放前等待的秒数（0 = 立即播放）

            public GameObject Prefab => _prefab;
            public EffectAnchor Anchor => _anchor;
            public Transform CustomAnchor => _customAnchor;
            public Vector3 Offset => _offset;
            public float Delay => _delay;
        }

        // ── Inspector：触发过滤 ──

        [Header("触发过滤")]
        [SerializeField] private BattleTeam _sourceTeam = BattleTeam.Player; // 仅响应此阵营的攻击方
        [SerializeField] private string _heroID;                            // 非空时仅响应该角色的攻击

        // ── Inspector：特效序列 ──

        [Header("单攻特效序列")]
        [SerializeField] private ParticleSequenceEntry[] _singleEffects; // 单攻时按顺序播放（订阅 OnUnitAttack）

        [Header("群攻特效序列")]
        [SerializeField] private ParticleSequenceEntry[] _aoeEffects;    // 群攻时按顺序播放（订阅 OnAoeAttack）

        // ── 内部状态 ──

        private Coroutine _playCoroutine;

        #region 生命周期

        private void OnEnable()
        {
            BattleEventCenter.OnUnitAttack += OnUnitAttack;
            BattleEventCenter.OnAoeAttack += OnAoeAttack;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnUnitAttack -= OnUnitAttack;
            BattleEventCenter.OnAoeAttack -= OnAoeAttack;
            StopSequence();
        }

        #endregion

        #region 事件回调

        private void OnUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (!MatchesFilter(source)) return;
            RestartSequence(PlaySequence(source, target, _singleEffects));
        }

        private void OnAoeAttack(BattleEntityData source, IReadOnlyList<BattleEntityData> targets)
        {
            if (!MatchesFilter(source)) return;

            // 群攻特效锚点：Target 解析到第一个存活目标；Source / Custom 锚点行为不变
            BattleEntityData anchorTarget = (targets != null && targets.Count > 0) ? targets[0] : null;
            RestartSequence(PlaySequence(source, anchorTarget, _aoeEffects));
        }

        /// <summary>终止上一次序列后启动新序列，防止多次攻击重叠播放</summary>
        private void RestartSequence(IEnumerator routine)
        {
            StopSequence();
            _playCoroutine = StartCoroutine(routine);
        }

        /// <summary>终止未完成的序列，避免协程访问已禁用的对象</summary>
        private void StopSequence()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        /// <summary>
        /// 依次生成每个特效：先等待该元素自己的 Delay，再解析锚点 → Instantiate → 播放 → 定时销毁。
        /// </summary>
        private IEnumerator PlaySequence(BattleEntityData source, BattleEntityData target, ParticleSequenceEntry[] entries)
        {
            if (entries == null) yield break;

            foreach (ParticleSequenceEntry entry in entries)
            {
                if (entry.Delay > 0f)
                    yield return new WaitForSeconds(entry.Delay);

                if (entry.Prefab == null) continue;

                Transform anchor = ResolveAnchor(entry, source, target);
                if (anchor == null)
                {
                    Debug.LogWarning($"[AttackEffectPlayer] 无法解析锚点 {entry.Anchor}，跳过该特效");
                    continue;
                }

                // 本地偏移随锚点朝向旋转，使特效贴合当前作战朝向
                Vector3 spawnPos = anchor.position + anchor.rotation * entry.Offset;

                // 保留特效预制体原本的旋转（不强制对齐锚点朝向）
                GameObject instance = Instantiate(entry.Prefab, spawnPos, entry.Prefab.transform.rotation);

                PlayAndAutoDestroy(instance);
            }

            _playCoroutine = null;
        }

        #endregion

        #region 锚点解析

        /// <summary>
        /// 根据锚点类型返回生成用的 Transform：
        /// Source / Target 走 BattleManager 的 entity→Transform 映射，Custom 直接返回 Inspector 拖入的挂点。
        /// </summary>
        private Transform ResolveAnchor(ParticleSequenceEntry entry, BattleEntityData source, BattleEntityData target)
        {
            switch (entry.Anchor)
            {
                case EffectAnchor.Source:
                    return ResolveEntityTransform(source);
                case EffectAnchor.Target:
                    return ResolveEntityTransform(target);
                case EffectAnchor.Custom:
                    return entry.CustomAnchor;
                default:
                    return null;
            }
        }

        /// <summary>通过 BattleManager 解析实体的 Transform（复用镜头 / 跳字脚本同一套映射）</summary>
        private static Transform ResolveEntityTransform(BattleEntityData entity)
        {
            if (entity == null || BattleManager.Instance == null) return null;

            if (BattleManager.Instance.TryGetEntityTransform(entity.heroID, out Transform t))
                return t;

            return null;
        }

        #endregion

        #region 实例化与销毁

        /// <summary>
        /// 播放实例的根节点粒子，并按粒子寿命自动销毁实例。
        /// 子粒子（子发射器）由父粒子驱动，无需手动 Play。
        /// </summary>
        private static void PlayAndAutoDestroy(GameObject instance)
        {
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps == null)
                ps = instance.GetComponentInChildren<ParticleSystem>(); // 回退：根节点无粒子时找第一个子粒子

            if (ps == null)
            {
                Debug.LogWarning("[AttackEffectPlayer] 特效预制体不含 ParticleSystem，实例不会被自动销毁");
                return;
            }

            ps.Play();

            // 非循环一次性爆点：存活时间 = 发射时长 + 单粒子最大寿命
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(instance, lifetime);
        }

        #endregion

        #region 过滤

        /// <summary>
        /// 判断攻击方是否符合本组件的触发条件：阵营必须匹配，且设置了 _heroID 时角色 ID 也必须一致。
        /// </summary>
        private bool MatchesFilter(BattleEntityData source)
        {
            if (source == null) return false;
            if (source.team != _sourceTeam) return false;
            if (!string.IsNullOrEmpty(_heroID) && source.heroID != _heroID) return false;
            return true;
        }

        #endregion
    }
}
