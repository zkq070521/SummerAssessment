using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DialogueUI : MonoBehaviour
{
    private static DialogueUI _instance;
    public static DialogueUI Instance { get { return _instance; } }

    [Header("Basic Elements")]
    public GameObject dialoguePanel;
    public GameObject person;
    public Image image;
    public TextMeshProUGUI mainText;
    public Button nextButton;

    [Header("Option")]
    public RectTransform optionPanel;
    public OptionUI optionPrefab;

    [Header("Data")]
    public DialogueData_SO currentData;

    int currentIndex = 0;

    // 添加对话控制器的引用
    private DialogueController _currentDialogueController;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        nextButton.onClick.AddListener(ContinueDialogue);
    }

    void ContinueDialogue()
    {
        currentIndex++;
        if (currentIndex < currentData.dialoguePiece.Count)
        {
            UpdateMainDialogue(currentData.dialoguePiece[currentIndex]);
        }
        else
        {
            // 对话结束，关闭面板并恢复玩家控制
            dialoguePanel.SetActive(false);

            // 查找并调用对话控制器的EndDialogue方法
            if (_currentDialogueController != null)
            {
                _currentDialogueController.EndDialogue();
                _currentDialogueController = null;
            }
        }
    }

    public void UpdateDialogueDatas(DialogueData_SO data)
    {
        currentData = data;
        currentIndex = 0;

        // 保存当前对话控制器的引用（需要在OpenDialogue时传入）
    }

    public void UpdateMainDialogue(DialoguePiece piece)
    {
        dialoguePanel.SetActive(true);

        if (piece.image != null)
        {
            image.enabled = true;
            image.sprite = piece.image;
        }
        else
        {
            image.enabled = false;
        }

        if (mainText != null)
        {
            DOTween.To(
                () => string.Empty,
                value => mainText.text = value,
                piece.text,
                1.5f
            ).SetEase(Ease.Linear);
        }
        else
        {
            Debug.LogError("mainText 未赋值！");
        }

        if (piece.options.Count == 0 && currentData.dialoguePiece.Count > 0)
        {
            nextButton.gameObject.SetActive(true);
        }
        else
            nextButton.gameObject.SetActive(false);

        CreateOption(piece);
    }

    void CreateOption(DialoguePiece piece)
    {
        // 你的选项创建逻辑
    }

    // 添加这个方法，在开始对话时设置对话控制器
    public void SetDialogueController(DialogueController controller)
    {
        _currentDialogueController = controller;
    }
}