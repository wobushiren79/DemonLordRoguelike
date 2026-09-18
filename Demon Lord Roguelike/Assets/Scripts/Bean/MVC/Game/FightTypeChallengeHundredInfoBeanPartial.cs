using System;
using System.Collections.Generic;
public partial class FightTypeChallengeHundredInfoBean
{
    protected long[] enemyIdList;
    protected int[] difficultyLevelList;
    protected long[] fightSceneIdList;

    /// <summary>
    /// 获取进攻敌人ID列表(enemy_ids用,分割; 生成100只怪时每只独立从该列表随机抽取)
    /// </summary>
    public long[] GetEnemyIdList()
    {
        if (enemyIdList == null)
            enemyIdList = enemy_ids.SplitForArrayLong(',');
        return enemyIdList;
    }

    /// <summary>
    /// 随机获取一个进攻敌人ID(每只怪独立随机, 总量100)
    /// </summary>
    public long GetRandomEnemyId()
    {
        return GetEnemyIdList().GetRandomData();
    }

    /// <summary>
    /// 获取可抽中难度列表(difficulty_levels用,分割)
    /// </summary>
    public int[] GetDifficultyLevelList()
    {
        if (difficultyLevelList == null)
            difficultyLevelList = difficulty_levels.SplitForArrayInt(',');
        return difficultyLevelList;
    }

    /// <summary>
    /// 本行是否可被指定难度抽中(世界最高已解锁难度在 difficulty_levels 列内)
    /// </summary>
    /// <param name="difficultyLevel">世界当前最高已解锁难度</param>
    public bool IsMatchDifficulty(int difficultyLevel)
    {
        int[] levelList = GetDifficultyLevelList();
        for (int i = 0; i < levelList.Length; i++)
        {
            if (levelList[i] == difficultyLevel)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取随机战斗场景(fight_scene_ids用,分割; 随机其一)
    /// </summary>
    public long GetRandomFightScene()
    {
        if (fightSceneIdList == null)
            fightSceneIdList = fight_scene_ids.SplitForArrayLong(',');
        return fightSceneIdList.GetRandomData();
    }

    /// <summary>
    /// 获取随机道路数量(解析 road_num 字段, 支持单值"x"或区间"x-y")
    /// </summary>
    public int GetRandomRoadNum()
    {
        return RandomUtil.GetRandomIntByRangeString(road_num, 1);
    }

    /// <summary>
    /// 获取随机道路长度(解析 road_length 字段, 支持单值"x"或区间"x-y")
    /// </summary>
    public int GetRandomRoadLength()
    {
        return RandomUtil.GetRandomIntByRangeString(road_length, 1);
    }

    /// <summary>
    /// 获取随机魔晶奖励数量(解析 reward_crystal 字段: 单值"x"固定或区间"x-y"随机; 解析失败返回0)
    /// </summary>
    public int GetRandomRewardCrystal()
    {
        return RandomUtil.GetRandomIntByRangeString(reward_crystal, 0);
    }

    /// <summary>
    /// 获取进攻敌人强度倍率(attack_intensity_baserate; 敌人HP/护甲/攻击力×该值; ≤0按1处理)
    /// </summary>
    public float GetIntensityRate()
    {
        if (attack_intensity_baserate <= 0f)
            return 1f;
        return attack_intensity_baserate;
    }
}
public partial class FightTypeChallengeHundredInfoCfg
{
    /// <summary>
    /// 获取指定难度可抽中的全部配置行(difficulty_levels 含该难度的行)
    /// </summary>
    /// <param name="unlockDifficultyMax">世界当前最高已解锁难度</param>
    /// <returns>匹配的配置行列表(无匹配返回空列表)</returns>
    public static List<FightTypeChallengeHundredInfoBean> GetMatchRows(int unlockDifficultyMax)
    {
        List<FightTypeChallengeHundredInfoBean> matchList = new List<FightTypeChallengeHundredInfoBean>();
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            if (itemData.Value.IsMatchDifficulty(unlockDifficultyMax))
                matchList.Add(itemData.Value);
        }
        return matchList;
    }

    /// <summary>
    /// 从指定难度可抽中的配置行中随机一行(无匹配返回null, 调用方需落回默认模式)
    /// </summary>
    /// <param name="unlockDifficultyMax">世界当前最高已解锁难度</param>
    /// <returns>随机匹配行; 无匹配返回null</returns>
    public static FightTypeChallengeHundredInfoBean GetRandomRow(int unlockDifficultyMax)
    {
        var matchList = GetMatchRows(unlockDifficultyMax);
        if (matchList.Count == 0)
            return null;
        return matchList.GetRandomData();
    }
}
