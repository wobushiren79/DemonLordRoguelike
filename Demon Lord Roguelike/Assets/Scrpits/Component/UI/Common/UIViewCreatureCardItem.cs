using DG.Tweening;
using Spine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
using UnityEngine.UI;

public partial class UIViewCreatureCardItem : BaseUIView, IPointerEnterHandler, IPointerExitHandler
{
    public CreatureCardItemBean cardData = new CreatureCardItemBean();

    [Header("��Ƭ���������ӳ�ʱ��")]
    public float animCardCreateDelayTime = 0.05f;
    [Header("��Ƭ��������ʱ��")]
    public float animCardCreateTimeType1 = 0.8f;
    [Header("��Ƭ��������ʱ��")]
    public float animCardCreateTimeType2 = 0.4f;
    [Header("��Ƭ����������������")]
    public Ease animCardCreateEase = Ease.OutBack;

    [Header("��Ƭѡ�񶯻�����ʱ��")]
    public float animCardSelectStartTime = 0.25f;
    [Header("��Ƭѡ�񶯻���������-����")]
    public Ease animCardSelectStart = Ease.OutBack;
    [Header("��Ƭѡ�񶯻��Ŵ����")]
    public float animCardSelectStartScale = 1.6f;

    [Header("��Ƭѡ�񶯻��˳�ʱ��")]
    public float animCardSelectEndTime = 0.5f;
    [Header("��Ƭѡ�񶯻���������-�˳�")]
    public Ease animCardSelectEnd = Ease.OutBack;

    protected Tween animForCreate;//������Ƭ����
    protected Tween animForSelectStart;//ѡ��Ƭ����
    protected Tween animForSelectEnd;//ѡ��Ƭ����
    protected Tween animForSelectKeepStart;//ѡ��Ƭ���ö���
    protected Tween animForSelectKeepEnd;//ѡ��Ƭ���ö���

    public MaskUIView maskUI;//���ִ���

    //���¼�ѡ��չʾ����
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
    /// ��������
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
    /// ����ϡ�ж�
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
    /// ��������
    /// </summary>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// ���õȼ�
    /// </summary>
    public void SetLevel(int level)
    {
        ui_Level.text = $"{level}";
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetAttribute(int attDamage, int lifeMax)
    {
        ui_AttributeItemText_Att.text = $"{attDamage}";
        ui_AttributeItemText_Life.text = $"{lifeMax}";
    }

    /// <summary>
    /// ���ÿ�Ƭͼ��
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        var creatureInfo = creatureData.GetCreatureInfo();
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        //���ù�������
        SpineHandler.Instance.SetSkeletonDataAsset(ui_Icon, creatureModel.res_name);
        string[] skinArray = creatureData.GetSkinArray();
        //�޸�Ƥ��
        SpineHandler.Instance.ChangeSkeletonSkin(ui_Icon.Skeleton, skinArray);

        ui_Icon.ShowObj(true);
        //����UI��С������
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
    /// ���ÿ���״̬
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
    /// ˢ�¿���״̬
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
    /// ��ť���
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
    /// ���ѡ��
    /// </summary>
    public void OnClickSelect()
    {
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnClickSelect, this);
    }

    #region �������

    /// <summary>
    /// ����-����
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
    /// ����-�˳�
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

    #region �������
    /// <summary>
    /// ��������
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
    /// ������ж���
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
    /// Keep�����ر�
    /// </summary>
    public void KillAnimForKeep()
    {
        if (animForSelectKeepStart != null && animForSelectKeepStart.IsPlaying())
            animForSelectKeepStart.Kill();
        if (animForSelectKeepEnd != null && animForSelectKeepEnd.IsPlaying())
            animForSelectKeepEnd.Kill();
    }

    /// <summary>
    /// �ر�ѡ�񶯻�
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
