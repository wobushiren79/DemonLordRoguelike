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
    /// 获取敌人池数组(普通取 enemy_ids / BOSS 取 enemy_boss_ids, &amp; 分隔; 结果缓存)
    /// </summary>
    /// <param name="isBoss">true=取BOSS池</param>
    /// <returns>敌人 npcInfoId 数组</returns>
    public long[] GetEnemyIdArray(bool isBoss)
    {
        if (isBoss)
        {
            if (emenyBossIds == null)
                emenyBossIds = enemy_boss_ids.SplitForArrayLong('&');
            return emenyBossIds;
        }
        if (emenyIds == null)
            emenyIds = enemy_ids.SplitForArrayLong('&');
        return emenyIds;
    }

    /// <summary>
    /// 获取随机的敌人种类数量(解析 enemy_type_num / enemy_boss_type_num, 支持单值"x"或区间"x-y")
    /// 未配置(留空)返回0, 表示保持原逻辑: 每次出怪从整池全随机
    /// </summary>
    /// <param name="isBoss">true=取BOSS种类数量(enemy_boss_type_num)</param>
    /// <returns>本次冻结的敌人种类数量; 0=未配置(全随机)</returns>
    public int GetRandomEnemyTypeNum(bool isBoss)
    {
        string value = isBoss ? enemy_boss_type_num : enemy_type_num;
        if (value.IsNull())
            return 0;
        return ParseRandomRange(value, 0);
    }

    /// <summary>
    /// 按配置随出本难度的敌人种类集合(传送门创建时调用一次并冻结, 之后重新挑战/切难度均沿用):
    /// 未配置种类数量返回空列表(出怪时从整池全随机); 配置后从池中随机取不重复的 N 种(N 由区间随机), N 不小于池大小时取全池
    /// </summary>
    /// <param name="isBoss">true=取BOSS种类(池为 enemy_boss_ids)</param>
    /// <returns>冻结的敌人种类列表; 空列表=未配置(全随机)</returns>
    public List<long> GetRandomEnemyTypeIdList(bool isBoss)
    {
        List<long> result = new List<long>();
        int typeNum = GetRandomEnemyTypeNum(isBoss);
        if (typeNum <= 0)
            return result;
        long[] pool = GetEnemyIdArray(isBoss);
        if (pool == null || pool.Length == 0)
            return result;
        //池内去重(同一种敌人可能被重复配置), 同时避免洗牌后取到重复种类
        List<long> distinctPool = new List<long>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (!distinctPool.Contains(pool[i]))
                distinctPool.Add(pool[i]);
        }
        if (typeNum >= distinctPool.Count)
            return distinctPool;
        //洗牌后取前 N 个: 保证种类不重复且随机
        for (int i = distinctPool.Count - 1; i > 0; i--)
        {
            int index = UnityEngine.Random.Range(0, i + 1);
            long temp = distinctPool[i];
            distinctPool[i] = distinctPool[index];
            distinctPool[index] = temp;
        }
        for (int i = 0; i < typeNum; i++)
            result.Add(distinctPool[i]);
        return result;
    }

    /// <summary>
    /// 按 attack_boss_num 随出本难度的BOSS出场阵容(每个元素=一只BOSS的 npcId, 元素个数=BOSS数量):
    /// 每只BOSS从 bossTypeIdList(冻结的BOSS种类) 中随机取; bossTypeIdList 为空时从 enemy_boss_ids 整池随机。
    /// 世界生成时调用一次并冻结, 战斗出怪与传送门弹窗展示(去重后的种类)共用同一份, 保证「预览=实际出场」
    /// </summary>
    /// <param name="bossTypeIdList">本难度冻结的BOSS种类(空/null=从 enemy_boss_ids 整池随机)</param>
    /// <returns>BOSS出场阵容; BOSS数量<=0 或池为空时返回空列表</returns>
    public List<long> GetRandomEnemyBossSpawnIdList(List<long> bossTypeIdList)
    {
        List<long> result = new List<long>();
        int bossNum = GetRandomBossNum();
        if (bossNum <= 0)
            return result;
        //种类池为空(未配置种类数量)时用整池; 池本身为空属配置错误, 直接不出BOSS
        List<long> randomPool = bossTypeIdList;
        if (randomPool == null || randomPool.Count == 0)
        {
            long[] bossPool = GetEnemyIdArray(true);
            if (bossPool == null || bossPool.Length == 0)
                return result;
            randomPool = new List<long>(bossPool);
        }
        for (int i = 0; i < bossNum; i++)
        {
            long bossId = randomPool.GetRandomData();
            if (bossId > 0)
                result.Add(bossId);
        }
        return result;
    }

    /// <summary>
    /// 本难度是否会刷出BOSS(attack_boss_num 配置了大于0的数量; 留空/0/区间上限为0 均视为不刷BOSS)
    /// 注: 传送门弹窗的BOSS展示已改用世界生成时冻结的出场阵容(GameWorldInfoRandomBean.GetDifficultyEnemyBossSpawnIdList),
    /// 该方法仅保留做配置层面的快速判定
    /// </summary>
    /// <returns>true=会刷BOSS</returns>
    public bool HasBossSpawn()
    {
        if (attack_boss_num.IsNull())
            return false;
        string value = attack_boss_num.Trim();
        int splitIndex = value.IndexOf('-');
        if (splitIndex < 0)
            return int.TryParse(value, out int single) && single > 0;
        return int.TryParse(value.Substring(splitIndex + 1).Trim(), out int max) && max > 0;
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
    /// 获取该难度的背景色(解析 bg_color 字段; 为空或解析失败返回白色)
    /// 注意: bg_color 字段需在 Excel 源表新增列后, 经 Unity 配置导出工具重新生成 Bean 才会存在
    /// </summary>
    public UnityEngine.Color GetBGColor()
    {
        if (bg_color.IsNull())
            return UnityEngine.Color.white;
        return ColorUtil.ParseHtmlString(bg_color);
    }

    /// <summary>
    /// 获取该难度的通关声望奖励(reward_reputation, 完整通关征服时发放给玩家的声望数量)
    /// 注意: reward_reputation 字段需在 Excel 源表新增列后, 经 Unity 配置导出工具重新生成 Bean 才会存在
    /// </summary>
    public int GetRewardReputation()
    {
        return reward_reputation;
    }

    /// <summary>
    /// 获取当前关卡普通敌人的累计强度倍率(HP/护甲/攻击力)
    /// 公式: attack_intensity_baserate × attack_intensity_addrate^(currentFightNum-1)
    /// 基础倍率 attack_intensity_baserate 为该难度每关恒定乘区(第1关也生效), 每关倍率 attack_intensity_addrate 第1关为1之后逐关相乘;
    /// 两者非法(≤0)时均按 1 处理
    /// </summary>
    /// <param name="currentFightNum">当前关卡数(从 1 开始)</param>
    public float GetCurrentIntensityRate(int currentFightNum)
    {
        float baseRate = attack_intensity_baserate;
        if (baseRate <= 0f)
            baseRate = 1f;
        float rate = attack_intensity_addrate;
        if (rate <= 0f)
            rate = 1f;
        if (currentFightNum <= 1 || rate == 1f)
            return baseRate;
        return baseRate * UnityEngine.Mathf.Pow(rate, currentFightNum - 1);
    }

    /// <summary>
    /// 获取随机魔晶奖励数量(解析 reward_crystal 字段, 与其它区间字段同格式: 单值"x"或区间"x-y"; 解析失败返回0)
    /// 单值"200"按 200 固定发放; 区间"100-200"在 [100,200] 闭区间内随机
    /// 注: reward_crystal 字段需在 Excel 源表第2行类型行改为 string 后, 经 Unity 配置导出工具重新生成 Bean 才会生效
    /// </summary>
    public int GetRandomRewardCrystal()
    {
        return ParseRandomRange(reward_crystal, 0);
    }

    /// <summary>
    /// int 单值(兼容旧 int 字段未重新生成前的过渡态, 重生成后此重载不再被调用)
    /// </summary>
    private static int ParseRandomRange(int value, int defaultValue)
    {
        return value;
    }

    /// <summary>
    /// 解析 "x" 或 "x-y" 格式的字符串为一个随机整数（转调 RandomUtil.GetRandomIntByRangeString，保留本入口兼容既有调用）
    /// 单值"x"直接返回 x; 区间"x-y"返回 [x,y] 闭区间内的随机整数; 解析失败返回 defaultValue
    /// </summary>
    /// <param name="value">配置字符串</param>
    /// <param name="defaultValue">解析失败或空时的默认值</param>
    public static int ParseRandomRange(string value, int defaultValue = 0)
    {
        return RandomUtil.GetRandomIntByRangeString(value, defaultValue);
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

    /// <summary>
    /// 获取指定世界在配置表中存在的最高难度等级(无任何配置返回0)
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <returns>该世界配置存在的最高难度等级</returns>
    public static int GetMaxLevel(long worldId)
    {
        int maxLevel = 0;
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = itemData.Value;
            if (fightTypeConquerInfo.world_id == worldId && fightTypeConquerInfo.level > maxLevel)
            {
                maxLevel = fightTypeConquerInfo.level;
            }
        }
        return maxLevel;
    }
}
