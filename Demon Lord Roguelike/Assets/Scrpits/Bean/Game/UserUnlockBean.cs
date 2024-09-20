using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserUnlockBean
{
    public Dictionary<long, UserUnlockWorldBean> unlockWorldData;//解锁的世界难度
    public int unlockWorldMapRefreshNum = 3;//解锁的世界刷新数量

    public UserUnlockBean()
    {
        unlockWorldData = new Dictionary<long, UserUnlockWorldBean>() { { 1, new UserUnlockWorldBean(1) } };
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
        if (unlockWorldData.TryGetValue(worldId,out UserUnlockWorldBean targetData))
        {
            return targetData;
        }
        return null;
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