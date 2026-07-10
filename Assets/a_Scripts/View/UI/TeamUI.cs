using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TeamPanel UI 同步控制器 — 监听角色切换事件，同步 4 个 HeroIcon 槽位的
/// 名字、头像、HP条、能量条，并高亮当前角色
/// 挂载到 TeamPanel GameObject 上
/// </summary>
public class TeamUI : MonoBehaviour
{
    [Header("队伍数据")]
    [SerializeField] private TeamData_SO _teamData;

    private HeroIconSlot[] _slots = new HeroIconSlot[4];
    private int _highlightedIndex = -1;

    private void Start()
    {
        BuildSlots();
        RefreshAll();
        HighlightSlot(0);
    }

    private void OnEnable()
    {
        GameEvents.OnCharacterSwitched += OnCharacterSwitched;
    }

    private void OnDisable()
    {
        GameEvents.OnCharacterSwitched -= OnCharacterSwitched;
    }

    /// <summary>
    /// 按 Transform 子级顺序自动绑定 4 个 HeroIcon 槽位
    /// </summary>
    private void BuildSlots()
    {
        int childCount = Mathf.Min(transform.childCount, 4);
        for (int i = 0; i < childCount; i++)
        {
            Transform iconRoot = transform.GetChild(i);
            _slots[i] = new HeroIconSlot
            {
                root = iconRoot,
                background = iconRoot.GetComponent<Image>(),
                nameText = iconRoot.Find("NameText")?.GetComponent<TMP_Text>(),
                avatarImage = iconRoot.Find("Avatar")?.GetComponent<Image>(),
                hpBar = FindFillImage(iconRoot.Find("HPBar")),
                energyBar = FindFillImage(iconRoot.Find("EnergyBar")),
            };
        }
    }

    /// <summary>
    /// 查找 Fill 子物体的 Image，若无 Fill 子物体则返回父级 Image
    /// </summary>
    private static Image FindFillImage(Transform parent)
    {
        if (parent == null) return null;
        Transform fill = parent.Find("Fill");
        return fill != null ? fill.GetComponent<Image>() : parent.GetComponent<Image>();
    }

    /// <summary>
    /// 从 TeamData 刷新全部 4 个槽位
    /// </summary>
    private void RefreshAll()
    {
        if (_teamData == null) return;

        for (int i = 0; i < _slots.Length && i < _teamData.teamMembers.Count; i++)
        {
            RefreshSlot(i);
        }

        // 隐藏超出队伍人数的多余槽位
        for (int i = _teamData.teamMembers.Count; i < _slots.Length; i++)
        {
            _slots[i].root?.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 刷新单个槽位（名字、头像、HP、能量）
    /// </summary>
    private void RefreshSlot(int i)
    {
        if (_teamData == null || i >= _teamData.teamMembers.Count) return;

        HeroData hero = _teamData.teamMembers[i];
        HeroIconSlot slot = _slots[i];
        if (slot.root == null) return;

        if (hero == null)
        {
            slot.root.gameObject.SetActive(false);
            return;
        }

        slot.root.gameObject.SetActive(true);

        if (slot.nameText != null)
            slot.nameText.text = hero.heroName;


        if (slot.hpBar != null)
            slot.hpBar.fillAmount = hero.maxHP > 0f ? hero.currentHP / hero.maxHP : 0f;

        if (slot.energyBar != null)
            slot.energyBar.fillAmount = hero.maxEnergy > 0f ? hero.currentEnergy / hero.maxEnergy : 0f;
    }

    private void OnCharacterSwitched(HeroData hero, int index)
    {
        RefreshSlot(index);
        HighlightSlot(index);
    }

    /// <summary>
    /// 高亮当前角色槽位（修改底板 alpha），取消其他高亮
    /// </summary>
    private void HighlightSlot(int index)
    {
        if (index == _highlightedIndex) return;
        _highlightedIndex = index;

        for (int i = 0; i < _slots.Length; i++)
        {
            Image bg = _slots[i].background;
            if (bg == null) continue;

            Color color = bg.color;
            color.a = (i == index) ? 0.3f : 0f;
            bg.color = color;
        }
    }

    /// <summary>
    /// 单个 HeroIcon 槽位的 UI 组件引用
    /// </summary>
    [System.Serializable]
    private struct HeroIconSlot
    {
        public Transform root;
        public Image background;
        public TMP_Text nameText;
        public Image avatarImage;
        public Image hpBar;
        public Image energyBar;
    }
}
