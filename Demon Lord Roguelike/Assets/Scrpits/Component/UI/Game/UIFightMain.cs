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
        ui_UIViewFightMainAttCreateProgress.gameObject.SetActive(false);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        RegisterEvent(EventsInfo.Toast_NoEnoughCreateMagic, EventForNoEnoughCreateMagic);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<GameFightCreatureEntity>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<GameFightCreatureEntity>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
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
        gameFightLogic.fightData.GetAttCreateInitData(out int fightNum);
        SetAttCreateData(fightNum);
        //ˢ��һ��UI
        RefreshUIData();
    }

    /// <summary>
    /// ˢ��UI����
    /// </summary>
    public void RefreshUIData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //����ħ��ֵ
        SetMagicData(gameFightLogic.fightData.currentMagic);
        SetAttCreateProgress(gameFightLogic.fightData.gameStage, gameFightLogic.fightData.gameProgress);
    }

    /// <summary>
    /// ���ý�������
    /// </summary>
    public void SetAttCreateData(int fightNum)
    {
        foreach (var itemData in dicAttProgress)
        {
            DestroyImmediate(itemData.Value.gameObject);
        }
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
    public void SetAttCreateProgress(int stage, float progress)
    {
        if (dicAttProgress.TryGetValue(stage,out UIViewFightMainAttCreateProgress progressView))
        {
            progressView.SetProgress(progress);
        }
    }

    /// <summary>
    /// ���õ�ǰħ��
    /// </summary>
    public void SetMagicData(int magic)
    {
        ui_MagicText.text = $"{magic}";
    }

    /// <summary>
    /// ��ʼ����Ƭ�б�
    /// </summary>
    public void SetCreatureCardList(List<FightCreatureBean> listCreatureData)
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
            //��������Ŀ��� ÿ����Ƭ����ȥ�������
            float ovrW = (cardW * maxIndex) - screenWidth;
            cardW = cardW - (ovrW / maxIndex);
        }

        float posOffset = cardW * currentIndex - (cardW * maxIndex / 2) + (cardW / 2);
        return new Vector2(posOffset, -100);
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
        ui_MagicText.DOKill();
        ui_MagicText.DOColor(Color.red, 0.05f).SetLoops(6, LoopType.Yoyo).OnComplete(() =>
        {
            ui_MagicText.color = Color.white;
        });
    }

    /// <summary>
    /// �¼�-ѡ��Ƭ
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(FightCreatureBean targetData)
    {

    }

    /// <summary>
    /// �¼�-ȡ��ѡ��Ŀ�Ƭ
    /// </summary>
    public void EventForGameFightLogicUnSelectCard(GameFightCreatureEntity gameFightCreatureEntity)
    {

    }

    /// <summary>
    /// �¼�-���ÿ�Ƭ
    /// </summary>
    public void EventForGameFightLogicPutCard(GameFightCreatureEntity gameFightCreatureEntity)
    {
        RefreshUIData();
    }
    #endregion
}