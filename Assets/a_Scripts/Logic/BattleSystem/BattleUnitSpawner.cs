using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗单位生成器 — 读取 GameManager 队伍数据，在指定位置实例化角色预制体
/// 配置在战斗场景的 GameObject 上
/// </summary>
public class BattleUnitSpawner : MonoBehaviour
{
    public TeamData_SO teamData; // 用于获取玩家队伍数据
    [Header("出生点")]
    public Transform[] playerSpawnPoints;   // 最多 4 个，左侧
    public Transform[] enemySpawnPoints;    // 最多 3 个，右侧

    [Header("父节点")]
    public Transform playerPartyRoot;
    public Transform enemyPartyRoot;

    public void Start()
    {
        SpawnAll();
    }
    // ──────────── 核心方法 ────────────

    /// <summary>
    /// 从 GameManager 读取队伍数据并生成所有单位
    /// </summary>
    public void SpawnAll()
    {
        ClearAll();

        var playerTeam = GameManager.Instance.GetPlayerTeam();
        var enemyTeam = GameManager.Instance.GetEnemyTeam();
        SpawnTeam();

        //     PlayerUnits = SpawnTeam(playerTeam, playerSpawnPoints, playerPartyRoot, BattleTeam.Player);
        //     EnemyUnits = SpawnTeam(enemyTeam, enemySpawnPoints, enemyPartyRoot, BattleTeam.Enemy);
    }

    /// <summary>
    /// 生成单个队伍
    /// </summary>
    private void SpawnTeam()
    {
        for (int i = 0; i < teamData.teamMembers.Count; i++)
        {

            HeroData hero = teamData.teamMembers[i];
            if (hero == null)
            {
                Debug.LogWarning($"[Spawner] teamMembers[{i}] 未赋值，跳过");
                continue;
            }
            GameObject prefab = hero.battlePrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[Spawner] {hero.heroName} 的 battlePrefab 未赋值，跳过");
                continue;
            }
            GameObject go = Instantiate(prefab, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation, playerPartyRoot);
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
    }


}
