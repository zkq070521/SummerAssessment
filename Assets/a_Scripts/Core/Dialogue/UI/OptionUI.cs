using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 对话选项按钮 — 由 DialogueUI 动态实例化，用户点击后通过 targetID 跳转到目标对话片段
/// </summary>
public class OptionUI : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("thisButton")] private Button _thisButton;
    [SerializeField, FormerlySerializedAs("optionText")] private TextMeshProUGUI _optionText;

    private DialogueOption _currentOption;

    private void Awake()
    {
        if (_thisButton == null)
            _thisButton = GetComponent<Button>();
        if (_optionText == null)
            _optionText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// 填充选项数据并绑定点击回调
    /// </summary>
    public void SetupOption(DialogueOption option)
    {
        _currentOption = option;
        if (_optionText != null)
            _optionText.text = option.text;

        if (_thisButton != null)
        {
            _thisButton.onClick.RemoveAllListeners();
            _thisButton.onClick.AddListener(OnOptionSelected);
        }
    }

    private void OnOptionSelected()
    {
        if (_currentOption == null) return;
        DialogueUI.Instance.HandleOptionSelection(_currentOption);
    }
}
