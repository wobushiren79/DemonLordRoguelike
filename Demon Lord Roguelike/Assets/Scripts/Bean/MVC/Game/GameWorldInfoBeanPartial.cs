
using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
/// <summary>
/// 游戏世界随机数据
/// </summary>
public partial class GameWorldInfoRandomBean
{
    public long worldId;
    //游戏类型
    public GameFightTypeEnum gameFightType;
    //道路数量
    public int roadNum;
    //道路长度
    public int roadLength;
    //UI显示位置(用 Vector2Bean 包装,规避 Newtonsoft.Json 序列化 Vector2 时 normalized 属性递归导致的栈溢出)
    public Vector2Bean uiPosition = new Vector2Bean();
    //关卡数量
    public int fightNum;
    //图标种子
    public int iconSeed;
    //难度等级
    public int difficultyLevel;
    //各难度对应的随机数据(创建时把所有已解锁难度一次性随出来, 切换难度直接取用, 保证来回切换时同一难度的道路/关卡数恒定)
    public List<GameWorldDifficultyRandomBean> listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();

    public GameWorldInfoRandomBean()
    {
        difficultyLevel = 1;
    }

    /// <summary>
    /// 随机设置游戏类型
    /// </summary>
    public void SetGameFightTypeRandom(long worldId)
    {
        this.worldId = worldId;
        var gameWorldInfo = GameWorldInfoCfg.GetItemData(worldId);
        //随机世界模式
        List<GameFightTypeEnum> listRandomGameFightType = new List<GameFightTypeEnum>() 
        { 
            GameFightTypeEnum.Conquer //默认有征服模式
        };
        //如果无尽模式解锁了
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var UserUnlock = userData.GetUserUnlockData();
        if (UserUnlock.CheckIsUnlock(gameWorldInfo.unlock_id_infinite))
        {
            listRandomGameFightType.Add(GameFightTypeEnum.Infinite);
        }
        var randomIndex = UnityEngine.Random.Range(0, listRandomGameFightType.Count);
        gameFightType = listRandomGameFightType[randomIndex];

        //设置随机数据
        SetRandomData(gameFightType);
    }

    /// <summary>
    /// 设置随机数据
    /// </summary>
    /// <param name="gameFightTypeEnum"></param>
    public void SetRandomData(GameFightTypeEnum gameFightTypeEnum)
    {
        switch (gameFightTypeEnum)
        {
            case GameFightTypeEnum.Conquer:
                SetRandomDataForConquer();
                break;
            case GameFightTypeEnum.Infinite:
                SetRandomDataForInfinite();
                break;
        }
    }


    /// <summary>
    /// 设置征服模式数据
    /// 创建时一次性把所有已解锁难度(1~已解锁最高)的随机数据都随出来缓存到 listDifficultyRandom,
    /// 之后切换难度直接取用, 保证来回切换同一难度时道路/关卡数恒定, 且气泡/实际战斗读取的是各自难度的数据.
    /// </summary>
    public void SetRandomDataForConquer()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //已解锁的最高难度(默认难度), 至少为1
        int unlockDifficultyMax = Mathf.Max(1, userUnlock.GetUnlockGameWorldConquerDifficultyLevel(worldId));
        //预生成 1~已解锁最高 的每个难度随机数据
        listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();
        for (int level = 1; level <= unlockDifficultyMax; level++)
        {
            GameWorldDifficultyRandomBean difficultyRandom = CreateDifficultyRandom(level);
            if (difficultyRandom != null)
                listDifficultyRandom.Add(difficultyRandom);
        }
        if (listDifficultyRandom.Count == 0)
        {
            LogUtil.LogError($"初始化征服游戏模式失败 worldId:{worldId} unlockDifficultyMax:{unlockDifficultyMax}");
            return;
        }
        //默认难度取已解锁最高, 并同步当前道路/关卡数据
        SetDifficultyLevel(unlockDifficultyMax);
    }

    /// <summary>
    /// 切换当前难度等级, 并把当前道路数/道路长度/关卡数同步为该难度预生成的随机数据
    /// (FightBeanForConquer 与气泡均直接读取这些字段, 故切换难度后必须同步才能反映新难度)
    /// </summary>
    /// <param name="targetDifficultyLevel">目标难度等级</param>
    public void SetDifficultyLevel(int targetDifficultyLevel)
    {
        difficultyLevel = targetDifficultyLevel;
        //仅征服模式按难度同步道路/关卡数据; 无尽模式无难度概念, 保留 SetRandomDataForInfinite 随出的字段值
        if (gameFightType != GameFightTypeEnum.Conquer)
            return;
        GameWorldDifficultyRandomBean difficultyRandom = GetDifficultyRandom(targetDifficultyLevel);
        if (difficultyRandom == null)
            return;
        roadNum = difficultyRandom.roadNum;
        roadLength = difficultyRandom.roadLength;
        fightNum = difficultyRandom.fightNum;
    }

    /// <summary>
    /// 获取指定难度的随机数据; 若尚未生成(老存档/未解锁仅预览的难度)则懒生成并缓存, 保证同一难度数值稳定
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>该难度的随机数据, 无对应征服配置时返回 null</returns>
    public GameWorldDifficultyRandomBean GetDifficultyRandom(int targetDifficultyLevel)
    {
        if (listDifficultyRandom == null)
            listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();
        GameWorldDifficultyRandomBean difficultyRandom = listDifficultyRandom.Find(item => item.difficultyLevel == targetDifficultyLevel);
        if (difficultyRandom == null)
        {
            difficultyRandom = CreateDifficultyRandom(targetDifficultyLevel);
            if (difficultyRandom != null)
                listDifficultyRandom.Add(difficultyRandom);
        }
        return difficultyRandom;
    }

    /// <summary>
    /// 按征服配置生成单个难度的随机数据(道路数/道路长度/关卡数, 均支持单值"x"或区间"x-y")
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>该难度的随机数据, 无对应征服配置时返回 null</returns>
    protected GameWorldDifficultyRandomBean CreateDifficultyRandom(int targetDifficultyLevel)
    {
        FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, targetDifficultyLevel);
        if (fightTypeConquerInfo == null)
            return null;
        return new GameWorldDifficultyRandomBean()
        {
            difficultyLevel = targetDifficultyLevel,
            //随机道路数量(road_num)
            roadNum = fightTypeConquerInfo.GetRandomRoadNum(),
            //随机道路长度(road_length)
            roadLength = fightTypeConquerInfo.GetRandomRoadLength(),
            //随机关卡数量(fight_num)
            fightNum = fightTypeConquerInfo.GetRandomFightNum(),
            //预生成本难度的通关奖励(与通关领奖同规则), 一次性随出并冻结, 保证 UIPopupPortalDetails 预览=通关实领
            listReward = RewardSelectBean.CreateRewardListForConquer(fightTypeConquerInfo),
            //记录生成时的装备奖励池解锁签名, 供解锁新魔物掉落后判定是否需重新生成
            rewardUnlockSign = RewardSelectBean.GetConquerEquipPoolSign(),
        };
    }

    /// <summary>
    /// 获取指定难度预生成的通关奖励(装备+魔晶); 与通关领奖同规则, 保证 UIPopupPortalDetails 预览=通关实领。
    /// 以下情况会重新生成并缓存: 尚未生成(老存档), 或解锁了新魔物掉落致装备奖励池变化(签名变化)。
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>该难度的奖励列表; 无对应征服配置时返回 null</returns>
    public List<ItemBean> GetDifficultyReward(int targetDifficultyLevel)
    {
        GameWorldDifficultyRandomBean difficultyRandom = GetDifficultyRandom(targetDifficultyLevel);
        if (difficultyRandom == null)
            return null;
        //当前装备奖励池的解锁签名(解锁新魔物掉落后会变化)
        int currentUnlockSign = RewardSelectBean.GetConquerEquipPoolSign();
        //无预生成奖励(老存档), 或解锁池已变化(解锁了新魔物掉落) → 重新生成并刷新签名
        bool needRegenerate = difficultyRandom.listReward == null
            || difficultyRandom.listReward.Count == 0
            || difficultyRandom.rewardUnlockSign != currentUnlockSign;
        if (needRegenerate)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, targetDifficultyLevel);
            if (fightTypeConquerInfo != null)
            {
                difficultyRandom.listReward = RewardSelectBean.CreateRewardListForConquer(fightTypeConquerInfo);
                difficultyRandom.rewardUnlockSign = currentUnlockSign;
            }
        }
        return difficultyRandom.listReward;
    }

    /// <summary>
    /// 设置无限模式数据
    /// </summary>
    public void SetRandomDataForInfinite()
    {
        int roadNumRandom = UnityEngine.Random.Range(1, 7);
        roadNum = roadNumRandom;
    }
}

[Serializable]
/// <summary>
/// 游戏世界-单个难度的随机数据(征服模式各难度的道路数/道路长度/关卡数, 创建传送门时一次性随出并缓存)
/// </summary>
public class GameWorldDifficultyRandomBean
{
    //难度等级
    public int difficultyLevel;
    //道路数量
    public int roadNum;
    //道路长度
    public int roadLength;
    //关卡数量
    public int fightNum;
    //本难度预生成的通关奖励(装备+魔晶); 创建传送门时一次性随出并冻结, 保证 UIPopupPortalDetails 预览=通关实领
    public List<ItemBean> listReward = new List<ItemBean>();
    //生成奖励时的"装备奖励池解锁签名"(可生成装备的已解锁生物模型数量); 解锁新魔物掉落后此值变化, GetDifficultyReward 据此重新生成奖励
    public int rewardUnlockSign;
}