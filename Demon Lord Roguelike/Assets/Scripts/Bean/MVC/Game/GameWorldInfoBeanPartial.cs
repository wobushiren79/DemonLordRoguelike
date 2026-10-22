
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 游戏世界配置
/// </summary>
public partial class GameWorldInfoBean
{

}

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
    //各难度对应的随机数据(创建时把所有已解锁难度一次性随出来, 切换难度直接取用, 保证来回切换时同一难度的道路/关卡数恒定;
    //传送门创建后研究新解锁的难度在首次访问时懒补齐并冻结)
    public List<GameWorldDifficultyRandomBean> listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();
    //挑战100勇士-各难度预生成的随机数据(创建时把 1~已解锁最高难度 一次性随出并冻结, 切换难度直接取用;
    //传送门创建后研究新解锁的难度在首次访问时懒补齐并冻结)
    public List<GameWorldChallengeHundredRandomBean> listChallengeHundredRandom = new List<GameWorldChallengeHundredRandomBean>();
    //挑战100勇士-当前活动难度的冻结配置行id(FightTypeChallengeHundredInfo; 仅 gameFightType==ChallengeHundred 时有意义, 0=未冻结)
    public long challengeHundredRowId;
    //挑战100勇士-当前活动难度的冻结3箱通关奖励(预览=实领, 与征服 listDifficultyRandom.listReward 同契约)
    public List<ItemBean> listRewardChallengeHundred;
    //挑战100勇士-当前活动难度生成奖励时的装备奖励池解锁签名(池变化时 GetChallengeHundredReward 重新生成)
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
            //100勇士有独立难度链(不再跟随征服难度), 难度1默认可用
            int unlockDifficultyMax = Mathf.Max(1, UserUnlock.GetUnlockChallengeHundredDifficultyLevel(worldId));
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

        //无尽模式判定(概率=基础10%+无尽概率研究等级×10%, 满级100%): 前提是该世界至少已解锁一个无尽难度(允许跳级)
        if (!existInfinite && UserUnlock.GetUnlockInfiniteDifficultyLevel(worldId) >= 2 && UnityEngine.Random.Range(0, 100) < UserUnlock.GetUnlockInfiniteShowRate())
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
                //防御性分支: 正常由 SetGameFightTypeRandom 直接调 SetRandomDataForChallengeHundred;
                //走到这里说明只有冻结行id(如旧数据重放), 按旧存档懒重建各难度数据并保留当前难度
                gameFightType = GameFightTypeEnum.ChallengeHundred;
                EnsureChallengeHundredRandomData();
                if (listChallengeHundredRandom == null || listChallengeHundredRandom.Count == 0)
                    LogUtil.LogError($"初始化挑战100勇士模式失败 worldId:{worldId} challengeHundredRowId:{challengeHundredRowId}");
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
            //无尽模式: 仅同步该难度预生成的道路数/道路长度(无关卡数/奖励); 不走 GetDifficultyRandom(会连带生成征服通关奖励),
            //研究新解锁难度未预生成时由 GetInfiniteDifficultyRandom 懒补齐(只补道路数据)
            GameWorldDifficultyRandomBean difficultyRandom = GetInfiniteDifficultyRandom(targetDifficultyLevel);
            if (difficultyRandom == null)
                return;
            roadNum = difficultyRandom.roadNum;
            roadLength = difficultyRandom.roadLength;
        }
        else if (gameFightType == GameFightTypeEnum.ChallengeHundred)
        {
            //挑战100勇士: 同步该难度预生成的配置行/道路/奖励(战斗与本模式气泡均读这些活动字段)
            GameWorldChallengeHundredRandomBean challengeHundredRandom = GetChallengeHundredRandom(targetDifficultyLevel);
            if (challengeHundredRandom == null)
                return;
            challengeHundredRowId = challengeHundredRandom.challengeHundredRowId;
            roadNum = challengeHundredRandom.roadNum;
            roadLength = challengeHundredRandom.roadLength;
            fightNum = 1;
            listRewardChallengeHundred = challengeHundredRandom.listRewardChallengeHundred;
            rewardUnlockSignChallengeHundred = challengeHundredRandom.rewardUnlockSignChallengeHundred;
        }
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
            //冻结本难度普通关/BOSS关战斗场景(同世界重复进入场地固定, 不再每次进战斗重新随机)
            fightSceneId = fightTypeConquerInfo.GetRandomFightScene(false),
            bossFightSceneId = fightTypeConquerInfo.GetRandomFightScene(true),
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
    /// 获取本世界指定难度已冻结的战斗场景id(创建传送门时随出并冻结, 同世界重复进入场地固定);
    /// 老存档该字段缺失时按配置懒补齐并写回(补齐后同样固定), 无冻结数据(如无尽测试入口未解锁难度)时按配置随机兜底
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <param name="isBoss">是否取BOSS关场景(挑战100勇士无普通/BOSS场景之分, 忽略该参数)</param>
    /// <returns>战斗场景id; 无可用配置时返回0</returns>
    public long GetFightSceneId(int targetDifficultyLevel, bool isBoss)
    {
        if (gameFightType == GameFightTypeEnum.ChallengeHundred)
            return GetChallengeHundredFightSceneId(targetDifficultyLevel);
        //征服与无尽共用 listDifficultyRandom; 无尽走 GetInfiniteDifficultyRandom, 避免连带生成征服通关奖励
        GameWorldDifficultyRandomBean difficultyRandom = gameFightType == GameFightTypeEnum.Infinite
            ? GetInfiniteDifficultyRandom(targetDifficultyLevel)
            : GetDifficultyRandom(targetDifficultyLevel);
        if (difficultyRandom == null)
            return GetRandomFightSceneFromConfig(targetDifficultyLevel, isBoss);
        //老存档无冻结场景 → 懒补齐并写回(补齐后同世界重复进入不再变化)
        if (difficultyRandom.fightSceneId <= 0)
            difficultyRandom.fightSceneId = GetRandomFightSceneFromConfig(targetDifficultyLevel, false);
        if (difficultyRandom.bossFightSceneId <= 0)
            difficultyRandom.bossFightSceneId = GetRandomFightSceneFromConfig(targetDifficultyLevel, true);
        return isBoss ? difficultyRandom.bossFightSceneId : difficultyRandom.fightSceneId;
    }

    /// <summary>
    /// 获取挑战100勇士指定难度已冻结的战斗场景id(缺失时按该难度冻结配置行懒补齐并写回)
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>战斗场景id; 无可用配置时返回0</returns>
    protected long GetChallengeHundredFightSceneId(int targetDifficultyLevel)
    {
        GameWorldChallengeHundredRandomBean challengeHundredRandom = GetChallengeHundredRandom(targetDifficultyLevel);
        if (challengeHundredRandom == null)
        {
            //无该难度冻结数据(未解锁/测试): 按当前活动冻结行随机兜底
            FightTypeChallengeHundredInfoBean currentRow = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
            return currentRow == null ? 0 : currentRow.GetRandomFightScene();
        }
        if (challengeHundredRandom.fightSceneId <= 0)
        {
            FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRandom.challengeHundredRowId);
            if (challengeHundredInfo != null)
                challengeHundredRandom.fightSceneId = challengeHundredInfo.GetRandomFightScene();
        }
        return challengeHundredRandom.fightSceneId;
    }

    /// <summary>
    /// 按征服配置行随机一个战斗场景(不冻结; 仅用于老存档补齐与无冻结数据时兜底)
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <param name="isBoss">是否BOSS场景</param>
    /// <returns>战斗场景id; 无对应配置返回0</returns>
    protected long GetRandomFightSceneFromConfig(int targetDifficultyLevel, bool isBoss)
    {
        FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, targetDifficultyLevel);
        if (fightTypeConquerInfo == null)
            return 0;
        return fightTypeConquerInfo.GetRandomFightScene(isBoss);
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
            GameWorldDifficultyRandomBean difficultyRandom = CreateInfiniteDifficultyRandom(level);
            if (difficultyRandom != null)
                listDifficultyRandom.Add(difficultyRandom);
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
    /// 获取指定无尽难度预生成的道路数据(道路数/道路长度); 已解锁但未预生成时懒补齐并缓存
    /// (传送门创建后研究新解锁的无尽难度, 首次预览/切换时走此路径; 冻结后同一难度数据不再变化)
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>该难度的随机数据; 未解锁/无对应征服配置时返回null</returns>
    public GameWorldDifficultyRandomBean GetInfiniteDifficultyRandom(int targetDifficultyLevel)
    {
        if (listDifficultyRandom == null)
            listDifficultyRandom = new List<GameWorldDifficultyRandomBean>();
        GameWorldDifficultyRandomBean difficultyRandom = listDifficultyRandom.Find(item => item.difficultyLevel == targetDifficultyLevel);
        if (difficultyRandom != null)
            return difficultyRandom;
        //仅补齐已解锁难度(逐档判定, 允许跳级)
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        if (!userUnlock.CheckInfiniteUnlock(worldId, targetDifficultyLevel))
            return null;
        difficultyRandom = CreateInfiniteDifficultyRandom(targetDifficultyLevel);
        if (difficultyRandom != null)
            listDifficultyRandom.Add(difficultyRandom);
        return difficultyRandom;
    }

    /// <summary>
    /// 按同难度征服行的道路区间创建单个无尽难度的随机数据(仅道路数/道路长度, 无关卡数与通关奖励)
    /// </summary>
    /// <param name="targetDifficultyLevel">难度等级</param>
    /// <returns>单难度随机数据; 无对应征服配置时返回null</returns>
    protected GameWorldDifficultyRandomBean CreateInfiniteDifficultyRandom(int targetDifficultyLevel)
    {
        FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, targetDifficultyLevel);
        if (fightTypeConquerInfo == null)
            return null;
        return new GameWorldDifficultyRandomBean()
        {
            difficultyLevel = targetDifficultyLevel,
            //随机道路数量/长度(复用同难度征服行 road_num/road_length 区间)
            roadNum = fightTypeConquerInfo.GetRandomRoadNum(),
            roadLength = fightTypeConquerInfo.GetRandomRoadLength(),
            //冻结本难度战斗场景(无尽每轮都是BOSS, 取BOSS场景池; 普通场景一并冻结保持一致)
            fightSceneId = fightTypeConquerInfo.GetRandomFightScene(false),
            bossFightSceneId = fightTypeConquerInfo.GetRandomFightScene(true),
            //无尽无关卡数概念, 占位1
            fightNum = 1,
        };
    }

    /// <summary>
    /// 设置挑战100勇士模式数据(单关100只怪):
    /// 按 1~已解锁最高100勇士难度 逐难度预生成配置行/道路/3箱奖励并冻结, 默认难度取最高已解锁难度;
    /// 保证气泡预览=实领=实际战斗, 且来回切换同一难度时数据恒定。
    /// </summary>
    /// <param name="challengeHundredInfo">最高难度抽中的配置行(不匹配最高难度/为空时自动重抽)</param>
    /// <param name="preferredDifficulty">指定默认难度(测试入口用; 0=取最高已解锁; 大于研究上限时仅用于测试预生成)</param>
    public void SetRandomDataForChallengeHundred(FightTypeChallengeHundredInfoBean challengeHundredInfo, int preferredDifficulty = 0)
    {
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        int maxDifficulty = Mathf.Max(1, userUnlock.GetUnlockChallengeHundredDifficultyLevel(worldId));
        //测试入口可指定超过当前研究上限的难度(仅生成测试数据)
        if (preferredDifficulty > maxDifficulty)
            maxDifficulty = preferredDifficulty;
        int defaultDifficulty = preferredDifficulty > 0 ? preferredDifficulty : maxDifficulty;
        BuildChallengeHundredRandomList(maxDifficulty, defaultDifficulty, challengeHundredInfo);
        if (listChallengeHundredRandom == null || listChallengeHundredRandom.Count == 0)
        {
            LogUtil.LogError($"初始化挑战100勇士模式失败: 无可用配置 worldId:{worldId} maxDifficulty:{maxDifficulty}");
            return;
        }
        //默认难度取指定难度/最高已解锁难度
        SetDifficultyLevel(defaultDifficulty);
    }

    /// <summary>
    /// 旧存档兼容: 若尚未生成各难度数据, 按当前冻结行与当前难度懒重建(保留当前难度, 并补齐其他已解锁难度)
    /// </summary>
    public void EnsureChallengeHundredRandomData()
    {
        if (gameFightType != GameFightTypeEnum.ChallengeHundred)
            return;
        if (listChallengeHundredRandom != null && listChallengeHundredRandom.Count > 0)
            return;
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        int maxDifficulty = Mathf.Max(1, userUnlock.GetUnlockChallengeHundredDifficultyLevel(worldId));
        //旧存档的活动难度可能来自征服难度, 夹到新的100勇士难度范围内
        int activeDifficulty = Mathf.Clamp(difficultyLevel <= 0 ? maxDifficulty : difficultyLevel, 1, maxDifficulty);
        FightTypeChallengeHundredInfoBean currentRow = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
        BuildChallengeHundredRandomList(maxDifficulty, activeDifficulty, currentRow);
        if (listChallengeHundredRandom == null || listChallengeHundredRandom.Count == 0)
        {
            LogUtil.LogError($"重建挑战100勇士难度数据失败 worldId:{worldId} challengeHundredRowId:{challengeHundredRowId}");
            return;
        }
        SetDifficultyLevel(activeDifficulty);
    }

    /// <summary>
    /// 构建 1~maxDifficulty 每档的挑战100勇士随机数据(配置行/道路/奖励)
    /// </summary>
    /// <param name="maxDifficulty">已解锁最高难度</param>
    /// <param name="preferredDifficulty">优先使用传入行的难度(旧存档/测试); 传入行不匹配时自动重抽</param>
    /// <param name="preferredRow">优先使用的配置行(可为空)</param>
    protected void BuildChallengeHundredRandomList(int maxDifficulty, int preferredDifficulty, FightTypeChallengeHundredInfoBean preferredRow)
    {
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        int bossRate = userUnlock.GetUnlockChallengeHundredBossRate();
        listChallengeHundredRandom = new List<GameWorldChallengeHundredRandomBean>();
        for (int level = 1; level <= maxDifficulty; level++)
        {
            //优先难度且传入行匹配该难度时沿用传入行, 否则按BOSS概率重新抽行
            FightTypeChallengeHundredInfoBean row;
            if (level == preferredDifficulty && preferredRow != null && preferredRow.IsMatchDifficulty(level))
                row = preferredRow;
            else
                row = FightTypeChallengeHundredInfoCfg.GetRandomRow(level, bossRate);
            if (row == null)
            {
                LogUtil.LogError($"挑战100勇士难度{level}无匹配配置行 worldId:{worldId}");
                continue;
            }
            listChallengeHundredRandom.Add(CreateChallengeHundredRandom(level, row));
        }
    }

    /// <summary>
    /// 按配置行创建单难度的挑战100勇士随机数据(道路/奖励一次性冻结)
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <param name="challengeHundredInfo">配置行</param>
    /// <returns>单难度随机数据</returns>
    protected GameWorldChallengeHundredRandomBean CreateChallengeHundredRandom(int difficultyLevel, FightTypeChallengeHundredInfoBean challengeHundredInfo)
    {
        return new GameWorldChallengeHundredRandomBean()
        {
            difficultyLevel = difficultyLevel,
            challengeHundredRowId = challengeHundredInfo.id,
            roadNum = challengeHundredInfo.GetRandomRoadNum(),
            roadLength = challengeHundredInfo.GetRandomRoadLength(),
            //冻结本难度战斗场景(同世界重新挑战沿用同一场地)
            fightSceneId = challengeHundredInfo.GetRandomFightScene(),
            listRewardChallengeHundred = RewardSelectBean.CreateRewardListForChallengeHundred(challengeHundredInfo, difficultyLevel),
            rewardUnlockSignChallengeHundred = RewardSelectBean.GetConquerEquipPoolSign(),
        };
    }

    /// <summary>
    /// 获取指定难度的挑战100勇士预生成数据; 老存档列表为空时懒重建,
    /// 传送门创建后研究新解锁的难度(列表里没有该档)首次访问时补生成并缓存(冻结后不再变化)
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <returns>该难度的随机数据; 未解锁/无匹配配置行返回null</returns>
    public GameWorldChallengeHundredRandomBean GetChallengeHundredRandom(int difficultyLevel)
    {
        //旧存档兼容: 首次访问时懒重建(Ensure 内部只在构建成功后才会回调 SetDifficultyLevel, 不会递归)
        if (listChallengeHundredRandom == null || listChallengeHundredRandom.Count == 0)
            EnsureChallengeHundredRandomData();
        GameWorldChallengeHundredRandomBean challengeHundredRandom = listChallengeHundredRandom?.Find(item => item.difficultyLevel == difficultyLevel);
        if (challengeHundredRandom == null)
            challengeHundredRandom = CreateChallengeHundredRandomByLevel(difficultyLevel);
        return challengeHundredRandom;
    }

    /// <summary>
    /// 补齐单个难度的挑战100勇士预生成数据: 仅对已解锁难度生效(研究新解锁后首次预览/切换该难度时调用), 生成后缓存冻结;
    /// 未解锁难度(详情弹窗仅作锁链预览)/无匹配配置行时返回null
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <returns>该难度的随机数据; 不可生成时返回null</returns>
    protected GameWorldChallengeHundredRandomBean CreateChallengeHundredRandomByLevel(int difficultyLevel)
    {
        if (gameFightType != GameFightTypeEnum.ChallengeHundred)
            return null;
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        //逐档判定(允许跳级): 未解锁档不预生成, 避免为锁链预览浪费数据
        if (!userUnlock.CheckChallengeHundredUnlock(worldId, difficultyLevel))
            return null;
        FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetRandomRow(difficultyLevel, userUnlock.GetUnlockChallengeHundredBossRate());
        if (challengeHundredInfo == null)
            return null;
        if (listChallengeHundredRandom == null)
            listChallengeHundredRandom = new List<GameWorldChallengeHundredRandomBean>();
        GameWorldChallengeHundredRandomBean challengeHundredRandom = CreateChallengeHundredRandom(difficultyLevel, challengeHundredInfo);
        listChallengeHundredRandom.Add(challengeHundredRandom);
        return challengeHundredRandom;
    }

    /// <summary>
    /// 获取指定难度挑战100勇士预生成的通关奖励(3箱); 与通关领奖同一份, 保证 UIPopupPortalDetails 预览=通关实领。
    /// 以下情况会重新生成并缓存: 尚未生成, 或解锁了新魔物掉落致装备奖励池变化(签名变化)。
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <returns>3箱奖励列表; 无对应数据/配置缺失时返回null</returns>
    public List<ItemBean> GetChallengeHundredReward(int difficultyLevel)
    {
        GameWorldChallengeHundredRandomBean challengeHundredRandom = GetChallengeHundredRandom(difficultyLevel);
        if (challengeHundredRandom == null)
            return null;
        //当前装备奖励池的解锁签名(解锁新魔物掉落后会变化)
        int currentUnlockSign = RewardSelectBean.GetConquerEquipPoolSign();
        bool needRegenerate = challengeHundredRandom.listRewardChallengeHundred == null
            || challengeHundredRandom.listRewardChallengeHundred.Count == 0
            || challengeHundredRandom.rewardUnlockSignChallengeHundred != currentUnlockSign;
        if (needRegenerate)
        {
            FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRandom.challengeHundredRowId);
            if (challengeHundredInfo == null)
                return null;
            //按该难度取档重新生成
            challengeHundredRandom.listRewardChallengeHundred = RewardSelectBean.CreateRewardListForChallengeHundred(challengeHundredInfo, difficultyLevel);
            challengeHundredRandom.rewardUnlockSignChallengeHundred = currentUnlockSign;
        }
        //同步活动字段, 保证无参版本与战斗结算读取同一份
        if (difficultyLevel == this.difficultyLevel)
        {
            challengeHundredRowId = challengeHundredRandom.challengeHundredRowId;
            roadNum = challengeHundredRandom.roadNum;
            roadLength = challengeHundredRandom.roadLength;
            fightNum = 1;
            listRewardChallengeHundred = challengeHundredRandom.listRewardChallengeHundred;
            rewardUnlockSignChallengeHundred = challengeHundredRandom.rewardUnlockSignChallengeHundred;
        }
        return challengeHundredRandom.listRewardChallengeHundred;
    }

    /// <summary>
    /// 获取当前活动难度的挑战100勇士预生成通关奖励(3箱); 旧存档尚未生成各难度数据时按原有活动字段回退
    /// </summary>
    /// <returns>3箱奖励列表; 冻结行配置缺失时返回null</returns>
    public List<ItemBean> GetChallengeHundredReward()
    {
        if (listChallengeHundredRandom != null && listChallengeHundredRandom.Count > 0)
            return GetChallengeHundredReward(difficultyLevel);
        //旧存档回退: 沿用原有活动字段重新生成
        int currentUnlockSign = RewardSelectBean.GetConquerEquipPoolSign();
        bool needRegenerate = listRewardChallengeHundred == null
            || listRewardChallengeHundred.Count == 0
            || rewardUnlockSignChallengeHundred != currentUnlockSign;
        if (needRegenerate)
        {
            FightTypeChallengeHundredInfoBean challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRowId);
            if (challengeHundredInfo == null)
                return null;
            listRewardChallengeHundred = RewardSelectBean.CreateRewardListForChallengeHundred(challengeHundredInfo, difficultyLevel);
            rewardUnlockSignChallengeHundred = currentUnlockSign;
        }
        return listRewardChallengeHundred;
    }

    /// <summary>
    /// 是否为指定难度的挑战100勇士-BOSS挑战(该难度配置行 challenge_type==1)
    /// </summary>
    /// <param name="difficultyLevel">难度等级</param>
    /// <returns>true=BOSS挑战(真勇士)</returns>
    public bool IsChallengeHundredBossChallenge(int difficultyLevel)
    {
        GameWorldChallengeHundredRandomBean challengeHundredRandom = GetChallengeHundredRandom(difficultyLevel);
        if (challengeHundredRandom == null)
            return false;
        var challengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(challengeHundredRandom.challengeHundredRowId);
        return challengeHundredInfo != null && challengeHundredInfo.IsBossChallenge();
    }

    /// <summary>
    /// 当前活动难度是否为挑战100勇士-BOSS挑战(困难): 该模式且冻结配置行 challenge_type==1;
    /// 用于奖励翻倍等困难判定(传送门BG/气泡底色已改为按模式着色, 不再用此方法)
    /// </summary>
    /// <returns>true=BOSS挑战(真勇士)</returns>
    public bool IsChallengeHundredBossChallenge()
    {
        if (gameFightType != GameFightTypeEnum.ChallengeHundred)
            return false;
        if (listChallengeHundredRandom != null && listChallengeHundredRandom.Count > 0)
            return IsChallengeHundredBossChallenge(difficultyLevel);
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
    //本难度普通关战斗场景id(创建传送门时一次性随出并冻结, 同世界重复进入/同一次run各普通关均使用该场地)
    public long fightSceneId;
    //本难度BOSS关战斗场景id(创建传送门时一次性随出并冻结; 征服BOSS关使用, 无尽每轮也走BOSS场景池)
    public long bossFightSceneId;
    //本难度预生成的通关奖励(装备+魔晶); 创建传送门时一次性随出并冻结, 保证 UIPopupPortalDetails 预览=通关实领
    public List<ItemBean> listReward = new List<ItemBean>();
    //生成奖励时的"装备奖励池解锁签名"(可生成装备的已解锁生物模型数量); 解锁新魔物掉落后此值变化, GetDifficultyReward 据此重新生成奖励
    public int rewardUnlockSign;
}

[Serializable]
/// <summary>
/// 游戏世界-挑战100勇士单个难度的随机数据(配置行/道路数/道路长度/战斗场景/通关奖励, 创建传送门时逐难度随出并缓存)
/// </summary>
public class GameWorldChallengeHundredRandomBean
{
    //难度等级
    public int difficultyLevel;
    //本难度冻结的配置行id(FightTypeChallengeHundredInfo)
    public long challengeHundredRowId;
    //道路数量
    public int roadNum;
    //道路长度
    public int roadLength;
    //本难度战斗场景id(创建传送门时一次性随出并冻结, 同世界重新挑战使用同一场地)
    public long fightSceneId;
    //本难度预生成的通关奖励(普通3箱/BOSS挑战装备6箱); 保证预览=实领
    public List<ItemBean> listRewardChallengeHundred = new List<ItemBean>();
    //生成奖励时的"装备奖励池解锁签名"; 解锁新魔物掉落后此值变化, GetChallengeHundredReward 据此重新生成
    public int rewardUnlockSignChallengeHundred;
}