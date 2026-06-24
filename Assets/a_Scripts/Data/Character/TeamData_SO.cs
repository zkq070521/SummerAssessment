using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 队伍配置 — 存储大世界探索队伍（最多 4 人），按顺序对应数字键 1-4
/// 在 Assets/DataSO/ 下创建 .asset 实例，拖入 HeroData 资产即可
/// </summary>
[CreateAssetMenu(fileName = "New TeamData", menuName = "Game/TeamData")]
public class TeamData_SO : ScriptableObject
{
    [Header("队伍成员（按顺序对应按键 1-4）")]
    public List<HeroData> teamMembers = new();
}
