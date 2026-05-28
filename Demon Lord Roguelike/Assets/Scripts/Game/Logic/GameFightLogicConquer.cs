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

        //失败或通关BOSS关 → 弹出结算UI(走完整结算流程，需要清理战场)
        if (!isWin || isBossFight)
        {
            //清理战场（AI / BUFF）
            ClearGameForSimple();
            //打开结算UI
            var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
            uiFightSettlement.SetData(fightData, ActionForUIFightSettlementExit);
            uiFightSettlement.actionForNext = ActionForUIFightSettlementNext;
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
        //刷新进攻进度条（新一关的进攻数据从 0 开始）
        uiFightMain.RefreshUIData();

        //恢复游戏状态继续战斗
        ChangeGameState(GameStateEnum.Gaming);
    }
    #endregion

    #region 回调
    /// <summary>
    /// 回调-点击退出结算UI
    /// 失败或通关BOSS 都直接返回基地
    /// </summary>
    public void ActionForUIFightSettlementExit()
    {
        EndGameAndReturnToBase();
    }

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
            rewardSelectData.InitData(fightData);
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
        //成就统计-征服模式完整通关
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        if (fightDataForConquer != null && fightDataForConquer.fightTypeConquerInfo != null)
        {
            int difficultyLevel = fightDataForConquer.fightTypeConquerInfo.level;
            EventHandler.Instance.TriggerEvent(EventsInfo.Achievement_ConquerComplete, difficultyLevel);
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
    /// 结束游戏返回基地
    /// </summary>
    private void EndGameAndReturnToBase()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //清理深渊馈赠数据
            BuffHandler.Instance.manager.ClearAbyssalBlessing();
            //保存用户数据
            GameDataHandler.Instance.manager.SaveUserData();
            //返回基地
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }
    #endregion
}
