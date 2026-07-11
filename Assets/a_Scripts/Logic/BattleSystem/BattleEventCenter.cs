using System;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗事件中心 — 静态事件总线
    /// 所有事件基于纯数据 BattleEntityData，与视觉表现完全解耦
    /// </summary>
    public static class BattleEventCenter
    {
        // ──────────── 伤害事件 ────────────

        /// <summary>造成伤害时触发</summary>
        public static event Action<BattleEntityData, BattleEntityData, int, bool> OnDamageDealt;

        public static void TriggerDamageDealt(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
        {
            if (OnDamageDealt != null)
                OnDamageDealt(source, target, damage, isCritical);
        }

        // ──────────── 回合事件 ────────────

        /// <summary>回合所属队伍切换时触发</summary>
        public static event Action<BattleTeam> OnTurnChanged;

        public static void TriggerTurnChanged(BattleTeam team)
        {
            if (OnTurnChanged != null)
                OnTurnChanged(team);
        }

        /// <summary>某个单位获得行动回合时触发</summary>
        public static event Action<BattleEntityData> OnTurnStart;

        public static void TriggerTurnStart(BattleEntityData entity)
        {
            if (OnTurnStart != null)
                OnTurnStart(entity);
        }

        // ──────────── 战斗生命周期 ────────────

        /// <summary>战斗开始时触发</summary>
        public static event Action OnBattleStart;

        public static void TriggerBattleStart()
        {
            if (OnBattleStart != null)
                OnBattleStart();
        }

        /// <summary>战斗结束时触发，传入获胜方</summary>
        public static event Action<BattleTeam> OnBattleEnd;

        public static void TriggerBattleEnd(BattleTeam winner)
        {
            if (OnBattleEnd != null)
                OnBattleEnd(winner);
        }

        // ──────────── 单位事件 ────────────

        /// <summary>单位死亡时触发（BattleManager 检测 HP 归零后广播）</summary>
        public static event Action<BattleEntityData> OnUnitDeath;

        public static void TriggerUnitDeath(BattleEntityData entity)
        {
            if (OnUnitDeath != null)
                OnUnitDeath(entity);
        }

        /// <summary>单位入场时触发</summary>
        public static event Action<BattleEntityData> OnUnitSpawned;

        public static void TriggerUnitSpawned(BattleEntityData entity)
        {
            if (OnUnitSpawned != null)
                OnUnitSpawned(entity);
        }

        /// <summary>单位执行攻击动作时触发</summary>
        public static event Action<BattleEntityData, BattleEntityData> OnUnitAttack;

        public static void TriggerUnitAttack(BattleEntityData source, BattleEntityData target)
        {
            if (OnUnitAttack != null)
                OnUnitAttack(source, target);
        }

        /// <summary>单位受击时触发</summary>
        public static event Action<BattleEntityData> OnUnitHit;

        public static void TriggerUnitHit(BattleEntityData target)
        {
            if (OnUnitHit != null)
                OnUnitHit(target);
        }

        // ──────────── 摄像机事件 ────────────

        /// <summary>请求摄像机震动</summary>
        public static event Action<float, float> OnCameraShake;

        public static void TriggerCameraShake(float intensity, float duration)
        {
            if (OnCameraShake != null)
                OnCameraShake(intensity, duration);
        }
    }

}
