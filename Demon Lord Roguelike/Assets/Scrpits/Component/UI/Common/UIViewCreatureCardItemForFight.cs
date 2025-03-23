using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//战斗卡片特殊设置
public partial class UIViewCreatureCardItemForFight : UIViewCreatureCardItem, IPointerEnterHandler, IPointerExitHandler
{
    [Header("卡片创建动画延迟时间")]
    public float animCardCreateDelayTime = 0.05f;
    [Header("卡片创建动画时间")]
    public float animCardCreateTimeType1 = 0.8f;
    [Header("卡片创建动画时间")]
    public float animCardCreateTimeType2 = 0.4f;
    [Header("卡片创建动画缓动函数")]
    public Ease animCardCreateEase = Ease.OutBack;

    [Header("卡片选择动画进入时间")]
    public float animCardSelectStartTime = 0.25f;
    [Header("卡片选择动画缓动函数-进入")]
    public Ease animCardSelectStart = Ease.OutBack;
    [Header("卡片选择动画放大参数")]
    public float animCardSelectStartScale = 1.6f;

    [Header("卡片选择动画退出时间")]
    public float animCardSelectEndTime = 0.5f;
    [Header("卡片选择动画缓动函数-退出")]
    public Ease animCardSelectEnd = Ease.OutBack;

    protected Tween animForCreate;//创建卡片动画
    protected Tween animForSelectStart;//选择卡片动画
    protected Tween animForSelectEnd;//选择卡片动画
    protected Tween animForSelectKeepStart;//选择卡片避让动画
    protected Tween animForSelectKeepEnd;//选择卡片避让动画
    public void Update()
    {
        //处理cd倒计时
        if(cardData.cardState == CardStateEnum.FightRest)
        {
            CreatureBean creatureData = cardData.creatureData;
            if(creatureData.creatureInfo.create_cd == 0)
            {
                ui_CDTime.gameObject.SetActive(false);
                return;
            } 
            int cdTime = Mathf.CeilToInt(creatureData.creatureInfo.create_cd - creatureData.creatureStateTimeUpdate);
            float progress = (creatureData.creatureInfo.create_cd - creatureData.creatureStateTimeUpdate) / creatureData.creatureInfo.create_cd;
            if (cdTime < 0)
                cdTime = 0;
            if (progress < 0)
                progress = 0;
            SetCDTime($"{cdTime}",progress);
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseState cardUseState, Vector2 originalCardPos)
    {
        this.cardData.cardState = CardStateEnum.FightIdle;

        this.cardData.originalCardPos = originalCardPos;
        this.cardData.originalSibling = transform.GetSiblingIndex();
        gameObject.name = $"UIViewCreatureCardItem_{cardData.originalSibling}";
        //注册 避开卡片的事件
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForForSelectKeep);
        //战斗事件
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<string, CreatureStateEnum>(EventsInfo.GameFightLogic_CreatureChangeState, EventForGameFightLogicCreatureChangeState);

        SetData(creatureData, cardUseState);
    }

    #region 重写
    /// <summary>
    /// 刷新状态
    /// </summary>
    public override void RefreshCardState(CardStateEnum cardState)
    {
        base.RefreshCardState(cardState);
        switch (cardState)
        {
            case CardStateEnum.FightIdle:
                break;
            case CardStateEnum.FightSelect:
                break;
            case CardStateEnum.Fighting:
                ui_Mask.gameObject.SetActive(true);
                break;
            case CardStateEnum.FightRest:
                ui_Mask.gameObject.SetActive(true);
                ui_CDTime.gameObject.SetActive(true);
                break;
        }
    }
    #endregion

    #region 触摸相关事件
    /// <summary>
    /// 触摸-进入
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerEnter_{cardData.originalSibling}");
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //设置层级最上
        transform.SetAsLastSibling();
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, true);
        //进入事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, this);
    }

    /// <summary>
    /// 触摸-退出
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{cardData.originalSibling}");
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //还原层级
        transform.SetSiblingIndex(cardData.originalSibling);
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, cardData.originalSibling, cardData.originalCardPos, false);
        //离开事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerExit, this);
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
        if (this != targetView)
        {
            switch (cardData.cardState)
            {
                //如果是其他选择状态 要取消选择
                case CardStateEnum.FightSelect:
                    SetCardState(CardStateEnum.FightIdle);
                    break;
                default:
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
        //设置卡片状态
        switch (cardData.cardState)
        {
            case CardStateEnum.FightSelect:
                SetCardState(CardStateEnum.FightIdle);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 事件-放置卡片
    /// </summary>
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        if (this != targetView)
            return;
        //设置卡片状态
        SetCardState(CardStateEnum.Fighting);
    }

    /// <summary>
    /// 事件-生物状态修改
    /// </summary>
    public void EventForGameFightLogicCreatureChangeState(string creatureId,CreatureStateEnum creatureState)
    {
        if (!cardData.creatureData.creatureId.Equals(creatureId))
            return;
        if(creatureState == CreatureStateEnum.Idle)
        {
            SetCardState(CardStateEnum.FightIdle);
        }
        else if(creatureState == CreatureStateEnum.Rest)
        {
            SetCardState(CardStateEnum.FightRest);
        }
    }
    #endregion


    #region 动画相关
    /// <summary>
    /// 创建动画
    /// </summary>
    public void AnimForCreateShow(int animType, int index)
    {
        ClearAnim();
        if (animType == 1)
        {
            rectTransform.anchoredPosition = new Vector2(cardData.originalCardPos.x + Screen.width, cardData.originalCardPos.y);
            animForCreate = rectTransform
                .DOAnchorPos(cardData.originalCardPos, animCardCreateTimeType1)
                .SetEase(animCardCreateEase)
                .SetDelay(index * animCardCreateDelayTime);
        }
        else if (animType == 2)
        {
            rectTransform.anchoredPosition = new Vector2(cardData.originalCardPos.x, -500);
            animForCreate = rectTransform
                .DOAnchorPos(cardData.originalCardPos, animCardCreateTimeType2)
                .SetEase(animCardCreateEase)
                .SetDelay(index * animCardCreateDelayTime);
        }
    }

    /// <summary>
    /// 清除所有动画
    /// </summary>
    public void ClearAnim()
    {
        if (animForCreate != null && animForCreate.IsPlaying())
        {
            animForCreate.Complete();
        }
        if (animForSelectStart != null && animForSelectStart.IsPlaying())
        {
            animForSelectStart.Complete();
        }
        if (animForSelectEnd != null && animForSelectEnd.IsPlaying())
        {
            animForSelectEnd.Complete();
        }
        if (animForSelectKeepStart != null && animForSelectKeepStart.IsPlaying())
        {
            animForSelectKeepStart.Complete();
        }
        if (animForSelectKeepEnd != null && animForSelectKeepEnd.IsPlaying())
        {
            animForSelectKeepEnd.Complete();
        }
    }

    /// <summary>
    /// Keep动画关闭
    /// </summary>
    public void KillAnimForKeep()
    {
        if (animForSelectKeepStart != null && animForSelectKeepStart.IsPlaying())
            animForSelectKeepStart.Kill();
        if (animForSelectKeepEnd != null && animForSelectKeepEnd.IsPlaying())
            animForSelectKeepEnd.Kill();
    }

    /// <summary>
    /// 关闭选择动画
    /// </summary>
    public void KillAnimForSelect()
    {
        if (animForSelectStart != null && animForSelectStart.IsPlaying())
            animForSelectStart.Kill();
        if (animForSelectEnd != null && animForSelectEnd.IsPlaying())
            animForSelectEnd.Kill();
    }
    #endregion
}
