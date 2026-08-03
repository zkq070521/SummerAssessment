using BehaviorDesigner.Runtime.Tasks;
using BattleSystem;

/// <summary>
/// BD 条件节点：检查上一个攻击的玩家角色是否存在且存活。
///
/// 用于行为树 Selector 的"仇恨攻击"分支 ——
/// 如果上一个玩家攻击者存活，则优先攻击它（仇恨机制）；
/// 否则回退到随机选择。
///
/// 使用场景：EnemyBattleAI_Default 行为树的 Aggro Attack Sequence 第一条
/// </summary>
[TaskCategory("Battle")]
[TaskDescription("检查 BattleManager.LastPlayerAttacker 是否存在且存活")]
public class HasLastAttacker : Conditional
{
    public override TaskStatus OnUpdate()
    {
        if (BattleManager.Instance == null)
            return TaskStatus.Failure;

        BattleEntityData last = BattleManager.Instance.LastPlayerAttacker;
        if (last != null && last.isAlive)
            return TaskStatus.Success;

        return TaskStatus.Failure;
    }
}
