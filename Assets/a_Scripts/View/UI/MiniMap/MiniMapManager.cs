using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图系统主控制器 — 管理俯视相机、RenderTexture、标记图标注册/移除、
/// 世界坐标映射、全屏地图切换。
/// 挂载到场景中的 MiniMapManager GameObject 上。
/// 依赖：俯视 Camera（子物体或外部引用）、Canvas 中的 MiniMapPanel
/// </summary>
[DefaultExecutionOrder(-50)]
public class MiniMapManager : MonoBehaviour
{
    [Header("俯视相机")]
    [SerializeField] private Camera _overheadCamera;
    [SerializeField] private float _cameraHeight = 80f;
    [SerializeField] private float _orthographicSize = 40f;

    [Header("RenderTexture")]
    [SerializeField] private RenderTexture _miniMapRT;
    [SerializeField] private Vector2Int _rtResolution = new Vector2Int(512, 512);

    [Header("UI 引用")]
    [SerializeField] private RectTransform _miniMapPanel;
    [SerializeField] private RawImage _miniMapRawImage;
    [SerializeField] private RectTransform _playerArrow;
    [SerializeField] private RectTransform _iconsContainer;
    [SerializeField] private float _miniMapUISize = 220f;

    [Header("图标预制体")]
    [SerializeField] private GameObject _npcIconPrefab;
    [SerializeField] private GameObject _questIconPrefab;
    [SerializeField] private GameObject _teleportIconPrefab;
    [SerializeField] private GameObject _enemyIconPrefab;

    [Header("全屏地图")]
    [SerializeField] private GameObject _fullMapPanel;
    [SerializeField] private RawImage _fullMapRawImage;
    [SerializeField] private float _fullMapToggleCooldown = 0.3f;

    [Header("缩放")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 15f;
    [SerializeField] private float _maxZoom = 80f;

    /// <summary>小地图覆盖的世界范围（以玩家为中心的正方形边长），单位：米</summary>
    private const float MAP_WORLD_SIZE = 80f;

    private Transform _playerTransform;
    private readonly List<MiniMapIconEntry> _registeredIcons = new List<MiniMapIconEntry>();
    private readonly Dictionary<MiniMapIconType, GameObject> _iconPrefabMap = new Dictionary<MiniMapIconType, GameObject>();
    private readonly Dictionary<MiniMapIconType, Stack<GameObject>> _iconPool = new Dictionary<MiniMapIconType, Stack<GameObject>>();

    private float _lastToggleTime = float.MinValue;
    private bool _isFullMapOpen;

    private void Awake()
    {
        InitializeRT();
        InitializeCamera();
        BuildIconPrefabMap();
        BuildIconPools();
    }

    private void Start()
    {
        // 延迟一帧查找 Player，确保 CharacterSwapManager 已完成初始化
        FindPlayer();
        if (_fullMapPanel != null)
            _fullMapPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnCharacterSwitched += OnCharacterSwitched;
    }

    private void OnDisable()
    {
        GameEvents.OnCharacterSwitched -= OnCharacterSwitched;
    }

    private void OnDestroy()
    {
        if (_miniMapRT != null)
        {
            _miniMapRT.Release();
            //Destroy(_miniMapRT);
        }
    }

    private void LateUpdate()
    {
        UpdateOverheadCamera();
        UpdatePlayerArrow();
        UpdateAllIcons();

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
            ToggleFullMap();
    }

    #region 初始化

    private void InitializeRT()
    {
        if (_miniMapRT == null)
        {
            _miniMapRT = new RenderTexture(_rtResolution.x, _rtResolution.y, 16, RenderTextureFormat.ARGB32);
            _miniMapRT.name = "MiniMapRT";
            _miniMapRT.Create();
        }

        if (_miniMapRawImage != null && _miniMapRawImage.texture == null)
            _miniMapRawImage.texture = _miniMapRT;

        if (_fullMapRawImage != null && _fullMapRawImage.texture == null)
            _fullMapRawImage.texture = _miniMapRT;
    }

    private void InitializeCamera()
    {
        if (_overheadCamera == null)
        {
            Debug.LogError("[MiniMapManager] 俯视相机未赋值！请在 Inspector 中拖入。");
            return;
        }

        _overheadCamera.orthographic = true;
        _overheadCamera.orthographicSize = _orthographicSize;
        _overheadCamera.targetTexture = _miniMapRT;
        _overheadCamera.clearFlags = CameraClearFlags.SolidColor;
        _overheadCamera.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
        _overheadCamera.cullingMask = LayerMask.GetMask("Terrain", "Default", "Ground");
        _overheadCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    /// <summary>
    /// 建立图标类型到预制体的映射，用于运行时查找
    /// </summary>
    private void BuildIconPrefabMap()
    {
        _iconPrefabMap.Clear();
        AddIfNotNull(MiniMapIconType.NPC, _npcIconPrefab);
        AddIfNotNull(MiniMapIconType.QuestTarget, _questIconPrefab);
        AddIfNotNull(MiniMapIconType.TeleportPoint, _teleportIconPrefab);
        AddIfNotNull(MiniMapIconType.Enemy, _enemyIconPrefab);
    }

    private void AddIfNotNull(MiniMapIconType type, GameObject prefab)
    {
        if (prefab != null) _iconPrefabMap[type] = prefab;
    }

    /// <summary>
    /// 预构建图标对象池，避免运行时 Instantiate/Destroy 开销
    /// </summary>
    private void BuildIconPools()
    {
        foreach (var kvp in _iconPrefabMap)
        {
            _iconPool[kvp.Key] = new Stack<GameObject>();
        }
    }

    private void FindPlayer()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _playerTransform = playerGO.transform;
        else
            Debug.LogWarning("[MiniMapManager] 场景中未找到 Tag=\"Player\" 的物体，玩家箭头不可用");
    }

    #endregion

    #region 每帧更新

    /// <summary>
    /// 俯视相机跟随玩家 XZ 位置，不跟随旋转（保持上北下南）
    /// </summary>
    private void UpdateOverheadCamera()
    {
        if (_overheadCamera == null || _playerTransform == null) return;

        Vector3 pos = _playerTransform.position;
        _overheadCamera.transform.position = new Vector3(pos.x, pos.y + _cameraHeight, pos.z);
        _overheadCamera.orthographicSize = _orthographicSize;
    }

    /// <summary>
    /// 更新玩家三角箭头位置与旋转
    /// </summary>
    private void UpdatePlayerArrow()
    {
        if (_playerArrow == null || _playerTransform == null) return;

        Vector2 uiPos = WorldToMiniMapUI(_playerTransform.position);
        _playerArrow.anchoredPosition = uiPos;

        float yaw = _playerTransform.eulerAngles.y;
        _playerArrow.localRotation = Quaternion.Euler(0f, 0f, -yaw);
    }

    /// <summary>
    /// 同步所有已注册标记的 UI 位置
    /// </summary>
    private void UpdateAllIcons()
    {
        for (int i = _registeredIcons.Count - 1; i >= 0; i--)
        {
            var entry = _registeredIcons[i];
            if (entry.target == null)
            {
                ReturnIconToPool(entry);
                _registeredIcons.RemoveAt(i);
                continue;
            }

            if (entry.uiElement != null)
            {
                Vector2 uiPos = WorldToMiniMapUI(entry.target.position);
                entry.uiElement.anchoredPosition = uiPos;

                // 超出小地图范围的标记隐藏
                bool visible = Mathf.Abs(uiPos.x) <= _miniMapUISize * 0.55f
                            && Mathf.Abs(uiPos.y) <= _miniMapUISize * 0.55f;
                entry.uiElement.gameObject.SetActive(visible);
            }
        }
    }

    #endregion

    #region 坐标映射

    /// <summary>
    /// 将世界坐标映射到小地图 UI 局部坐标（以玩家为中心）
    /// </summary>
    public Vector2 WorldToMiniMapUI(Vector3 worldPos)
    {
        if (_playerTransform == null) return Vector2.zero;

        Vector3 playerPos = _playerTransform.position;
        float normX = (worldPos.x - playerPos.x) / MAP_WORLD_SIZE;
        float normY = (worldPos.z - playerPos.z) / MAP_WORLD_SIZE;

        return new Vector2(normX * _miniMapUISize, normY * _miniMapUISize);
    }

    #endregion

    #region 图标注册 / 移除

    /// <summary>
    /// 注册一个世界空间物体为小地图标记，自动创建 UI 图标元素
    /// </summary>
    public void RegisterIcon(Transform target, MiniMapIconType iconType)
    {
        if (target == null) return;
        if (iconType == MiniMapIconType.Player)
        {
            Debug.LogWarning("[MiniMapManager] 玩家标记由管理器内部维护，请勿手动注册 Player 类型");
            return;
        }

        // 防止重复注册
        for (int i = 0; i < _registeredIcons.Count; i++)
        {
            if (_registeredIcons[i].target == target)
                return;
        }

        var uiElement = GetIconFromPool(iconType);
        if (uiElement == null) return;

        var entry = new MiniMapIconEntry
        {
            target = target,
            iconType = iconType,
            uiElement = uiElement
        };
        _registeredIcons.Add(entry);
    }

    /// <summary>
    /// 从注册表移除指定物体的标记并回收 UI 元素
    /// </summary>
    public void UnregisterIcon(Transform target)
    {
        for (int i = _registeredIcons.Count - 1; i >= 0; i--)
        {
            if (_registeredIcons[i].target == target)
            {
                ReturnIconToPool(_registeredIcons[i]);
                _registeredIcons.RemoveAt(i);
                return;
            }
        }
    }

    private RectTransform GetIconFromPool(MiniMapIconType iconType)
    {
        if (!_iconPrefabMap.TryGetValue(iconType, out var prefab)) return null;
        if (_iconsContainer == null) return null;

        var pool = _iconPool[iconType];
        GameObject icon;
        if (pool.Count > 0)
        {
            icon = pool.Pop();
            icon.SetActive(true);
        }
        else
        {
            icon = Instantiate(prefab, _iconsContainer);
        }

        return icon.GetComponent<RectTransform>();
    }

    private void ReturnIconToPool(MiniMapIconEntry entry)
    {
        if (entry.uiElement == null) return;

        entry.uiElement.gameObject.SetActive(false);
        if (_iconPool.TryGetValue(entry.iconType, out var pool))
            pool.Push(entry.uiElement.gameObject);
    }

    #endregion

    #region 全屏地图

    /// <summary>
    /// 切换全屏大地图开关（M 键或 Tab 键触发）
    /// </summary>
    public void ToggleFullMap()
    {
        if (Time.unscaledTime - _lastToggleTime < _fullMapToggleCooldown) return;
        _lastToggleTime = Time.unscaledTime;

        _isFullMapOpen = !_isFullMapOpen;
        if (_fullMapPanel != null)
            _fullMapPanel.SetActive(_isFullMapOpen);

        GameEvents.TriggerMiniMapToggled(_isFullMapOpen);

        // 全屏地图打开时暂停游戏逻辑
        Time.timeScale = _isFullMapOpen ? 0f : 1f;
    }

    /// <summary>
    /// 关闭全屏地图（由外部关闭按钮调用）
    /// </summary>
    public void CloseFullMap()
    {
        if (!_isFullMapOpen) return;

        _isFullMapOpen = false;
        if (_fullMapPanel != null)
            _fullMapPanel.SetActive(false);

        GameEvents.TriggerMiniMapToggled(false);
        Time.timeScale = 1f;
    }

    #endregion

    #region 缩放

    /// <summary>
    /// 缩放小地图可视范围（滚轮控制）
    /// </summary>
    public void Zoom(float delta)
    {
        _orthographicSize = Mathf.Clamp(_orthographicSize - delta * _zoomSpeed, _minZoom, _maxZoom);
    }

    #endregion

    #region 事件响应

    private void OnCharacterSwitched(HeroData hero, int index)
    {
        // 角色切换后重新查找 Player Transform
        FindPlayer();
    }

    #endregion
}
