using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 创建一个可序列化的事件类，方便在 Inspector 里拖拽绑定
[System.Serializable]
public class OnHitEnemyEvent : UnityEvent<GameObject, Vector3> { }

public static class GameEvents
{
    // 静态事件：命中敌人时触发，参数：敌人对象、命中位置
    public static event System.Action<GameObject, Vector3> OnHitEnemy;

    // 触发方法
    public static void TriggerHitEnemy(GameObject enemy, Vector3 hitPoint)
    {
        OnHitEnemy?.Invoke(enemy, hitPoint);
    }
}