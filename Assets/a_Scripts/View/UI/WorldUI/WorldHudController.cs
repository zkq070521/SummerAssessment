using UnityEngine;

/// <summary>
/// 大世界 HUD 显示控制器 — 监听对话开始/结束事件，隐藏/恢复小地图、队伍、按钮三个面板。
/// 挂载到 WorldCanva（世界 UI 的 Canvas）上，在 Inspector 中拖入 MiniCameraPanel / TeamPanel / ButtonPanel。
/// </summary>
public class WorldHudController : MonoBehaviour
{
    [Header("对话期间隐藏的 HUD 面板")]
    [SerializeField] private GameObject _miniMapPanel;   // MiniCameraPanel（小地图）
    [SerializeField] private GameObject _teamPanel;      // TeamPanel（角色队伍）
    [SerializeField] private GameObject _buttonPanel;    // ButtonPanel（主按钮）

    private void OnEnable()
    {
        GameEvents.OnDialogueStarted += OnDialogueStarted;
        GameEvents.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnDialogueStarted -= OnDialogueStarted;
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
    }

    private void OnDialogueStarted() => SetHudVisible(false);

    private void OnDialogueEnded() => SetHudVisible(true);

    /// <summary>
    /// 统一设置三个 HUD 面板的激活状态
    /// </summary>
    private void SetHudVisible(bool visible)
    {
        if (_miniMapPanel != null) _miniMapPanel.SetActive(visible);
        if (_teamPanel != null) _teamPanel.SetActive(visible);
        if (_buttonPanel != null) _buttonPanel.SetActive(visible);
    }
}
