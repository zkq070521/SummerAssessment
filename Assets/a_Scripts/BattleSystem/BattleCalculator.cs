using UnityEngine;

/// <summary>
/// 伤害计算器 — 纯静态方法，无副作用
/// </summary>
public static class BattleCalculator
{
    /// <summary>
    /// 计算最终伤害（含暴击、防御减免）
    /// </summary>
    /// <param name="attacker">攻击者属性</param>
    /// <param name="defender">防御者属性</param>
    /// <param name="isCritical">是否暴击（输出）</param>
    /// <returns>最终伤害值</returns>
    public static int CalculateDamage(BattleEntityData attacker, BattleEntityData defender, out bool isCritical)
    {
        // 基础伤害 = 攻击力
        float baseDamage = attacker.attack;

        // 防御减免：伤害 = 攻击力 * (100 / (100 + 防御力))
        float reductionFactor = 100f / (100f + defender.defense);
        float damage = baseDamage * reductionFactor;

        // 暴击判定
        isCritical = Random.value < attacker.critRate;
        if (isCritical)
            damage *= attacker.critDamage;

        // 最低伤害保底
        damage = Mathf.Max(1f, damage);

        return Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// 计算治疗效果
    /// </summary>
    public static int CalculateHeal(int baseHeal, BattleEntityData target)
    {
        int heal = Mathf.RoundToInt(baseHeal * (1f + Random.Range(-0.1f, 0.1f)));
        return Mathf.Max(1, heal);
    }

    /// <summary>
    /// 判断是否命中（闪避判定）
    /// </summary>
    public static bool IsHit(BattleEntityData attacker, BattleEntityData defender)
    {
        float hitRate = 1f - (defender.defense * 0.002f); // 每点防御提供0.2%闪避
        return Random.value < Mathf.Clamp01(hitRate);
    }

    /// <summary>
    /// 根据速度决定行动顺序（降序）
    /// </summary>
    public static int CompareSpeed(BattleEntityData a, BattleEntityData b)
    {
        return b.speed.CompareTo(a.speed);
    }
}
