using System;
using System.Collections.Generic;
public partial class ResearchInfoBean
{
    public List<long> preUnlockIds;
    public long[] arrayPayCrystal;

    /// <summary>
    /// 获取所有前置商店道具ID
    /// </summary>
    /// <returns></returns>
    public List<long> GetPreUnlockIdsForLine()
    {
        if (preUnlockIds == null)
        {
            preUnlockIds = new List<long>();
            var arrayData = pre_unlock_ids.SplitForArrayStr(',');
            for (int i = 0; i < arrayData.Length; i++)
            {
                var itemData = arrayData[i];
                if (itemData.Contains("|"))
                {
                    var arrayIds = itemData.SplitForArrayLong('|');
                    preUnlockIds.AddRange(arrayIds);
                }
                else
                {
                    preUnlockIds.Add(long.Parse(itemData));
                }
            }
        }
        return preUnlockIds;
    }

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <returns></returns>
    public ResearchInfoTypeEnum GetResearchType()
    {
        return (ResearchInfoTypeEnum)research_type;
    }

    /// <summary>
    /// 获取支付的水晶
    /// </summary>
    /// <param name="researchLevel"></param>
    /// <returns></returns>
    public long GetPayCrystal(int researchLevel)
    {
        if (arrayPayCrystal == null)
        {
            if (pay_crystal.Contains(','))
            {
                arrayPayCrystal = pay_crystal.SplitForArrayLong(',');
            }
            else if (pay_crystal.Contains('*'))
            {
                float[] arrayBaseData = pay_crystal.SplitForArrayFloat('*');
                arrayPayCrystal = new long[level_max];
                float itemPay = arrayBaseData[0] * arrayBaseData[1]; 
                for (int i = 0; i < arrayPayCrystal.Length; i++)
                {
                    arrayPayCrystal[i] = (long)(arrayBaseData[0] + (itemPay * i));   
                }
            }
            else
            {
                arrayPayCrystal = new long[] { long.Parse(pay_crystal) };
            }   
        }
        if (researchLevel > arrayPayCrystal.Length)
        {
            researchLevel = arrayPayCrystal.Length;
        }
        else if(researchLevel < 1)
        {
            researchLevel = 1;
        }
        return arrayPayCrystal[researchLevel - 1];
    }
}

public partial class ResearchInfoCfg
{
    public static Dictionary<ResearchInfoTypeEnum, List<ResearchInfoBean>> dicResearchInfoByType;
    public static Dictionary<long, ResearchInfoBean> dicResearchInfoByUnlockId;

    /// <summary>
    /// 通过解锁ID获取研究
    /// </summary>
    public static ResearchInfoBean GetItemDataByUnlockId(long unlockId)
    {
        if (dicResearchInfoByUnlockId == null)
        {
            dicResearchInfoByUnlockId = new Dictionary<long, ResearchInfoBean>();
            var allData = GetAllArrayData();
            allData.ForEach((key, value) =>
            {
                dicResearchInfoByUnlockId.Add(value.unlock_id, value);
            });
        }
        if (dicResearchInfoByUnlockId.TryGetValue(unlockId, out var data))
        {
            return data;
        }
        return null;
    }

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
