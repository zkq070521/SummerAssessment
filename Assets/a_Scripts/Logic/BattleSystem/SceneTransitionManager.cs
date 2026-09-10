using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景过渡管理器（单例）— 统一按「场景名」加载场景，并编排战斗过场动画。
///
/// 1. 通用入口 LoadScene(string)：淡入淡出过渡（供按钮等通用场景切换）。
/// 2. 战斗过场：命中敌人后，按「碎屏 → 黑幕 → 白线从左到右 → 加载战斗场景」串行播放。
///
/// 黑幕与白线在运行时自动生成（顶层 Canvas），无需在编辑器配置；
/// 它们作为本组件所在场景对象（GameManager）的子物体，切场景时随旧场景一起销毁。
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("通用过渡")]
    [SerializeField] private Animator _transitionAnim;           // 过渡 Animator（可空）
    [SerializeField] private float _transitionDuration = 1.5f;   // 过渡动画时长（秒）

    [Header("战斗过场")]
    [SerializeField] private BreakScreen _breakScreen;            // 碎屏特效（可空，留空则 Awake 自动查找）
    [SerializeField][Min(0f)] private float _preBreakDelay = 2f;      // 碎片铺满后停留到爆炸的间隔（秒）
    [SerializeField][Min(0f)] private float _afterBreakDelay = 3f;    // 碎开后停留展示的时长（秒）
    [SerializeField][Min(0f)] private float _blackFadeDuration = 0.5f;  // 黑幕淡入时长（秒）
    [SerializeField][Min(0f)] private float _whiteLineDuration = 0.8f;  // 白线从左到右延伸时长（秒）
    [SerializeField][Min(0f)] private float _postLineDelay = 0.4f;      // 白线结束后到加载的停顿（秒）

    [Header("命中敌人后进入的战斗场景名")]
    [SerializeField] private string _battleSceneName = "Battle1";

    // ── 运行时生成的过渡 UI ──
    private Image _blackOverlay;
    private Image _whiteLine;

    private void Awake()
    {
        Instance = this;

        // 缓存碎屏引用（避免 Update 中查找；留空则自动找场景里的 BreakScreen）
        if (_breakScreen == null)
            _breakScreen = FindObjectOfType<BreakScreen>();

        EnsureOverlayCreated();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.OnHitEnemy += OnHitEnemyHandler;
    private void OnDisable() => GameEvents.OnHitEnemy -= OnHitEnemyHandler;

    /// <summary>命中敌人 → 播放战斗过场后进入战斗场景</summary>
    private void OnHitEnemyHandler(GameObject enemy, Vector3 hitPoint)
    {
        StartCoroutine(BattleTransitionRoutine());
    }

    /// <summary>按场景名加载场景（通用淡入淡出过渡）。</summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] 场景名为空，无法加载");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransitionManager] 场景 \"{sceneName}\" 未加入 Build Settings，请先在 File → Build Settings 中添加");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (_transitionAnim != null)
            _transitionAnim.SetTrigger("StartTransition");

        yield return new WaitForSeconds(_transitionDuration);

        yield return SceneManager.LoadSceneAsync(sceneName);
    }

    // ── 战斗过场 ──

    private IEnumerator BattleTransitionRoutine()
    {
        // 阶段 1：碎屏（截屏 → 铺碎片 → 定住停留）
        if (_breakScreen != null)
        {
            yield return _breakScreen.ShowAsync();
            yield return new WaitForSeconds(_preBreakDelay);
            _breakScreen.Break();
            // 碎开后停留展示，再进入黑幕
            yield return new WaitForSeconds(_afterBreakDelay);
        }

        // 阶段 2：黑幕淡入
        yield return FadeBlackIn(_blackFadeDuration);

        // 阶段 3：白线从左到右延伸
        yield return ExtendWhiteLine(_whiteLineDuration);

        // 阶段 4：稍等后加载战斗场景（黑幕 + 白线随旧场景一起销毁）
        yield return new WaitForSeconds(_postLineDelay);

        if (Application.CanStreamedLevelBeLoaded(_battleSceneName))
            yield return SceneManager.LoadSceneAsync(_battleSceneName);
        else
            Debug.LogError($"[SceneTransitionManager] 战斗场景 \"{_battleSceneName}\" 未加入 Build Settings，请先在 File → Build Settings 中添加");
    }

    /// <summary>
    /// 战斗结束退出过场（黑幕 → 白线 → 加载目标场景，无碎屏）。
    /// 供 BattleManager 在返回大世界时调用；目标场景加载后本管理器随旧场景一起销毁，黑幕自然"揭开"新场景。
    /// </summary>
    public void PlayExitTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] 场景名为空，无法加载");
            return;
        }

        StartCoroutine(ExitTransitionRoutine(sceneName));
    }

    private IEnumerator ExitTransitionRoutine(string sceneName)
    {
        // 阶段 1：黑幕淡入
        yield return FadeBlackIn(_blackFadeDuration);

        // 阶段 2：白线从左到右延伸
        yield return ExtendWhiteLine(_whiteLineDuration);

        // 阶段 3：稍等后加载目标场景（黑幕 + 白线随旧场景销毁，自然揭开新场景）
        yield return new WaitForSeconds(_postLineDelay);

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            yield return SceneManager.LoadSceneAsync(sceneName);
        else
            Debug.LogError($"[SceneTransitionManager] 场景 \"{sceneName}\" 未加入 Build Settings，请先在 File → Build Settings 中添加");
    }

    /// <summary>黑幕从透明淡入到全黑</summary>
    private IEnumerator FadeBlackIn(float duration)
    {
        if (_blackOverlay == null) yield break;

        Color color = _blackOverlay.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / duration);
            _blackOverlay.color = color;
            yield return null;
        }
        color.a = 1f;
        _blackOverlay.color = color;
    }

    /// <summary>白线 localScale.x 从 0 到 1（pivot 在左边缘，天然左→右延伸）</summary>
    private IEnumerator ExtendWhiteLine(float duration)
    {
        if (_whiteLine == null) yield break;

        RectTransform lineRT = _whiteLine.rectTransform;
        lineRT.localScale = new Vector3(0f, 1f, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            lineRT.localScale = new Vector3(t, 1f, 1f);
            yield return null;
        }
        lineRT.localScale = new Vector3(1f, 1f, 1f);
    }

    // ── 运行时生成过渡 UI ──

    /// <summary>生成顶层 Canvas + 全屏黑幕 + 白线（Filled 水平填充）</summary>
    private void EnsureOverlayCreated()
    {
        // Canvas（ScreenSpaceOverlay，置顶盖住其他 UI）
        GameObject canvasGO = new GameObject("BattleTransitionCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 黑幕（全屏，初始透明）
        GameObject blackGO = new GameObject("BlackOverlay");
        blackGO.transform.SetParent(canvasGO.transform, false);
        _blackOverlay = blackGO.AddComponent<Image>();
        _blackOverlay.color = new Color(0f, 0f, 0f, 0f);
        _blackOverlay.raycastTarget = false;
        RectTransform blackRT = _blackOverlay.rectTransform;
        blackRT.anchorMin = Vector2.zero;
        blackRT.anchorMax = Vector2.one;
        blackRT.offsetMin = Vector2.zero;
        blackRT.offsetMax = Vector2.zero;

        // 白线（Simple 类型，pivot 在左边缘，靠 localScale.x 从 0→1 实现左→右延伸，初始 0 宽不可见）
        GameObject lineGO = new GameObject("WhiteLine");
        lineGO.transform.SetParent(canvasGO.transform, false);
        _whiteLine = lineGO.AddComponent<Image>();
        _whiteLine.color = Color.white;
        _whiteLine.raycastTarget = false;
        RectTransform lineRT = _whiteLine.rectTransform;
        lineRT.anchorMin = new Vector2(0f, 0.5f);
        lineRT.anchorMax = new Vector2(1f, 0.5f);
        lineRT.pivot = new Vector2(0f, 0.5f);       // 缩放原点在左边缘
        lineRT.sizeDelta = new Vector2(0f, 4f);     // 高 4px，全宽
        lineRT.anchoredPosition = Vector2.zero;
        lineRT.localScale = new Vector3(0f, 1f, 1f); // 初始 0 宽，不可见
    }
}
