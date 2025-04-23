using System;
using System.Collections.Generic;
public partial class ResearchInfoBean
{
    public long[] preResearchIds;
    
    /// <summary>
    /// 获取所有前置商店道具ID
    /// </summary>
    /// <returns></returns>
    public long[] GetPreResearchIds()
    {
        if (preResearchIds == null)
        {
            preResearchIds = new long[0];
            preResearchIds = pre_research_ids.SplitForArrayLong(',');
        }
        return preResearchIds;
    }

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <returns></returns>
    public ResearchInfoTypeEnum GetResearchType()
    {
        return (ResearchInfoTypeEnum)research_type;
    }
}
public partial class ResearchInfoCfg
{
   public static Dictionary<ResearchInfoTypeEnum, List<ResearchInfoBean>> dicResearchInfoByType;

    /// <summary>
    /// 按类型获取数据
    /// </summary>
    public static List<ResearchInfoBean> GetResearchInfoByType(ResearchInfoTypeEnum targetResearchInfoType)
    {
        if (dicResearchInfoByType == null)
        {
            dicResearchInfoByType = new Dictionary<ResearchInfoTypeEnum, List<ResearchInfoBean>>();
            var allData = GetAllData();
            allData.ForEach((key, value) =>
            {
                var researchType = value.GetResearchType();
                if (dicResearchInfoByType.TryGetValue(researchType, out var listData))
                {
                    listData.Add(value);
                }
                else
                {
                    dicResearchInfoByType.Add(researchType, new List<ResearchInfoBean>() { value });
                }
            });
        }
        if (dicResearchInfoByType.TryGetValue(targetResearchInfoType, out List<ResearchInfoBean> listData))
        {
            return listData;
        }
        else
        {
            return null;
        }
    }
}
