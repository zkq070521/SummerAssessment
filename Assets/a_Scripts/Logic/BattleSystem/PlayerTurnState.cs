using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家回合状态 — 等待玩家选择行动（攻击/技能/防御/道具）
/// 通过 BattleUIManager 的事件回调接收玩家指令
/// </summary>
public class PlayerTurnState : IBattleState
{
    private readonly BattleStateManager _manager;

    // 当前行动中的角色索引
    private int _currentActorIndex;
    private bool _actionExecuted;
    private Coroutine _routine;

    public PlayerTurnState(BattleStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        _currentActorIndex = 0;
        _actionExecuted = false;

        // 触发回合切换事件
        BattleEventCenter.TriggerTurnChanged(BattleTeam.Player);

        // 筛选出存活角色
        var alivePlayers = _manager.PlayerParty.FindAll(p => p.isAlive);
        if (alivePlayers.Count == 0)
        {
            _manager.ChangeState<BattleEndState>();
            return;
        }

        // 启用 UI 行动按钮
        if (_manager.battleUI != null)
        {
            _manager.battleUI.ShowActionPanel(true);
            _manager.battleUI.OnActionSelected += OnPlayerAction;
            _manager.battleUI.SetStatusText($"玩家回合 - 第 {_manager.TurnCount} 回合");
        }

        _routine = _manager.StartCoroutine(ProcessPlayerTurn());
    }

    public void Execute()
    {
        // 更新血条等 UI 状态（由 UI 管理器自行处理）
    }

    public void Exit()
    {
        if (_routine != null)
        {
            _manager.StopCoroutine(_routine);
            _routine = null;
        }

        if (_manager.battleUI != null)
        {
            _manager.battleUI.ShowActionPanel(false);
            _manager.battleUI.OnActionSelected -= OnPlayerAction;
        }
    }

    private IEnumerator ProcessPlayerTurn()
    {
        var alivePlayers = _manager.PlayerParty.FindAll(p => p.isAlive);

        for (_currentActorIndex = 0; _currentActorIndex < alivePlayers.Count; _currentActorIndex++)
        {
            _actionExecuted = false;

            var actor = alivePlayers[_currentActorIndex];
            if (_manager.battleUI != null)
                _manager.battleUI.SetStatusText($"轮到 {actor.entityName}");

            // 高亮当前角色
            // 等待玩家选择行动（OnPlayerAction 回调会置 _actionExecuted = true）
            yield return new WaitUntil(() => _actionExecuted);
        }

        // 所有角色行动完毕，切换到敌人回合
        _manager.ChangeState<EnemyTurnState>();
    }

    /// <summary>
    /// 玩家行动回调（由 UI 按钮触发）
    /// </summary>
    private void OnPlayerAction(BattleAction action)
    {
        if (_actionExecuted) return;

        var alivePlayers = _manager.PlayerParty.FindAll(p => p.isAlive);
        if (_currentActorIndex >= alivePlayers.Count) return;

        var actor = alivePlayers[_currentActorIndex];
        var targets = _manager.EnemyParty.FindAll(e => e.isAlive);

        if (targets.Count == 0)
        {
            _manager.ChangeState<BattleEndState>();
            return;
        }

        // 默认攻击第一个存活敌人（简化版目标选择）
        var target = targets[0];

        switch (action)
        {
            case BattleAction.Attack:
                ExecuteAttack(actor, target);
                break;
            case BattleAction.Defend:
                ExecuteDefend(actor);
                break;
            case BattleAction.Skill:
                ExecuteAttack(actor, target); // 技能暂用普通攻击逻辑
                break;
        }

        // 检查敌人是否全灭
        if (_manager.CheckBattleEnd() != BattleResult.None)
        {
            _manager.ChangeState<BattleEndState>();
            return;
        }

        _actionExecuted = true;
    }

    private void ExecuteAttack(BattleEntityData actor, BattleEntityData target)
    {
        // 通过状态机的事件触发方法执行（会发布 BattleEventCenter 事件）
        _manager.ExecuteAttack(actor, target, null, null);
    }

    private void ExecuteDefend(BattleEntityData actor)
    {
        // 防御：临时增加防御（简化实现）
        if (_manager.battleUI != null)
            _manager.battleUI.AddBattleLog($"{actor.entityName} 进入防御状态");
    }
}

/// <summary>玩家可选行动</summary>
public enum BattleAction
{
    Attack,
    Skill,
    Defend,
    Item,
    Flee
}
