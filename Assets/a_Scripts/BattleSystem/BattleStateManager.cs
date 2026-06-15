using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗状态机核心 — 管理状态注册、切换、执行
/// 使用依赖注入方式接收状态实例和 BattleUIManager 引用
/// </summary>
[RequireComponent(typeof(BattleUIManager))]
public class BattleStateManager : MonoBehaviour
{
    [Header("战斗引用")]
    public BattleUIManager battleUI;
    public Transform playerPartyRoot;   // 玩家队伍父节点
    public Transform enemyPartyRoot;    // 敌人队伍父节点

    [Header("战斗配置")]
    public string battleSceneName = "BattleScene";
    public string overworldSceneName = "Overworld";

    /// <summary>当前状态</summary>
    public IBattleState CurrentState { get; private set; }

    /// <summary>当前状态名称（Inspector 调试用）</summary>
    [SerializeField] private string _currentStateName;

    /// <summary>玩家队伍实体列表</summary>
    public List<BattleEntityData> PlayerParty { get; private set; } = new();

    /// <summary>敌人队伍实体列表</summary>
    public List<BattleEntityData> EnemyParty { get; private set; } = new();

    // 已注册状态
    private readonly Dictionary<Type, IBattleState> _states = new();

    // 回合计数
    public int TurnCount { get; private set; }

    // 事件
    public event Action<IBattleState> OnStateEntered;
    public event Action<IBattleState> OnStateExited;

    private void Awake()
    {
        battleUI = GetComponent<BattleUIManager>();
    }

    private void Start()
    {
        RegisterState<BattleBeginState>(new BattleBeginState(this));
        RegisterState<PlayerTurnState>(new PlayerTurnState(this));
        RegisterState<EnemyTurnState>(new EnemyTurnState(this));
        RegisterState<BattleEndState>(new BattleEndState(this));
    }

    private void Update()
    {
        CurrentState?.Execute();
    }

    #region 状态管理

    /// <summary>
    /// 注册状态
    /// </summary>
    public void RegisterState<T>(T state) where T : IBattleState
    {
        _states[typeof(T)] = state;
    }

    /// <summary>
    /// 切换到指定类型的状态
    /// </summary>
    public void ChangeState<T>() where T : IBattleState
    {
        var type = typeof(T);
        if (!_states.TryGetValue(type, out var newState))
        {
            Debug.LogError($"[BattleStateManager] 状态 {type.Name} 未注册");
            return;
        }

        CurrentState?.Exit();
        OnStateExited?.Invoke(CurrentState);

        CurrentState = newState;
        _currentStateName = type.Name;

        CurrentState.Enter();
        OnStateEntered?.Invoke(CurrentState);

        Debug.Log($"[BattleStateManager] → {type.Name}");
    }

    /// <summary>
    /// 获取已注册的状态
    /// </summary>
    public T GetState<T>() where T : IBattleState
    {
        return (T)_states[typeof(T)];
    }

    #endregion

    #region 战斗数据

    /// <summary>
    /// 初始化战斗（从 GameManager 获取数据）
    /// </summary>
    public void InitializeBattle(List<BattleEntityData> playerTeam, List<BattleEntityData> enemyTeam)
    {
        PlayerParty.Clear();
        EnemyParty.Clear();

        foreach (var p in playerTeam)
            PlayerParty.Add(p.Clone());

        foreach (var e in enemyTeam)
            EnemyParty.Add(e.Clone());

        TurnCount = 0;
    }

    /// <summary>
    /// 增加回合计数
    /// </summary>
    public void AdvanceTurn()
    {
        TurnCount++;
    }

    /// <summary>
    /// 检查战斗是否结束
    /// </summary>
    public BattleResult CheckBattleEnd()
    {
        bool allEnemiesDead = EnemyParty.TrueForAll(e => !e.isAlive);
        bool allPlayersDead = PlayerParty.TrueForAll(p => !p.isAlive);

        if (allEnemiesDead) return BattleResult.Victory;
        if (allPlayersDead) return BattleResult.Defeat;
        return BattleResult.None;
    }

    #endregion
}

/// <summary>战斗结果</summary>
public enum BattleResult
{
    None,
    Victory,
    Defeat,
    Flee
}
