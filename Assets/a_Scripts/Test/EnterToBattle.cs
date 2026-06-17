using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterToBattle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {



            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance 为空！");
                return;
            }

            // 检查场景名称
            if (string.IsNullOrEmpty(GameManager.Instance.battleSceneName))
            {
                Debug.LogError("battleSceneName 未在 GameManager 中设置！");
                return;
            }

            Debug.Log($"切换到场景：{GameManager.Instance.battleSceneName}");

            // 检查 SceneTransitionManager
            // if (SceneTransitionManager.Instance == null)
            // {
            //     Debug.LogError("SceneTransitionManager.Instance 为空！");
            //     return;
            // }

            // SceneTransitionManager.Instance.LoadScene(GameManager.Instance.battleSceneName);
        }
        else
        {
            Debug.Log($"碰到的是 {other.tag}，不是 Player，忽略");
        }
    }

}