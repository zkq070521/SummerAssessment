using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BattleSystem;

/// <summary>
/// BD 动作节点：选择上一个攻击者（LastPlayerAttacker）作为攻击目标。
///
/// 将目标 heroID 写入 SharedString TargetHeroID，供 BattleEnemyAgent 读取后执行攻击。
/// 要求 HasLastAttacker 已返回 Success（即上一个攻击者存在且存活），
/// 否则本节点不应被行为树选中执行。
///
/// 使用场景：EnemyBattleAI_Default 行为树 Aggro Attack Sequence 的第二条
/// </summary>
[TaskCategory("Battle")]
[TaskDescription("选择 LastPlayerAttacker 作为攻击目标，写入 TargetHeroID")]
public class SelectAggroTarget : Action
{
    /// <summary>输出：选中的目标角色 heroID</summary>
    public SharedString targetHeroID;

    public override TaskStatus OnUpdate()
    {
        if (BattleManager.Instance == null)
            return TaskStatus.Failure;

        BattleEntityData last = BattleManager.Instance.LastPlayerAttacker;
        if (last != null && last.isAlive)
        {
            targetHeroID.Value = last.heroID;
            return TaskStatus.Success;
        }

        return TaskStatus.Failure;
    }
}
