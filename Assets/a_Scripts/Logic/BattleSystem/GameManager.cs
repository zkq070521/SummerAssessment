using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏管理器 — 持久化单例，管理玩家状态、存档、战斗数据
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("玩家数据")]
    public int playerLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public int gold = 0;

    [Header("玩家队伍配置")]
    public List<BattleEntityData> playerTeamTemplate = new();

    [Header("当前遭遇的敌人（战斗前设置）")]
    public List<BattleEntityData> currentEnemyTeam = new();

    [Header("场景")]
    public string battleSceneName;
    public string overworldSceneName;

    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(GameManager));
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化玩家队伍数据
        if (playerTeamTemplate.Count == 0)
            InitializeDefaultTeam();
    }

    #region 队伍管理

    private void InitializeDefaultTeam()
    {
        playerTeamTemplate.Add(new BattleEntityData
        {
            entityName = "勇者",
            maxHP = 200,
            currentHP = 200,
            attack = 25,
            defense = 12,
            speed = 10,
            critRate = 0.1f,
            critDamage = 2f
        });

        playerTeamTemplate.Add(new BattleEntityData
        {
            entityName = "法师",
            maxHP = 120,
            currentHP = 120,
            attack = 35,
            defense = 5,
            speed = 12,
            critRate = 0.15f,
            critDamage = 1.8f
        });

        playerTeamTemplate.Add(new BattleEntityData
        {
            entityName = "牧师",
            maxHP = 150,
            currentHP = 150,
            attack = 15,
            defense = 8,
            speed = 8,
            critRate = 0.05f,
            critDamage = 1.5f
        });
    }

    /// <summary>
    /// 获取当前玩家队伍的克隆（用于战斗）
    /// </summary>
    public List<BattleEntityData> GetPlayerTeam()
    {
        var team = new List<BattleEntityData>();
        foreach (var template in playerTeamTemplate)
            team.Add(template.Clone());
        return team;
    }

    /// <summary>
    /// 战斗结束后同步数据
    /// </summary>
    public void SyncPlayerTeam(List<BattleEntityData> battleResult)
    {
        for (int i = 0; i < playerTeamTemplate.Count && i < battleResult.Count; i++)
        {
            playerTeamTemplate[i].currentHP = battleResult[i].currentHP;
            playerTeamTemplate[i].isAlive = battleResult[i].isAlive;
        }
    }

    /// <summary>
    /// 获取当前敌人队伍的克隆（用于战斗）
    /// </summary>
    public List<BattleEntityData> GetEnemyTeam()
    {
        var team = new List<BattleEntityData>();
        foreach (var template in currentEnemyTeam)
            team.Add(template.Clone());
        return team;
    }

    /// <summary>
    /// 设置当前战斗的敌人并加载战斗场景
    /// </summary>
    public void BeginBattle(List<BattleEntityData> enemyTeam)
    {
        currentEnemyTeam.Clear();
        foreach (var e in enemyTeam)
            currentEnemyTeam.Add(e.Clone());

        if (!string.IsNullOrEmpty(battleSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
        else
            Debug.LogError("[GameManager] battleSceneName 未设置！");
    }

    #endregion

    #region 经验 & 金币

    public void AddBattleReward(int exp, int goldReward)
    {
        currentExp += exp;
        gold += goldReward;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            playerLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.5f);
            Debug.Log($"[GameManager] 升级！当前等级：{playerLevel}");

            // 升级提升属性
            foreach (var member in playerTeamTemplate)
            {
                member.maxHP += 10;
                member.currentHP = member.maxHP;
                member.attack += 2;
                member.defense += 1;
            }
        }
    }

    #endregion
}
