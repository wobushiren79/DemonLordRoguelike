using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public partial class UIFightMain : BaseUIComponent
{
    //�������ɵĿ�Ƭ
    public List<UIViewCreatureCardItem> listCreatureCard = new List<UIViewCreatureCardItem>();
    //���еĽ�������
    public Dictionary<int, UIViewFightMainAttCreateProgress> dicAttProgress = new Dictionary<int, UIViewFightMainAttCreateProgress>();

    public override void Awake()
    {
        base.Awake();
        ui_CreatureCardItem.gameObject.SetActive(false);
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
        ui_UIViewFightMainAttCreateProgress.gameObject.SetActive(false);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        if (!isOpenInit)
            RefreshUIData();
    }

    public override void OpenUI()
    {
        base.OpenUI();
        RegisterEvent(EventsInfo.Toast_NoEnoughCreateMagic, EventForNoEnoughCreateMagic);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);

        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_ShowDetails, EventForShowCardDetails);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_HideDetails, EventForHideCardDetails);

        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //��ʼ���п�Ƭ
        SetCreatureCardList(gameFightLogic.fightData.listDefCreatureData);
        //���ý�������
        SetAttackData(gameFightLogic.fightData.fightAttackData);
        //ˢ��һ��UI
        RefreshUIData();
    }

    /// <summary>
    /// ˢ��UI����
    /// </summary>
    public void RefreshUIData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        float progressAttack = gameFightLogic.fightData.fightAttackData.GetAttackProgress();
        float progressAttackTime = 0;
        var currentAttackDetail = gameFightLogic.fightData.fightAttackData.GetCurrentAttackDetailData();
        if (currentAttackDetail != null) progressAttackTime = currentAttackDetail.timeNextAttack;
        SetAttCreateProgress(progressAttack, progressAttackTime);
    }

    /// <summary>
    /// ���ý�������
    /// </summary>
    public void SetAttackData(FightAttackBean fightAttackData)
    {
        int fightNum = 1;
        foreach (var itemData in dicAttProgress)
        {
            DestroyImmediate(itemData.Value.gameObject);
        }
        dicAttProgress.Clear();
        //��ȡ����X�����ֵ
        float contentXMax = ui_AttCreate.sizeDelta.x / 2;
        //��ȡ��������������
        float itemW = ui_AttCreate.sizeDelta.x / fightNum;
        for (int i = 0; i < fightNum; i++)
        {
            GameObject objItem = Instantiate(ui_AttCreate.gameObject, ui_UIViewFightMainAttCreateProgress.gameObject);
            objItem.name = $"ProgressItem_{i + 1}";
            objItem.transform.SetAsFirstSibling();
            RectTransform rtf = (RectTransform)objItem.transform;
            rtf.anchoredPosition = new Vector2(contentXMax - itemW * i, rtf.anchoredPosition.y);
            rtf.sizeDelta = new Vector2(itemW, rtf.sizeDelta.y);
            UIViewFightMainAttCreateProgress itemView = objItem.GetComponent<UIViewFightMainAttCreateProgress>();
            itemView.SetProgress(0);
            dicAttProgress.Add(i + 1, itemView);
        }
    }

    /// <summary>
    /// ���ý������ݽ���
    /// </summary>
    public void SetAttCreateProgress(float progress, float progressAnimTime)
    {
        int stage = 1;
        if (dicAttProgress.TryGetValue(stage, out UIViewFightMainAttCreateProgress progressView))
        {
            progressView.SetProgress(progress, animTime: progressAnimTime);
        }
    }

    /// <summary>
    /// ��ʼ����Ƭ�б�
    /// </summary>
    public void SetCreatureCardList(List<CreatureBean> listCreatureData)
    {
        //�����һ�¿�Ƭ
        ClearCardList();
        if (listCreatureData.IsNull())
        {
            LogUtil.LogError($"��ʼ����Ƭ�б�ʧ�ܣ���Ƭ����Ϊnull");
            return;
        }
        for (int i = 0; i < listCreatureData.Count; i++)
        {
            var itemData = listCreatureData[i];
            var itemCardObj = Instantiate(ui_CardContent.gameObject, ui_CreatureCardItem.gameObject);
            var itemCardView = itemCardObj.GetComponent<UIViewCreatureCardItem>();
            var posTarget = GetCardItemPos(i, listCreatureData.Count);
            //��������
            itemCardView.SetData(itemData, posTarget);

            listCreatureCard.Add(itemCardView);
        }

        //չʾ��Ƭ��������
        int animTypeRandom = Random.Range(1, 3);
        ShowCardCreateAnim(animTypeRandom);
    }

    /// <summary>
    /// չʾ��Ƭ��������
    /// </summary>
    public void ShowCardCreateAnim(int animType)
    {
        if (listCreatureCard.IsNull())
            return;
        for (int i = 0; i < listCreatureCard.Count; i++)
        {
            var itemView = listCreatureCard[i];
            itemView.AnimForCreateShow(animType, i);
        }
    }

    /// <summary>
    /// ��ȡ��Ƭԭʼλ��
    /// </summary>
    /// <param name="currentIndex"></param>
    /// <param name="maxIndex"></param>
    /// <returns></returns>
    public Vector2 GetCardItemPos(int currentIndex, int maxIndex)
    {
        float cardW = ui_CreatureCardItem.sizeDelta.x + 10;
        float cardH = ui_CreatureCardItem.sizeDelta.y;
        float screenWidth = Screen.width - cardW;

        //�����������Ļ
        if ((cardW * maxIndex) > screenWidth)
        {
            //��������Ŀ�� ÿ����Ƭ����ȥ������
            float ovrW = (cardW * maxIndex) - screenWidth;
            cardW = cardW - (ovrW / maxIndex);
        }

        float posOffset = cardW * currentIndex - (cardW * maxIndex / 2) + (cardW / 2);
        return new Vector2(posOffset, -80);
    }

    /// <summary>
    /// ��տ�Ƭ
    /// </summary>
    public void ClearCardList()
    {
        if (listCreatureCard.IsNull())
            return;
        for (int i = 0; i < listCreatureCard.Count; i++)
        {
            var itemCard = listCreatureCard[i];
            DestroyImmediate(itemCard.gameObject);
        }
        listCreatureCard.Clear();
    }

    #region �¼�

    /// <summary>
    /// û���㹻��ħ��
    /// </summary>
    public void EventForNoEnoughCreateMagic()
    {
        ui_ViewBaseInfoContent.PlayAnimForMagicNoEnough();
    }

    /// <summary>
    /// �¼�-ѡ��Ƭ
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(UIViewCreatureCardItem targetView)
    {
        EventForHideCardDetails(targetView);
    }

    /// <summary>
    /// �¼�-ȡ��ѡ��Ŀ�Ƭ
    /// </summary>
    public void EventForGameFightLogicUnSelectCard(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// �¼�-���ÿ�Ƭ
    /// </summary>
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        RefreshUIData();
    }

    public void EventForShowCardDetails(UIViewCreatureCardItem targetView)
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //û��ѡ�п�Ƭʱ����ʾ
        if (gameFightLogic.selectCreature != null)
            return;
        ui_ViewCreatureCardDetails.ShowObj(true);
        ui_ViewCreatureCardDetails.SetData(targetView.cardData.creatureData);
    }

    public void EventForHideCardDetails(UIViewCreatureCardItem targetView)
    {
        ui_ViewCreatureCardDetails.ShowObj(false);
    }


    /// <summary>
    /// �¼�-����ѡ�п�Ƭ
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// �¼�-�����뿪
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// �¼�-���
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //ս���еĿ�Ƭ���ܵ��
        if (targetView.cardData.cardState == CardStateEnum.Fighting)
            return;
        int createMagic = targetView.cardData.creatureData.GetCreateMagic();
        if (gameFightLogic.fightData.currentMagic < createMagic)
        {
            //ħ������
            EventHandler.Instance.TriggerEvent(EventsInfo.Toast_NoEnoughCreateMagic);
            return;
        }
        GameFightLogic fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        fightLogic.SelectCard(targetView);
    }
    #endregion
}
