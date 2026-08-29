using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景加载按钮 — 挂到任意 UI Button 上，配置目标场景名，点击后按名字加载场景。
///
/// 依赖场景中的 SceneTransitionManager 单例。
/// 使用方式：把本组件加到按钮 GameObject，在 Inspector 填入 _sceneName（如 "Dance"）。
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneLoadButton : MonoBehaviour
{
    [SerializeField] private string _sceneName;   // 目标场景名（需已加入 Build Settings）

    private Button _button;

    private void Awake() => _button = GetComponent<Button>();

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (string.IsNullOrEmpty(_sceneName))
        {
            Debug.LogWarning($"[SceneLoadButton] {gameObject.name} 未配置场景名");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[SceneLoadButton] 场景中不存在 SceneTransitionManager，无法加载场景");
            return;
        }

        SceneTransitionManager.Instance.LoadScene(_sceneName);
    }
}
