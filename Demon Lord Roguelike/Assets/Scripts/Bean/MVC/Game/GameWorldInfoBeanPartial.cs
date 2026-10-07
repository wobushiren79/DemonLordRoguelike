
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
    //挑战100勇士-冻结的配置行id(FightTypeChallengeHundredInfo; 仅 gameFightType==ChallengeHundred 时有意义, 0=未冻结)
    public long challengeHundredRowId;
    //挑战100勇士-冻结的3箱通关奖励(预览=实领, 与征服 listDifficultyRandom.listReward 同契约)
    public List<ItemBean> listRewardChallengeHundred;
    //挑战100勇士-生成奖励时的装备奖励池解锁签名(池变化时 GetChallengeHundredReward 重新生成)
    public int rewardUnlockSignChallengeHundred;

    public GameWorldInfoRandomBean()
    {
        difficultyLevel = 1;
    }

    /// <summary>
    /// 随机设置游戏类型
    /// </summary>
    /// <param name="worldId">世界id</param>
    /// <param name="listExistWorld">传送门已存在的世界列表(挑战100勇士/无尽全场各只刷1个: 已存在则跳过对应模式判定; 旧存档存量多个不清理, 仅不再新增)</param>
    public void SetGameFightTypeRandom(long worldId, List<GameWorldInfoRandomBean> listExistWorld)
    {
        this.worldId = worldId;
        var gameWorldInfo = GameWorldInfoCfg.GetItemData(worldId);
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var UserUnlock = userData.GetUserUnlockData();
        //特殊模式全场唯一标记(已存在则跳过该模式判定)
        bool existChallengeHundred = false;
        bool existInfinite = false;
        if (listExistWorld != null)
        {
            for (int i = 0; i < listExistWorld.Count; i++)
            {
                GameFightTypeEnum existType = listExistWorld[i].gameFightType;
                if (existType == GameFightTypeEnum.ChallengeHundred)
                    existChallengeHundred = true;
                else if (existType == GameFightTypeEnum.Infinite)
                    existInfinite = true;
            }
        }

        //优先判定挑战100勇士出现概率(研究等级×10%): 命中且当前最高已解锁难度有匹配配置行时才生成为该模式, 否则落回原随机
        int challengeHundredShowRate = UserUnlock.GetUnlockChallengeHundredShowRate();
        if (!existChallengeHundred && challengeHundredShowRate > 0 && UnityEngine.Random.Range(0, 100) < challengeHundredShowRate)
        {
            int unlockDifficultyMax = Mathf.Max(1, UserUnlock.GetUnlockGameWorldConquerDifficultyLevel(worldId));
            //两段抽取: 先按BOSS挑战出现概率(基础10%+研究每级+10%,满级50%)判定普通/BOSS, 再在命中类型的配置行内等概率随机
            int challengeHundredBossRate = UserUnlock.GetUnlockChallengeHundredBossRate();
            FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetRandomRow(unlockDifficultyMax, challengeHundredBossRate);
            if (challengeHundredInfo != null)
            {
                gameFightType = GameFightTypeEnum.ChallengeHundred;
                SetRandomDataForChallengeHundred(challengeHundredInfo);
                return;
            }
        }

        //无尽模式判定(概率=基础10%+无尽概率研究等级×10%, 满级100%): 前提是该世界无尽已解锁(unlock_id_infinite=无尽研究起始ID, 即难度2无尽节点的解锁ID)
        if (!existInfinite && UserUnlock.CheckIsUnlock(gameWorldInfo.unlock_id_infinite) && UnityEngine.Random.Range(0, 100) < UserUnlock.GetUnlockInfiniteShowRate())
        {
            gameFightType = GameFightTypeEnum.Infinite;
            SetRandomDataForInfinite();
            return;
        }

        //落回征服(默认模式)
        gameFightType = GameFightTypeEnum.Conquer;

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
            case GameFightTypeEnum.ChallengeHundred:
                //防御性分支: 正常由 SetGameFightTypeRandom 直接调 SetRandomDataForChallengeHundred; 走到这里说明只有冻结行id(如旧数据重放), 按行id找回配置
                var challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
                if (challengeHundredInfo != null)
                {
                    SetRandomDataForChallengeHundred(challengeHundredInfo);
                }
                else
                {
                    LogUtil.LogError($"初始化挑战100勇士模式失败 worldId:{worldId} challengeHundredRowId:{challengeHundredRowId}");
                }
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
    /// (FightBeanForConquer/FightBeanForInfinite 与气泡均直接读取这些字段, 故切换难度后必须同步才能反映新难度)
    /// </summary>
    /// <param name="targetDifficultyLevel">目标难度等级</param>
    public void SetDifficultyLevel(int targetDifficultyLevel)
    {
        difficultyLevel = targetDifficultyLevel;
        if (gameFightType == GameFightTypeEnum.Conquer)
        {
            GameWorldDifficultyRandomBean difficultyRandom = GetDifficultyRandom(targetDifficultyLevel);
            if (difficultyRandom == null)
                return;
            roadNum = difficultyRandom.roadNum;
            roadLength = difficultyRandom.roadLength;
            fightNum = difficultyRandom.fightNum;
        }
        else if (gameFightType == GameFightTypeEnum.Infinite)
        {
            //无尽模式: 仅同步该难度预生成的道路数/道路长度(无关卡数/奖励); 不走 GetDifficultyRandom 懒生成(那会连带生成征服通关奖励), 未预生成的难度保留当前值
            GameWorldDifficultyRandomBean difficultyRandom = listDifficultyRandom?.Find(item => item.difficultyLevel == targetDifficultyLevel);
            if (difficultyRandom == null)
                return;
            roadNum = difficultyRandom.roadNum;
            roadLength = difficultyRandom.roadLength;
        }
        //其余模式(挑战100勇士)无难度概念, 直接返回
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
    /// 设置无尽模式数据: 按无尽已解锁难度(2~已解锁最高)逐难度预生成道路数/道路长度(取自同难度征服行 road_num/road_length 区间, 切换难度直接取用保证恒定);
    /// 默认难度=最高已解锁无尽难度; 无尽无关卡数/通关奖励概念(fightNum占位1, listReward留空)
    /// </summary>
    public void SetRandomDataForInfinite()
    {
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        //已解锁的最高无尽难度(默认难度); 无尽无难度1, 至少为2
        int unlockInfiniteMax = userUnlock.GetUnlockInfiniteDifficultyLevel(worldId);
        if (unlockInfiniteMax < 2)
        {
            LogUtil.LogError($"初始化无尽模式失败: 无尽未解锁 worldId:{worldId}");
            unlockInfiniteMax = 2;
        }
        //预生成 2~已解锁最高 的每个无尽难度随机数据(仅道路数/道路长度; 无关卡数与通关奖励)
        listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();
        for (int level = 2; level <= unlockInfiniteMax; level++)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, level);
            if (fightTypeConquerInfo == null)
                continue;
            listDifficultyRandom.Add(new GameWorldDifficultyRandomBean()
            {
                difficultyLevel = level,
                //随机道路数量/长度(复用同难度征服行 road_num/road_length 区间)
                roadNum = fightTypeConquerInfo.GetRandomRoadNum(),
                roadLength = fightTypeConquerInfo.GetRandomRoadLength(),
                //无尽无关卡数概念, 占位1
                fightNum = 1,
            });
        }
        if (listDifficultyRandom.Count == 0)
        {
            LogUtil.LogError($"初始化无尽模式失败: 无可用征服配置 worldId:{worldId} unlockInfiniteMax:{unlockInfiniteMax}");
            return;
        }
        //默认难度取已解锁最高, 并同步当前道路数据
        SetDifficultyLevel(unlockInfiniteMax);
    }

    /// <summary>
    /// 设置挑战100勇士模式数据(单关100只怪; 冻结配置行/道路/3箱奖励, 保证气泡预览=实领=实际战斗)
    /// </summary>
    /// <param name="challengeHundredInfo">抽中的配置行</param>
    public void SetRandomDataForChallengeHundred(FightTypeChallengeHundredInfoBean challengeHundredInfo)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //冻结配置行(怪物构成/强度/奖励稀有度/刷怪时长均来自该行)
        challengeHundredRowId = challengeHundredInfo.id;
        //道路数量/长度从配置行区间随出并冻结
        roadNum = challengeHundredInfo.GetRandomRoadNum();
        roadLength = challengeHundredInfo.GetRandomRoadLength();
        //固定单关
        fightNum = 1;
        //难度仅记录当前最高已解锁难度(气泡展示用+逐难度对齐字段取档依据), 不影响强度(强度由配置行 attack_intensity_baserate 自配)
        difficultyLevel = Mathf.Max(1, userUnlock.GetUnlockGameWorldConquerDifficultyLevel(worldId));
        //预生成并冻结3箱通关奖励(装备池空=3箱全魔晶, 否则3箱全装备; BOSS挑战翻倍: 装备6件/魔晶数量x2; 逐难度字段按 difficultyLevel 取档)
        listRewardChallengeHundred = RewardSelectBean.CreateRewardListForChallengeHundred(challengeHundredInfo, difficultyLevel);
        rewardUnlockSignChallengeHundred = RewardSelectBean.GetConquerEquipPoolSign();
    }

    /// <summary>
    /// 获取挑战100勇士预生成的通关奖励(3箱); 与通关领奖同一份, 保证 UIPopupPortalDetails 预览=通关实领。
    /// 以下情况会重新生成并缓存: 尚未生成, 或解锁了新魔物掉落致装备奖励池变化(签名变化)。
    /// </summary>
    /// <returns>3箱奖励列表; 冻结行配置缺失时返回 null</returns>
    public List<ItemBean> GetChallengeHundredReward()
    {
        //当前装备奖励池的解锁签名(解锁新魔物掉落后会变化)
        int currentUnlockSign = RewardSelectBean.GetConquerEquipPoolSign();
        bool needRegenerate = listRewardChallengeHundred == null
            || listRewardChallengeHundred.Count == 0
            || rewardUnlockSignChallengeHundred != currentUnlockSign;
        if (needRegenerate)
        {
            FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
            if (challengeHundredInfo == null)
                return null;
            //按冻结难度取档重新生成(与生成时同一难度, 保证预览=实领)
            listRewardChallengeHundred = RewardSelectBean.CreateRewardListForChallengeHundred(challengeHundredInfo, difficultyLevel);
            rewardUnlockSignChallengeHundred = currentUnlockSign;
        }
        return listRewardChallengeHundred;
    }

    /// <summary>
    /// 是否为挑战100勇士-BOSS挑战(困难): 该模式且冻结配置行 challenge_type==1;
    /// 用于传送门BG/悬停气泡的深紫底色(ColorUtil.ChallengeHundredBossPurple)
    /// </summary>
    public bool IsChallengeHundredBossChallenge()
    {
        if (gameFightType != GameFightTypeEnum.ChallengeHundred)
            return false;
        var challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
        return challengeHundredInfo != null && challengeHundredInfo.IsBossChallenge();
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