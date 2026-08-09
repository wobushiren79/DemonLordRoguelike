using System.Collections.Generic;
using UnityEngine;
public partial class ResearchInfoBean
{
    public List<long> preUnlockIds;
    public long[] arrayPayCrystal;

    #region pre_data 前置解锁条件(特殊条件)
    /// <summary>pre_data 解析缓存(条件枚举 → 要求数值)</summary>
    public Dictionary<ResearchPreConditionEnum, long> dicPreDataCondition;

    /// <summary>pre_data 解析是否出错(出错时 CheckPreDataIsMeet 恒 false, 用节点隐藏来暴露配置错误)</summary>
    public bool isPreDataParseError;

    /// <summary>
    /// 获取 pre_data 解析后的前置条件字典(&amp; 与关系, 单条格式 条件枚举名:数值, 数值缺省为1)
    /// </summary>
    /// <returns>条件字典(条件枚举 → 要求数值)</returns>
    public Dictionary<ResearchPreConditionEnum, long> GetPreDataConditions()
    {
        if (dicPreDataCondition == null)
        {
            //拆分走通用扩展 SplitForDictionaryEnumLong(缺省值补1, 无法识别的条目回收到 listErrorKey)
            var listErrorKey = new List<string>();
            dicPreDataCondition = pre_data.SplitForDictionaryEnumLong<ResearchPreConditionEnum>(listErrorKey, defaultValue: 1);
            isPreDataParseError = listErrorKey.Count > 0;
            for (int i = 0; i < listErrorKey.Count; i++)
            {
                LogUtil.LogError($"研究(id:{id}) pre_data 存在无法识别的条件:{listErrorKey[i]}");
            }
        }
        return dicPreDataCondition;
    }

    /// <summary>
    /// 检测 pre_data 前置条件是否全部满足(&amp; 与关系)
    /// </summary>
    /// <returns>true=全部满足(或无 pre_data 配置)</returns>
    public bool CheckPreDataIsMeet()
    {
        if (pre_data.IsNull())
            return true;
        //先触发解析(内含错误标记), 再判错误标记, 避免首次调用时标记尚未生成
        var dicCondition = GetPreDataConditions();
        if (isPreDataParseError)
            return false;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        foreach (var itemCondition in dicCondition)
        {
            if (!CheckPreDataConditionIsMeet(userData, itemCondition.Key, itemCondition.Value))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 检测单条 pre_data 前置条件是否满足
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <param name="condition">条件类型</param>
    /// <param name="value">要求数值</param>
    private static bool CheckPreDataConditionIsMeet(UserDataBean userData, ResearchPreConditionEnum condition, long value)
    {
        //世界1(剑与魔法)征服模式难度通关次数：难度 = 枚举值 - World1ConquerCompleteCount1 + 1, 要求通关次数 >= value
        if (condition >= ResearchPreConditionEnum.World1ConquerCompleteCount1 && condition <= ResearchPreConditionEnum.World1ConquerCompleteCount10)
        {
            int difficultyLevel = (int)(condition - ResearchPreConditionEnum.World1ConquerCompleteCount1) + 1;
            return userData.GetUserAchievementData().GetConquerCompleteCount(1, difficultyLevel) >= value;
        }
        LogUtil.LogError($"未处理的研究前置条件类型:{condition}");
        return false;
    }

    #endregion

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
    /// 获取带「待解锁等级数值详情」的研究名称(如 空格突进（距离1.5）/突进冷却（2.5秒）)
    /// 仅对多语言模板含 {Value} 占位的节点生效(当前为空格突进 SpaceDash/突进冷却 SpaceDashCD),其余节点原样返回 name_language;
    /// 数值取 待解锁等级=min(当前等级+1,满级) 对应的效果值——每级只显示当前要解锁那一级的距离/冷却。
    /// 替换走多语言通用机制 TextHandler.GetTextReplace + TextReplaceEnum.Value(与成就 GetLevelDescription 同口径)。
    /// </summary>
    /// <param name="currentLevel">当前已解锁的研究等级</param>
    /// <returns>填充数值后的研究名称</returns>
    public string GetNameLanguageWithLevelDetail(int currentLevel)
    {
        string nameLanguage = name_language;
        if (nameLanguage.IsNull() || !nameLanguage.Contains("{Value}"))
            return nameLanguage;
        //待解锁等级:未满级取下一级,满级停留在满级数值
        int targetLevel = Mathf.Min(currentLevel + 1, level_max);
        float detailValue;
        if (unlock_id == (long)UnlockEnum.SpaceDash)
        {
            detailValue = UserUnlockBean.GetSpaceDashDistanceForLevel(targetLevel);
        }
        else if (unlock_id == (long)UnlockEnum.SpaceDashCD)
        {
            detailValue = UserUnlockBean.GetSpaceDashCDForLevel(targetLevel);
        }
        else
        {
            return nameLanguage;
        }
        var dicReplace = new Dictionary<TextReplaceEnum, string>
        {
            { TextReplaceEnum.Value, $"{detailValue}" },
        };
        return TextHandler.Instance.GetTextReplace(nameLanguage, dicReplace);
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
