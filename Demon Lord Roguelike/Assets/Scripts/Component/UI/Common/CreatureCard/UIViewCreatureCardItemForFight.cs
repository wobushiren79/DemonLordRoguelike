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
    protected float FightRestRCDTime = 0;//复活CD时间（需要在重置是刷新数据）

    //卡片快捷按键(1-9)，按阵容排序分配；-1 表示无快捷键(阵容序号超过9)
    protected int pressKeyNum = -1;

    /// <summary>
    /// 当前卡片的快捷按键序号(1-9)；-1 表示无快捷键(阵容序号超过9)。
    /// 由 UIFightMain 接收 InputActionUIEnum.N1~N9 后按此序号派发选择。
    /// </summary>
    public int PressKeyNum => pressKeyNum;


    public void Update()
    {
        GameStateEnum gameState = GameHandler.Instance.manager.GetGameState();
        //游戏中才刷新
        if (gameState != GameStateEnum.Gaming)
        {
            return;
        }
        //处理cd倒计时
        if (cardData.cardState == CardStateEnum.FightRest)
        {
            CreatureBean creatureData = cardData.creatureData;
            if (FightRestRCDTime == 0)
            {
                ui_CDTime.gameObject.SetActive(false);
                return;
            }
            int cdTime = Mathf.CeilToInt(FightRestRCDTime - creatureData.RCDTimeUpdate);
            float progress = (FightRestRCDTime - creatureData.RCDTimeUpdate) / FightRestRCDTime;
            if (cdTime < 0)
                cdTime = 0;
            if (progress < 0)
                progress = 0;
            SetCDTime($"{cdTime}", progress);
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState, Vector2 originalCardPos)
    {
        //判断一下场上是否已经有该生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //如果场上已经有该生物 则设置为战斗中
        if (gameFightLogic.fightData.GetCreatureById(creatureData.creatureUUId, CreatureFightTypeEnum.FightDefense) != null)
        {
            this.cardData.cardState = CardStateEnum.Fighting;
        }
        //如果场上没有该生物 则设置为待机
        else
        {
            this.cardData.cardState = CardStateEnum.FightIdle;
        }

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
        //按阵容排序设置快捷按键(1-9)与按键提示
        SetPressKey(cardData.originalSibling);
    }

    /// <summary>
    /// 刷新卡片布局位置(卡片数量变化导致整体重排时调用，仅更新位置/层级，不改动卡片战斗状态)
    /// </summary>
    /// <param name="originalCardPos">重排后的目标位置</param>
    /// <param name="sibling">重排后的层级序号</param>
    public void RefreshCardPos(Vector2 originalCardPos, int sibling)
    {
        cardData.originalCardPos = originalCardPos;
        cardData.originalSibling = sibling;
        transform.SetSiblingIndex(sibling);
        gameObject.name = $"UIViewCreatureCardItem_{sibling}";
        //归位到新位置(直接设置，避免与选择/避让动画冲突)
        rectTransform.anchoredPosition = originalCardPos;
        //重排后同步刷新快捷按键(1-9)与按键提示
        SetPressKey(sibling);
    }

    #region 快捷按键
    /// <summary>
    /// 按阵容排序设置卡片的快捷按键(1-9)及按键提示显示。
    /// index 0-8 对应按键 1-9；序号超过 9 (index&gt;=9) 的卡片不设置快捷键并隐藏提示。
    /// </summary>
    /// <param name="index">卡片在阵容中的排序序号(从0开始)</param>
    public void SetPressKey(int index)
    {
        if (index >= 0 && index < 9)
        {
            pressKeyNum = index + 1;
            //设置提示文本(显隐由 UIViewPressCommon 按全局「按键提示显示」设置控制)
            if (ui_UIViewPressCommon != null)
                ui_UIViewPressCommon.SetData($"{pressKeyNum}");
        }
        else
        {
            pressKeyNum = -1;
            //超过9的卡片无快捷键，标记为无效并隐藏(不受全局开关影响)
            if (ui_UIViewPressCommon != null)
                ui_UIViewPressCommon.HideForNoKey();
        }
    }

    /// <summary>
    /// 快捷按键选择处理：等效于点击该卡片进行选择；若该卡片已处于选中状态，再次触发则取消选中(切换效果)。
    /// 由 UIFightMain 接收 InputActionUIEnum.N1~N9 后按 pressKeyNum 派发调用(替代旧的 Input.GetKeyDown 轮询)。
    /// 战斗中/CD中的卡片不可选择(与点击逻辑一致，最终由 UIFightMain.EventForOnClickSelect 校验)。
    /// </summary>
    public void HandleForPressKeySelect()
    {
        if (pressKeyNum < 1 || pressKeyNum > 9)
            return;
        //游戏中才响应快捷键
        if (GameHandler.Instance.manager.GetGameState() != GameStateEnum.Gaming)
            return;
        //屏幕锁定/弹窗遮挡时(与按钮点击同一门禁)不响应快捷键
        if (!UIHandler.Instance.manager.CanClickUIButtons)
            return;
        //战斗中/CD中的卡片不可选择
        if (cardData.cardState == CardStateEnum.Fighting || cardData.cardState == CardStateEnum.FightRest)
            return;
        //已选中时再次触发同一快捷键则取消选中
        if (cardData.cardState == CardStateEnum.FightSelect)
        {
            GameHandler.Instance.manager.GetGameLogic<GameFightLogic>().UnSelectCard();
            return;
        }
        //触发与点击相同的选择流程
        OnClickSelect();
    }
    #endregion

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
                ui_SelectBg.gameObject.SetActive(true);
                break;
            case CardStateEnum.Fighting:
                ui_Mask.gameObject.SetActive(true);
                break;
            case CardStateEnum.FightRest:
                FightRestRCDTime = cardData.creatureData.GetRCD();
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
    public void EventForGameFightLogicCreatureChangeState(string creatureUUID, CreatureStateEnum creatureState)
    {
        if (!cardData.creatureData.creatureUUId.Equals(creatureUUID))
            return;
        if (creatureState == CreatureStateEnum.Idle)
        {
            SetCardState(CardStateEnum.FightIdle);
        }
        else if (creatureState == CreatureStateEnum.Rest)
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
