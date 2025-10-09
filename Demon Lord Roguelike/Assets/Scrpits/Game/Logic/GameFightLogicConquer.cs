using UnityEngine;

public class GameFightLogicConquer : GameFightLogic
{

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

    /// <summary>
    /// 处理结算
    /// </summary>
    public void HandleForChangeGameStateSettlement()
    {
        //清理
        ClearGameForSimple();
        //打开结算UI
        var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
        uiFightSettlement.SetData(fightData);
        uiFightSettlement.actionForNext = ActionForUIFightSettlementNext;
    }

    /// <summary>
    /// 开始下一关
    /// </summary>
    public void StartNextGame()
    {
        //开始下一关
        FightBeanForConquer fightDataForConquer = fightData as FightBeanForConquer;
        fightDataForConquer.InitNextData();

        float animTimeForShowMask = 0.5f;
        //展示mask遮罩
        UIHandler.Instance.ShowMask(animTimeForShowMask, null, () =>
        {
            WorldHandler.Instance.EnterGameForFightScene(fightData);
        }, false);
    }

    #region 回调
    /// <summary>
    /// 回调-结算界面关闭
    /// </summary>
    public void ActionForUIFightSettlementNext()
    {
        //如果已经是最后一关 打开奖励UI
        if (fightData.fightNum >= fightData.figthNumMax)
        {
            //TODO 先返回基地
            //清理深渊馈赠数据
            BuffHandler.Instance.manager.ClearAbyssalBlessing();
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            WorldHandler.Instance.EnterGameForBaseScene(userData, true);
        }
        //如果不是最后一关 打开深渊馈赠UI
        else
        {
            //打开选择深渊馈赠选择界面
            var uiFightAbyssalBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
            uiFightAbyssalBlessing.SetData(ActionForUIFightAbyssalBlessingSelect, ActionForUIFightAbyssalBlessingSkip);
        }
    }

    /// <summary>
    /// 回调-选择深渊馈赠界面关闭
    /// </summary>
    public void ActionForUIFightAbyssalBlessingSkip()
    {
        LogUtil.Log("ActionForUIFightAbyssalBlessingSkip 跳过深渊馈赠");
        StartNextGame();
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
        //开始下一局游戏
        StartNextGame();
    }
    #endregion
}
