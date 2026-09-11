using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 受击特效播放器 — 订阅 BattleEventCenter.OnUnitHit，在受击角色的世界位置实例化粒子特效预制体并播放，
    /// 播放结束后自动销毁。
    ///
    /// 与 AttackEffectPlayer（攻击特效）、DamageNumberSpawner（伤害跳字）同属表现层，
    /// 通过事件订阅与逻辑层（BattleManager）解耦。
    ///
    /// 注意：特效预制体的 ParticleSystem 建议勾选 Play On Awake；粒子寿命按「发射时长 + 单粒子最大寿命」估算。
    /// </summary>
    public class HitEffectPlayer : MonoBehaviour
    {
        [Header("触发过滤")]
        [SerializeField] private BattleTeam _targetTeam = BattleTeam.Enemy; // 仅响应此阵营的受击方
        [SerializeField] private string _heroID;                            // 非空时仅响应该角色的受击

        [Header("受击特效")]
        [SerializeField] private GameObject _hitParticlePrefab;   // 受击时在角色位置播放的粒子预制体
        [SerializeField] private Vector3 _offset;                 // 相对角色位置的偏移（微调生成位置）

        private void OnEnable()
        {
            BattleEventCenter.OnUnitHit += OnUnitHit;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnUnitHit -= OnUnitHit;
        }

        private void OnUnitHit(BattleEntityData target)
        {
            if (!MatchesFilter(target)) return;
            if (_hitParticlePrefab == null) return;

            Transform targetTransform = ResolveEntityTransform(target);
            if (targetTransform == null)
            {
                Debug.LogWarning($"[HitEffectPlayer] 无法解析受击方 {target.heroName} 的 Transform，跳过特效");
                return;
            }

            // 生成位置随受击方朝向旋转，贴合当前作战朝向
            Vector3 spawnPos = targetTransform.position + targetTransform.rotation * _offset;
            GameObject instance = Instantiate(_hitParticlePrefab, spawnPos, _hitParticlePrefab.transform.rotation);

            PlayAndAutoDestroy(instance);
        }

        /// <summary>通过 BattleManager 解析实体的 Transform（复用镜头 / 特效同一套映射）</summary>
        private static Transform ResolveEntityTransform(BattleEntityData entity)
        {
            if (entity == null || BattleManager.Instance == null) return null;
            if (BattleManager.Instance.TryGetEntityTransform(entity.heroID, out Transform t)) return t;
            return null;
        }

        /// <summary>播放实例根节点粒子，并按粒子寿命自动销毁实例</summary>
        private static void PlayAndAutoDestroy(GameObject instance)
        {
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps == null)
                ps = instance.GetComponentInChildren<ParticleSystem>(); // 回退：根节点无粒子时找第一个子粒子

            if (ps == null)
            {
                Debug.LogWarning("[HitEffectPlayer] 受击特效预制体不含 ParticleSystem，实例不会被自动销毁");
                return;
            }

            ps.Play();

            // 非循环一次性爆点：存活时间 = 发射时长 + 单粒子最大寿命
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(instance, lifetime);
        }

        /// <summary>判断受击方是否符合触发条件：阵营匹配，且设置了 _heroID 时角色 ID 也必须一致</summary>
        private bool MatchesFilter(BattleEntityData target)
        {
            if (target == null) return false;
            if (target.team != _targetTeam) return false;
            if (!string.IsNullOrEmpty(_heroID) && target.heroID != _heroID) return false;
            return true;
        }
    }
}
