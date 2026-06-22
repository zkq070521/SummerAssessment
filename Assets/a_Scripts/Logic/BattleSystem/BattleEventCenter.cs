using System;
using UnityEngine;

/// <summary>
/// 战斗事件中心 — 静态事件总线
/// 所有战斗相关系统通过此类发布/订阅事件，实现完全解耦
/// </summary>
public static class BattleEventCenter
{
    // ──────────── 伤害事件 ────────────

    /// <summary>造成伤害时触发</summary>
    public static event Action<BattleUnit, BattleUnit, int, bool> OnDamageDealt;

    /// <param name="source">攻击者</param>
    /// <param name="target">目标</param>
    /// <param name="damage">伤害值</param>
    /// <param name="isCritical">是否暴击</param>
    public static void TriggerDamageDealt(BattleUnit source, BattleUnit target, int damage, bool isCritical)
    {
        OnDamageDealt?.Invoke(source, target, damage, isCritical);
    }

    // ──────────── 回合事件 ────────────

    /// <summary>回合切换时触发</summary>
    public static event Action<BattleTeam> OnTurnChanged;

    public static void TriggerTurnChanged(BattleTeam team)
    {
        OnTurnChanged?.Invoke(team);
    }

    // ──────────── 战斗生命周期 ────────────

    /// <summary>战斗开始时触发</summary>
    public static event Action OnBattleStart;

    public static void TriggerBattleStart()
    {
        OnBattleStart?.Invoke();
    }

    /// <summary>战斗结束时触发</summary>
    public static event Action<BattleResult> OnBattleEnd;

    public static void TriggerBattleEnd(BattleResult result)
    {
        OnBattleEnd?.Invoke(result);
    }

    // ──────────── 单位事件 ────────────

    /// <summary>单位死亡时触发</summary>
    public static event Action<BattleUnit> OnUnitDeath;

    public static void TriggerUnitDeath(BattleUnit unit)
    {
        OnUnitDeath?.Invoke(unit);
    }

    /// <summary>单位生成时触发</summary>
    public static event Action<BattleUnit> OnUnitSpawned;

    public static void TriggerUnitSpawned(BattleUnit unit)
    {
        OnUnitSpawned?.Invoke(unit);
    }

    /// <summary>单位执行攻击动画时触发（供摄像机响应）</summary>
    public static event Action<BattleUnit, BattleUnit> OnUnitAttack;

    public static void TriggerUnitAttack(BattleUnit source, BattleUnit target)
    {
        OnUnitAttack?.Invoke(source, target);
    }

    /// <summary>单位受击动画时触发（供摄像机响应）</summary>
    public static event Action<BattleUnit> OnUnitHit;

    public static void TriggerUnitHit(BattleUnit target)
    {
        OnUnitHit?.Invoke(target);
    }

    // ──────────── 摄像机事件 ────────────

    /// <summary>请求摄像机震动</summary>
    public static event Action<float, float> OnCameraShake;

    /// <param name="intensity">震动强度</param>
    /// <param name="duration">持续时间（秒）</param>
    public static void TriggerCameraShake(float intensity, float duration)
    {
        OnCameraShake?.Invoke(intensity, duration);
    }
}

/// <summary>队伍类型</summary>
public enum BattleTeam
{
    Player,
    Enemy
}
