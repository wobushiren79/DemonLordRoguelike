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
    /// 成就"已领取等级数"字典(单行多级模型)
    /// Key: 成就ID(对应 AchievementInfoBean.id, 每个可升级成就一条)
    /// Value: 已领取的等级数 N(0=一级都没领, 1=已领第1级, ... 达到等级总数=整族完成)
    /// 仅持久化"已领取到第几级"; 某级是否"达成可领"由统计数据 vs 该级目标值实时计算, 不入存档。
    /// 等级门控: 只能领取"已领取数+1"那一级, 不能跳级。
    /// 注: 旧版本曾用 achievementStates(按每档id存Unlocked=2), 数据模型已变, 旧档该字段被忽略、领取进度从0重算。
    /// </summary>
    public Dictionary<long, int> achievementLevelClaimed = new Dictionary<long, int>();

    #endregion

    #region 统计数据(累计)

    /// <summary>
    /// 累计击杀生物数量(所有正式战斗模式)
    /// </summary>
    public long totalKillCount;

    /// <summary>
    /// 累计征服模式通关次数(按世界×难度分别统计)
    /// 外层 Key: 世界id(对应 GameWorldInfoBean.id)
    /// 内层 Key: 难度等级(1~10)
    /// 内层 Value: 通关次数
    /// 旧存档字段 conquerCompleteCountByLevel(仅按难度)已废弃, 读取旧档时该统计会从0重新累计
    /// </summary>
    public Dictionary<long, Dictionary<int, long>> conquerCompleteCountByWorldLevel = new Dictionary<long, Dictionary<int, long>>();

    #endregion

    #region 成就已领取等级

    /// <summary>
    /// 获取该成就已领取的等级数(0=尚未领取任何等级)
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    public int GetClaimedLevelCount(long achievementId)
    {
        return achievementLevelClaimed.TryGetValue(achievementId, out int count) ? count : 0;
    }

    /// <summary>
    /// 设置该成就已领取的等级数 —— 仅在领奖成功时调用
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    /// <param name="count">已领取等级数</param>
    public void SetClaimedLevelCount(long achievementId, int count)
    {
        achievementLevelClaimed[achievementId] = count;
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
    /// 增加征服模式通关次数(按世界×难度)
    /// </summary>
    /// <param name="worldId">世界id</param>
    /// <param name="difficultyLevel">难度等级</param>
    /// <param name="delta">增量</param>
    public void AddConquerCompleteCount(long worldId, int difficultyLevel, long delta = 1)
    {
        if (delta <= 0) return;
        if (!conquerCompleteCountByWorldLevel.TryGetValue(worldId, out var byLevel))
        {
            byLevel = new Dictionary<int, long>();
            conquerCompleteCountByWorldLevel[worldId] = byLevel;
        }
        if (byLevel.TryGetValue(difficultyLevel, out long curr))
        {
            byLevel[difficultyLevel] = curr + delta;
        }
        else
        {
            byLevel[difficultyLevel] = delta;
        }
    }

    /// <summary>
    /// 获取指定世界指定难度的征服通关次数
    /// </summary>
    /// <param name="worldId">世界id</param>
    /// <param name="difficultyLevel">难度等级</param>
    public long GetConquerCompleteCount(long worldId, int difficultyLevel)
    {
        if (conquerCompleteCountByWorldLevel.TryGetValue(worldId, out var byLevel)
            && byLevel.TryGetValue(difficultyLevel, out long count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// 获取指定世界的征服通关总次数(该世界所有难度合计)
    /// </summary>
    /// <param name="worldId">世界id</param>
    public long GetConquerCompleteCountByWorld(long worldId)
    {
        long total = 0;
        if (conquerCompleteCountByWorldLevel.TryGetValue(worldId, out var byLevel))
        {
            foreach (var item in byLevel)
            {
                total += item.Value;
            }
        }
        return total;
    }

    /// <summary>
    /// 获取征服模式总通关次数(所有世界所有难度合计)
    /// </summary>
    public long GetTotalConquerCompleteCount()
    {
        long total = 0;
        foreach (var byLevel in conquerCompleteCountByWorldLevel.Values)
        {
            foreach (var item in byLevel)
            {
                total += item.Value;
            }
        }
        return total;
    }

    #endregion
}
