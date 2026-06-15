using UnityEngine;

/// <summary>
/// 战斗实体数据 — 角色/敌人的属性配置
/// </summary>
[System.Serializable]
public class BattleEntityData
{
    [Header("基础属性")]
    public string entityName = "未命名";
    public int maxHP = 100;
    public int currentHP;

    [Header("战斗属性")]
    public int attack = 10;
    public int defense = 5;
    public int speed = 8;          // 决定行动顺序
    public float critRate = 0.05f; // 暴击率
    public float critDamage = 1.5f;// 暴击倍率

    [Header("状态")]
    public bool isAlive = true;

    public BattleEntityData Clone()
    {
        return new BattleEntityData
        {
            entityName = entityName,
            maxHP = maxHP,
            currentHP = currentHP,
            attack = attack,
            defense = defense,
            speed = speed,
            critRate = critRate,
            critDamage = critDamage,
            isAlive = isAlive
        };
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        if (currentHP <= 0)
            isAlive = false;
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        isAlive = true;
    }
}
