/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public class UserDataBean : BaseBean
{
    //拥有的金币数量（魔晶石）
    public long coin;

    //阵容最大数量
    public int lineupMax = 10;

    //阵容生物
    public List<string> listLineupCreature = new List<string>();
    //背包里的所有生物
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();


    /// <summary>
    /// 增加金币
    /// </summary>
    public void AddCoin(int coinNum)
    {
        coin += coinNum;
        if (coin < 0)
        {
            coin = 0;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Coin_Change);
    }

    /// <summary>
    /// 添加背包生物
    /// </summary>
    public void AddBackpackCreature(CreatureBean creatureData)
    {
        listBackpackCreature.Add(creatureData);
    }
}