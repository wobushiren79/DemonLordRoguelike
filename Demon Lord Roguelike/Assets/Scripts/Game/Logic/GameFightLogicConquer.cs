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
            //深渊馈赠「奖励多多」：增加生成的奖励物品数量(领奖宝箱按奖励数量实时生成，自动多出对应宝箱)，须在InitData生成前设置
            rewardSelectData.createItemNum += fightDataForConquer.rewardAddItemNum;
            rewardSelectData.InitData(fightData);
            //深渊馈赠「再来一瓶」：增加可选择奖励次数
            rewardSelectData.selectNumMax += fightDataForConquer.rewardAddSelectNum;
            //可选次数不超过实际奖励数量，避免多余次数无对应宝箱可开
            if (rewardSelectData.selectNumMax > rewardSelectData.listReward.Count)
                rewardSelectData.selectNumMax = rewardSelectData.listReward.Count;
            uiRewardSelect.SetData(rewardSelectData, ActionForUIRewardSelectEnd);
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
        }
        EndGameAndReturnToBase();
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
    /// 普通关卡发放 reward_exp，BOSS关卡发放 reward_exp_boss；
    /// 经验直接累加到生物存档对象(CreatureBean.levelExp)，随返回基地时统一保存落盘
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
        //本场出战阵容(防御方)生物，dlDefenseCreatureData 内为存档生物对象的引用
        var listDefenseCreature = fightDataForConquer.dlDefenseCreatureData?.List;
        if (listDefenseCreature == null)
            return;
        for (int i = 0; i < listDefenseCreature.Count; i++)
        {
            var creatureData = listDefenseCreature[i];
            if (creatureData == null)
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
