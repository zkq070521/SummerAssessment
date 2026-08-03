using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        // ── Inspector ──
        [Header("队伍数据")]
        [SerializeField] public TeamData_SO _playerTeamDataSO;       // 玩家队伍（HeroData 资产）
        [SerializeField] private BattleEntityData[] _enemyTeamData;  // 敌人配置（直接在 Inspector 填）

        [Header("出生点")]
        public Transform[] playerSpawnPoints;   // 最多 4 个，左侧
        public Transform[] enemySpawnPoints;    // 最多 3 个，右侧

        [Header("父节点")]
        public Transform playerPartyRoot;
        public Transform enemyPartyRoot;

        [Header("敌人 AI")]
        [SerializeField] private BehaviorDesigner.Runtime.ExternalBehaviorTree _defaultEnemyAI;

        // ── 单例 ──
        public static BattleManager Instance { get; private set; }

        // ── 公开属性 ──
        public TurnManager TurnManager { get; private set; }
        public BattleEntityData CurrentActor { get; private set; }

        /// <summary>上一个执行攻击的玩家角色（用于敌人 AI 仇恨系统）</summary>
        public BattleEntityData LastPlayerAttacker { get; private set; }

        public IReadOnlyList<BattleEntityData> PlayerTeam => _playerTeam;//提供给外部只读
        public IReadOnlyList<BattleEntityData> EnemyTeam => _enemyTeam;
        public bool IsBattleStarted { get; private set; }

        // ── 内部状态 ──
        private readonly List<BattleEntityData> _playerTeam = new List<BattleEntityData>();//克隆之后装在这里
        private readonly List<BattleEntityData> _enemyTeam = new List<BattleEntityData>();

        /// <summary>heroID → 已生成实体的 Transform（供摄像机等 View 层查询）</summary>
        private readonly Dictionary<string, Transform> _entityTransformMap = new Dictionary<string, Transform>();

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

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SpawnAll();
            StartBattle();
        }

        public void SpawnAll()
        {
            ClearAll();
            SpawnPlayerTeam();
            SpawnEnemyTeam();
        }

        /// <summary>生成玩家队伍模型</summary>
        private void SpawnPlayerTeam()
        {
            if (_playerTeamDataSO == null) return;

            for (int i = 0; i < _playerTeamDataSO.teamMembers.Count; i++)
            {
                HeroData hero = _playerTeamDataSO.teamMembers[i];
                if (hero == null)
                {
                    Debug.LogWarning($"[BattleManager] teamMembers[{i}] 未赋值，跳过");
                    continue;
                }
                if (hero.battlePrefab == null)
                {
                    Debug.LogWarning($"[BattleManager] {hero.heroName} 的 battlePrefab 未赋值，跳过");
                    continue;
                }
                GameObject instance = Instantiate(hero.battlePrefab, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation, playerPartyRoot);
                _entityTransformMap[hero.heroID] = instance.transform;
            }
        }

        /// <summary>生成敌人队伍模型</summary>
        private void SpawnEnemyTeam()
        {
            if (_enemyTeamData == null) return;

            for (int i = 0; i < _enemyTeamData.Length; i++)
            {
                BattleEntityData template = _enemyTeamData[i];
                if (template == null || template.battlePrefab == null)
                {
                    Debug.LogWarning($"[BattleManager] 敌人[{i}] 配置无效，跳过");
                    continue;
                }
                Transform spawnPoint = i < enemySpawnPoints.Length ? enemySpawnPoints[i] : enemySpawnPoints[0];
                GameObject instance = Instantiate(template.battlePrefab, spawnPoint.position, spawnPoint.rotation, enemyPartyRoot);
                _entityTransformMap[template.heroID] = instance.transform;
            }
        }

        /// <summary>
        /// 清除所有已生成的单位
        /// </summary>
        public void ClearAll()
        {
            foreach (Transform child in playerPartyRoot)
                Destroy(child.gameObject);
            foreach (Transform child in enemyPartyRoot)
                Destroy(child.gameObject);
            _entityTransformMap.Clear();
        }

        /// <summary>
        /// 根据 heroID 获取已生成角色的 Transform（供摄像机等 View 层查询）
        /// </summary>
        /// <param name="heroID">角色唯一标识</param>
        /// <param name="transform">输出的 Transform；未找到时为 null</param>
        /// <returns>是否找到对应 Transform</returns>
        public bool TryGetEntityTransform(string heroID, out Transform transform)
        {
            return _entityTransformMap.TryGetValue(heroID, out transform);
        }

        /// <summary>
        /// 根据 heroID 查找玩家队伍中的 BattleEntityData
        /// </summary>
        /// <param name="heroID">角色唯一标识</param>
        /// <returns>对应的 BattleEntityData；未找到或已死亡时返回 null</returns>
        public BattleEntityData TryGetPlayerByHeroID(string heroID)
        {
            if (string.IsNullOrEmpty(heroID)) return null;

            for (int i = 0; i < _playerTeam.Count; i++)
            {
                if (_playerTeam[i].heroID == heroID && _playerTeam[i].isAlive)
                    return _playerTeam[i];
            }
            return null;
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
            InitializeEnemyAgents();

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
                Debug.LogWarning($"[BattleManager] 目标 {target.heroName} 已阵亡，无法攻击");
                return;
            }

            // 1. 伤害计算（保底 1 点）
            int rawDamage = Mathf.RoundToInt(source.attack - target.defense);
            int damage = Mathf.Max(1, rawDamage);

            // 2. 暴击判定
            bool isCritical = Random.value < source.critRate;
            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * source.critDamage);
            }

            // 3. 应用伤害
            target.TakeDamage(damage);

            // 4. 广播事件
            BattleEventCenter.TriggerUnitAttack(source, target);
            BattleEventCenter.TriggerDamageDealt(source, target, damage, isCritical);

            // 5. 追踪最后一个攻击的玩家（敌人 AI 仇恨目标）
            if (source.team == BattleTeam.Player)
                LastPlayerAttacker = source;

            Debug.Log($"[BattleManager] {source.heroName} → {target.heroName}: " +
                      $"{(isCritical ? "暴击! " : "")}{damage} 点伤害 (剩余 HP: {target.currentHP})");

            // 6. 死亡处理
            if (!target.isAlive)
            {
                BattleEventCenter.TriggerUnitDeath(target);
                TurnManager.RemoveUnit(target);
                RemoveFromTeam(target);
                Debug.Log($"[BattleManager] {target.heroName} 阵亡");
            }

            // 7. 检查战斗结束
            CheckBattleEnd();
        }

        /// <summary>
        /// 执行攻击动作 + 等待动画时长后推进回合
        /// </summary>
        public void ExecuteActionWithTurn(BattleEntityData source, BattleEntityData target)
        {
            ExecuteAction(source, target);
            StartCoroutine(WaitAndNextTurn());
        }

        /// <summary>启动等待动画后推进回合的协程（供群攻等外部调用）</summary>
        public void StartCoroutineWaitAndNextTurn()
        {
            StartCoroutine(WaitAndNextTurn());
        }

        /// <summary>等待模拟动画时长后推进到下一回合</summary>
        private IEnumerator WaitAndNextTurn()
        {
            yield return new WaitForSeconds(1.5f);
            NextTurn();
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
                StartCoroutine(ReturnToOverworld());
                return true;
            }

            if (_playerTeam.Count == 0)
            {
                Debug.Log("[BattleManager] 战斗结束 — 玩家失败！");
                BattleEventCenter.TriggerBattleEnd(BattleTeam.Enemy);
                IsBattleStarted = false;
                StartCoroutine(ReturnToOverworld());
                return true;
            }

            return false;
        }

        private const string OVERWORLD_SCENE = "SampleScene";

        /// <summary>等待片刻后卸载 Battle1，回到开放世界</summary>
        private IEnumerator ReturnToOverworld()
        {
            yield return new WaitForSeconds(2f);
            SceneManager.LoadSceneAsync(OVERWORLD_SCENE);
        }

        // ── 内部方法 ──

        /// <summary>
        /// 从 TeamData_SO 创建玩家队伍的 BattleEntityData
        /// HeroData（ScriptableObject）→ BattleEntityData（运行时数据）
        /// </summary>
        private void CreatePlayerTeam()
        {
            _playerTeam.Clear();

            if (_playerTeamDataSO == null)
            {
                Debug.LogWarning("[BattleManager] 未设置玩家队伍数据 _playerTeamDataSO");
                return;
            }

            foreach (HeroData hero in _playerTeamDataSO.teamMembers)
            {
                if (hero == null) continue;

                BattleEntityData entity = new BattleEntityData
                {
                    heroName = hero.heroName,
                    heroID = hero.heroID,
                    team = BattleTeam.Player,
                    maxHP = hero.maxHP,
                    currentHP = hero.maxHP,
                    attack = hero.attack,
                    defense = hero.defense,
                    speed = hero.speed,
                    critRate = 0.05f,
                    critDamage = 1.5f,
                    maxEnergy = hero.maxEnergy,
                    currentEnergy = hero.maxEnergy,
                    element = hero.element,
                    path = hero.path,
                    isAlive = true
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

        /// <summary>
        /// 为每个敌人生成的 GameObject 添加 BattleEnemyAgent 并绑定对应的 BattleEntityData。
        /// 必须在 CreateEnemyTeam() 之后、首回合开始前调用。
        /// </summary>
        private void InitializeEnemyAgents()
        {
            for (int i = 0; i < _enemyTeam.Count; i++)
            {
                BattleEntityData entity = _enemyTeam[i];
                if (!_entityTransformMap.TryGetValue(entity.heroID, out Transform t))
                {
                    Debug.LogWarning($"[BattleManager] 未找到敌人 {entity.heroName} (heroID={entity.heroID}) 的 Transform，跳过 AI 初始化");
                    continue;
                }

                BattleEnemyAgent agent = t.gameObject.GetComponent<BattleEnemyAgent>();
                if (agent == null)
                    agent = t.gameObject.AddComponent<BattleEnemyAgent>();

                agent.Initialize(entity, _defaultEnemyAI);
                Debug.Log($"[BattleManager] 敌人 AI 已初始化: {entity.heroName} (heroID={entity.heroID})");
            }
        }
    }
}
