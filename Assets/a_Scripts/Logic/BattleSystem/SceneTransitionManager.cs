using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景过渡管理器（单例）— 统一以「场景名」加载场景，可扩展。
///
/// 提供通用入口 LoadScene(string sceneName)，配合过渡动画淡入淡出。
/// 所有场景切换（按钮点击、命中敌人进入战斗等）都应通过本组件按名字触发，
/// 新增场景无需改代码，只需调用 LoadScene(场景名)。
///
/// 挂载在常驻 GameObject 上（当前为 SampleScene 的 "GameManager"），
/// Inspector 拖入过渡 Animator（可空）。
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("过渡动画")]
    [SerializeField] private Animator _transitionAnim;           // 过渡 Animator（可空）
    [SerializeField] private float _transitionDuration = 1.5f;   // 过渡动画时长（秒）

    [Header("命中敌人后进入的战斗场景名")]
    [SerializeField] private string _battleSceneName = "Battle1";

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable() => GameEvents.OnHitEnemy += OnHitEnemyHandler;
    private void OnDisable() => GameEvents.OnHitEnemy -= OnHitEnemyHandler;

    /// <summary>命中敌人 → 进入战斗场景</summary>
    private void OnHitEnemyHandler(GameObject enemy, Vector3 hitPoint) => LoadScene(_battleSceneName);

    /// <summary>按场景名加载场景（带过渡动画）。</summary>
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
}
