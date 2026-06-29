using System;
using System.Collections.Generic;

/// <summary>
/// 排序筛选弹窗(UIDialogOrderFilter)确认回传结果。
/// 统一承载:排序键(多选优先级) + 名字模糊条件 + 等级区间条件 + 稀有度多选条件。
/// 约定语义:名字/等级/稀有度为「命中即置顶」条件——不删行、全部展示,调用方把命中项排到列表前面,再按排序键次级排序。
/// 内置 Match* 便捷判定(对应条件为空即恒命中),供各调用方复用。
/// </summary>
public class OrderFilterResultBean
{
    #region 数据
    //排序键(按优先级从高到低,index0=主键;来自 ContentData/ContentOther 选中项;为空则不重排)
    public List<OrderFilterTypeEnum> sortTypes = new List<OrderFilterTypeEnum>();
    //名字模糊查询(大小写不敏感的子串匹配;为空/null 则不按名字筛选)
    public string nameFilter;
    //等级下限(含;默认 0 即不限下限)
    public int levelMin = 0;
    //等级上限(含;默认 int.MaxValue 即不限上限)
    public int levelMax = int.MaxValue;
    //选中的稀有度(为空则不按稀有度筛选,显示全部)
    public List<RarityEnum> rarities = new List<RarityEnum>();
    #endregion

    #region 筛选判定
    /// <summary>
    /// 名字是否匹配(大小写不敏感子串;筛选为空时恒为 true)
    /// </summary>
    /// <param name="name">待匹配的名字</param>
    public bool MatchName(string name)
    {
        if (string.IsNullOrEmpty(nameFilter))
            return true;
        if (string.IsNullOrEmpty(name))
            return false;
        return name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 等级是否落在区间内(含端点)
    /// </summary>
    /// <param name="level">待匹配的等级</param>
    public bool MatchLevel(int level)
    {
        return level >= levelMin && level <= levelMax;
    }

    /// <summary>
    /// 稀有度是否匹配(rarities 为空则不限,恒为 true)
    /// </summary>
    /// <param name="rarity">待匹配的稀有度(int,对应 RarityEnum 值)</param>
    public bool MatchRarity(int rarity)
    {
        if (rarities == null || rarities.Count == 0)
            return true;
        return rarities.Contains((RarityEnum)rarity);
    }
    #endregion
}
