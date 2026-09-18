using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挑战100勇士战斗逻辑（单关100只怪；通关3箱3抽全手动开箱；魔王放卡不耗蓝；不发成就/声望，无关卡间深渊馈赠）
/// </summary>
public class GameFightLogicChallengeHundred : GameFightLogic
{
    #region 重写方法
    /// <summary>
    /// 改变游戏状态
    /// </summary>
    public override void ChangeGameState(GameStateEnum gameState)
    {
        base.ChangeGameState(gameState);
        switch (gameState)
        {
            case GameStateEnum.Settlement:
                HandleForChangeGameStateSettlement();
                break;
        }
    }

    /// <summary>
    /// 放卡跳过魔力消耗（本模式魔王蓝量无限）
    /// </summary>
    public override bool IsSkipPutCardMPCost()
    {
        return true;
    }
    #endregion

    #region 状态处理
    /// <summary>
    /// 处理结算状态
    /// 单关模式：胜利(100勇士清空)或失败(魔王核心死亡)都走完整结算流程
    /// </summary>
    public void HandleForChangeGameStateSettlement()
    {
        bool isWin = fightData.gameIsWin;

        //胜利 → 给本场出战阵容生物发放等级经验（配置行 reward_exp；失败不发）
        if (isWin)
        {
            FightBeanForChallengeHundred fightDataForChallengeHundred = fightData as FightBeanForChallengeHundred;
            int addExp = fightDataForChallengeHundred?.fightTypeChallengeHundredInfo?.reward_exp ?? 0;
            AddLevelExpForLineupCreature(fightDataForChallengeHundred, addExp);
        }

        //清理战场（AI / BUFF）
        ClearGameForSimple();
        //打开结算UI
        var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
        uiFightSettlement.SetData(fightData, ActionForUIFightSettlementNext);
    }
    #endregion

    #region 回调
    /// <summary>
    /// 回调-点击下一步
    /// 胜利 → 进入奖励界面（3箱3抽，无首箱保底自动开）
    /// 失败 → 直接结束游戏返回基地
    /// </summary>
    public void ActionForUIFightSettlementNext()
    {
        FightBeanForChallengeHundred fightDataForChallengeHundred = fightData as FightBeanForChallengeHundred;
        bool isWin = fightData.gameIsWin;

        if (isWin && fightDataForChallengeHundred != null)
        {
            //通关 → 打开领奖界面
            var uiRewardSelect = UIHandler.Instance.OpenUIAndCloseOther<UIRewardSelect>();
            RewardSelectBean rewardSelectData = new RewardSelectBean();
            //3箱3抽全手动开箱（无首箱保底自动开，可开宝箱数=总数，无需征服的 -1 钳制）
            rewardSelectData.selectNumMax = 3;
            rewardSelectData.isAutoOpenFirstBox = false;
            //基础奖励直接取进入传送门时预生成并冻结的3箱奖励(UIPopupPortalDetails 预览=实领)
            var baseReward = fightDataForChallengeHundred.gameWorldInfoRandomData.GetChallengeHundredReward();
            rewardSelectData.InitDataForReward(baseReward, null, 0);
            //isClearLastGame:true → 进入领奖场景前先卸载本场战斗场景并清理战斗实体
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
        //通关一次世界: 回满刷新次数 + 清空全部传送门世界(下次打开传送门UI时全量重新生成); 随 EndGameAndReturnToBase 的 SaveUserData 一并落盘
        var userTempData = GameDataHandler.Instance.manager.GetUserData().GetUserTempData();
        userTempData.RefillPortalRefreshNum();
        userTempData.ClearPortalWorldInfoRandomData();
        EndGameAndReturnToBase();
    }
    #endregion

    #region 工具
    /// <summary>
    /// 胜利后给本场出战阵容(防御方)生物累加指定经验
    /// 经验直接累加到生物存档对象(CreatureBean.levelExp)，随返回基地时统一保存落盘，魔王(不吃经验)与已达等级上限的生物跳过
    /// </summary>
    /// <param name="fightDataForChallengeHundred">挑战100勇士战斗数据</param>
    /// <param name="addExp">本次累加的经验值（配置行 reward_exp）</param>
    private void AddLevelExpForLineupCreature(FightBeanForChallengeHundred fightDataForChallengeHundred, int addExp)
    {
        if (fightDataForChallengeHundred == null || addExp <= 0)
            return;
        //本场出战阵容(防御方)生物，dlDefenseCreatureData 内为存档生物对象的引用
        var listDefenseCreature = fightDataForChallengeHundred.dlDefenseCreatureData?.List;
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
            //战斗run结束(输赢皆经此处): 显式分发议案EndGame消耗钩子(各议案自身按模式门控, 征服专属议案不会在本模式消耗)
            userData.GetUserTempData().TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.GameFightLogicEndGame);
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
