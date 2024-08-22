using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//战斗卡片特殊设置
public partial class UIViewCreatureCardItem
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(FightCreatureBean fightCreatureData, Vector2 originalCardPos)
    {
        this.cardData.fightCreatureData = fightCreatureData;
        this.cardData.fightCreatureData.stateForCard = CardStateEnum.FightIdle;

        this.cardData.originalCardPos = originalCardPos;
        this.cardData.originalSibling = transform.GetSiblingIndex();
        gameObject.name = $"UIViewCreatureCardItem_{cardData.originalSibling}";
        //注册 避开卡片的事件
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForForSelectKeep);
        //战斗事件
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_RefreshCard, EventForGameFightLogicRefreshCard);

        SetData(fightCreatureData.creatureData, CardUseState.Fight);
    }

    #region 触摸相关事件
    /// <summary>
    /// 触摸-进入
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
        //设置层级最上
        transform.SetAsLastSibling();
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, true);
    }

    /// <summary>
    /// 触摸-退出
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
        //还原层级
        transform.SetSiblingIndex(cardData.originalSibling);
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, false);
        //隐藏卡片详情
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_HideDetails, this);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 事件 避让卡片
    /// </summary>
    /// <param name="targetIndex">目标序列</param>
    public void EventForForSelectKeep(int targetIndex, Vector2 targetPos, bool isKeep)
    {
        if (isKeep)
        {
            int offsetIndex = Mathf.Abs(cardData.originalSibling - targetIndex);
            //当前卡距离目标卡的距离
            float disTwoCard = Mathf.Abs(cardData.originalCardPos.x - targetPos.x);
            //单张卡间距
            float disOneCard = disTwoCard / offsetIndex;
            //获取最靠近的卡片应该移动的位置（卡片的一半加上扩大后的一半 减去 最靠近卡片的距离）
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
                //把Keep动画关闭
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
                //把Keep动画关闭
                KillAnimForKeep();
                animForSelectKeepEnd = rectTransform
                    .DOAnchorPos(cardData.originalCardPos, animCardSelectEndTime)
                    .SetEase(animCardSelectEnd);
            }
        }
        //LogUtil.Log($"EventForSelectKeep originalSibling_{originalSibling} targetIndex_{targetIndex} targetPos_{targetPos}  isKeep_{isKeep}");
    }

    /// <summary>
    /// 事件-选择卡片
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
            //设置卡片状态
            SetCardState(CardStateEnum.FightSelect);
        }
    }

    /// <summary>
    /// 事件-取消选择的卡片
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
            //设置卡片状态
            SetCardState(CardStateEnum.FightIdle);
        }
    }

    /// <summary>
    /// 事件-放置卡片
    /// </summary>
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
            return;
        //设置卡片状态
        SetCardState(CardStateEnum.Fighting);
    }

    /// <summary>
    /// 事件-刷新卡片
    /// </summary>
    public void EventForGameFightLogicRefreshCard(UIViewCreatureCardItem targetView)
    {
        if (this.cardData.fightCreatureData != targetView.cardData.fightCreatureData)
            return;
        //刷新卡片状态
        RefreshCardState(targetView.cardData.fightCreatureData.stateForCard);
    }
    #endregion
}
