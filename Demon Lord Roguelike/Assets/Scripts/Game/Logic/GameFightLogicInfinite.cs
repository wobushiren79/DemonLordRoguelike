using UnityEngine;

public class GameFightLogicInfinite : GameFightLogic
{

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
    /// 进攻队列耗尽时追加下一轮进攻波次(无尽模式核心机制: 队列永不空 → 基类胜利判定"队列空+场上无敌"永不满足, 只有魔王死亡才结束)
    /// </summary>
    /// <returns>true=已补充新波次</returns>
    protected override bool TryRefillNextAttackQueue()
    {
        if (fightData is FightBeanForInfinite fightDataForInfinite)
        {
            fightDataForInfinite.AppendNextRoundAttackData();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检测游戏是否结束(无尽模式重写: 屏蔽胜利判定, 永不胜利; 只有魔王(防守核心)死亡才失败结束)
    /// 与 refill 钩子构成双重保险: 即使存在队列真空的极端时序, 也不会误触发胜利结算
    /// </summary>
    public override void CheckGameEnd()
    {
        //已进入结算/结束状态时不再重复检测，防止 ChangeGameState 被反复触发
        if (gameState == GameStateEnum.Settlement || gameState == GameStateEnum.End)
            return;
        //如果魔王死了
        if (fightData.fightDefenseCoreCreature.IsDead())
        {
            fightData.gameIsWin = false;
            //进入结算状态
            ChangeGameState(GameStateEnum.Settlement);
        }
    }

    /// <summary>
    /// 处理结算(清理战场→打开通用结算面板, 仅展示战绩排行榜; 无尽无奖励/经验/声望/成就)
    /// </summary>
    public void HandleForChangeGameStateSettlement()
    {
        //清理
        ClearGameForSimple();
        //打开结算UI
        var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
        uiFightSettlement.SetData(fightData, ActionForUIFightSettlementExit);
    }

    #region 回调
    /// <summary>
    /// 回调-点击退出结算: 参照挑战100勇士 EndGameAndReturnToBase, 但无经验/声望/成就/宝箱/领奖
    /// 仅保留: 议案EndGame消耗钩子(敌人强度等议案按run消耗, 缺了会永久残留到后续所有战斗) + 清深渊馈赠(防御性) + 还原阵容生物战斗状态 + 存盘 + 回基地;
    /// 不调用 RefillPortalRefreshNum/ClearPortalWorldInfoRandomData(那是"通关一次世界"语义, 无尽无通关)
    /// </summary>
    public void ActionForUIFightSettlementExit()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //战斗run结束: 显式分发议案EndGame消耗钩子(各议案自身按模式门控, 征服专属议案不会在本模式消耗)
            userData.GetUserTempData().TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.GameFightLogicEndGame);
            //清理深渊馈赠数据
            BuffHandler.Instance.manager.ClearAbyssalBlessing();
            //存盘前还原阵容生物战斗状态(Fight/Rest → Idle), 避免中间状态写入存档导致阵容只剩1个
            RestoreDefenseCreatureFightState();
            //保存用户数据(战斗中拾取的魔晶已即时入账, 此处统一落盘)
            GameDataHandler.Instance.manager.SaveUserData();
            //返回基地
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }
    #endregion
}
