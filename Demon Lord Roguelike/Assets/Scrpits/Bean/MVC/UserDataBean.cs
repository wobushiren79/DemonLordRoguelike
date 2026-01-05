/*
* FileName: UserData 
* Author: AppleCoffee 
* CreateTime: 2024-07-16-17:44:25 
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;

[Serializable]
public class UserDataBean : BaseBean
{
    public bool isErrorData = false;
    //保存下标
    public int saveIndex = 0;
    //备份保存的下表
    public int saveRemarkIndex = 0;
    //拥有的魔晶
    public long crystal;
    //拥有的声望
    public long reputation;
    //用户名字
    public string userName;
    //游戏事件
    public long gameTime;
    //背包里的所有道具
    public List<ItemBean> listBackpackItems = new List<ItemBean>();
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
    //用户限制数据
    public UserLimmitBean userLimmitData;
    //用户进阶数据
    public UserAscendBean userAscendData;
    //临时存储数据
    public UserTempBean userTempBean;
    
    /// <summary>
    /// 获取用户进阶数据
    /// </summary>
    public UserAscendBean GetUserAscendData()
    {
        if (userAscendData == null)
            userAscendData = new UserAscendBean();
        return userAscendData;
    }

    /// <summary>
    /// 获取用户临时数据
    /// </summary>
    public UserTempBean GetUserTempData()
    {
        if (userTempBean == null)
            userTempBean = new UserTempBean();
        return userTempBean;
    }

    /// <summary>
    /// 获取用户解锁数据
    /// </summary>
    /// <returns></returns>
    public UserUnlockBean GetUserUnlockData()
    {
        if (userUnlockData == null)
            userUnlockData = new UserUnlockBean();
        return userUnlockData;
    }

    /// <summary>
    /// 获取用户限制数据
    /// </summary>
    /// <returns></returns>
    public UserLimmitBean GetUserLimmitData()
    {
        if (userLimmitData == null)
            userLimmitData = new UserLimmitBean();
        return userLimmitData;
    }

    /// <summary>
    /// 清除游戏地图进度
    /// </summary>
    public void ClearGameWorldMapData()
    {
        gameWorldMapData = null;
    }

    #region 魔晶相关
    /// <summary>
    /// 增加金币
    /// </summary>
    public void AddCrystal(long crystalNum)
    {
        crystal += crystalNum;
        if (crystal < 0)
        {
            crystal = 0;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Crystal_Change);
    }

    /// <summary>
    /// 增加声望
    /// </summary>
    public void AddReputation(long reputationNum)
    {
        reputation += reputationNum;
        if (reputation < 0)
        {
            reputation = 0;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Reputation_Change);
    }

    /// <summary>
    /// 检测是否有足够的魔晶
    /// </summary>
    /// <param name="checkNum">检测数量</param>
    /// <param name="isHint">是否提示</param>
    /// <param name="isAddCrystal">是否扣除</param>
    public bool CheckHasCrystal(long checkNum, bool isHint = false, bool isAddCrystal = false)
    {
        if (crystal >= checkNum)
        {
            if (isAddCrystal)
            {
                AddCrystal(-checkNum);
            }
            return true;
        }
        else
        {
            if (isHint)
            {
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000001));
            }
            return false;
        }
    }

    /// <summary>
    /// 检测是否有足够的声望
    /// </summary>
    /// <param name="checkNum">检测数量</param>
    /// <param name="isHint">是否提示</param>
    /// <param name="isAddReputation">是否扣除</param>
    /// <returns></returns>
    public bool CheckHasReputation(long checkNum, bool isHint = false, bool isAddReputation = false)
    {
        if (reputation >= checkNum)
        {
            if (isAddReputation)
            {
                AddReputation(-checkNum);
            }
            return true;
        }
        else
        {
            if (isHint)
            {
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000002));
            }
            return false;
        }
    }
    #endregion

    #region 道具相关
    /// <summary>
    /// 增加道具
    /// </summary>
    public void AddItem(ItemBean itemData)
    {
        if (itemData == null || itemData.itemId == 0)
            return;
        listBackpackItems.Add(itemData);
        EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Item_Change);
    }

    public void AddItem(long itemId, int num = 1)
    {
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        if (itemInfo == null)
            return;
        int maxNum = itemInfo.num_max;
        //容错处理 最大数量默认最小为1
        if (maxNum == 0) maxNum = 1;

        // 尝试在现有道具堆中添加
        foreach (var item in listBackpackItems)
        {
            if (item.itemId == itemId && item.itemNum < maxNum)
            {
                int availableSpace = maxNum - item.itemNum;
                if (num <= availableSpace)
                {
                    // 全部数量可以加入这个堆
                    item.itemNum += num;
                    return;
                }
                else
                {
                    // 部分数量加入这个堆，填满它
                    item.itemNum = maxNum;
                    num -= availableSpace;
                    // 继续循环寻找其他可堆叠的道具
                }
            }
        }

        // 没有找到可堆叠的道具或还有剩余数量，创建新的道具堆
        while (num > 0)
        {
            int addAmount = Math.Min(num, maxNum);
            listBackpackItems.Add(new ItemBean
            {
                itemId = itemId,
                itemNum = addAmount
            });
            num -= addAmount;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Item_Change);
    }
    
    /// <summary>
    /// 移除背包里的道具
    /// </summary>
    public void RemoveItem(ItemBean itemData)
    {
        if (itemData == null || itemData.itemId == 0)
            return;
        listBackpackItems.Remove(itemData);
        EventHandler.Instance.TriggerEvent(EventsInfo.Backpack_Item_Change);
    }
    #endregion

    #region 阵容生物相关
    /// <summary>
    /// 获取阵容生物ID
    /// </summary>
    public List<string> GetLineupCreatureIds(int lineupIndex)
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
    /// 获取阵容生物
    /// </summary>
    public List<CreatureBean> GetLineupCreature(int lineupIndex)
    {
        List<CreatureBean> listCreature = new List<CreatureBean>();
        List<string> listLineupCreature = GetLineupCreatureIds(lineupIndex);
        for (int i = 0; i < listLineupCreature.Count; i++)
        {
            string creatureId = listLineupCreature[i];
            var creatureData = GetBackpackCreature(creatureId);
            if (creatureData == null)
                continue;
            listCreature.Add(creatureData);
        }
        return listCreature;
    }

    /// <summary>
    /// 获取阵容生物所在位置
    /// </summary>
    public int GetLineupCreaturePosIndex(int lineupIndex, string creatureUUId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureUUId))
        {
            if (listCreatureUUId.Contains(creatureUUId))
            {
                return listCreatureUUId.IndexOf(creatureUUId);
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
    public bool AddLineupCreature(int lineupIndex, string creatureUUId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureUUId))
        {
            if (listCreatureUUId.Contains(creatureUUId))
            {
                return false;
            }
            else
            {
                listCreatureUUId.Add(creatureUUId);
            }
        }
        else
        {
            dicLineupCreature.Add(lineupIndex, new List<string>() { creatureUUId });
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
    /// 检测是否包含在阵容里
    /// </summary>
    public bool CheckIsLineup(int lineupIndex, string creatureUUId)
    {
        if (dicLineupCreature.TryGetValue(lineupIndex, out List<string> listCreatureId))
        {
            if (listCreatureId != null && listCreatureId.Contains(creatureUUId))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取生物所在阵容
    /// </summary>
    public int GetLinupIndex(string creatureUUId)
    {
        foreach (var item in dicLineupCreature)
        {
            if (item.Value.Contains(creatureUUId))
            {
                return item.Key;
            }
        }
        return 0;
    }
    #endregion

    #region  背包生物相关
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
    public CreatureBean GetBackpackCreature(string creatureUUId)
    {
        for (int i = 0; i < listBackpackCreature.Count; i++)
        {
            var itemCreature = listBackpackCreature[i];
            if (itemCreature.creatureUUId.Equals(creatureUUId))
            {
                return itemCreature;
            }
        }
        return null;
    }
    #endregion
}