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
    public Dictionary<int, List<string>> dicLineupCreature =new Dictionary<int, List<string>>();
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
    /// 获取阵容生物所在位置
    /// </summary>
    public int GetLineupCreaturePosIndex(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId))
            {
                return listCreatureId.IndexOf(creatureId); ;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// 添加阵容生物
    /// </summary>
    public bool AddLineupCreature(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId)) 
            {
                return false;
            }
            else
            {
                listCreatureId.Add(creatureId);
            }
        }
        else
        {
            dicLineupCreature.Add(lineupIndex,new List<string>() { creatureId });
        }
        return true;
    }

    /// <summary>
    /// 移除阵容生物
    /// </summary>
    public bool RemoveLineupCreature(int lineupIndex, string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId.Contains(creatureId))
            {
                listCreatureId.Remove(creatureId);
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// 添加背包生物
    /// </summary>
    public void AddBackpackCreature(CreatureBean creatureData)
    {
        listBackpackCreature.Add(creatureData);
    }

    /// <summary>
    /// 检测是否包含在阵容里
    /// </summary>
    public bool CheckIsLineup(int lineupIndex,string creatureId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex,out List<string> listCreatureId))
        {
            if (listCreatureId != null && listCreatureId.Contains(creatureId))
            {
                return true;
            }
        }
        return false;
    }
}