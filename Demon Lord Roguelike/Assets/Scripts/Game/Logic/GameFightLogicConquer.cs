using System.Threading.Tasks;
using UnityEngine;

public class GameFightLogicConquer : GameFightLogic
{
    #region 重写方法
    /// <summary>
    /// 加载场景之后
    /// </summary>
    public override async Task PreGameForAfterLoadFightScene()
    {
        await base.PreGameForAfterLoadFightScene();
        //加载上一场比赛还在场上的防御生物
        FightBeanForConquer fightBeanForConquer = fightData as FightBeanForConquer;
        var listLastDefenseFightCreatureData = fightBeanForConquer.listLastDefenseFightCreatureData;
        if (listLastDefenseFightCreatureData != null)
        {
            for (int i = 0; i < listLastDefenseFightCreatureData.Count; i++)
            {
                var itemFightCreatureData = listLastDefenseFightCreatureData[i];
                CreatureHandler.Instance.CreateDefenseCreatureEntity(itemFightCreatureData.creatureData, itemFightCreatureData.positionCreate);
            }
            fightBeanForConquer.listLastDefenseFightCreatureData = null;
        }
        return;
    }

    /// <summary>
    /// 改变游戏状态
    /// </summary>
    public override void ChangeGameState(GameStateEnum gameState)
    {
        base.ChangeGameState(gameState);
        switch (gameState)
        {
            case GameStateEnum.Pre:
                break;
            case GameStateEnum.Gaming:
                break;
            case GameStateEnum.End:
                break;
            case GameStateEnum.Settlement:
                HandleForChangeGameStateSettlement();
                break;
        }
    }
    #endregion

    #region 状态处理
    /// <summary>
    /// 处理结算状态
    /// 仅在以下两种场景弹出结算UI：
    /// 1) 魔王(防御核心)死亡——游戏失败
    /// 2) 通关最后一关(BOSS)——游戏胜利
    /// 其余情况（非BOSS关胜利）直接弹出深渊馈赠选择UI，保留战场状态以便后续在同场景内继续战斗
    /// </summary>
    public void HandleForChangeGameStateSettlement()
    {
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        bool isWin = fightData.gameIsWin;
        bool isBossFight = fightDataForConquer != null && fightDataForConquer.IsBossFight();

        //关卡胜利 → 给本场出战阵容生物发放等级经验（每关进入结算时仅触发一次，失败不发）
        if (isWin)
        {
            AddLevelExpForLineupCreature(fightDataForConquer, isBossFight);
        }

        //失败或通关BOSS关 → 弹出结算UI(走完整结算流程，需要清理战场)
        if (!isWin || isBossFight)
        {
            //清理战场（AI / BUFF）
            ClearGameForSimple();
            //打开结算UI
            var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
            uiFightSettlement.SetData(fightData, ActionForUIFightSettlementNext);
        }
        //非BOSS关胜利 → 直接弹出深渊馈赠选择UI（不清理战场，保留防御生物及BUFF）
        else
        {
            OpenAbyssalBlessingUI();
        }
    }

    /// <summary>
    /// 打开深渊馈赠选择UI
    /// </summary>
    private void OpenAbyssalBlessingUI()
    {
        var uiFightAbyssalBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
        uiFightAbyssalBlessing.SetData(ActionForUIFightAbyssalBlessingSelect, ActionForUIFightAbyssalBlessingSkip);
    }
    #endregion

    #region 关卡推进
    /// <summary>
    /// 推进到下一关
    /// 下一关为BOSS关 → 走完整GameFight重载加载BOSS场景
    /// 否则 → 在当前场景内继续战斗，仅刷新进攻数据
    /// </summary>
    private void GoToNextLevel()
    {
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        if (fightDataForConquer == null)
            return;

        if (fightDataForConquer.IsNextBossFight())
        {
            StartNextGameForBoss();
        }
        else
        {
            ContinueNextLevelInSameScene();
        }
    }

    /// <summary>
    /// 重载战斗场景进入BOSS关
    /// </summary>
    private void StartNextGameForBoss()
    {
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        //准备BOSS关数据(保留防御生物并标记 fightNum++)
        fightDataForConquer.InitNextData();

        //清理战场（AI / BUFF），即将重载场景
        ClearGameForSimple();

        float animTimeForShowMask = 0.5f;
        //展示mask遮罩并加载BOSS战斗场景
        UIHandler.Instance.ShowMask(animTimeForShowMask, null, () =>
        {
            WorldHandler.Instance.EnterGameForFightScene(fightData);
        }, false);
    }

    /// <summary>
    /// 在当前场景内继续战斗，生成下一关的敌人
    /// 注意：不调用 UIFightMain.InitData()，避免卡片被销毁重建后丢失 Rest/Fighting 等卡片状态
    /// </summary>
    private void ContinueNextLevelInSameScene()
    {
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        //仅更新进攻数据，保留防御生物 / 魔王 / BUFF 等所有现场状态
        fightDataForConquer.InitNextDataForContinue();

        //重新打开战斗主UI（之前因为打开馈赠UI被关闭）
        var uiFightMain = UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
        //补齐在馈赠界面期间新增的防御生物卡片（如深渊馈赠增殖复制，触发时UIFightMain已关闭，事件无法送达）
        uiFightMain.SyncCreatureCardList();
        //刷新进攻进度条（新一关的进攻数据从 0 开始）
        uiFightMain.RefreshUIData();

        //恢复游戏状态继续战斗
        ChangeGameState(GameStateEnum.Gaming);
    }
    #endregion

    #region 回调
    /// <summary>
    /// 回调-点击下一步
    /// 通关BOSS → 进入奖励界面
    /// 失败 → 直接结束游戏返回基地
    /// </summary>
    public void ActionForUIFightSettlementNext()
    {
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        bool isWin = fightData.gameIsWin;
        bool isBossFight = fightDataForConquer != null && fightDataForConquer.IsBossFight();

        if (isWin && isBossFight)
        {
            //通关BOSS → 打开领奖界面
            var uiRewardSelect = UIHandler.Instance.OpenUIAndCloseOther<UIRewardSelect>();
            RewardSelectBean rewardSelectData = new RewardSelectBean();
            //基础奖励直接取进入传送门时预生成并冻结的这次奖励(UIPopupPortalDetails 预览=实领)
            var gameWorldInfoRandom = fightDataForConquer.gameWorldInfoRandomData;
            var baseReward = gameWorldInfoRandom.GetDifficultyReward(gameWorldInfoRandom.difficultyLevel);
            //深渊馈赠「奖励多多」：在预生成基础奖励之后追加额外奖励件数(领奖宝箱按奖励数量实时生成，自动多出对应宝箱)
            rewardSelectData.InitDataForReward(baseReward, fightDataForConquer.fightTypeConquerInfo, fightDataForConquer.rewardAddItemNum);
            //深渊馈赠「再来一瓶」：增加可选择奖励次数
            rewardSelectData.selectNumMax += fightDataForConquer.rewardAddSelectNum;
            //可选次数不超过实际奖励数量，避免多余次数无对应宝箱可开
            if (rewardSelectData.selectNumMax > rewardSelectData.listReward.Count)
                rewardSelectData.selectNumMax = rewardSelectData.listReward.Count;
            //isClearLastGame:true → 进入领奖场景前先卸载本场BOSS战斗场景并清理战斗实体，避免BOSS战斗场景残留叠加在领奖场景上
            uiRewardSelect.SetData(rewardSelectData, ActionForUIRewardSelectEnd, isClearLastGame: true);
        }
        else
        {
            //失败 → 结束游戏返回基地
            EndGameAndReturnToBase();
        }
    }

    /// <summary>
    /// 回调-领奖结束
    /// </summary>
    public void ActionForUIRewardSelectEnd()
    {
        //成就统计-征服模式完整通关(按世界×难度)
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        if (fightDataForConquer != null && fightDataForConquer.fightTypeConquerInfo != null)
        {
            long worldId = fightDataForConquer.gameWorldInfoRandomData != null
                ? fightDataForConquer.gameWorldInfoRandomData.worldId
                : fightDataForConquer.fightTypeConquerInfo.world_id;
            int difficultyLevel = fightDataForConquer.fightTypeConquerInfo.level;
            EventHandler.Instance.TriggerEvent(EventsInfo.Achievement_ConquerComplete, worldId, difficultyLevel);
            //完整通关声望奖励: 解锁「征服通关获得声望」研究后, 按当前难度配置 reward_reputation 增加玩家声望
            AddReputationForConquerComplete(fightDataForConquer.fightTypeConquerInfo);
        }
        //通关一次世界: 回满刷新次数 + 清空全部传送门世界(列表置空, 下次打开传送门UI时 InitMap 缓存为空→全量重新生成); 随 EndGameAndReturnToBase 的 SaveUserData 一并落盘
        var userTempData = GameDataHandler.Instance.manager.GetUserData().GetUserTempData();
        userTempData.RefillPortalRefreshNum();
        userTempData.ClearPortalWorldInfoRandomData();
        EndGameAndReturnToBase();
    }

    /// <summary>
    /// 完整通关征服后按难度发放声望奖励
    /// 研究门控: 需解锁 UnlockEnum.ConquerReputationReward「征服通关获得声望」; 声望值取当前难度配置 reward_reputation
    /// 在 EndGameAndReturnToBase 的 SaveUserData 之前调用, 随存档一并落盘
    /// </summary>
    /// <param name="conquerInfo">本次通关的征服难度配置</param>
    private void AddReputationForConquerComplete(FightTypeConquerInfoBean conquerInfo)
    {
        if (conquerInfo == null)
            return;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //未解锁「征服通关获得声望」研究则不发放
        if (!userData.GetUserUnlockData().CheckIsUnlock(UnlockEnum.ConquerReputationReward))
            return;
        int reputationReward = conquerInfo.GetRewardReputation();
        if (reputationReward <= 0)
            return;
        userData.AddReputation(reputationReward);
    }

    /// <summary>
    /// 回调-选择深渊馈赠界面跳过
    /// </summary>
    public void ActionForUIFightAbyssalBlessingSkip()
    {
        LogUtil.Log("ActionForUIFightAbyssalBlessingSkip 跳过深渊馈赠");
        GoToNextLevel();
    }

    /// <summary>
    /// 回调-选择深渊馈赠
    /// </summary>
    public void ActionForUIFightAbyssalBlessingSelect(AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        LogUtil.Log("ActionForUIFightAbyssalBlessingSelect 选择深渊馈赠");
        //添加选中的深渊馈赠
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        fightDataForConquer.AddAbyssalBlessing(abyssalBlessingInfo);
        //推进到下一关
        GoToNextLevel();
    }
    #endregion

    #region 工具
    /// <summary>
    /// 关卡胜利后给本场出战阵容生物增加等级经验
    /// 普通关卡发放 reward_exp，BOSS关卡发放 reward_exp_boss；基础发放后触发
    /// GameFightLogic_AddExp 事件供终焉议会"经验翻倍"等效果再加成
    /// </summary>
    /// <param name="fightDataForConquer">征服模式战斗数据</param>
    /// <param name="isBossFight">当前是否为BOSS关</param>
    private void AddLevelExpForLineupCreature(FightBeanForConquer fightDataForConquer, bool isBossFight)
    {
        if (fightDataForConquer == null || fightDataForConquer.fightTypeConquerInfo == null)
            return;
        var conquerInfo = fightDataForConquer.fightTypeConquerInfo;
        //按关卡类型取对应经验值
        int addExp = isBossFight ? conquerInfo.reward_exp_boss : conquerInfo.reward_exp;
        if (addExp <= 0)
            return;
        //基础经验入账
        AddLevelExpForLineupCreature(fightDataForConquer, addExp);
        //事件通知(终焉议会"经验翻倍"等效果据此再加成)
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_AddExp, addExp);
    }

    /// <summary>
    /// 给本场出战阵容(防御方)生物累加指定经验
    /// 供关卡结算基础发放与终焉议会"经验翻倍"效果复用；经验直接累加到生物存档对象
    /// (CreatureBean.levelExp)，随返回基地时统一保存落盘，魔王(不吃经验)与已达等级上限的生物跳过
    /// </summary>
    /// <param name="fightDataForConquer">征服模式战斗数据</param>
    /// <param name="addExp">本次累加的经验值</param>
    public void AddLevelExpForLineupCreature(FightBeanForConquer fightDataForConquer, int addExp)
    {
        if (fightDataForConquer == null || addExp <= 0)
            return;
        //本场出战阵容(防御方)生物，dlDefenseCreatureData 内为存档生物对象的引用
        var listDefenseCreature = fightDataForConquer.dlDefenseCreatureData?.List;
        if (listDefenseCreature == null)
            return;
        for (int i = 0; i < listDefenseCreature.Count; i++)
        {
            var creatureData = listDefenseCreature[i];
            if (creatureData == null)
                continue;
            //魔王隐藏等级且不吃经验:防御核心,即便意外混入出战阵容也跳过不加经验
            if (creatureData.IsDemonLord())
                continue;
            //已达等级上限的生物不再累加经验
            if (creatureData.IsMaxLevel())
                continue;
            creatureData.levelExp += addExp;
        }
    }

    /// <summary>
    /// 结束游戏返回基地
    /// </summary>
    private void EndGameAndReturnToBase()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //清理深渊馈赠数据
            BuffHandler.Instance.manager.ClearAbyssalBlessing();
            //存盘前还原阵容生物战斗状态(Fight/Rest → Idle)，避免中间状态写入存档导致阵容只剩1个
            RestoreDefenseCreatureFightState();
            //保存用户数据
            GameDataHandler.Instance.manager.SaveUserData();
            //返回基地
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }
    #endregion
}
