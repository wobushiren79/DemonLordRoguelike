using System.Collections.Generic;

/// <summary>
/// 成就类型枚举
/// </summary>
public enum AchievementTypeEnum
{
    None = 0,
    Kill = 1,           //击杀生物
    PlayTime = 2,       //游玩时间(单位:秒)
    ConquerComplete = 3,//征服模式通关(按难度)
}

/// <summary>
/// 成就状态枚举
/// </summary>
public enum AchievementStateEnum
{
    NotReached = 0,//未达成
    Reached = 1,   //达成未解锁(可手动点击领取)
    Unlocked = 2,  //已解锁(已领取奖励)
}

public partial class AchievementInfoBean
{
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
}

public partial class AchievementInfoCfg
{
    /// <summary>
    /// 按排序获取所有成就(用于UI展示)
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
}
