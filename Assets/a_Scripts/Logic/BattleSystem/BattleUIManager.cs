using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 UI 管理器 — 控制按钮、血条、战斗日志
/// 所有 UI 引用通过序列化字段注入
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BattleUIManager : MonoBehaviour
{
    [Header("行动按钮")]
    public Button attackButton;
    public Button skillButton;
    public Button defendButton;
    public Button itemButton;
    public Button fleeButton;

    [Header("信息显示")]
    public Text statusText;
    public Text turnText;
    public Text battleLogText;

    [Header("血条预制体")]
    public GameObject hpBarPrefab;
    public Transform playerBarsRoot;
    public Transform enemyBarsRoot;

    [Header("过渡效果")]
    public CanvasGroup battleStartOverlay;
    public float fadeInDuration = 1f;

    // 行动选择事件
    public event Action<BattleAction> OnActionSelected;

    // 缓存的血条引用
    private readonly List<HPBarController> _playerBars = new();
    private readonly List<HPBarController> _enemyBars = new();

    private CanvasGroup _canvasGroup;
    private BattleStateManager _manager;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // 绑定按钮事件
        if (attackButton != null) attackButton.onClick.AddListener(() => OnActionButton(BattleAction.Attack));
        if (skillButton != null) skillButton.onClick.AddListener(() => OnActionButton(BattleAction.Skill));
        if (defendButton != null) defendButton.onClick.AddListener(() => OnActionButton(BattleAction.Defend));
        if (itemButton != null) itemButton.onClick.AddListener(() => OnActionButton(BattleAction.Item));
        if (fleeButton != null) fleeButton.onClick.AddListener(() => OnActionButton(BattleAction.Flee));
    }

    /// <summary>
    /// 初始化 UI（由 BattleBeginState 调用）
    /// </summary>
    public void Initialize(BattleStateManager manager)
    {
        _manager = manager;

        // 创建血条
        CreateHPBars(manager.PlayerParty, playerBarsRoot, _playerBars);
        CreateHPBars(manager.EnemyParty, enemyBarsRoot, _enemyBars);

        // 初始更新
        UpdateEntityUI(manager.PlayerParty, manager.EnemyParty);

        // 隐藏行动面板
        ShowActionPanel(false);
    }

    #region 血条管理

    private void CreateHPBars(List<BattleEntityData> entities, Transform root, List<HPBarController> barList)
    {
        if (hpBarPrefab == null || root == null) return;

        barList.Clear();
        foreach (var entity in entities)
        {
            var go = Instantiate(hpBarPrefab, root);
            var bar = go.GetComponent<HPBarController>();
            if (bar != null)
            {
                bar.Setup(entity);
                barList.Add(bar);
            }
        }
    }

    /// <summary>
    /// 更新所有实体血条
    /// </summary>
    public void UpdateEntityUI(List<BattleEntityData> players, List<BattleEntityData> enemies)
    {
        for (int i = 0; i < _playerBars.Count && i < players.Count; i++)
            _playerBars[i].Refresh(players[i]);

        for (int i = 0; i < _enemyBars.Count && i < enemies.Count; i++)
            _enemyBars[i].Refresh(enemies[i]);
    }

    #endregion

    #region 面板控制

    /// <summary>
    /// 显示/隐藏行动面板
    /// </summary>
    public void ShowActionPanel(bool show)
    {
        if (attackButton != null) attackButton.gameObject.SetActive(show);
        if (skillButton != null) skillButton.gameObject.SetActive(show);
        if (defendButton != null) defendButton.gameObject.SetActive(show);
        if (itemButton != null) itemButton.gameObject.SetActive(show);
        if (fleeButton != null) fleeButton.gameObject.SetActive(show);
    }

    private void OnActionButton(BattleAction action)
    {
        OnActionSelected?.Invoke(action);
    }

    #endregion

    #region 信息显示

    /// <summary>
    /// 设置状态文字
    /// </summary>
    public void SetStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    /// <summary>
    /// 添加战斗日志
    /// </summary>
    public void AddBattleLog(string text)
    {
        if (battleLogText != null)
            battleLogText.text += $"\n{text}";
    }

    #endregion

    #region 过渡效果

    /// <summary>
    /// 播放战斗开始效果
    /// </summary>
    public IEnumerator ShowBattleStartEffect()
    {
        if (battleStartOverlay == null) yield break;

        battleStartOverlay.gameObject.SetActive(true);
        battleStartOverlay.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            battleStartOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }

        battleStartOverlay.alpha = 0f;
        battleStartOverlay.gameObject.SetActive(false);
    }

    #endregion
}

/// <summary>
/// 单个血条控制器
/// </summary>
public class HPBarController : MonoBehaviour
{
    public Slider hpSlider;
    public Text nameText;
    public Text hpText;

    private BattleEntityData _entity;

    public void Setup(BattleEntityData entity)
    {
        _entity = entity;
        Refresh(entity);
    }

    public void Refresh(BattleEntityData entity)
    {
        _entity = entity;
        if (hpSlider != null)
            hpSlider.value = (float)entity.currentHP / entity.maxHP;
        if (hpText != null)
            hpText.text = $"{entity.currentHP}/{entity.maxHP}";
        if (nameText != null)
            nameText.text = entity.entityName;
    }
}
