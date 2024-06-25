using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class UIViewCreatureCardItem 
{

    /// <summary>
    /// ս���е��
    /// </summary>
    public void OnClickSelectForFight()
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //ս���еĿ�Ƭ���ܵ��
        if (fightCreatureData.stateForCard == CardStateEnum.Fighting)
            return;
        int createMagic = fightCreatureData.GetCreateMagic();
        if (gameFightLogic.fightData.currentMagic < createMagic)
        {            
            //ħ������
            EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
            return;
        }
        GameFightLogic fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        fightLogic.SelectCard(fightCreatureData);
    
    }

    #region �¼�
    /// <summary>
    /// �¼�-ѡ��Ƭ
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
            //���ÿ�Ƭ״̬
            SetCardState(CardStateEnum.FightSelect);
        }
    }

    /// <summary>
    /// �¼�-ȡ��ѡ��Ŀ�Ƭ
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
            //���ÿ�Ƭ״̬
            SetCardState(CardStateEnum.FightIdle);
        }
    }

    /// <summary>
    /// �¼�-���ÿ�Ƭ
    /// </summary>
    public void EventForGameFightLogicPutCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
            return;
        //���ÿ�Ƭ״̬
        SetCardState(CardStateEnum.Fighting);
    }

    /// <summary>
    /// �¼�-ˢ�¿�Ƭ
    /// </summary>
    public void EventForGameFightLogicRefreshCard(FightCreatureBean fightCreatureDataTarget)
    {
        if (this.fightCreatureData != fightCreatureDataTarget)
            return;
        //ˢ�¿�Ƭ״̬
        RefreshCardState();
    }
    #endregion
}
