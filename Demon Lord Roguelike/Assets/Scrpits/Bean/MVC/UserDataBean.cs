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
    //保存下标
    public int saveIndex = 0;
    //拥有的金币数量（魔晶石）
    public long coin;
    //阵容最大数量
    public int lineupMax = 10;
    //用户名字
    public string userName;

    //阵容生物
    public Dictionary<int, List<string>> dicLineupCreature = new Dictionary<int, List<string>>();
    //背包里的所有生物
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();
    //魔王自己的数据
    public CreatureBean selfCreature;

    //游戏进度地图
    public GameWorldMapBean gameWorldMapData;

    //用户解锁数据
    public UserUnlockBean userUnlockData;

    /// <summary>
    /// 获取用户解锁数据
    /// </summary>
    /// <returns></returns>
    public UserUnlockBean  GetUserUnlockData()
    {
        if(userUnlockData == null)
            userUnlockData = new UserUnlockBean();
        //容错处理
        if (userUnlockData.unlockWorldMapRefreshNum > 50)
            userUnlockData.unlockWorldMapRefreshNum = 50;
        return userUnlockData;
    }

    /// <summary>
    /// 清除游戏地图进度
    /// </summary>
    public void ClearGameWorldMapData()
    {
        gameWorldMapData = null;
    }

    /// <summary>
    /// 检测是否有足够的金币
    /// </summary>
    /// <param name="checkNum">检测数量</param>
    /// <param name="isHint">是否提示</param>
    /// <param name="isAddCoin">是否扣除</param>
    public bool CheckHasCoin(int checkNum, bool isHint = false,bool isAddCoin = false)
    {
        if(coin >= checkNum)
        {
            if(isAddCoin)
            {
                AddCoin(-checkNum);
            }
            return true;
        }
        else
        {
            if (isHint)
            {     
                UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(3000001));
            }
            return false;
        }
    }

    /// <summary>
    /// 增加金币
    /// </summary>
    public void AddCoin(long coinNum)
    {
        coin += coinNum;
        if (coin < 0)
        {
            coin = 0;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Coin_Change);
    }

    /// <summary>
    /// 获取阵容生物
    /// </summary>
    public List<string> GetLineupCreature(int lineupIndex)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            return listCreatureId;
        }
        else
        {
            List<string> targetList = new List<string>();
            dicLineupCreature.Add(lineupIndex, targetList);
            return targetList;
        }
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
    /// 获取背包里的生物
    /// </summary>
    public CreatureBean GetBackpackCreature(string creatureId)
    {
        for (int i = 0; i < listBackpackCreature.Count; i++)
        {
            var itemCreature = listBackpackCreature[i];
            if (itemCreature.creatureId.Equals(creatureId))
            {
                return itemCreature;
            }
        }
        return null;
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