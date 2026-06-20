using System.Collections;
using UnityEngine;

/// <summary>
/// 战斗场景总调度器 — 在战斗场景加载后自动运行
/// 编排：生成单位 → 初始化状态机 → 播放摄像机入场 → 开始战斗
/// </summary>
public class BattleSceneController : MonoBehaviour
{
    [Header("核心引用")]
    public BattleUnitSpawner unitSpawner;
    public BattleCameraController cameraController;
    public BattleStateManager stateManager;

    [Header("延迟配置")]
    public float spawnToCameraDelay = 0.3f;      // 生成完到摄像机入场的延迟
    public float entranceToBattleDelay = 0.5f;    // 入场完成到战斗开始的延迟

    private void Start()
    {
        // 自动查找引用（若未手动拖拽）
        if (unitSpawner == null)
            unitSpawner = FindFirstObjectByType<BattleUnitSpawner>();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<BattleCameraController>();
        if (stateManager == null)
            stateManager = FindFirstObjectByType<BattleStateManager>();

        if (unitSpawner == null || stateManager == null)
        {
            Debug.LogError("[BattleScene] 缺少核心组件！");
            return;
        }

        // 禁用探索场景的玩家输入
        InputService.SetInputEnabled(false);

        // 开始战斗初始化流程
        StartCoroutine(BattleInitRoutine());
    }

    private IEnumerator BattleInitRoutine()
    {
        // 1. 生成所有战斗单位
        unitSpawner.SpawnAll();

        // 2. 将生成的数据注入状态机
        stateManager.InitializeBattle(
            GameManager.Instance.GetPlayerTeam(),
            GameManager.Instance.GetEnemyTeam()
        );

        // 注意：spawner 生成的 BattleUnit 列表可以通过以下方式传给状态机后续使用
        // 状态机可以通过 BattleEventCenter 事件间接操作，无需直接持有引用

        yield return new WaitForSeconds(spawnToCameraDelay);

        // 3. 触发战斗开始事件（摄像机入场由 EventCenter 驱动）
        BattleEventCenter.TriggerBattleStart();

        yield return new WaitForSeconds(entranceToBattleDelay);

        // 4. 进入第一个战斗状态（BeginState → 初始化 UI → PlayerTurn）
        stateManager.ChangeState<BattleBeginState>();
    }

    /// <summary>
    /// 清理战斗场景（返回探索场景时调用）
    /// </summary>
    public void CleanupBattle()
    {
        unitSpawner.ClearAll();
        cameraController.ResetCamera();
        InputService.SetInputEnabled(true);
    }
}
