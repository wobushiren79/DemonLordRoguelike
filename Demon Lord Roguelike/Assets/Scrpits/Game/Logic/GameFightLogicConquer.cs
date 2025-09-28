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
    /// 回调-结算界面关闭
    /// </summary>
    public void ActionForUIFightSettlementNext()
    {
        //打开选择深渊馈赠选择界面
        var uiFightAbyssalBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();    
        
    }
}
