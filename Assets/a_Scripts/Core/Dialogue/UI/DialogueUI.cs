using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 对话 UI 主控 — 单例，管理对话面板、打字机效果、分支选项、
/// 线性推进和对话结束。由 DialogueController 启动，通过 OptionUI 处理分支选择
/// </summary>
public class DialogueUI : MonoBehaviour
{
    private static DialogueUI _instance;
    public static DialogueUI Instance => _instance;

    [Header("基础元素")]
    [SerializeField, FormerlySerializedAs("dialoguePanel")] private GameObject _dialoguePanel;
    [SerializeField, FormerlySerializedAs("image")] private Image _speakerImage;
    [SerializeField, FormerlySerializedAs("mainText")] private TextMeshProUGUI _mainText;
    [SerializeField, FormerlySerializedAs("nextButton")] private Button _nextButton;

    [Header("选项")]
    [SerializeField, FormerlySerializedAs("optionPanel")] private RectTransform _optionPanel;
    [SerializeField, FormerlySerializedAs("optionPrefab")] private OptionUI _optionPrefab;

    [Header("打字机效果")]
    [SerializeField] private float _typewriterDuration = 1.5f;

    private DialogueData_SO _currentData;
    private int _currentIndex;
    private DialogueController _currentDialogueController;
    private Tweener _typewriterTweener;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (_nextButton != null)
            _nextButton.onClick.AddListener(ContinueDialogue);
    }

    private void OnDestroy()
    {
        // 防御性清理 DOTween，避免已销毁对象仍被 Tween 引用
        _typewriterTweener?.Kill();
    }

    #region 公共入口（由 DialogueController 调用）

    /// <summary>
    /// 保存当前对话控制器引用，用于结束时回调 EndDialogue
    /// </summary>
    public void SetDialogueController(DialogueController controller)
    {
        _currentDialogueController = controller;
    }

    /// <summary>
    /// 加载新对话数据并显示第一句
    /// </summary>
    public void UpdateDialogueDatas(DialogueData_SO data)
    {
        _currentData = data;
        _currentIndex = 0;
    }

    /// <summary>
    /// 渲染指定的对话片段到 UI
    /// </summary>
    public void UpdateMainDialogue(DialoguePiece piece)
    {
        if (_dialoguePanel != null)
            _dialoguePanel.SetActive(true);

        // 说话人头像
        if (_speakerImage != null)
        {
            _speakerImage.enabled = piece.image != null;
            if (piece.image != null)
                _speakerImage.sprite = piece.image;
        }

        // 打字机效果 — 先杀掉旧 Tween 防止重叠
        if (_mainText != null)
        {
            _typewriterTweener?.Kill();
            _typewriterTweener = DOTween.To(
                () => string.Empty,
                value => _mainText.text = value,
                piece.text,
                _typewriterDuration
            ).SetEase(Ease.Linear);
        }

        // 有选项 → 显示选项按钮；无选项 → 显示"继续"按钮
        if (piece.options.Count > 0)
        {
            if (_nextButton != null)
                _nextButton.gameObject.SetActive(false);
            CreateOption(piece);
        }
        else
        {
            CreateOption(null); // 清空旧选项
            if (_nextButton != null)
                _nextButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 处理玩家选择的选项 — 通过 targetID 跳转到目标片段
    /// </summary>
    public void HandleOptionSelection(DialogueOption option)
    {
        if (option == null || _currentData == null) return;

        DialoguePiece target = FindPieceByID(option.targetID);
        if (target != null)
        {
            UpdateMainDialogue(target);
        }
        else
        {
            Debug.LogWarning($"[DialogueUI] 未找到 ID=\"{option.targetID}\" 的片段，对话结束");
            EndConversation();
        }
    }

    #endregion

    #region 内部逻辑

    /// <summary>
    /// 点击"继续"按钮 → 推进到下一条 Piece（按列表顺序）
    /// </summary>
    private void ContinueDialogue()
    {
        if (_currentData == null) return;

        _currentIndex++;
        if (_currentIndex < _currentData.dialoguePiece.Count)
        {
            UpdateMainDialogue(_currentData.dialoguePiece[_currentIndex]);
        }
        else
        {
            EndConversation();
        }
    }

    /// <summary>
    /// 结束对话 — 关闭面板、通知控制器恢复玩家控制
    /// </summary>
    private void EndConversation()
    {
        _typewriterTweener?.Kill();

        if (_dialoguePanel != null)
            _dialoguePanel.SetActive(false);

        CreateOption(null); // 清理选项按钮

        if (_currentDialogueController != null)
        {
            _currentDialogueController.EndDialogue();
            _currentDialogueController = null;
        }
    }

    /// <summary>
    /// 根据 ID 在对话数据中查找目标片段
    /// </summary>
    private DialoguePiece FindPieceByID(string id)
    {
        if (_currentData == null || string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < _currentData.dialoguePiece.Count; i++)
        {
            if (_currentData.dialoguePiece[i].ID == id)
                return _currentData.dialoguePiece[i];
        }

        return null;
    }

    #endregion

    #region 选项按钮生成

    /// <summary>
    /// 根据 Piece 的 options 列表实例化/清空选项按钮
    /// 传入 null 或空列表 → 清空选项面板
    /// </summary>
    private void CreateOption(DialoguePiece piece)
    {
        // 清空已有选项
        if (_optionPanel != null)
        {
            for (int i = _optionPanel.childCount - 1; i >= 0; i--)
            {
                Destroy(_optionPanel.GetChild(i).gameObject);
            }
        }

        if (piece == null || piece.options.Count == 0) return;
        if (_optionPrefab == null)
        {
            Debug.LogError("[DialogueUI] optionPrefab 未赋值！");
            return;
        }

        if (_optionPanel != null)
            _optionPanel.gameObject.SetActive(true);

        for (int i = 0; i < piece.options.Count; i++)
        {
            OptionUI optionUI = Instantiate(_optionPrefab, _optionPanel);
            optionUI.SetupOption(piece.options[i]);
        }
    }

    #endregion
}
