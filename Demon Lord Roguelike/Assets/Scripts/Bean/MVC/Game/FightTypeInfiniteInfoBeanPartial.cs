using System;
using System.Collections.Generic;
using UnityEngine;

public partial class FightTypeInfiniteInfoBean
{
    /// <summary>
    /// 获取指定轮次的轮次强度倍率(仅无尽表自身乘区, 不含征服基础倍率与终焉议会加成)
    /// 公式: round_intensity_addrate^(round-1); 第1轮恒为1; 配置非法(≤0)按1处理
    /// </summary>
    /// <param name="round">当前轮次(从1开始)</param>
    /// <returns>轮次强度倍率</returns>
    public float GetRoundIntensityRate(int round)
    {
        if (round_intensity_addrate <= 0f)
            return 1f;
        if (round <= 1)
            return 1f;
        return Mathf.Pow(round_intensity_addrate, round - 1);
    }
}

public partial class FightTypeInfiniteInfoCfg
{
    /// <summary>
    /// 获取指定世界指定难度的无尽模式配置行(无配置返回null)
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <param name="difficultyLevel">无尽难度等级(2~10)</param>
    /// <returns>配置行; 无配置返回null</returns>
    public static FightTypeInfiniteInfoBean GetItemData(long worldId, int difficultyLevel)
    {
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            if (itemData.Value.world_id == worldId && itemData.Value.level == difficultyLevel)
                return itemData.Value;
        }
        return null;
    }

    /// <summary>
    /// 获取指定世界无尽配置表中存在的最高难度等级(无任何配置返回0; 供传送门详情弹窗展示未解锁预览)
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <returns>配置表中的最高难度等级; 无配置返回0</returns>
    public static int GetMaxLevel(long worldId)
    {
        int maxLevel = 0;
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            if (itemData.Value.world_id == worldId && itemData.Value.level > maxLevel)
                maxLevel = itemData.Value.level;
        }
        return maxLevel;
    }
}
