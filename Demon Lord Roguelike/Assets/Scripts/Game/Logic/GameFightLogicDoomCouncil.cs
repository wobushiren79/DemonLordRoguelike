using System.Threading.Tasks;
using UnityEngine;

public class GameFightLogicDoomCouncil : GameFightLogic
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

    #region 回调
    /// <summary>
    /// 回调-结算界面关闭
    /// </summary> 
    public async void ActionForUIFightSettlementNext()
    {                    
        //展示投票结果
        var voteEndUI = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilVoteEnd>();    
        voteEndUI.VoteEndShow(fightData.gameIsWin);
        await new WaitForSeconds(2f);

        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            //保存用户数据
            UserDataBean userData = GameDataHandler.Instance.manager.SaveUserData();
            //返回基地
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }
    #endregion
}
