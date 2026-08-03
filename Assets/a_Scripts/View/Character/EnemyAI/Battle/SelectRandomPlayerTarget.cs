using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BattleSystem;
using UnityEngine;

/// <summary>
/// BD 动作节点：从存活玩家中随机选择一个作为攻击目标。
///
/// 当没有仇恨目标（尚无玩家攻击过，或上次攻击者已阵亡）时，
/// 作为回退策略随机选取一个存活玩家。
///
/// 使用场景：EnemyBattleAI_Default 行为树 Selector 的兜底分支
/// </summary>
[TaskCategory("Battle")]
[TaskDescription("从存活玩家中随机选择攻击目标，写入 TargetHeroID")]
public class SelectRandomPlayerTarget : Action
{
    /// <summary>输出：选中的目标角色 heroID</summary>
    public SharedString targetHeroID;

    public override TaskStatus OnUpdate()
    {
        if (BattleManager.Instance == null)
            return TaskStatus.Failure;

        IReadOnlyList<BattleEntityData> playerTeam = BattleManager.Instance.PlayerTeam;
        if (playerTeam == null || playerTeam.Count == 0)
            return TaskStatus.Failure;

        // 收集所有存活玩家
        var alivePlayers = new List<BattleEntityData>(playerTeam.Count);
        for (int i = 0; i < playerTeam.Count; i++)
        {
            if (playerTeam[i] != null && playerTeam[i].isAlive)
                alivePlayers.Add(playerTeam[i]);
        }

        if (alivePlayers.Count == 0)
            return TaskStatus.Failure;

        int index = Random.Range(0, alivePlayers.Count);
        targetHeroID.Value = alivePlayers[index].heroID;
        return TaskStatus.Success;
    }
}
