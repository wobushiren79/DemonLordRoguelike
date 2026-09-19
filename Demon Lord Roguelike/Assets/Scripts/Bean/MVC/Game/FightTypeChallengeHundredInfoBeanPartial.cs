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
    /// 是否BOSS挑战(challenge_type==1; BOSS挑战通关宝箱奖励翻倍: 装备件数x2/魔晶数量x2)
    /// </summary>
    public bool IsBossChallenge()
    {
        return challenge_type == 1;
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

    #region 逐难度对齐字段取值

    //逐难度对齐字段约定: 单值=全部难度共用; 逗号分隔多值=与 difficulty_levels 等长, 按难度下标取对应档

    protected string[] intensityRateValueList;
    protected string[] dropCrystalValueList;
    protected string[] rewardCrystalValueList;
    protected string[] rewardEquipRarityValueList;
    protected string[] rewardExpValueList;

    /// <summary>
    /// 取难度在 difficulty_levels 中的对齐下标; 不在列时取距离最近的档(理论上行被抽中即含该难度, 此为防御钳位)
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    protected int GetDifficultyValueIndex(int difficultyLevel)
    {
        int[] levelList = GetDifficultyLevelList();
        if (levelList.Length == 0)
            return 0;
        int bestIndex = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < levelList.Length; i++)
        {
            int distance = Math.Abs(levelList[i] - difficultyLevel);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
                if (distance == 0)
                    break;
            }
        }
        return bestIndex;
    }

    /// <summary>
    /// 从逐难度对齐字段取值: 单值=全部难度共用; 多值按难度对齐下标取档, 下标越界钳到末位
    /// </summary>
    protected string GetValueForDifficulty(string[] valueList, int difficultyLevel)
    {
        if (valueList == null || valueList.Length == 0)
            return "";
        if (valueList.Length == 1)
            return valueList[0];
        int index = GetDifficultyValueIndex(difficultyLevel);
        if (index >= valueList.Length)
            index = valueList.Length - 1;
        return valueList[index];
    }

    /// <summary>
    /// 获取指定难度下的进攻敌人强度倍率(attack_intensity_baserate 逐难度对齐值; 解析失败或≤0按1处理)
    /// </summary>
    /// <param name="difficultyLevel">难度等级(冻结于传送门随机数据 difficultyLevel)</param>
    public float GetIntensityRate(int difficultyLevel)
    {
        if (intensityRateValueList == null)
            intensityRateValueList = (attack_intensity_baserate ?? "").Split(',');
        string value = GetValueForDifficulty(intensityRateValueList, difficultyLevel);
        if (!float.TryParse(value, out float rate) || rate <= 0f)
            return 1f;
        return rate;
    }

    /// <summary>
    /// 获取指定难度下的击杀掉落魔晶(drop_crystal 逐难度对齐值; 解析失败按0)
    /// </summary>
    public int GetDropCrystal(int difficultyLevel)
    {
        if (dropCrystalValueList == null)
            dropCrystalValueList = (drop_crystal ?? "").Split(',');
        string value = GetValueForDifficulty(dropCrystalValueList, difficultyLevel);
        if (!int.TryParse(value, out int dropCrystal))
            return 0;
        return dropCrystal;
    }

    /// <summary>
    /// 获取指定难度下的随机魔晶奖励数量(reward_crystal 逐难度对齐值, 单档仍为"x"固定或"x-y"区间随机; 解析失败返回0)
    /// </summary>
    public int GetRandomRewardCrystal(int difficultyLevel)
    {
        if (rewardCrystalValueList == null)
            rewardCrystalValueList = (reward_crystal ?? "").Split(',');
        string value = GetValueForDifficulty(rewardCrystalValueList, difficultyLevel);
        return RandomUtil.GetRandomIntByRangeString(value, 0);
    }

    /// <summary>
    /// 获取指定难度下的装备稀有度(reward_equip_rarity 逐难度对齐值; 解析失败按1, 避免稀有度配置取空)
    /// </summary>
    public int GetRewardEquipRarity(int difficultyLevel)
    {
        if (rewardEquipRarityValueList == null)
            rewardEquipRarityValueList = (reward_equip_rarity ?? "").Split(',');
        string value = GetValueForDifficulty(rewardEquipRarityValueList, difficultyLevel);
        if (!int.TryParse(value, out int rarity) || rarity <= 0)
            return 1;
        return rarity;
    }

    /// <summary>
    /// 获取指定难度下的通关经验(reward_exp 逐难度对齐值; 解析失败按0)
    /// </summary>
    public int GetRewardExp(int difficultyLevel)
    {
        if (rewardExpValueList == null)
            rewardExpValueList = (reward_exp ?? "").Split(',');
        string value = GetValueForDifficulty(rewardExpValueList, difficultyLevel);
        if (!int.TryParse(value, out int exp))
            return 0;
        return exp;
    }

    #endregion
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
