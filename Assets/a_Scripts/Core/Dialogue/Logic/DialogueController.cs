using Cinemachine;
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

    [Header("对话相机")]
    [SerializeField] private CinemachineVirtualCamera _dialogueCamera;   // 对话时切换到的虚拟相机（挂在对谈角色子物体下）
    [SerializeField] private int _dialogueCameraPriority = 100;          // 对话相机激活时的优先级，需高于大世界默认相机（FreeLook 默认 10）

    [Header("对话奖励")]
    [SerializeField] private ItemData[] _rewardItems;   // 对话完成后发放的物品
    [SerializeField] private string _rewardID;          // 奖励唯一 ID（用于跨场景去重；留空则用 NPC 名称）

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

        SetDialogueCamera(false);   // 初始关闭对话相机，默认使用大世界相机
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

        GameEvents.TriggerDialogueStarted();   // 通知 HUD 隐藏（小地图/队伍/按钮）

        SetDialogueCamera(true);   // 切到对话相机

        Debug.Log($"[DialogueController] 对话已启动：{_currentData.name}");
    }

    /// <summary>
    /// 对话结束时由 DialogueUI 回调，恢复玩家控制和隐藏鼠标
    /// </summary>
    public void EndDialogue()
    {
        SetDialogueCamera(false);   // 切回大世界相机

        GrantDialogueReward();   // 发放对话奖励（事件驱动，InventoryManager 去重）

        SetPlayerControl(true);
        ShowCursor(false);

        GameEvents.TriggerDialogueEnded();   // 通知 HUD 恢复显示
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

    /// <summary>
    /// 切换对话相机：active=true 时抬高对话相机优先级（CinemachineBrain 自动 Blend 过去），
    /// false 时降回 0，自动切回大世界默认相机（FreeLook）。
    /// </summary>
    private void SetDialogueCamera(bool active)
    {
        if (_dialogueCamera == null) return;
        _dialogueCamera.Priority = active ? _dialogueCameraPriority : 0;
    }

    /// <summary>
    /// 发放对话奖励 — 通过 GameEvents.OnRewardClaimed 事件交给 InventoryManager，
    /// 由其按 rewardID 去重（保证跨场景只发一次）。
    /// </summary>
    private void GrantDialogueReward()
    {
        if (_rewardItems == null || _rewardItems.Length == 0)
        {
            Debug.LogWarning($"[DialogueController] {gameObject.name} 未配置 _rewardItems，无奖励发放");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError($"[DialogueController] 场景中不存在 InventoryManager，奖励无法入包！请在 SampleScene 创建一个挂 InventoryManager 组件的空物体");
            return;
        }

        string rewardID = string.IsNullOrEmpty(_rewardID) ? gameObject.name : _rewardID;
        Debug.Log($"[DialogueController] 触发奖励发放 rewardID=\"{rewardID}\"，共 {_rewardItems.Length} 件");
        GameEvents.TriggerRewardClaimed(rewardID, _rewardItems);
    }
}
