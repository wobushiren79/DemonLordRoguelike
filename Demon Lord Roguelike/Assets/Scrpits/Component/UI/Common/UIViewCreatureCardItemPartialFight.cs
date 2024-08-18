using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//ս����Ƭ��������
public partial class UIViewCreatureCardItem 
{
    public FightCreatureBean fightCreatureData;//��Ƭ����

    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(FightCreatureBean fightCreatureData, Vector2 originalCardPos)
    {
        this.fightCreatureData = fightCreatureData;
        this.fightCreatureData.stateForCard = CardStateEnum.FightIdle;

        this.originalCardPos = originalCardPos;
        this.originalSibling = transform.GetSiblingIndex();
        gameObject.name = $"UIViewCreatureCardItem_{originalSibling}";
        //ע�� �ܿ���Ƭ���¼�
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForForSelectKeep);
        //ս���¼�
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_RefreshCard, EventForGameFightLogicRefreshCard);

        SetData(fightCreatureData.creatureData,CardUseState.Show);
    }
    #region ��������¼�
    /// <summary>
    /// ����-����
    /// </summary>
    /// <param name="eventData"></param>
    void OnPointerEnterForFight(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerEnter_{originalSibling}");
        timeUpdateForShowDetails = 0;
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //���ò㼶����
        transform.SetAsLastSibling();
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, true);
    }

    /// <summary>
    /// ����-�˳�
    /// </summary>
    /// <param name="eventData"></param>
    void OnPointerExitForFight(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{originalSibling}");
        timeUpdateForShowDetails = -1;
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //��ԭ�㼶
        transform.SetSiblingIndex(originalSibling);
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, false);
        //���ؿ�Ƭ����
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_HideDetails, fightCreatureData);
    }
    #endregion

    /// <summary>
    /// ս���е��
    /// </summary>
    public void OnClickSelectForFight()
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //ս���еĿ�Ƭ���ܵ��
        if (fightCreatureData.stateForCard == CardStateEnum.Fighting)
            return;
        int createMagic = fightCreatureData.creatureData.GetCreateMagic();
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
    /// �¼� ���ÿ�Ƭ
    /// </summary>
    /// <param name="targetIndex">Ŀ������</param>
    public void EventForForSelectKeep(int targetIndex, Vector2 targetPos, bool isKeep)
    {
        if (isKeep)
        {
            int offsetIndex = Mathf.Abs(originalSibling - targetIndex);
            //��ǰ������Ŀ�꿨�ľ���
            float disTwoCard = Mathf.Abs(originalCardPos.x - targetPos.x);
            //���ſ����
            float disOneCard = disTwoCard / offsetIndex;
            //��ȡ����Ŀ�ƬӦ���ƶ���λ�ã���Ƭ��һ�����������һ�� ��ȥ �����Ƭ�ľ��룩
            float closeCardMoveX = (rectTransform.sizeDelta.x + rectTransform.sizeDelta.x * animCardSelectStartScale) / 2f - disOneCard;
            float subDataX = closeCardMoveX - (disOneCard / 2) * (offsetIndex - 1);
            if (subDataX <= 0)
                return;
            Vector2 offsetPos = Vector2.zero;
            if (originalSibling > targetIndex)
            {
                offsetPos = new Vector2(subDataX, 0);
            }
            else if (originalSibling < targetIndex)
            {
                offsetPos = new Vector2(-subDataX, 0);
            }
            if (offsetPos != Vector2.zero)
            {
                //��Keep�����ر�
                KillAnimForKeep();
                animForSelectKeepStart = rectTransform
                    .DOAnchorPos(originalCardPos + offsetPos, animCardSelectStartTime)
                    .SetEase(animCardSelectStart);
            }
        }
        else
        {
            if (rectTransform.anchoredPosition != originalCardPos)
            {
                //��Keep�����ر�
                KillAnimForKeep();
                animForSelectKeepEnd = rectTransform
                    .DOAnchorPos(originalCardPos, animCardSelectEndTime)
                    .SetEase(animCardSelectEnd);
            }
        }
        //LogUtil.Log($"EventForSelectKeep originalSibling_{originalSibling} targetIndex_{targetIndex} targetPos_{targetPos}  isKeep_{isKeep}");
    }

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
        RefreshCardState(fightCreatureDataTarget.stateForCard);
    }
    #endregion
}
