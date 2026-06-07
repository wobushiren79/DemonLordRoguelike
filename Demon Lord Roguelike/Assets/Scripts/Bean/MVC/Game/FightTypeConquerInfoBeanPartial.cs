using System;
using System.Collections.Generic;
public partial class FightTypeConquerInfoBean
{
    protected long[] fightSceneIds;
    protected long[] fightSceneBossIds;
    protected long[] emenyIds;
    protected long[] emenyBossIds;

    /// <summary>
    /// 获取随机战斗场景
    /// </summary>
    public long GetRandomFightScene(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = fightSceneBossIds;
        }
        else
        {
            targetIds = fightSceneIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = fight_scene_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = fight_scene_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
    }

    /// <summary>
    /// 获取战斗敌人数据
    /// </summary>
    /// <param name="isBoss"></param>
    public long GetRandomEmenyId(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = emenyBossIds;
        }
        else
        {
            targetIds = emenyIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = enemy_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = enemy_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
    }

    /// <summary>
    /// 获取随机关卡次数(解析 fight_num 字段, 支持单值"x"或区间"x-y")
    /// </summary>
    public int GetRandomFightNum()
    {
        return ParseRandomRange(fight_num, 1);
    }

    /// <summary>
    /// 获取随机道路数量(解析 road_num 字段, 支持单值"x"或区间"x-y")
    /// </summary>
    public int GetRandomRoadNum()
    {
        return ParseRandomRange(road_num, 1);
    }

    /// <summary>
    /// 获取随机道路长度(解析 road_length 字段, 支持单值"x"或区间"x-y")
    /// </summary>
    public int GetRandomRoadLength()
    {
        return ParseRandomRange(road_length, 1);
    }

    /// <summary>
    /// 获取随机BOSS数量(解析 attack_boss_num 字段, 支持单值"x"或区间"x-y", 无配置返回0)
    /// </summary>
    public int GetRandomBossNum()
    {
        return ParseRandomRange(attack_boss_num, 0);
    }

    /// <summary>
    /// 获取当前关卡普通敌人的累计强度倍率(HP/护甲/攻击力)
    /// 以 attack_intensity_addrate 为每关倍率, 第 1 关为 1, 之后逐关相乘: rate^(currentFightNum-1)
    /// attack_intensity_addrate 非法(≤0)时按 1 处理
    /// </summary>
    /// <param name="currentFightNum">当前关卡数(从 1 开始)</param>
    public float GetCurrentIntensityRate(int currentFightNum)
    {
        float rate = attack_intensity_addrate;
        if (rate <= 0f)
            rate = 1f;
        if (currentFightNum <= 1 || rate == 1f)
            return 1f;
        return UnityEngine.Mathf.Pow(rate, currentFightNum - 1);
    }

    /// <summary>
    /// 解析 "x" 或 "x-y" 格式的字符串为一个随机整数
    /// 单值"x"直接返回 x; 区间"x-y"返回 [x,y] 闭区间内的随机整数; 解析失败返回 defaultValue
    /// </summary>
    /// <param name="value">配置字符串</param>
    /// <param name="defaultValue">解析失败或空时的默认值</param>
    public static int ParseRandomRange(string value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        value = value.Trim();
        int splitIndex = value.IndexOf('-');
        //没有"-"则视为单个数值
        if (splitIndex < 0)
        {
            return int.TryParse(value, out int single) ? single : defaultValue;
        }
        //区间格式 x-y
        string minStr = value.Substring(0, splitIndex).Trim();
        string maxStr = value.Substring(splitIndex + 1).Trim();
        if (int.TryParse(minStr, out int min) && int.TryParse(maxStr, out int max))
        {
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }
            return UnityEngine.Random.Range(min, max + 1);
        }
        return defaultValue;
    }
}
public partial class FightTypeConquerInfoCfg
{

    public static FightTypeConquerInfoBean GetItemData(long worldId, int difficultyLevel)
    {
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = itemData.Value;
            if (fightTypeConquerInfo.world_id == worldId && fightTypeConquerInfo.level == difficultyLevel)
            {
                return fightTypeConquerInfo;
            }
        }
        return null;
    }
}
