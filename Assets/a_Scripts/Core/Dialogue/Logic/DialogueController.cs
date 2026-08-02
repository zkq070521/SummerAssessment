using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 对话触发器 — 挂载到 NPC 身上，检测玩家进入范围 → 按 E 键启动对话
/// 对话期间锁定玩家控制并显示鼠标，对话结束后恢复
/// </summary>
public class DialogueController : MonoBehaviour
{
    public GameObject dialoguePanel;
    [Header("对话数据")]
    [SerializeField, FormerlySerializedAs("currentData")] private DialogueData_SO _currentData;

    [Header("交互提示")]
    [SerializeField] private GameObject _interactHint;

    private bool _canTalk;
    private GameObject _player;
    private PlayerMovementController _playerController;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _playerController = _player.GetComponent<PlayerMovementController>();
        }
        else
        {
            Debug.LogError($"[DialogueController] {gameObject.name}：找不到 Tag=\"Player\" 的物体");
        }

        if (_interactHint != null)
            _interactHint.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        else
            Debug.LogError($"[DialogueController] {gameObject.name}：未设置 dialoguePanel");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && _currentData != null)
        {
            _canTalk = true;
            if (_interactHint != null)
                _interactHint.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _canTalk = false;
            if (_interactHint != null)
                _interactHint.SetActive(false);
        }
    }

    private void Update()
    {
        if (_canTalk && Input.GetKeyDown(KeyCode.E))
        {
            OpenDialogue();
        }
    }

    private void OpenDialogue()
    {
        if (DialogueUI.Instance == null)
        {
            Debug.LogError($"[DialogueController] DialogueUI.Instance 为 null！场景中是否有挂载 DialogueUI 的 GameObject？");
            return;
        }
        if (_currentData == null)
        {
            Debug.LogError($"[DialogueController] {gameObject.name} 的 currentData 未赋值！");
            return;
        }
        if (_currentData.dialoguePiece.Count == 0)
        {
            Debug.LogError($"[DialogueController] {gameObject.name} 的对话数据中没有 DialoguePiece！");
            return;
        }

        DialogueUI.Instance.SetDialogueController(this);
        DialogueUI.Instance.UpdateDialogueDatas(_currentData);
        DialogueUI.Instance.UpdateMainDialogue(_currentData.dialoguePiece[0]);

        SetPlayerControl(false);
        ShowCursor(true);

        if (_interactHint != null)
            _interactHint.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        Debug.Log($"[DialogueController] 对话已启动：{_currentData.name}");
    }

    /// <summary>
    /// 对话结束时由 DialogueUI 回调，恢复玩家控制和隐藏鼠标
    /// </summary>
    public void EndDialogue()
    {
        SetPlayerControl(true);
        ShowCursor(false);
    }

    /// <summary>
    /// 启用/禁用玩家移动控制
    /// </summary>
    private void SetPlayerControl(bool enabled)
    {
        if (_playerController != null)
            _playerController.enabled = enabled;
    }

    /// <summary>
    /// 显示/隐藏鼠标并切换锁定模式
    /// </summary>
    private void ShowCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
