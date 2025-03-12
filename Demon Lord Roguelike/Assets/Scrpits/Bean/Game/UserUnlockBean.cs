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
    //解锁的生物
    public HashSet<long> unlockCreatureData = new HashSet<long>();
    //解锁的世界难度
    public Dictionary<long, UserUnlockWorldBean> unlockWorldData;

    //解锁的世界刷新数量
    public int unlockWorldMapRefreshNum = 3;

    public UserUnlockBean()
    {
        unlockWorldData = new Dictionary<long, UserUnlockWorldBean>() { { 1, new UserUnlockWorldBean(1) } };
    }

    /// <summary>
    /// 增加解锁生物
    /// </summary>
    public void AddUnlockForCreature(long unlockId)
    {
        if (!unlockCreatureData.Contains(unlockId))
        {
            unlockCreatureData.Add(unlockId);
        }
    }

    /// <summary>
    /// 检测是否解锁生物
    /// </summary>
    public bool CheckIsUnlockForCreature(List<long> unlockIds)
    {
        for (int i = 0; i < unlockIds.Count; i++)
        {
            var unlockId = unlockIds[i];
            //只要有一个未解锁 那就都未解锁
            if (!CheckIsUnlockForCreature(unlockId))
            {
                return false;
            }
        }
        return true;
    }
    public bool CheckIsUnlockForCreature(long unlockId)
    {
        if (unlockCreatureData.Contains(unlockId))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 增加解锁
    /// </summary>
    public void AddUnlock(long unlockId)
    {
        if (!unlockInfoData.ContainsKey(unlockId))
        {
            unlockInfoData.Add(unlockId, new UserUnlockInfoBean(unlockId));
        }
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
        long[] unlockIds = unlockStr.SplitForArrayLong(',');
        return CheckIsUnlock(unlockIds);
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
    public bool CheckIsUnlock(long unlockId)
    {
        if (unlockInfoData.ContainsKey(unlockId))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测是否解锁对应的世界
    /// </summary>
    public bool CheckIsUnlockWorld(long worldId)
    {
        if (unlockWorldData.ContainsKey(worldId))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取解锁世界数据
    /// </summary>
    public UserUnlockWorldBean GetUnlockWorldData(long worldId)
    {
        if (unlockWorldData.TryGetValue(worldId, out UserUnlockWorldBean targetData))
        {
            return targetData;
        }
        return null;
    }
}

//基础解锁数据
public class UserUnlockInfoBean
{
    public long unlockId;

    public UserUnlockInfoBean(long unlockId)
    {
        this.unlockId = unlockId;
    }
}

//世界相关解锁数据
public class UserUnlockWorldBean
{
    public long worldId;
    public int difficultyLevel = 1;
    public int difficultyInfinite = 0;

    public UserUnlockWorldBean(long worldId)
    {
        this.worldId = worldId;
        difficultyLevel = 1;
    }
}