using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人行为树 — 简单决策系统
/// Selector（选择器）：依次尝试子节点，直到一个成功
/// Sequence（序列）：依次执行所有子节点，全部成功才算成功
/// </summary>
public class EnemyAI_BT
{
    /// <summary>
    /// 评估 AI 并返回行动决策
    /// </summary>
    public EnemyAction Evaluate(BattleEntityData self, List<BattleEntityData> playerParty)
    {
        var alivePlayers = playerParty.FindAll(p => p.isAlive);
        if (alivePlayers.Count == 0)
            return new EnemyAction(EnemyActionType.Defend, -1);

        // 行为树根节点
        var root = new SelectorNode();

        // 策略 1: 如果血量低，防御
        root.AddChild(new ConditionNode(() => (float)self.currentHP / self.maxHP < 0.3f,
            new ActionNode(() => new EnemyAction(EnemyActionType.Defend, -1))));

        // 策略 2: 如果敌人有高威胁目标（这里简单选择血量最低的玩家）
        root.AddChild(new ActionNode(() =>
        {
            int targetIndex = FindWeakestTarget(alivePlayers);
            return new EnemyAction(EnemyActionType.Attack, targetIndex);
        }));

        // 执行行为树
        return root.Execute();
    }

    /// <summary>
    /// 寻找最弱目标（血量最低）
    /// </summary>
    private int FindWeakestTarget(List<BattleEntityData> players)
    {
        int weakestIndex = 0;
        int lowestHP = int.MaxValue;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].currentHP < lowestHP)
            {
                lowestHP = players[i].currentHP;
                weakestIndex = i;
            }
        }

        return weakestIndex;
    }
}

#region 行为树节点

/// <summary>行为树节点接口</summary>
public interface IBTNode
{
    EnemyAction Execute();
}

/// <summary>选择器 — 依次尝试子节点，返回第一个成功的结果</summary>
public class SelectorNode : IBTNode
{
    private readonly List<IBTNode> _children = new();

    public void AddChild(IBTNode node) => _children.Add(node);

    public EnemyAction Execute()
    {
        foreach (var child in _children)
        {
            var result = child.Execute();
            // 非 Defend 表示有实际行动（成功）
            if (result.actionType != EnemyActionType.Defend || _children.Count == 1)
                return result;
        }
        return new EnemyAction(EnemyActionType.Defend, -1);
    }
}

/// <summary>条件节点 — 条件满足时执行子节点</summary>
public class ConditionNode : IBTNode
{
    private readonly System.Func<bool> _condition;
    private readonly IBTNode _child;

    public ConditionNode(System.Func<bool> condition, IBTNode child)
    {
        _condition = condition;
        _child = child;
    }

    public EnemyAction Execute()
    {
        if (_condition())
            return _child.Execute();
        return new EnemyAction(EnemyActionType.Defend, -1);
    }
}

/// <summary>行动节点 — 执行具体逻辑</summary>
public class ActionNode : IBTNode
{
    private readonly System.Func<EnemyAction> _action;

    public ActionNode(System.Func<EnemyAction> action)
    {
        _action = action;
    }

    public EnemyAction Execute() => _action();
}

#endregion

#region 敌人行动模型

/// <summary>敌人行动类型</summary>
public enum EnemyActionType
{
    Attack,
    Skill,
    Defend
}

/// <summary>敌人行动决策</summary>
public struct EnemyAction
{
    public EnemyActionType actionType;
    public int targetIndex;

    public EnemyAction(EnemyActionType type, int target)
    {
        actionType = type;
        targetIndex = target;
    }
}

#endregion
