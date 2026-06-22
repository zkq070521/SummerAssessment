using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗单位生成器 — 读取 GameManager 队伍数据，在指定位置实例化角色预制体
/// 配置在战斗场景的 GameObject 上
/// </summary>
public class BattleUnitSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CharacterPrefabEntry
    {
        public string prefabId;          // 与 BattleEntityData.prefabId 匹配
        public GameObject prefab;        // 对应的角色预制体
    }

    [Header("预制体映射")]
    public CharacterPrefabEntry[] characterPrefabs;

    [Header("出生点")]
    public Transform[] playerSpawnPoints;   // 最多 4 个，左侧
    public Transform[] enemySpawnPoints;    // 最多 3 个，右侧

    [Header("父节点")]
    public Transform playerPartyRoot;
    public Transform enemyPartyRoot;

    // ── 生成结果 ──
    public List<BattleUnit> PlayerUnits { get; private set; } = new();
    public List<BattleUnit> EnemyUnits { get; private set; } = new();

    // ──────────── 核心方法 ────────────

    /// <summary>
    /// 从 GameManager 读取队伍数据并生成所有单位
    /// </summary>
    public void SpawnAll()
    {
        ClearAll();

        var playerTeam = GameManager.Instance.GetPlayerTeam();
        var enemyTeam = GameManager.Instance.GetEnemyTeam();

        PlayerUnits = SpawnTeam(playerTeam, playerSpawnPoints, playerPartyRoot, BattleTeam.Player);
        EnemyUnits = SpawnTeam(enemyTeam, enemySpawnPoints, enemyPartyRoot, BattleTeam.Enemy);
    }

    /// <summary>
    /// 生成单个队伍
    /// </summary>
    private List<BattleUnit> SpawnTeam(List<BattleEntityData> team, Transform[] spawnPoints,
        Transform parent, BattleTeam teamType)
    {
        var units = new List<BattleUnit>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            // 超出队伍数据或该位置无角色 → 跳过
            if (i >= team.Count || team[i] == null)
                continue;

            var data = team[i];
            GameObject prefab = FindPrefab(data.prefabId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Spawner] 未找到 prefabId='{data.prefabId}' 的预制体，跳过");
                continue;
            }

            // 实例化
            Transform spawnPoint = spawnPoints[i];
            GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, parent);
            go.name = $"{teamType}_{data.entityName}";

            // 添加或获取 BattleUnit
            BattleUnit unit = go.GetComponent<BattleUnit>();
            if (unit == null)
                unit = go.AddComponent<BattleUnit>();

            unit.Setup(data, teamType);
            units.Add(unit);

            // 通知事件中心
            BattleEventCenter.TriggerUnitSpawned(unit);
        }

        return units;
    }

    /// <summary>
    /// 根据 prefabId 查找对应的预制体
    /// </summary>
    private GameObject FindPrefab(string prefabId)
    {
        if (string.IsNullOrEmpty(prefabId)) return null;

        foreach (var entry in characterPrefabs)
        {
            if (entry.prefabId == prefabId)
                return entry.prefab;
        }
        return null;
    }

    /// <summary>
    /// 清除所有已生成的单位
    /// </summary>
    public void ClearAll()
    {
        ClearUnits(PlayerUnits);
        ClearUnits(EnemyUnits);

        PlayerUnits.Clear();
        EnemyUnits.Clear();
    }

    private void ClearUnits(List<BattleUnit> units)
    {
        foreach (var unit in units)
        {
            if (unit != null && unit.gameObject != null)
                Destroy(unit.gameObject);
        }
    }
}
