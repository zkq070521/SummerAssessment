namespace BattleSystem
{
    /// <summary>
    /// 攻击类型枚举
    ///
    /// 区分单体攻击与群体攻击，供 BattleManager.ExecuteAction 决定是否广播单攻事件，
    /// 以及表现层区分行为：
    /// - AttackEffectPlayer 据此选择「单攻特效组」或「群攻特效组」
    /// - BattleAttackApproach 只在 Single 时冲撞（群攻走 OnAoeAttack，不触发 OnUnitAttack）
    ///
    /// - Single: 单体攻击，对单个目标结算伤害并广播一次 OnUnitAttack
    /// - AreaOfEffect: 群体攻击，对每个目标分别结算伤害，但只广播一次 OnAoeAttack
    /// </summary>
    public enum AttackKind
    {
        /// <summary>单体攻击</summary>
        Single,

        /// <summary>群体攻击（AoE）</summary>
        AreaOfEffect,
    }
}
