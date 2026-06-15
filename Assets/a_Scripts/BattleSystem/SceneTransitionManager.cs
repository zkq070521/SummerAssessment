using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景过渡管理器 — 单例，异步加载 + 协程过渡效果
/// 加载期间封锁玩家输入
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("过渡设置")]
    public float fadeDuration = 1f;
    public CanvasGroup fadeCanvasGroup;

    [Header("加载画面")]
    public GameObject loadingScreen;

    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(SceneTransitionManager));
                _instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>是否正在过渡中</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>异步加载进度 [0, 1]</summary>
    public float LoadProgress { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 加载场景（带过渡效果）
    /// </summary>
    public void LoadScene(string sceneName, Action onComplete = null)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, onComplete));
    }

    private IEnumerator TransitionRoutine(string sceneName, Action onComplete)
    {
        IsTransitioning = true;
        SetInputBlocked(true);

        // 1. 淡出
        if (fadeCanvasGroup != null)
            yield return Fade(1f, fadeDuration);

        // 2. 显示加载画面
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // 3. 异步加载场景
        var asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
        {
            LoadProgress = asyncOp.progress;
            yield return null;
        }
        LoadProgress = 1f;
        asyncOp.allowSceneActivation = true;

        // 等待场景激活
        yield return new WaitUntil(() => asyncOp.isDone);

        // 4. 隐藏加载画面
        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        // 5. 淡入
        if (fadeCanvasGroup != null)
            yield return Fade(0f, fadeDuration);

        SetInputBlocked(false);
        IsTransitioning = false;

        onComplete?.Invoke();
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 封锁/恢复玩家输入（不暂停游戏）
    /// 通过 InputService 事件广播，无需 FindObjectOfType
    /// </summary>
    private void SetInputBlocked(bool blocked)
    {
        InputService.SetInputEnabled(!blocked);
    }
}
