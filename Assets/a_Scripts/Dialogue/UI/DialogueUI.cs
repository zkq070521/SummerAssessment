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
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 可选：切换场景时不销毁
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
            dialoguePanel.SetActive(false);
    }

    public void UpdateDialogueDatas(DialogueData_SO data)
    {
        currentData = data;
        currentIndex = 0;
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
            () => string.Empty,           // getter: 起始值
            value => mainText.text = value, // setter: 逐帧设置文本
            piece.text,                     // 目标完整文本
            1.5f                          // 动画持续时间（秒）
        ).SetEase(Ease.Linear);           // 匀速显示
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
        //创建Option
        CreateOption(piece);

    }
    void CreateOption(DialoguePiece piece)
    {

    }



}
