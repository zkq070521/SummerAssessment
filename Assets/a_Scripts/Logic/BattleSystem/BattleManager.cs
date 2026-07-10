using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗管理器 — 回合制战斗总控制器（单例 MonoBehaviour）
    ///
    /// 挂载在战斗场景 GameObject 上。
    /// Inspector 中拖入玩家队伍数据（TeamData_SO）和敌人配置（BattleEntityData[]），
    /// 调用 StartBattle() 启动战斗。
    ///
    /// 纯数据驱动，不依赖任何视觉组件。
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        // ── 常量 ──
        private const float CRIT_RATE = 0.2f;
        private const float CRIT_DAMAGE_MULTIPLIER = 1.5f;

        // ── Inspector ──
        [Header("队伍数据")]
        [SerializeField] private TeamData_SO _playerTeamData;       // 玩家队伍（HeroData 资产）
        [SerializeField] private BattleEntityData[] _enemyTeamData;  // 敌人配置（直接在 Inspector 填）

        // ── 单例 ──
        public static BattleManager Instance { get; private set; }

        // ── 公开属性 ──
        public TurnManager TurnManager { get; private set; }
        public BattleEntityData CurrentActor { get; private set; }
        public IReadOnlyList<BattleEntityData> PlayerTeam => _playerTeam;
        public IReadOnlyList<BattleEntityData> EnemyTeam => _enemyTeam;
        public bool IsBattleStarted { get; private set; }

        // ── 内部状态 ──
        private readonly List<BattleEntityData> _playerTeam = new List<BattleEntityData>();
        private readonly List<BattleEntityData> _enemyTeam = new List<BattleEntityData>();

        // ── 生命周期 ──

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── 公共 API ──

        /// <summary>
        /// 启动战斗：从 Inspector 配置创建 BattleEntityData → 初始化 TurnManager → 推进到首回合
        /// </summary>
        public void StartBattle()
        {
            if (IsBattleStarted)
            {
                Debug.LogWarning("[BattleManager] 战斗已启动，跳过重复调用");
                return;
            }

            CreatePlayerTeam();
            CreateEnemyTeam();

            if (_playerTeam.Count == 0 && _enemyTeam.Count == 0)
            {
                Debug.LogError("[BattleManager] 未配置任何队伍数据，请在 Inspector 中设置");
                return;
            }

            // 合并双方队伍，初始化回合管理器
            List<BattleEntityData> allUnits = new List<BattleEntityData>(_playerTeam);
            allUnits.AddRange(_enemyTeam);
            TurnManager = new TurnManager(allUnits);

            IsBattleStarted = true;

            Debug.Log($"[BattleManager] 战斗开始 — 玩家 {_playerTeam.Count} 人, 敌人 {_enemyTeam.Count} 人");

            BattleEventCenter.TriggerBattleStart();
            NextTurn();
        }

        /// <summary>
        /// 推进到下一回合：获取下一位行动者 → 广播 OnTurnStart → 检查战斗结束
        /// </summary>
        public void NextTurn()
        {
            if (!IsBattleStarted) return;
            if (CheckBattleEnd()) return;

            CurrentActor = TurnManager.GetNextActor();

            if (CurrentActor == null)
            {
                CheckBattleEnd();
                return;
            }

            BattleEventCenter.TriggerTurnChanged(CurrentActor.team);
            BattleEventCenter.TriggerTurnStart(CurrentActor);
        }

        /// <summary>
        /// 执行攻击动作：伤害计算 → 暴击判定 → 应用伤害 → 广播事件 → 死亡处理
        /// </summary>
        public void ExecuteAction(BattleEntityData source, BattleEntityData target)
        {
            if (source == null || target == null)
            {
                Debug.LogError("[BattleManager] ExecuteAction: source 或 target 为 null");
                return;
            }

            if (!target.isAlive)
            {
                Debug.LogWarning($"[BattleManager] 目标 {target.entityName} 已阵亡，无法攻击");
                return;
            }

            // 1. 伤害计算（保底 1 点）
            int rawDamage = source.attack - target.defense;
            int damage = Mathf.Max(1, rawDamage);

            // 2. 暴击判定
            bool isCritical = Random.value < CRIT_RATE;
            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * CRIT_DAMAGE_MULTIPLIER);
            }

            // 3. 应用伤害
            target.TakeDamage(damage);

            // 4. 广播事件
            BattleEventCenter.TriggerUnitAttack(source, target);
            BattleEventCenter.TriggerDamageDealt(source, target, damage, isCritical);

            Debug.Log($"[BattleManager] {source.entityName} → {target.entityName}: " +
                      $"{(isCritical ? "暴击! " : "")}{damage} 点伤害 (剩余 HP: {target.currentHP})");

            // 5. 死亡处理
            if (!target.isAlive)
            {
                BattleEventCenter.TriggerUnitDeath(target);
                TurnManager.RemoveUnit(target);
                RemoveFromTeam(target);
                Debug.Log($"[BattleManager] {target.entityName} 阵亡");
            }

            // 6. 检查战斗结束
            CheckBattleEnd();
        }

        /// <summary>
        /// 检查战斗结束条件
        /// </summary>
        /// <returns>true = 战斗已结束</returns>
        public bool CheckBattleEnd()
        {
            if (!IsBattleStarted) return true;

            if (_enemyTeam.Count == 0)
            {
                Debug.Log("[BattleManager] 战斗结束 — 玩家胜利！");
                BattleEventCenter.TriggerBattleEnd(BattleTeam.Player);
                IsBattleStarted = false;
                return true;
            }

            if (_playerTeam.Count == 0)
            {
                Debug.Log("[BattleManager] 战斗结束 — 玩家失败！");
                BattleEventCenter.TriggerBattleEnd(BattleTeam.Enemy);
                IsBattleStarted = false;
                return true;
            }

            return false;
        }

        // ── 内部方法 ──

        /// <summary>
        /// 从 TeamData_SO 创建玩家队伍的 BattleEntityData
        /// HeroData（ScriptableObject）→ BattleEntityData（运行时数据）
        /// </summary>
        private void CreatePlayerTeam()
        {
            _playerTeam.Clear();

            if (_playerTeamData == null)
            {
                Debug.LogWarning("[BattleManager] 未设置玩家队伍数据 _playerTeamData");
                return;
            }

            foreach (HeroData hero in _playerTeamData.teamMembers)
            {
                if (hero == null) continue;

                BattleEntityData entity = new BattleEntityData
                {
                    entityName = hero.heroName,
                    maxHP = Mathf.RoundToInt(hero.maxHP),
                    currentHP = Mathf.RoundToInt(hero.maxHP),
                    attack = Mathf.RoundToInt(hero.attack),
                    defense = Mathf.RoundToInt(hero.defense),
                    speed = Mathf.RoundToInt(hero.speed),
                    critRate = 0.05f,
                    critDamage = 1.5f,
                    isAlive = true,
                    team = BattleTeam.Player
                };

                _playerTeam.Add(entity);
            }
        }

        /// <summary>
        /// 创建敌人队伍的 BattleEntityData（直接拷贝 Inspector 配置）
        /// </summary>
        private void CreateEnemyTeam()
        {
            _enemyTeam.Clear();

            if (_enemyTeamData == null) return;

            foreach (BattleEntityData template in _enemyTeamData)
            {
                if (template == null) continue;

                BattleEntityData entity = template.Clone();
                entity.team = BattleTeam.Enemy;
                _enemyTeam.Add(entity);
            }
        }

        /// <summary>
        /// 从队伍列表中移除死亡单位
        /// </summary>
        private void RemoveFromTeam(BattleEntityData entity)
        {
            if (entity.team == BattleTeam.Player)
                _playerTeam.Remove(entity);
            else
                _enemyTeam.Remove(entity);
        }
    }
}
