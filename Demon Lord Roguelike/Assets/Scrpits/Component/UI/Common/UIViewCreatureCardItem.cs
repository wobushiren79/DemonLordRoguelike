using DG.Tweening;
using Spine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
using UnityEngine.UI;

public partial class UIViewCreatureCardItem : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{
    public CreatureCardItemBean cardData = new CreatureCardItemBean();

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

    //长事件选择展示详情
    protected float timeUpdateForShowDetails = -1;
    protected float timeMaxForShowDetails = 1;

    public override void Awake()
    {
        base.Awake();
        maskUI = transform.GetComponent<MaskUIView>();
    }

    public void Update()
    {
        if (timeUpdateForShowDetails >= 0)
        {
            timeUpdateForShowDetails += Time.deltaTime;
            if (timeUpdateForShowDetails >= timeMaxForShowDetails)
            {
                timeUpdateForShowDetails = -1;
                TriggerEvent(EventsInfo.UIViewCreatureCardItem_ShowDetails, this);
            }
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData,CardUseState cardUseState)
    {
        this.cardData.cardUseState = cardUseState;
        this.cardData.creatureData = creatureData;
        int attDamage = creatureData.GetAttDamage();
        int lifeMax = creatureData.GetLife();

        SetCardIcon(creatureData);
        SetAttribute(attDamage, lifeMax);
        SetName(creatureData.creatureName);
        SetLevel(creatureData.level);
        SetRarity(creatureData.rarity);
    }

    /// <summary>
    /// 设置稀有度
    /// </summary>
    public void SetRarity(int rarity)
    {
        if (rarity == 0)
            rarity = 1;
        var rarityInfo = RarityInfoCfg.GetItemData(rarity);
        ColorUtility.TryParseHtmlString(rarityInfo.ui_board_color, out Color boardColor);
        ui_CardBgBorad.color = boardColor;
        maskUI.ChangeDefColor(ui_CardBgBorad, boardColor);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetLevel(int level)
    {
        ui_Level.text = $"{level}";
    }

    /// <summary>
    /// 设置属性
    /// </summary>
    public void SetAttribute(int attDamage, int lifeMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{lifeMax}";
    }

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        var creatureInfo = creatureData.GetCreatureInfo();
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        //设置骨骼数据
        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, creatureModel.res_name);
        string[] skinArray = creatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);

        ui_Icon.ShowObj(true);
        //设置UI大小和坐标
        if (creatureModel.ui_data_s.IsNull())
        {
            ui_Icon.rectTransform.anchoredPosition = Vector2.zero;
            ui_Icon.rectTransform.localScale = Vector3.one;
        }
        else
        {
            string[] uiDataStr = creatureModel.ui_data_s.Split(';');
            ui_Icon.rectTransform.localScale = Vector3.one * float.Parse(uiDataStr[0]);

            float[] uiDataPosStr = uiDataStr[1].SplitForArrayFloat(',');
            ui_Icon.rectTransform.anchoredPosition = new Vector2(uiDataPosStr[0], uiDataPosStr[1]);
        }
    }

    /// <summary>
    /// 设置卡的状态
    /// </summary>
    public void SetCardState(CardStateEnum cardState)
    {
        this.cardData.cardState = cardState;
        if (this.cardData.fightCreatureData != null)
        {
            this.cardData.fightCreatureData.stateForCard = cardState;
        }
        RefreshCardState(cardState);
    }

    /// <summary>
    /// 刷新卡的状态
    /// </summary>
    public void RefreshCardState(CardStateEnum cardState)
    {
        ui_CardBg.color = Color.white;
        maskUI.HideMask();
        switch (cardState)
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

            case CardStateEnum.LineupSelect:
                maskUI.ShowMask();
                break;
            case CardStateEnum.LineupNoSelect:
                break;
        }
    }

    /// <summary>
    /// 按钮点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnSelect)
        {
            OnClickSelect();
        }
    }
    
    /// <summary>
    /// 点击选择
    /// </summary>
    public void OnClickSelect()
    {
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnClickSelect, this);
    }

    #region 点击触发

    /// <summary>
    /// 触摸-进入
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        switch (cardData.cardUseState)
        {
            case CardUseState.Fight:
                OnPointerEnterForFight(eventData);
                break;
        }
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, this);
    }

    /// <summary>
    /// 触摸-退出
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        switch (cardData.cardUseState)
        {
            case CardUseState.Fight:
                OnPointerExitForFight(eventData);
                break;
        }
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerExit, this);
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
