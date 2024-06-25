using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class UIViewCreatureCardItem 
{

    /// <summary>
    /// 战斗中点击
    /// </summary>
    public void OnClickSelectForFight()
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //战斗中的卡片不能点击
        if (fightCreatureData.stateForCard == CardStateEnum.Fighting)
            return;
        int createMagic = fightCreatureData.GetCreateMagic();
        if (gameFightLogic.fightData.currentMagic < createMagic)
        {            
            //魔力不足
            EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
            return;
        }
        GameFightLogic fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        fightLogic.SelectCard(fightCreatureData);
    
    }

    #region 事件
    /// <summary>
    /// 事件-选择卡片
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
        {
            switch (fightCreatureData.stateForCard)
            {
                case CardStateEnum.Fighting:
                    break;
                default:
                    SetCardState(CardStateEnum.FightIdle);
                    break;
            }
        }
        else
        {
            //设置卡片状态
            SetCardState(CardStateEnum.FightSelect);
        }
    }

    /// <summary>
    /// 事件-取消选择的卡片
    /// </summary>
    public void EventForGameFightLogicUnSelectCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
        {
            switch (fightCreatureData.stateForCard)
            {
                case CardStateEnum.FightSelect:
                    SetCardState(CardStateEnum.FightIdle);
                    break;
            }
        }
        else
        {
            //设置卡片状态
            SetCardState(CardStateEnum.FightIdle);
        }
    }

    /// <summary>
    /// 事件-放置卡片
    /// </summary>
    public void EventForGameFightLogicPutCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
            return;
        //设置卡片状态
        SetCardState(CardStateEnum.Fighting);
    }

    /// <summary>
    /// 事件-刷新卡片
    /// </summary>
    public void EventForGameFightLogicRefreshCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
            return;
        //刷新卡片状态
        RefreshCardState();
    }
    #endregion
}
