using System;

/// <summary>
/// 输入服务 — 全局输入封锁/恢复通道
/// SceneTransitionManager 等系统通过此服务封锁玩家输入，无需 FindObjectOfType
/// PlayerMovementController 在 Awake/OnEnable 时注册回调
/// </summary>
public static class InputService
{
    /// <summary>输入启用状态变化事件（参数：是否启用）</summary>
    public static event Action<bool> OnInputEnabledChanged;

    private static bool _inputEnabled = true;

    public static bool InputEnabled => _inputEnabled;

    /// <summary>
    /// 设置输入启用状态
    /// </summary>
    public static void SetInputEnabled(bool enabled)
    {
        if (_inputEnabled == enabled) return;
        _inputEnabled = enabled;
        OnInputEnabledChanged?.Invoke(enabled);
    }
}
