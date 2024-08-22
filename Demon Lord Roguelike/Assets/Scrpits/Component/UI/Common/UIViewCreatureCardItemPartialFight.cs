using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//ս����Ƭ��������
public partial class UIViewCreatureCardItem
{
    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(FightCreatureBean fightCreatureData, Vector2 originalCardPos)
    {
        this.cardData.fightCreatureData = fightCreatureData;
        this.cardData.fightCreatureData.stateForCard = CardStateEnum.FightIdle;

        this.cardData.originalCardPos = originalCardPos;
        this.cardData.originalSibling = transform.GetSiblingIndex();
        gameObject.name = $"UIViewCreatureCardItem_{cardData.originalSibling}";
        //ע�� �ܿ���Ƭ���¼�
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForForSelectKeep);
        //ս���¼�
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_RefreshCard, EventForGameFightLogicRefreshCard);

        SetData(fightCreatureData.creatureData, CardUseState.Fight);
    }

    #region ��������¼�
    /// <summary>
    /// ����-����
    /// </summary>
    /// <param name="eventData"></param>
    void OnPointerEnterForFight(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerEnter_{cardData.originalSibling}");
        timeUpdateForShowDetails = 0;
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //���ò㼶����
        transform.SetAsLastSibling();
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, true);
    }

    /// <summary>
    /// ����-�˳�
    /// </summary>
    /// <param name="eventData"></param>
    void OnPointerExitForFight(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{cardData.originalSibling}");
        timeUpdateForShowDetails = -1;
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //��ԭ�㼶
        transform.SetSiblingIndex(cardData.originalSibling);
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, false);
        //���ؿ�Ƭ����
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_HideDetails, this);
    }
    #endregion

    #region �¼�
    /// <summary>
    /// �¼� ���ÿ�Ƭ
    /// </summary>
    /// <param name="targetIndex">Ŀ������</param>
    public void EventForForSelectKeep(int targetIndex, Vector2 targetPos, bool isKeep)
    {
        if (isKeep)
        {
            int offsetIndex = Mathf.Abs(cardData.originalSibling - targetIndex);
            //��ǰ������Ŀ�꿨�ľ���
            float disTwoCard = Mathf.Abs(cardData.originalCardPos.x - targetPos.x);
            //���ſ����
            float disOneCard = disTwoCard / offsetIndex;
            //��ȡ����Ŀ�ƬӦ���ƶ���λ�ã���Ƭ��һ�����������һ�� ��ȥ �����Ƭ�ľ��룩
            float closeCardMoveX = (rectTransform.sizeDelta.x + rectTransform.sizeDelta.x * animCardSelectStartScale) / 2f - disOneCard;
            float subDataX = closeCardMoveX - (disOneCard / 2) * (offsetIndex - 1);
            if (subDataX <= 0)
                return;
            Vector2 offsetPos = Vector2.zero;
            if (cardData.originalSibling > targetIndex)
            {
                offsetPos = new Vector2(subDataX, 0);
            }
            else if (cardData.originalSibling < targetIndex)
            {
                offsetPos = new Vector2(-subDataX, 0);
            }
            if (offsetPos != Vector2.zero)
            {
                //��Keep�����ر�
                KillAnimForKeep();
                animForSelectKeepStart = rectTransform
                    .DOAnchorPos(cardData.originalCardPos + offsetPos, animCardSelectStartTime)
                    .SetEase(animCardSelectStart);
            }
        }
        else
        {
            if (rectTransform.anchoredPosition != cardData.originalCardPos)
            {
                //��Keep�����ر�
                KillAnimForKeep();
                animForSelectKeepEnd = rectTransform
                    .DOAnchorPos(cardData.originalCardPos, animCardSelectEndTime)
                    .SetEase(animCardSelectEnd);
            }
        }
        //LogUtil.Log($"EventForSelectKeep originalSibling_{originalSibling} targetIndex_{targetIndex} targetPos_{targetPos}  isKeep_{isKeep}");
    }

    /// <summary>
    /// �¼�-ѡ��Ƭ
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
        {
            switch (cardData.fightCreatureData.stateForCard)
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
    public void EventForGameFightLogicUnSelectCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
        {
            switch (cardData.fightCreatureData.stateForCard)
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
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
            return;
        //���ÿ�Ƭ״̬
        SetCardState(CardStateEnum.Fighting);
    }

    /// <summary>
    /// �¼�-ˢ�¿�Ƭ
    /// </summary>
    public void EventForGameFightLogicRefreshCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
            return;
        //ˢ�¿�Ƭ״̬
        RefreshCardState(targetView.cardData.fightCreatureData.stateForCard);
    }
    #endregion
}
