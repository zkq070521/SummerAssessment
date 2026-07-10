using BattleSystem;
using UnityEngine;

/// <summary>
/// 战斗实体数据 — 角色/敌人的纯数据配置（与视觉表现完全解耦）
/// 由 BattleManager 在 StartBattle 时创建，TurnManager 基于 speed 计算行动顺序
/// </summary>
[System.Serializable]
public class BattleEntityData
{
    [Header("基础信息")]
    public string entityName = "未命名";
    public string prefabId = "";          // 对应预制体的 ID（如 "Knight", "Mage"）

    [Header("基础属性")]
    public int maxHP = 100;
    public int currentHP;
    public int attack = 10;
    public int defense = 5;
    public int speed = 8;                 // 决定行动顺序，值越高速越快

    [Header("战斗属性")]
    public float critRate = 0.05f;        // 暴击率
    public float critDamage = 1.5f;       // 暴击倍率

    [Header("状态")]
    public bool isAlive = true;
    public BattleTeam team;               // 所属队伍（玩家 / 敌人）

    public BattleEntityData Clone()
    {
        return new BattleEntityData
        {
            entityName = entityName,
            prefabId = prefabId,
            maxHP = maxHP,
            currentHP = currentHP,
            attack = attack,
            defense = defense,
            speed = speed,
            critRate = critRate,
            critDamage = critDamage,
            isAlive = isAlive,
            team = team
        };
    }

    /// <summary>受到伤害，HP 归零时自动标记死亡</summary>
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        if (currentHP <= 0)
            isAlive = false;
    }

    /// <summary>恢复 HP</summary>
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        isAlive = true;
    }
}
