using System.Collections.Generic;
using BattleSystem;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 攻击按钮控制器 — 管理单攻/群攻两个按钮的交互逻辑
///
/// 操作方式：
///   A / D 键     — 左右切换瞄准目标（仅存活敌人间循环）
///   单攻按钮      — 对当前瞄准的敌人执行伤害
///   群攻按钮      — 第一次：进入群攻瞄准模式（瞄准全部敌人）
///                   第二次：对全部存活敌人执行伤害
///   群攻→单攻    — 取消群攻模式，回到瞄准第一个敌人，再按单攻执行
/// </summary>
public class AttackButton : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button _singleAttackButton;   // 单攻按钮
    [SerializeField] private Button _aoeAttackButton;       // 群攻按钮

    /// <summary>当前瞄准的敌人在 EnemyTeam 中的索引</summary>
    public int CurrentTargetIndex => _currentTargetIndex;

    /// <summary>是否处于群攻瞄准模式（第一次按下群攻后）</summary>
    public bool IsAoeAiming => _isAoeAiming;

    /// <summary>当前瞄准的敌人数据（可能为 null）</summary>
    public BattleEntityData CurrentTarget => GetAliveTargetAt(_currentTargetIndex);

    // ── 内部状态 ──
    private int _currentTargetIndex;
    private bool _isAoeAiming;

    // ── 生命周期 ──

    private void Start()
    {
        ResetToFirstAlive();
    }

    private void OnEnable()
    {
        if (_singleAttackButton != null)
            _singleAttackButton.onClick.AddListener(OnSingleAttackClicked);
        if (_aoeAttackButton != null)
            _aoeAttackButton.onClick.AddListener(OnAoeAttackClicked);
    }

    private void OnDisable()
    {
        if (_singleAttackButton != null)
            _singleAttackButton.onClick.RemoveListener(OnSingleAttackClicked);
        if (_aoeAttackButton != null)
            _aoeAttackButton.onClick.RemoveListener(OnAoeAttackClicked);
    }

    private void Update()
    {
        HandleTargetSwitchInput();
    }

    // ── 输入处理 ──

    /// <summary>
    /// A 键左移目标，D 键右移目标（仅在存活敌人间循环）
    /// </summary>
    private void HandleTargetSwitchInput()
    {
        // 仅玩家回合允许切换目标
        if (!IsPlayerTurn()) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            SwitchTarget(-1);
            Debug.Log("[AttackButton] 切换目标: ←");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SwitchTarget(1);
            Debug.Log("[AttackButton] 切换目标: →");
        }
    }

    /// <summary>
    /// 切换瞄准目标
    /// </summary>
    /// <param name="direction">-1 向左，+1 向右</param>
    private void SwitchTarget(int direction)
    {
        IReadOnlyList<BattleEntityData> enemies = GetEnemyTeam();
        if (enemies == null || enemies.Count == 0) return;

        // 收集存活敌人的索引列表
        List<int> aliveIndices = GetAliveIndices(enemies);
        if (aliveIndices.Count == 0)
        {
            Debug.Log("[AttackButton] 没有存活的敌人可以切换目标");
            return;
        }

        // 在存活列表中定位当前索引，若当前目标已死则回到首位
        int pos = aliveIndices.IndexOf(_currentTargetIndex);
        if (pos < 0)
        {
            _currentTargetIndex = aliveIndices[0];
        }
        else
        {
            pos = (pos + direction + aliveIndices.Count) % aliveIndices.Count;
            _currentTargetIndex = aliveIndices[pos];
        }

        LogTarget();
    }

    // ── 按钮回调 ──

    /// <summary>
    /// 单攻按钮：如果在群攻瞄准中则取消，否则对当前目标执行伤害
    /// </summary>
    private void OnSingleAttackClicked()
    {
        // 仅玩家回合允许操作
        if (!IsPlayerTurn()) return;

        if (_isAoeAiming)
        {
            // 群攻瞄准中按单攻 → 取消群攻，回到第一个敌人
            _isAoeAiming = false;
            ResetToFirstAlive();
            Debug.Log("[AttackButton] 取消群攻瞄准，回到单攻模式");
            return;
        }

        BattleEntityData source = GetCurrentActor();
        BattleEntityData target = GetAliveTargetAt(_currentTargetIndex);

        if (source == null || target == null)
        {
            Debug.LogWarning($"[AttackButton] 单攻失败: source={source != null}, target={target != null}. 请确认战斗已开始且目标存活");
            return;
        }

        BattleManager.Instance.ExecuteActionWithTurn(source, target, source.basicAttackMultiplier);
        Debug.Log($"[AttackButton] 单攻: {source.heroName} → {target.heroName}");
    }

    /// <summary>
    /// 群攻按钮：第一次进入瞄准模式，第二次对所有存活敌人执行伤害
    /// </summary>
    private void OnAoeAttackClicked()
    {
        // 仅玩家回合允许操作
        if (!IsPlayerTurn()) return;

        if (!_isAoeAiming)
        {
            // 第一次：进入群攻瞄准模式
            _isAoeAiming = true;
            Debug.Log("[AttackButton] 群攻瞄准中 — 再按一次确认攻击全部敌人");
            return;
        }

        // 第二次：执行群攻
        BattleEntityData source = GetCurrentActor();
        IReadOnlyList<BattleEntityData> enemies = GetEnemyTeam();

        if (source == null || enemies == null || enemies.Count == 0)
        {
            Debug.LogWarning($"[AttackButton] 群攻失败: source={source != null}, enemies={enemies?.Count ?? 0}. 请确认战斗已开始");
            _isAoeAiming = false;
            return;
        }

        // 先筛选出所有活着的敌人
        List<BattleEntityData> aliveEnemies = enemies.Where(e => e.isAlive).ToList();

        int hitCount = 0;
        foreach (BattleEntityData enemy in aliveEnemies)  // 遍历副本
        {
            BattleManager.Instance.ExecuteAction(source, enemy, source.ultimateMultiplier);
            hitCount++;
        }
        // 即使 ExecuteAction 修改了原始的 enemies 列表，也不会影响 aliveEnemies 的遍历

        Debug.Log($"[AttackButton] 群攻: {source.heroName} → {hitCount} 个敌人");
        _isAoeAiming = false;

        BattleManager.Instance.StartCoroutineWaitAndNextTurn();
    }

    // ── 辅助方法 ──

    /// <summary>重置瞄准到第一个存活敌人</summary>
    private void ResetToFirstAlive()
    {
        IReadOnlyList<BattleEntityData> enemies = GetEnemyTeam();
        if (enemies == null || enemies.Count == 0)
        {
            _currentTargetIndex = 0;
            return;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].isAlive)
            {
                _currentTargetIndex = i;
                LogTarget();
                return;
            }
        }

        // 全部阵亡，保留当前索引不变（由调用方处理 null）
    }

    /// <summary>获取指定索引的存活敌人，若已死亡或越界则返回 null 并自动修正索引</summary>
    private BattleEntityData GetAliveTargetAt(int index)
    {
        IReadOnlyList<BattleEntityData> enemies = GetEnemyTeam();
        if (enemies == null || enemies.Count == 0)
            return null;

        // 索引越界 → 先尝试修正（敌人被移除后索引可能失效）
        if (index < 0 || index >= enemies.Count)
        {
            ResetToFirstAlive();
            index = _currentTargetIndex;
        }

        if (index >= 0 && index < enemies.Count)
        {
            BattleEntityData target = enemies[index];
            if (target != null && target.isAlive)
                return target;

            // 目标已死 → 尝试回正到第一个存活敌人
            ResetToFirstAlive();
            if (_currentTargetIndex >= 0 && _currentTargetIndex < enemies.Count)
            {
                BattleEntityData newTarget = enemies[_currentTargetIndex];
                if (newTarget != null && newTarget.isAlive)
                    return newTarget;
            }
        }

        return null;
    }

    /// <summary>收集所有存活敌人的索引</summary>
    private static List<int> GetAliveIndices(IReadOnlyList<BattleEntityData> enemies)
    {
        List<int> alive = new List<int>(enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].isAlive)
                alive.Add(i);
        }
        return alive;
    }

    private static IReadOnlyList<BattleEntityData> GetEnemyTeam()
    {
        return BattleManager.Instance != null ? BattleManager.Instance.EnemyTeam : null;
    }

    private static BattleEntityData GetCurrentActor()
    {
        return BattleManager.Instance != null ? BattleManager.Instance.CurrentActor : null;
    }

    /// <summary>检查当前是否为玩家回合</summary>
    private static bool IsPlayerTurn()
    {
        BattleManager bm = BattleManager.Instance;
        return bm != null && bm.IsBattleStarted && bm.CurrentActor?.team == BattleTeam.Player;
    }

    private void LogTarget()
    {
        BattleEntityData target = GetAliveTargetAt(_currentTargetIndex);
        if (target != null)
            Debug.Log($"[AttackButton] 当前目标: {target.heroName} (索引 {_currentTargetIndex})");
    }
}
