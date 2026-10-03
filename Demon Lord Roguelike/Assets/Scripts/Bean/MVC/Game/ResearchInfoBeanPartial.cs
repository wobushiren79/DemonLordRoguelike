using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
public partial class ResearchInfoBean
{
    public List<long> preUnlockIds;
    public long[] arrayPayCrystal;

    #region details 详情描述(临时字段)
    /// <summary>
    /// [临时]详情描述-语言id(对应 excel_research_info 的 details[language] 列, 0=无详情)。
    /// 因本次未跑 Unity 重新生成 Entity 而手写在 Partial(与生成器产物保持一致, 仿 pre_data 先例);
    /// 重新生成 ResearchInfoBean.cs 后删除本 region(生成物自带 details/details_language, 且 CombineModReferenceIds 自动补 details 拼接)。
    /// </summary>
    public long details;

    /// <summary>
    /// [临时]详情描述多语言文本(懒加载缓存, 语言切换经 LanguageCache 版本号自动失效)
    /// </summary>
    [JsonIgnore]
    public string details_language { get => _details_language.Get(() => TextHandler.Instance.GetTextById(ResearchInfoCfg.fileName, details)); set => _details_language.Set(value); }
    private LanguageCache _details_language;
    #endregion

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
        //任意一个世界的无尽模式已解锁(任一世界无尽难度等级>0即满足; GetUnlockInfiniteDifficultyLevel 对未配置/未解锁的世界安全返回0)
        if (condition == ResearchPreConditionEnum.AnyWorldInfiniteUnlocked)
        {
            var userUnlock = userData.GetUserUnlockData();
            var arrayWorld = GameWorldInfoCfg.GetAllArrayData();
            for (int i = 0; i < arrayWorld.Length; i++)
            {
                if (userUnlock.GetUnlockInfiniteDifficultyLevel(arrayWorld[i].id) > 0)
                    return true;
            }
            return false;
        }
        //世界1(剑与魔法)征服模式难度通关次数：难度 = 枚举值 - World1ConquerCompleteCount1 + 1, 要求通关次数 >= value
        if (condition >= ResearchPreConditionEnum.World1ConquerCompleteCount1 && condition <= ResearchPreConditionEnum.World1ConquerCompleteCount10)
        {
            int difficultyLevel = (int)(condition - ResearchPreConditionEnum.World1ConquerCompleteCount1) + 1;
            return userData.GetUserAchievementData().GetConquerCompleteCount(1, difficultyLevel) >= value;
        }
        //扭蛋职业抽出次数：职业生物id = 枚举值本身(枚举值=职业id), 要求累计抽出数量 >= value
        if (condition >= ResearchPreConditionEnum.GashaponCreatureDrawCount1001 && condition <= ResearchPreConditionEnum.GashaponCreatureDrawCount7004)
        {
            long creatureId = (long)condition;
            return userData.GetUserAchievementData().GetGashaponCreatureDrawCount(creatureId) >= value;
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

    #region details 详情描述(按待解锁等级填充累计效果数值)

    /// <summary>
    /// 获取带「待解锁等级累计效果」的详情描述(如 （概率40%）/（距离4.5）/（祭品上限5）)
    /// details==0(无详情配置)返回空串; 模板不含 {Value} 时原样返回(兼容未来的静态详情);
    /// 数值取 待解锁等级=min(当前等级+1,满级) 对应的累计总效果, 公式统一收口 UserUnlockBean 的 static ForLevel 方法;
    /// 替换走多语言通用机制 TextHandler.GetTextReplace + TextReplaceEnum.Value(与成就 GetLevelDescription 同口径);
    /// 括号样式/前导空格由各语言详情文本自带(cn/tw/jp 全角无空格, 其余半角带前导空格), 调用方直接拼在名字后
    /// </summary>
    /// <param name="currentLevel">当前已解锁的研究等级</param>
    /// <returns>填充数值后的详情描述; 无详情时返回空串</returns>
    public string GetDetailsLanguageWithLevelDetail(int currentLevel)
    {
        string detailsLanguage = details_language;
        if (detailsLanguage.IsNull())
            return "";
        if (!detailsLanguage.Contains("{Value}"))
            return detailsLanguage;
        //待解锁等级:未满级取下一级,满级停留在满级数值
        int targetLevel = Mathf.Min(currentLevel + 1, level_max);
        string detailValue = GetLevelDetailValueString(targetLevel);
        if (detailValue == null)
        {
            LogUtil.LogError($"研究(id:{id}) 配置了详情但未登记等级公式(unlock_id:{unlock_id})");
            return detailsLanguage;
        }
        var dicReplace = new Dictionary<TextReplaceEnum, string>
        {
            { TextReplaceEnum.Value, detailValue },
        };
        return TextHandler.Instance.GetTextReplace(detailsLanguage, dicReplace);
    }

    /// <summary>
    /// 获取指定等级的效果数值显示串(按 unlock_id 分发到 UserUnlockBean 的 static ForLevel 公式, 22 个 level_max>1 节点)
    /// </summary>
    /// <param name="targetLevel">待解锁等级</param>
    /// <returns>数值显示串; 未登记公式返回 null(调用方报错并原样显示模板)</returns>
    private string GetLevelDetailValueString(int targetLevel)
    {
        long unlockId = unlock_id;
        //进阶设施数量 = creatureVatMax + 等级
        if (unlockId == (long)UnlockEnum.CreatureVatAdd)
            return $"{UserUnlockBean.GetCreatureVatNumForLevel(targetLevel)}";
        //进阶加速每次推进秒数(=倍率) = 等级
        if (unlockId == (long)UnlockEnum.CreatureVatAddProgress)
            return $"{UserUnlockBean.GetCreatureVatAddProgressForLevel(targetLevel)}";
        //进阶素材可选上限 = creatureVatMaterialMax + 等级
        if (unlockId == (long)UnlockEnum.CreatureVatMaterialNum)
            return $"{UserUnlockBean.GetCreatureVatMaterialMaxForLevel(targetLevel)}";
        //献祭祭品上限 = sacrificeMax + 等级
        if (unlockId == (long)UnlockEnum.SacrificeNum)
            return $"{UserUnlockBean.GetSacrificeMaxForLevel(targetLevel)}";
        //献祭失败保底概率 = 等级×5%(×100 转百分数, RoundToInt 防 0.05f 浮点尾差)
        if (unlockId == (long)UnlockEnum.SacrificePityRate)
            return $"{Mathf.RoundToInt(UserUnlockBean.GetSacrificeFailPityAddRateForLevel(targetLevel) * 100)}";
        //不同魔物献祭成功率 = 等级×5%(同上转百分数)
        if (unlockId == (long)UnlockEnum.SacrificeDifferentIdRate)
            return $"{Mathf.RoundToInt(UserUnlockBean.GetSacrificeDifferentIdRateForLevel(targetLevel) * 100)}";
        //传送门刷新次数 = 等级
        if (unlockId == (long)UnlockEnum.PortalRefreshNum)
            return $"{UserUnlockBean.GetPortalRefreshMaxForLevel(targetLevel)}";
        //是魔王就挑战100勇士出现概率 = 等级×10
        if (unlockId == (long)UnlockEnum.ChallengeHundredShowRate)
            return $"{UserUnlockBean.GetChallengeHundredShowRateForLevel(targetLevel)}";
        //无尽模式出现概率 = 基础10 + 等级×10(满级9级=100)
        if (unlockId == (long)UnlockEnum.InfiniteShowRate)
            return $"{UserUnlockBean.GetInfiniteShowRateForLevel(targetLevel)}";
        //孕育稀有度命中概率(R/SR/SSR 三条共用) = rarityBaseRate(10) + 等级
        if (unlockId == (long)UnlockEnum.GashaponRarityRRate || unlockId == (long)UnlockEnum.GashaponRaritySRRate || unlockId == (long)UnlockEnum.GashaponRaritySSRRate)
            return $"{UserUnlockBean.GetGashaponRarityRateForLevel(targetLevel)}";
        //魔汁机投入上限 = juicerCreatureMax + 等级
        if (unlockId == (long)UnlockEnum.JuicerNum)
            return $"{UserUnlockBean.GetJuicerCreatureMaxForLevel(targetLevel)}";
        //阵容生物上限 = lineupCreatureMax + 等级
        if (unlockId == (long)UnlockEnum.LineupCreatureAddNum)
            return $"{UserUnlockBean.GetLineupCreatureNumForLevel(targetLevel)}";
        //阵容数量 = lineupMax + 等级
        if (unlockId == (long)UnlockEnum.LineupNum)
            return $"{UserUnlockBean.GetLineupNumForLevel(targetLevel)}";
        //魔晶掉落额外存在时长 = 等级×5 秒
        if (unlockId == (long)UnlockEnum.DropCrystalLifeTime)
            return $"{UserUnlockBean.GetDropCrystalAddLifeTimeForLevel(targetLevel)}";
        //魔王魔力上限加成 = 等级×10
        if (unlockId == (long)UnlockEnum.DemonLordMPMax)
            return $"{UserUnlockBean.GetDemonLordMPMaxAddValueForLevel(targetLevel)}";
        //魔王魔力恢复加成 = 等级×1/秒
        if (unlockId == (long)UnlockEnum.DemonLordMPF)
            return $"{UserUnlockBean.GetDemonLordMPFAddValueForLevel(targetLevel)}";
        //深渊馈赠刷新次数 = 等级
        if (unlockId == (long)UnlockEnum.AbyssalBlessingRefreshNum)
            return $"{UserUnlockBean.GetAbyssalBlessingRefreshMaxForLevel(targetLevel)}";
        //空格突进距离 = 等级×SPACE_DASH_DISTANCE_PER_LEVEL
        if (unlockId == (long)UnlockEnum.SpaceDash)
            return $"{UserUnlockBean.GetSpaceDashDistanceForLevel(targetLevel)}";
        //突进冷却 = max(3-等级×0.5, 1)
        if (unlockId == (long)UnlockEnum.SpaceDashCD)
            return $"{UserUnlockBean.GetSpaceDashCDForLevel(targetLevel)}";
        //魔王自动拾取间隔 = 11-等级(秒)(气泡 targetLevel>=1, 不会命中 -1 禁用档)
        if (unlockId == (long)UnlockEnum.DemonLordAutoPickCrystal)
            return $"{UserUnlockBean.GetDemonLordAutoPickCrystalIntervalForLevel(targetLevel)}";
        //魔王每次拾取魔晶数量 = 1 + 等级
        if (unlockId == (long)UnlockEnum.DemonLordAutoPickCrystalNum)
            return $"{UserUnlockBean.GetDemonLordAutoPickCrystalCountForLevel(targetLevel)}";
        return null;
    }
    #endregion

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
