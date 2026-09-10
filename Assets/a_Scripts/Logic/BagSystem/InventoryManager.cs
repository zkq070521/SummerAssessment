using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 背包数据管理器（持久单例）— 保存背包物品、装备、以及"已领取奖励"记录，跨场景保留。
///
/// 与 BagPanelController（表现层）解耦：本组件只存数据，UI 通过 Instance 读写。
/// 通过 GameEvents.OnRewardClaimed 事件接收对话奖励，并按 rewardID 去重（每个奖励只发一次）。
///
/// 使用方式：在大世界场景创建一个空物体挂本组件即可（Awake 会自动 DontDestroyOnLoad 持久化）。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public const int BAG_SLOT_COUNT = 16;
    public const int EQUIP_SLOT_COUNT = 5;

    public static InventoryManager Instance { get; private set; }

    [Header("初始物品（测试用，仅首次创建时填入一次）")]
    [SerializeField] private ItemData[] _initialItems;

    private readonly ItemData[] _inventory = new ItemData[BAG_SLOT_COUNT];
    private readonly ItemData[] _equipment = new ItemData[EQUIP_SLOT_COUNT];

    /// <summary>已发放的奖励 ID 集合，保证每个奖励只发一次（跨场景保留）</summary>
    private readonly HashSet<string> _grantedRewards = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad 只对根物体生效；若被放在某个父物体下，先脱离父级再持久化
        if (transform.parent != null)
        {
            Debug.LogWarning("[InventoryManager] 检测到自身不是根物体，已自动脱离父级以便跨场景持久化");
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        // 初始物品只填一次（持久化，重进场景不会重复添加）
        for (int i = 0; i < _initialItems.Length && i < _inventory.Length; i++)
            _inventory[i] = _initialItems[i];

        Debug.Log("[InventoryManager] 已初始化，背包数据跨场景持久化");
    }

    private void OnEnable() => GameEvents.OnRewardClaimed += HandleRewardClaimed;
    private void OnDisable() => GameEvents.OnRewardClaimed -= HandleRewardClaimed;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── 数据访问 ──

    public ItemData GetBagItem(int index)
        => (index >= 0 && index < _inventory.Length) ? _inventory[index] : null;

    public ItemData GetEquipItem(int index)
        => (index >= 0 && index < _equipment.Length) ? _equipment[index] : null;

    /// <summary>把物品加入第一个空格子；背包已满返回 false</summary>
    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < _inventory.Length; i++)
        {
            if (_inventory[i] == null)
            {
                _inventory[i] = item;
                Debug.Log($"[InventoryManager] 物品 \"{item.itemName}\" 放入格子 {i}");
                return true;
            }
        }

        Debug.LogWarning($"[InventoryManager] 背包已满，无法放入 \"{item.itemName}\"");
        return false;
    }

    /// <summary>交换两个背包格子的物品</summary>
    public void MoveItem(int from, int to)
    {
        if (from < 0 || from >= _inventory.Length || to < 0 || to >= _inventory.Length) return;
        (_inventory[from], _inventory[to]) = (_inventory[to], _inventory[from]);
    }

    /// <summary>把背包物品装备到指定槽位（与槽位原有装备交换）。部位匹配由表现层校验。</summary>
    public void EquipItem(int bagIndex, int equipIndex)
    {
        if (bagIndex < 0 || bagIndex >= _inventory.Length || equipIndex < 0 || equipIndex >= _equipment.Length) return;

        ItemData previous = _equipment[equipIndex];
        _equipment[equipIndex] = _inventory[bagIndex];
        _inventory[bagIndex] = previous;
    }

    // ── 奖励领取（事件驱动 + 去重）──

    private void HandleRewardClaimed(string rewardID, IReadOnlyList<ItemData> items)
    {
        if (string.IsNullOrEmpty(rewardID)) return;

        if (_grantedRewards.Contains(rewardID))
        {
            Debug.LogWarning($"[InventoryManager] 奖励 \"{rewardID}\" 已领取过，跳过");
            return;
        }

        _grantedRewards.Add(rewardID);
        if (items == null) return;

        foreach (ItemData item in items)
            AddItem(item);

        Debug.Log($"[InventoryManager] 领取奖励 \"{rewardID}\"，共 {items.Count} 件，当前背包已放 {_inventory.Count(x => x != null)} 格");
    }
}
