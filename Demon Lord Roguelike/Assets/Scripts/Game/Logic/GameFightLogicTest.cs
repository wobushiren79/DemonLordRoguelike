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
        FightBeanForTest fightBeanForTest = fightData as FightBeanForTest;
        fightData.fightAttackData = ClassUtil.DeepCopy(fightBeanForTest.fightAttackDataRemark);
        //清理深渊馈赠数据
        WorldHandler.Instance.EnterGameForFightScene(fightData);
    }
}
