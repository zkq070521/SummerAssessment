using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionUI : MonoBehaviour
{
    public Button thisButton;
    public TextMeshProUGUI optionText;

    private DialogueOption currentOption;

    void Aake()
    {
        thisButton = GetComponent<Button>();
        optionText = GetComponentInChildren<TextMeshProUGUI>();
    }

    // public void SetupOption(DialogueOption option)
    // {
    //     currentOption = option;
    //     if (optionText != null)
    //     {
    //         optionText.text = option.text;
    //     }

    //     if (thisButton != null)
    //     {
    //         thisButton.onClick.RemoveAllListeners();
    //         thisButton.onClick.AddListener(OnOptionSelected);
    //     }
    // }

    // void OnOptionSelected()
    // {
    //     Debug.Log($"选项被点击: {currentOption.text}");
    //     DialogueUI.Instance.HandleOptionSelection(currentOption);
    // }
}
