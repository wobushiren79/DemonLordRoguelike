using System.Collections.Generic;

/// <summary>
/// 成就类型枚举
/// </summary>
public enum AchievementTypeEnum
{
    None = 0,
    Kill = 1,           //击杀生物
    PlayTime = 2,       //游玩时间(单位:秒)
    ConquerComplete = 3,//征服模式通关(按世界×难度: target_world=世界id, target_extra=难度)
    ConquerWorldClear = 4,//征服某世界-按【已通不同难度数】(target_world=世界id, 进度=该世界通关次数≥1的难度种类数)
}

/// <summary>
/// 成就(当前激活等级)状态枚举
/// </summary>
public enum AchievementStateEnum
{
    NotReached = 0,//未达成
    Reached = 1,   //达成未领取(可手动点击领取)
    Unlocked = 2,  //已领取(此处表示整族已全部领取完成)
}

public partial class AchievementInfoBean
{
    #region 解析缓存

    private long[] _targetValues;
    private long[] _rewardCrystals;

    #endregion

    #region 类型/目标

    /// <summary>
    /// 获取成就类型枚举
    /// </summary>
    public AchievementTypeEnum GetAchievementType()
    {
        return (AchievementTypeEnum)achievement_type;
    }

    /// <summary>
    /// 获取目标征服世界id(仅类型3=征服模式通关有效, 0表示不限定世界)
    /// </summary>
    public int GetTargetWorldId()
    {
        return target_world;
    }

    #endregion

    #region 等级(多级)

    /// <summary>
    /// 各级目标值数组(逗号分隔解析, 缓存)
    /// </summary>
    public long[] GetTargetValues()
    {
        if (_targetValues == null)
            _targetValues = target_values.IsNull() ? new long[0] : target_values.SplitForArrayLong(',');
        return _targetValues;
    }

    /// <summary>
    /// 各级奖励魔晶数组(逗号分隔解析, 缓存)
    /// </summary>
    public long[] GetRewardCrystals()
    {
        if (_rewardCrystals == null)
            _rewardCrystals = reward_crystals.IsNull() ? new long[0] : reward_crystals.SplitForArrayLong(',');
        return _rewardCrystals;
    }

    /// <summary>
    /// 该成就的等级总数(以 target_values 长度为准)
    /// </summary>
    public int GetLevelCount()
    {
        return GetTargetValues().Length;
    }

    /// <summary>
    /// 获取指定等级(0基索引)的目标值; 越界按边界取(无等级时返回0)
    /// </summary>
    public long GetLevelTargetValue(int levelIndex)
    {
        var values = GetTargetValues();
        if (values.Length == 0) return 0;
        if (levelIndex < 0) levelIndex = 0;
        if (levelIndex >= values.Length) levelIndex = values.Length - 1;
        return values[levelIndex];
    }

    /// <summary>
    /// 获取指定等级(0基索引)的奖励魔晶; 越界按边界取(无等级时返回0)
    /// </summary>
    public long GetLevelReward(int levelIndex)
    {
        var values = GetRewardCrystals();
        if (values.Length == 0) return 0;
        if (levelIndex < 0) levelIndex = 0;
        if (levelIndex >= values.Length) levelIndex = values.Length - 1;
        return values[levelIndex];
    }

    /// <summary>
    /// 获取指定等级(0基索引)的描述文本。
    /// 取 details 文本id 的 content_1 作为模板, 用 GetTextReplace 把占位符替换为该级"格式化后的目标值"
    /// (时长类换算为小时, 其余为原值)。该数值同时挂在通用占位符 {Name} 与本类型语义占位符
    /// ({KillNum}=击杀 / {Time_H}=时长) 下, 模板里用哪个都能替换。
    /// 例: 模板"累计击杀 {Name} 只生物" + 目标100 -> "累计击杀 100 只生物";
    ///     模板"累计游玩 {Time_H} 小时" + 目标7200秒 -> "累计游玩 2 小时"。
    /// </summary>
    public string GetLevelDescription(int levelIndex)
    {
        //模板 = details_language(框架自动生成的 content_1 属性, 带 LanguageCache 缓存; 整成就共用一条文本: name=content, details=content_1)
        string template = details_language;
        if (template.IsNull()) return "";
        string valueStr = FormatValueByType(GetLevelTargetValue(levelIndex));
        var dicReplace = new Dictionary<TextReplaceEnum, string>
        {
            { TextReplaceEnum.Name, valueStr },
        };
        //同一数值再挂到本类型语义占位符下(KillNum/Time_H), 模板可用更贴切的占位符
        dicReplace[GetValueReplaceKey()] = valueStr;
        return TextHandler.Instance.GetTextReplace(template, dicReplace);
    }

    /// <summary>
    /// 该成就"目标数值"对应的语义占位符枚举: 击杀=KillNum, 时长(小时)=Time_H, 其余(征服等)=Name。
    /// 描述模板既可用通用 {Name}, 也可用该语义占位符(如时长用 {Time_H} 更贴切)。
    /// </summary>
    private TextReplaceEnum GetValueReplaceKey()
    {
        switch (GetAchievementType())
        {
            case AchievementTypeEnum.Kill: 
                return TextReplaceEnum.KillNum;
            case AchievementTypeEnum.PlayTime: 
                return TextReplaceEnum.Time_H;
            default: 
                return TextReplaceEnum.Name;
        }
    }

    /// <summary>
    /// 按成就类型格式化数值用于显示: 时长类(PlayTime)把秒换算为小时(至多一位小数), 其余按原值。
    /// 进度文本与描述占位符统一走此方法, 保证口径一致。
    /// </summary>
    public string FormatValueByType(long value)
    {
        if (GetAchievementType() == AchievementTypeEnum.PlayTime)
        {
            const float secondsPerHour = 3600f;
            return (value / secondsPerHour).ToString("0.#");
        }
        return value.ToString();
    }

    #endregion
}

public partial class AchievementInfoCfg
{
    #region 列表

    /// <summary>
    /// 按 sort 升序(相同再按 id 升序)获取所有成就(每个可升级成就一行, 即 UI 卡片数据源)
    /// </summary>
    public static List<AchievementInfoBean> GetAllListSorted()
    {
        var allData = GetAllArrayData();
        List<AchievementInfoBean> listResult = new List<AchievementInfoBean>();
        if (allData == null)
            return listResult;
        for (int i = 0; i < allData.Length; i++)
        {
            listResult.Add(allData[i]);
        }
        listResult.Sort((a, b) =>
        {
            if (a.sort != b.sort) return a.sort.CompareTo(b.sort);
            return a.id.CompareTo(b.id);
        });
        return listResult;
    }

    #endregion
}
