using System.Collections;
using UnityEngine;

/// <summary>
/// 敌人回合状态 — 依次执行每个存活敌人的 AI 行为
/// </summary>
public class EnemyTurnState : IBattleState
{
    private readonly BattleStateManager _manager;
    private Coroutine _routine;

    public EnemyTurnState(BattleStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        if (_manager.battleUI != null)
            _manager.battleUI.SetStatusText("敌人回合");

        _routine = _manager.StartCoroutine(ProcessEnemyTurn());
    }

    public void Execute() { }

    public void Exit()
    {
        if (_routine != null)
        {
            _manager.StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator ProcessEnemyTurn()
    {
        var aliveEnemies = _manager.EnemyParty.FindAll(e => e.isAlive);

        foreach (var enemy in aliveEnemies)
        {
            var alivePlayers = _manager.PlayerParty.FindAll(p => p.isAlive);
            if (alivePlayers.Count == 0) break;

            // 1. AI 决策
            var ai = new EnemyAI_BT();
            EnemyAction decision = ai.Evaluate(enemy, _manager.PlayerParty);

            // 2. 执行行动
            switch (decision.actionType)
            {
                case EnemyActionType.Attack:
                    if (decision.targetIndex >= 0 && decision.targetIndex < alivePlayers.Count)
                    {
                        var target = alivePlayers[decision.targetIndex];
                        ExecuteEnemyAttack(enemy, target);
                    }
                    break;
                case EnemyActionType.Defend:
                    if (_manager.battleUI != null)
                        _manager.battleUI.AddBattleLog($"{enemy.entityName} 进入防御状态");
                    break;
                case EnemyActionType.Skill:
                    if (decision.targetIndex >= 0 && decision.targetIndex < alivePlayers.Count)
                    {
                        var target = alivePlayers[decision.targetIndex];
                        ExecuteEnemyAttack(enemy, target);
                    }
                    break;
            }

            // 3. 更新 UI
            if (_manager.battleUI != null)
                _manager.battleUI.UpdateEntityUI(_manager.PlayerParty, _manager.EnemyParty);

            // 4. 每个敌人行动间短暂延迟（体现节奏感）
            yield return new WaitForSeconds(0.5f);

            // 检查玩家是否全灭
            if (_manager.CheckBattleEnd() != BattleResult.None)
            {
                _manager.ChangeState<BattleEndState>();
                yield break;
            }
        }

        // 敌人全部行动完毕，回到玩家回合
        _manager.AdvanceTurn();
        _manager.ChangeState<PlayerTurnState>();
    }

    private void ExecuteEnemyAttack(BattleEntityData enemy, BattleEntityData target)
    {
        if (!BattleCalculator.IsHit(enemy, target))
        {
            if (_manager.battleUI != null)
                _manager.battleUI.AddBattleLog($"{enemy.entityName} 攻击 {target.entityName} 未命中！");
            return;
        }

        int damage = BattleCalculator.CalculateDamage(enemy, target, out bool isCrit);
        target.TakeDamage(damage);

        string critText = isCrit ? "（暴击！）" : "";
        if (_manager.battleUI != null)
            _manager.battleUI.AddBattleLog($"{enemy.entityName} 对 {target.entityName} 造成 {damage} 点伤害{critText}");
    }
}
