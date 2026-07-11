using System.Collections;
using System.Collections.Generic;
using BattleSystem;
using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    void Start()
    {
        BattleEventCenter.OnTurnChanged += ChangeUI;
    }

    public void ChangeUI(BattleTeam team)
    {
        if (team == BattleTeam.Player)
        {
            Debug.Log("玩家回合,接下来判断是哪个角色攻击");
        }
        else
        {
            Debug.Log("敌人回合,隐藏按钮UI");
        }
    }
}
