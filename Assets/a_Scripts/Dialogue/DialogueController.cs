using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    public DialogueData_SO currentData;
    public bool canTalk = false;

    private GameObject _player;
    private PlayerControllerSimple _playerController;

    void Start()
    {
        // 在Start中查找玩家对象和组件，避免每帧都Find
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
        {
            _playerController = _player.GetComponent<PlayerControllerSimple>();
        }
        else
        {
            Debug.LogError("找不到Player对象！请确保Player有Tag为'Player'");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentData != null)
        {
            canTalk = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canTalk = false;
        }
    }

    void Update()
    {
        if (canTalk && Input.GetKeyDown(KeyCode.E))
        {
            OpenDialogue();
        }
    }

    void OpenDialogue()
    {
        DialogueUI.Instance.SetDialogueController(this);
        // 开启对话时禁用玩家控制器
        SetPlayerControl(false);

        DialogueUI.Instance.UpdateDialogueDatas(currentData);
        DialogueUI.Instance.UpdateMainDialogue(currentData.dialoguePiece[0]);
    }

    /// <summary>
    /// 控制玩家是否可以移动/交互
    /// </summary>
    /// <param name="enabled">true=启用控制, false=禁用控制</param>
    private void SetPlayerControl(bool enabled)
    {
        if (_playerController != null)
        {
            _playerController.enabled = enabled;
            Debug.Log($"玩家控制器已{(enabled ? "启用" : "禁用")}");
        }

        // 可选：同时禁用CharacterController的移动
        // 但禁用PlayerControllerSimple就够了，因为移动逻辑在里面
    }

    /// <summary>
    /// 这个方法需要在对话完全结束时调用（比如关闭对话UI时）
    /// 需要在DialogueUI中调用这个方法
    /// </summary>
    public void EndDialogue()
    {
        SetPlayerControl(true);
    }
}