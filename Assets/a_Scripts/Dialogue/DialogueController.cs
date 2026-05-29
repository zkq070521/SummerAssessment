using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    public DialogueData_SO currentData;
    public bool canTalk = false;

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
        DialogueUI.Instance.UpdateDialogueDatas(currentData);
        DialogueUI.Instance.UpdateMainDialogue(currentData.dialoguePiece[0]); // 注意这里是0不是00
    }

}
