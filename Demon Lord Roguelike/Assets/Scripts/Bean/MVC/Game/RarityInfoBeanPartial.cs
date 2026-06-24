using System;
using System.Collections.Generic;
public partial class RarityInfoBean
{
    /// <summary>
    /// 获取稀有度枚举
    /// </summary>
    public RarityEnum GetRarityEnum()
    {
        return (RarityEnum)id;
    }
}
public partial class RarityInfoCfg
{
    /// <summary>
    /// 通过稀有度枚举获取配置
    /// </summary>
    public static RarityInfoBean GetItemData(RarityEnum key)
    {
        return GetItemData((long)key);
    }

    /// <summary>
    /// 获取指定稀有度的进阶所需时间(秒)。rarity≤0 视为 N(1);配置缺失或满级返回 0(表示不可进阶)。
    /// </summary>
    /// <param name="rarity">源稀有度(进阶前的稀有度)</param>
    /// <returns>进阶所需时间(秒),0 表示不可进阶</returns>
    public static int GetAscendTimeByRarity(int rarity)
    {
        int rarityForLookup = rarity <= 0 ? (int)RarityEnum.N : rarity;
        var rarityInfo = GetItemData(rarityForLookup);
        if (rarityInfo == null)
        {
            return 0;
        }
        return rarityInfo.ascend_time;
    }
}
