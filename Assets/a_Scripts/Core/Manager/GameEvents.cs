using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 创建一个可序列化的事件类，方便在 Inspector 里拖拽绑定
[System.Serializable]
public class OnHitEnemyEvent : UnityEvent<GameObject, Vector3> { }

public static class GameEvents
{
    /// <summary>命中敌人时触发 — 参数：敌人对象、命中位置</summary>
    public static event System.Action<GameObject, Vector3> OnHitEnemy;
    public static void TriggerHitEnemy(GameObject enemy, Vector3 hitPoint)
        => OnHitEnemy?.Invoke(enemy, hitPoint);

    /// <summary>
    /// 大世界角色切换时触发 — 参数：新角色的 HeroData、角色序号（0-3 对应按键 1-4）
    /// UI / 特效 / 音效等系统可通过此事件响应角色切换
    /// </summary>
    public static event System.Action<HeroData, int> OnCharacterSwitched;
    public static void TriggerCharacterSwitched(HeroData hero, int index)
        => OnCharacterSwitched?.Invoke(hero, index);

    /// <summary>
    /// 小地图全屏切换时触发 — 参数：true 为打开全屏地图，false 为关闭
    /// 其他系统（输入封锁、暂停逻辑等）可通过此事件响应地图开关
    /// </summary>
    public static event System.Action<bool> OnMiniMapToggled;
    public static void TriggerMiniMapToggled(bool isOpen)
        => OnMiniMapToggled?.Invoke(isOpen);
}