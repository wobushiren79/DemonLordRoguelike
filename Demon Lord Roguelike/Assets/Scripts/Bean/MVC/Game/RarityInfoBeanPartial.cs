using System;
using System.Collections.Generic;
public partial class RarityInfoBean
{
    #region gashapon_rate 孕育命中基础概率(临时字段)
    /// <summary>
    /// [临时]孕育命中基础概率(%)(对应 excel_rarity_info 的 gashapon_rate 列)。
    /// 因本次未跑 Unity 重新生成 Entity 而手写在 Partial(与生成器产物保持一致, 仿 ResearchInfoBeanPartial.details 先例);
    /// 重新生成 RarityInfoBean.cs 后删除本 region(生成物自带 gashapon_rate)。
    /// </summary>
    public float gashapon_rate;
    #endregion

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
