using Cinemachine;
using UnityEngine;

/// <summary>
/// 大世界角色切换管理器
/// 按 1/2/3/4 键在队伍成员间切换，始终维持场景中只有一个 Tag="Player" 的物体
/// 切换时在旧角色位置生成新角色预制体
/// </summary>
[DefaultExecutionOrder(-100)]
public class CharacterSwapManager : MonoBehaviour
{

    public CinemachineFreeLook mainCamera;
    [Header("队伍数据")]
    [SerializeField] private TeamData_SO _teamData;

    /// <summary>当前角色序号（-1 表示未初始化）</summary>
    public int CurrentIndex { get; private set; } = -1;

    /// <summary>当前玩家 GameObject</summary>
    public GameObject CurrentPlayer { get; private set; }

    /// <summary>当前角色 HeroData</summary>
    public HeroData CurrentHero =>
        CurrentIndex >= 0 && CurrentIndex < _teamData.teamMembers.Count
            ? _teamData.teamMembers[CurrentIndex]
            : null;

    private void Start()
    {
        if (_teamData == null || _teamData.teamMembers.Count == 0)
        {
            Debug.LogError("[CharacterSwapManager] TeamData 未配置或队伍为空！");
            return;
        }

        // 场景中需预先放置一个 Tag="Player" 的物体作为初始角色
        var existingPlayer = GameObject.FindWithTag("Player");
        if (existingPlayer != null)
        {
            CurrentPlayer = existingPlayer;
            Debug.Log($"[CharacterSwapManager] 已绑定场景中的初始角色：{existingPlayer.name}");
        }
        else
        {
            Debug.LogWarning("[CharacterSwapManager] 场景中未找到 Tag=\"Player\" 的物体，请手动放置初始角色");
        }
    }

    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SwapToCharacter(i);
        }
    }

    /// <summary>
    /// 切换到指定序号的队友
    /// </summary>
    public void SwapToCharacter(int index)
    {
        if (_teamData == null) return;
        if (index < 0 || index >= _teamData.teamMembers.Count) return;
        if (index == CurrentIndex) return;

        HeroData hero = _teamData.teamMembers[index];
        if (hero == null)
        {
            Debug.LogWarning($"[CharacterSwapManager] 队伍第 {index + 1} 位未配置 HeroData");
            return;
        }
        if (hero.modelPrefab == null)
        {
            Debug.LogWarning($"[CharacterSwapManager] {hero.heroName} 未配置 modelPrefab");
            return;
        }

        Vector3 lastPosition = transform.position;
        Quaternion lastRotation = transform.rotation;

        if (CurrentPlayer != null)
        {
            lastPosition = CurrentPlayer.transform.position;
            lastRotation = CurrentPlayer.transform.rotation;
            Destroy(CurrentPlayer);
        }

        SpawnCharacter(index, lastPosition, lastRotation);
    }

    private void SpawnCharacter(int index, Vector3 position, Quaternion rotation)
    {
        HeroData hero = _teamData.teamMembers[index];
        GameObject prefab = hero.modelPrefab;


        // 确保预制体有 CharacterController — PlayerMovementController 依赖它
        if (prefab.GetComponent<CharacterController>() == null)
            Debug.LogWarning($"[CharacterSwapManager] 预制体 {prefab.name} 缺少 CharacterController 组件");

        CurrentPlayer = Instantiate(prefab, position, rotation);
        mainCamera.Follow = CurrentPlayer.transform.GetChild(0);
        mainCamera.LookAt = CurrentPlayer.transform.GetChild(0);
        CurrentPlayer.tag = "Player";
        CurrentIndex = index;

        Debug.Log($"[CharacterSwapManager] 切换到 {hero.heroName}（{index + 1}号位）");

        GameEvents.TriggerCharacterSwitched(hero, index);
    }
}
