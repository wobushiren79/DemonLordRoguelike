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
    /// 成就解锁(已领取)字典
    /// Key: 成就ID(对应 AchievementInfoBean.id)
    /// Value: 固定为 (int)AchievementStateEnum.Unlocked(=2)
    /// 仅持久化"已领取"状态; "未达成/达成未领取"为运行时根据统计数据实时计算, 不入存档(节省空间且无需运行期判定)
    /// 兼容旧存档: 旧版本可能写入过 1(达成未领取), 读取时一律按 IsAchievementUnlocked 只认 ==2, 残留的 1 视为未领取并被重新计算
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

    #region 成就解锁(已领取)状态

    /// <summary>
    /// 该成就是否已解锁(已领取奖励)
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    public bool IsAchievementUnlocked(long achievementId)
    {
        return achievementStates.TryGetValue(achievementId, out int state)
               && state == (int)AchievementStateEnum.Unlocked;
    }

    /// <summary>
    /// 标记成就为已解锁(已领取奖励) —— 仅在领奖成功时调用
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    public void SetAchievementUnlocked(long achievementId)
    {
        achievementStates[achievementId] = (int)AchievementStateEnum.Unlocked;
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
