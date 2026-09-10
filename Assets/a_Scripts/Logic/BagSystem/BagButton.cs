using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包按钮 — 点击召唤出 BagPanel。
///
/// 挂到屏幕上的 Bag_Buton 上，在 Inspector 中引用 BagPanelController。
/// 使用方式：把本组件加到按钮 GameObject，拖入背包面板控制器引用即可。
/// </summary>
[RequireComponent(typeof(Button))]
public class BagButton : MonoBehaviour
{
    [SerializeField] private BagPanelController _bagPanel;   // 背包面板控制器

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
        if (_bagPanel == null)
        {
            Debug.LogWarning("[BagButton] 未引用 BagPanelController，请在 Inspector 中拖入");
            return;
        }

        _bagPanel.Open();
    }
}
