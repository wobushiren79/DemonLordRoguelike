using UnityEngine;

public class GameFightLogicTest : GameFightLogic
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
                //清理
                ClearGameForSimple();
                //打开结算UI
                var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
                uiFightSettlement.SetData(fightData);
                break;
        }
    }
}
