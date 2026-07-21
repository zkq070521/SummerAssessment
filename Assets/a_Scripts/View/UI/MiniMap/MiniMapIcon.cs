using UnityEngine;

/// <summary>
/// 小地图标记组件 — 挂载到场景中需要在地图上显示的物体上
/// （NPC、传送点、任务目标等），自动向 MiniMapManager 注册/注销。
/// 玩家标记由 MiniMapManager 内部维护，无需使用此组件。
/// </summary>
public class MiniMapIcon : MonoBehaviour
{
    [SerializeField] private MiniMapIconType _iconType = MiniMapIconType.NPC;
    [SerializeField] private bool _showOffScreen = true;

    private MiniMapManager _manager;
    private bool _managerResolved;

    private void Awake()
    {
        _manager = FindObjectOfType<MiniMapManager>();
        _managerResolved = _manager != null;
        if (!_managerResolved)
        {
            Debug.LogWarning($"[MiniMapIcon] 场景中未找到 MiniMapManager，{gameObject.name} 的标记不可用", this);
        }
    }

    private void OnEnable()
    {
        if (_managerResolved)
            _manager.RegisterIcon(transform, _iconType);
    }

    private void OnDisable()
    {
        if (_managerResolved)
            _manager.UnregisterIcon(transform);
    }

    /// <summary>
    /// 动态切换标记类型（运行时修改后自动重新注册）
    /// </summary>
    public void SetIconType(MiniMapIconType newType)
    {
        if (_iconType == newType) return;

        if (_managerResolved)
            _manager.UnregisterIcon(transform);

        _iconType = newType;

        if (_managerResolved)
            _manager.RegisterIcon(transform, _iconType);
    }
}
