using TMPro;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// 单个战斗队伍槽位 — 角色 icon 下方的 HPBar（Fill）与血量数字文本成对引用。
    ///
    /// 由 BattlePartyStatusUI 在 Inspector 中拖入，顺序需与 BattleManager.PlayerTeam 一致。
    /// hpBarFill 为 HPBar 下 Filled 型 Fill 子物体的 Image，通过 fillAmount 控制血条长度。
    /// </summary>
    [System.Serializable]
    public class BattlePartySlot
    {
        public Image hpBarFill;   // HPBar 的 Fill 子物体（Filled 型 Image）
        public TMP_Text hpText;   // 血量数字文本
    }
}
