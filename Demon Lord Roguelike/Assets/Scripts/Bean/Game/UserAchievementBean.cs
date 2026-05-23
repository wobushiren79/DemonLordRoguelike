using System;
using System.Collections.Generic;

/// <summary>
/// 用户成就数据存档
/// 用于保存玩家成就解锁状态、累计进度以及游戏统计数据
/// </summary>
[Serializable]
public class UserAchievementBean
{
    #region 成就状态数据

    /// <summary>
    /// 成就状态字典
    /// Key: 成就ID(对应 AchievementInfoBean.id)
    /// Value: 成就状态(0未达成 1达成未解锁 2已解锁)
    /// 未达成的成就不会存入字典(节省空间)
    /// </summary>
    public Dictionary<long, int> achievementStates = new Dictionary<long, int>();

    #endregion

    #region 统计数据(累计)

    /// <summary>
    /// 累计击杀生物数量(所有正式战斗模式)
    /// </summary>
    public long totalKillCount;

    /// <summary>
    /// 累计征服模式通关次数(按难度分别统计)
    /// Key: 难度等级(1~10)
    /// Value: 通关次数
    /// </summary>
    public Dictionary<int, long> conquerCompleteCountByLevel = new Dictionary<int, long>();

    #endregion

    #region 成就状态查询

    /// <summary>
    /// 获取成就状态
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    public AchievementStateEnum GetAchievementState(long achievementId)
    {
        if (achievementStates.TryGetValue(achievementId, out int state))
        {
            return (AchievementStateEnum)state;
        }
        return AchievementStateEnum.NotReached;
    }

    /// <summary>
    /// 设置成就状态
    /// </summary>
    public void SetAchievementState(long achievementId, AchievementStateEnum state)
    {
        if (state == AchievementStateEnum.NotReached)
        {
            achievementStates.Remove(achievementId);
        }
        else
        {
            achievementStates[achievementId] = (int)state;
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.Achievement_StateChange, achievementId);
    }

    #endregion

    #region 统计数据-击杀

    /// <summary>
    /// 增加击杀生物数
    /// </summary>
    public void AddKillCount(long delta = 1)
    {
        if (delta <= 0) return;
        totalKillCount += delta;
    }

    /// <summary>
    /// 获取累计击杀数
    /// </summary>
    public long GetTotalKillCount()
    {
        return totalKillCount;
    }

    #endregion

    #region 统计数据-征服通关

    /// <summary>
    /// 增加征服模式通关次数
    /// </summary>
    public void AddConquerCompleteCount(int difficultyLevel, long delta = 1)
    {
        if (delta <= 0) return;
        if (conquerCompleteCountByLevel.TryGetValue(difficultyLevel, out long curr))
        {
            conquerCompleteCountByLevel[difficultyLevel] = curr + delta;
        }
        else
        {
            conquerCompleteCountByLevel[difficultyLevel] = delta;
        }
    }

    /// <summary>
    /// 获取指定难度的征服通关次数
    /// </summary>
    public long GetConquerCompleteCount(int difficultyLevel)
    {
        if (conquerCompleteCountByLevel.TryGetValue(difficultyLevel, out long count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// 获取征服模式总通关次数(所有难度合计)
    /// </summary>
    public long GetTotalConquerCompleteCount()
    {
        long total = 0;
        foreach (var item in conquerCompleteCountByLevel)
        {
            total += item.Value;
        }
        return total;
    }

    #endregion
}
