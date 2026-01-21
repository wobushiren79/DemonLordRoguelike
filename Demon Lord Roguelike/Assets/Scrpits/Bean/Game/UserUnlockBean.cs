using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserUnlockBean
{
    //基础解锁数据
    public Dictionary<long, UserUnlockInfoBean> unlockInfoData = new Dictionary<long, UserUnlockInfoBean>();

    /// <summary>
    /// 增加解锁
    /// </summary>
    public void AddUnlock(long unlockId,int unlockLevel = 1)
    {
        if (unlockInfoData.TryGetValue(unlockId, out var unlockData))
        {
            unlockData.unlockLevel = unlockLevel;
        }
        else
        {
            unlockInfoData.Add(unlockId, new UserUnlockInfoBean(unlockId));
            EventHandler.Instance.TriggerEvent(EventsInfo.User_AddUnlock, unlockId);
        }
    }

    /// <summary>
    /// 检测解锁了多少个
    /// </summary>
    public int CheckIsUnlockNum(long[] unlockIds)
    {
        int unlockNum = 0;
        for (int i = 0; i < unlockIds.Length; i++)
        {
            var unlockId = unlockIds[i];
            //只要有一个未解锁 那就都未解锁
            if (CheckIsUnlock(unlockId))
            {
                unlockNum++;
            }
        }
        return unlockNum;
    }

    /// <summary>
    /// 检测是否解锁
    /// </summary>
    public bool CheckIsUnlock(string unlockStr)
    {
        if (unlockStr.IsNull())
        {
            LogUtil.LogError("检测解锁失败，unlockStr为null");
            return false;
        }
        string[] arrayDataStr = unlockStr.SplitForArrayStr(',');
        for (int i = 0; i < arrayDataStr.Length; i++)
        {
            var itemData = arrayDataStr[i];
            bool isUnlock = true;
            //如果包含或判定
            if (itemData.Contains("|"))
            {
                var unlockIdsOR = itemData.SplitForArrayLong('|');
                isUnlock = false;
                for (int f = 0; f < unlockIdsOR.Length; f++)
                {
                    //或判定 只要有一个解锁那这一组就都解锁了
                    var itemDataOR = unlockIdsOR[f];
                    if (CheckIsUnlock(itemDataOR))
                    {
                        isUnlock = true;
                        break;
                    }
                }
            }
            //其他情况直接转long
            else
            {
               isUnlock = CheckIsUnlock(long.Parse(itemData));
            }
            //只要有一个未解锁 那就都未解锁
            if(isUnlock == false)
            {
                return false;
            }
        }
        return true;
    }

    public bool CheckIsUnlock(long[] unlockIds)
    {
        for (int i = 0; i < unlockIds.Length; i++)
        {
            var unlockId = unlockIds[i];
            //只要有一个未解锁 那就都未解锁
            if (!CheckIsUnlock(unlockId))
            {
                return false;
            }
        }
        return true;
    }
    public bool CheckIsUnlock(UnlockEnum unlockEnum)
    {
        return CheckIsUnlock((long)unlockEnum);
    }
    public bool CheckIsUnlock(long unlockId)
    {
        if (unlockId == 0)
        {
            return true;
        }
        if (unlockInfoData.ContainsKey(unlockId))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取解锁的当前研究等级
    /// </summary>
    public int GetUnlockResearchLeveByUnlockEnum(UnlockEnum unlockEnum)
    {
        return GetUnlockResearchLeveByUnlockId((long)unlockEnum);
    }

    /// <summary>
    /// 获取解锁的当前研究等级
    /// </summary>
    public int GetUnlockResearchLeveByUnlockId(long unlockId)
    {
        ResearchInfoBean researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(unlockId);
        return GetUnlockResearchLevel(researchInfo);
    }

    /// <summary>
    /// 获取解锁的当前研究等级
    /// </summary>
    public int GetUnlockResearchLeveByResearchId(long researchId)
    {
        ResearchInfoBean researchInfo = ResearchInfoCfg.GetItemData(researchId);
        return GetUnlockResearchLevel(researchInfo);
    }

    /// <summary>
    /// 获取解锁的当前研究等级
    /// </summary>
    public int GetUnlockResearchLevel(ResearchInfoBean researchInfo)
    {
        if (unlockInfoData.TryGetValue(researchInfo.unlock_id, out var unlockData))
        {
            return unlockData.unlockLevel;
        }
        return 0;
    }


    /// <summary>
    /// 获取解锁传送门显示数量
    /// </summary>
    /// <returns></returns>
    public int GetUnlockPortalShowCount()
    {
        return 3 + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.PortalShowNum);
    }

    /// <summary>
    /// 获取解锁阵容数量
    /// </summary>
    /// <returns></returns>
    public int GetUnlockLineupNum()
    {
        return 1 + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.LineupNum);
    }

    /// <summary>
    /// 获取阵容生物上限
    /// </summary>
    /// <returns></returns>
    public int GetUnlockLineupCreatureNum()
    {
        return 6 + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.LineupCreatureAddNum);
    }

    /// <summary>
    /// 获取游戏世界-征服模式-难度
    /// </summary>
    /// <returns></returns>
    public int GetUnlockGameWorldConquerDifficultyLevel(long worldId)
    {
        var gameWorldInfo = GameWorldInfoCfg.GetItemData(worldId);
        return 1 + GetUnlockResearchLeveByUnlockId(gameWorldInfo.unlock_id_conquer_difficulty_level);
    }

    /// <summary>
    /// 获取生物升阶容器数量
    /// </summary>
    /// <returns></returns>
    public int GetUnlockCreatureVatNum()
    {
        bool isUnlockVat = CheckIsUnlock(UnlockEnum.CreatureVat);
        if (isUnlockVat)
        {
            int unlockLevel = GetUnlockResearchLeveByUnlockEnum(UnlockEnum.CreatureVatAdd);
            return 1 + unlockLevel;
        }
        else
        {
            return 0;
        }
    }


    /// <summary>
    /// 获取解锁的世界
    /// </summary>
    /// <returns></returns>
    public List<long> GetUnlockGameWorldIds()
    {
        List<long> listUnlockWorld = new List<long>()
        {
            //默认解锁第一个世界
            1,
        };
        var arrayWorld = GameWorldInfoCfg.GetAllArrayData();
        for (int i = 0; i < arrayWorld.Length; i++)
        {
            var itemWorldInfo = arrayWorld[i];
            if (CheckIsUnlock(itemWorldInfo.unlock_id))
            {
                listUnlockWorld.Add(itemWorldInfo.id);
            }
        }
        return listUnlockWorld;
    }
}

//基础解锁数据
public class UserUnlockInfoBean
{
    //解锁ID
    public long unlockId;
    //解锁等级
    public int unlockLevel;

    public UserUnlockInfoBean(long unlockId)
    {
        this.unlockId = unlockId;
        unlockLevel = 1;
    }
}