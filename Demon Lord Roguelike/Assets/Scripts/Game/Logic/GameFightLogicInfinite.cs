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
        uiFightSettlement.SetData(fightData, ActionForUIFightSettlementExit);
    }

    #region 回调
    /// <summary>
    /// 回调-点击退出
    /// </summary>
    public void ActionForUIFightSettlementExit()
    {
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }
    #endregion
}
