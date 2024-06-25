using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public partial class UIViewCreatureCardItem : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{
    public FightCreatureBean fightCreatureData;//卡片数据

    public Vector2 originalCardPos;//卡片的起始位置
    public int originalSibling;//卡片的原始层级

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

    public MaskUIView maskUI;//遮罩处理

    public override void Awake()
    {
        base.Awake();
        maskUI = transform.GetComponent<MaskUIView>();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(FightCreatureBean fightCreatureData, Vector2 originalCardPos)
    {
        this.fightCreatureData = fightCreatureData;
        this.fightCreatureData.stateForCard = CardStateEnum.FightIdle;

        this.originalCardPos = originalCardPos;
        this.originalSibling = transform.GetSiblingIndex();
        gameObject.name = $"UIViewCreatureCardItem_{originalSibling}";
        //注册 避开卡片的事件
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForSelectKeep);
        //战斗事件
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_RefreshCard, EventForGameFightLogicRefreshCard);
    }

    /// <summary>
    /// 设置卡的状态
    /// </summary>
    public void SetCardState(CardStateEnum cardState)
    {
        this.fightCreatureData.stateForCard = cardState;
        RefreshCardState();
    }

    /// <summary>
    /// 刷新卡的状态
    /// </summary>
    public void RefreshCardState()
    {
        ui_CardBg.color = Color.white;
        maskUI.HideMask();
        switch (this.fightCreatureData.stateForCard)
        {
            case CardStateEnum.FightIdle:
                break;
            case CardStateEnum.FightSelect:
                ui_CardBg.color = Color.green;
                break;
            case CardStateEnum.Fighting:
                maskUI.ShowMask();
                break;
            case CardStateEnum.FightRest:
                break;
        }
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnSelect)
        {
            OnClickSelectForFight();
        }
    }

    #region 点击触发
    /// <summary>
    /// 触摸-进入
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
       //LogUtil.Log($"OnPointerEnter_{originalSibling}");
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //设置层级最上
        transform.SetAsLastSibling();
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, true);
    }

    /// <summary>
    /// 触摸-退出
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{originalSibling}");
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //还原层级
        transform.SetSiblingIndex(originalSibling);
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, false);
    }
    #endregion

    #region 事件响应
    /// <summary>
    /// 事件 避让卡片
    /// </summary>
    /// <param name="targetIndex">目标序列</param>
    public void EventForSelectKeep(int targetIndex, Vector2 targetPos, bool isKeep)
    {
        if (isKeep)
        {
            int offsetIndex = Mathf.Abs(originalSibling - targetIndex);
            //当前卡距离目标卡的距离
            float disTwoCard = Mathf.Abs(originalCardPos.x - targetPos.x);
            //单张卡间距
            float disOneCard = disTwoCard / offsetIndex;
            //获取最靠近的卡片应该移动的位置（卡片的一半加上扩大后的一半 减去 最靠近卡片的距离）
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
                //把Keep动画关闭
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
                //把Keep动画关闭
                KillAnimForKeep();
                animForSelectKeepEnd = rectTransform
                    .DOAnchorPos(originalCardPos, animCardSelectEndTime)
                    .SetEase(animCardSelectEnd);
            }
        }
        //LogUtil.Log($"EventForSelectKeep originalSibling_{originalSibling} targetIndex_{targetIndex} targetPos_{targetPos}  isKeep_{isKeep}");
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
            rectTransform.anchoredPosition = new Vector2(originalCardPos.x + Screen.width, originalCardPos.y);
            animForCreate = rectTransform
                .DOAnchorPos(originalCardPos, animCardCreateTimeType1)
                .SetEase(animCardCreateEase)
                .SetDelay(index * animCardCreateDelayTime);
        }
        else if (animType == 2)
        {
            rectTransform.anchoredPosition = new Vector2(originalCardPos.x, -500);
            animForCreate = rectTransform
                .DOAnchorPos(originalCardPos, animCardCreateTimeType2)
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
