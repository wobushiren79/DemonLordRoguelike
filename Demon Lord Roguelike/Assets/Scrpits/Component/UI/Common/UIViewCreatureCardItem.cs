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
    public FightCreatureBean fightCreatureData;//��Ƭ����

    public Vector2 originalCardPos;//��Ƭ����ʼλ��
    public int originalSibling;//��Ƭ��ԭʼ�㼶

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

    public override void Awake()
    {
        base.Awake();
        maskUI = transform.GetComponent<MaskUIView>();
    }

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
        RegisterEvent<int, Vector2, bool>(EventsInfo.UIViewCreatureCardItem_SelectKeep, EventForSelectKeep);
        //ս���¼�
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_RefreshCard, EventForGameFightLogicRefreshCard);
    }

    /// <summary>
    /// ���ÿ���״̬
    /// </summary>
    public void SetCardState(CardStateEnum cardState)
    {
        this.fightCreatureData.stateForCard = cardState;
        RefreshCardState();
    }

    /// <summary>
    /// ˢ�¿���״̬
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

    #region �������
    /// <summary>
    /// ����-����
    /// </summary>
    /// <param name="eventData"></param>
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
       //LogUtil.Log($"OnPointerEnter_{originalSibling}");
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
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{originalSibling}");
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //��ԭ�㼶
        transform.SetSiblingIndex(originalSibling);
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, false);
    }
    #endregion

    #region �¼���Ӧ
    /// <summary>
    /// �¼� ���ÿ�Ƭ
    /// </summary>
    /// <param name="targetIndex">Ŀ������</param>
    public void EventForSelectKeep(int targetIndex, Vector2 targetPos, bool isKeep)
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
