using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public partial class UIFightMain : BaseUIComponent
{
    //所有生成的卡片
    public List<UIViewCreatureCardItem> listCreatureCard = new List<UIViewCreatureCardItem>();
    //所有的进攻进度
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
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<FightCreatureBean>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //初始所有卡片
        SetCreatureCardList(gameFightLogic.fightData.listDefCreatureData);
        //设置进攻波次
        gameFightLogic.fightData.GetAttCreateInitData(out int fightNum);
        SetAttCreateData(fightNum);
        //刷新一次UI
        RefreshUIData();
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //设置魔力值
        SetMagicData(gameFightLogic.fightData.currentMagic);
        SetAttCreateProgress(gameFightLogic.fightData.gameStage, gameFightLogic.fightData.gameProgress);
    }

    /// <summary>
    /// 设置进攻数据
    /// </summary>
    public void SetAttCreateData(int fightNum)
    {
        foreach (var itemData in dicAttProgress)
        {
            DestroyImmediate(itemData.Value.gameObject);
        }
        //获取左右X的最大值
        float contentXMax = ui_AttCreate.sizeDelta.x / 2;
        //获取单个进度条长度
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
    /// 设置进攻数据进度
    /// </summary>
    public void SetAttCreateProgress(int stage, float progress)
    {
        if (dicAttProgress.TryGetValue(stage,out UIViewFightMainAttCreateProgress progressView))
        {
            progressView.SetProgress(progress);
        }
    }

    /// <summary>
    /// 设置当前魔力
    /// </summary>
    public void SetMagicData(int magic)
    {
        ui_MagicText.text = $"{magic}";
    }

    /// <summary>
    /// 初始化卡片列表
    /// </summary>
    public void SetCreatureCardList(List<FightCreatureBean> listCreatureData)
    {
        //先清空一下卡片
        ClearCardList();
        if (listCreatureData.IsNull())
        {
            LogUtil.LogError($"初始化卡片列表失败，卡片数据为null");
            return;
        }
        for (int i = 0; i < listCreatureData.Count; i++)
        {
            var itemData = listCreatureData[i];
            var itemCardObj = Instantiate(ui_CardContent.gameObject, ui_CreatureCardItem.gameObject);
            var itemCardView = itemCardObj.GetComponent<UIViewCreatureCardItem>();
            var posTarget = GetCardItemPos(i, listCreatureData.Count);
            //设置数据
            itemCardView.SetData(itemData, posTarget);

            listCreatureCard.Add(itemCardView);
        }

        //展示卡片创建动画
        int animTypeRandom = Random.Range(1, 3);
        ShowCardCreateAnim(animTypeRandom);
    }

    /// <summary>
    /// 展示卡片创建动画
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
    /// 获取卡片原始位置
    /// </summary>
    /// <param name="currentIndex"></param>
    /// <param name="maxIndex"></param>
    /// <returns></returns>
    public Vector2 GetCardItemPos(int currentIndex, int maxIndex)
    {
        float cardW = ui_CreatureCardItem.sizeDelta.x + 10;
        float cardH = ui_CreatureCardItem.sizeDelta.y;
        float screenWidth = Screen.width - cardW;

        //如果超出了屏幕
        if ((cardW * maxIndex) > screenWidth)
        {
            //算出超出的宽度 每个卡片都减去这个宽度
            float ovrW = (cardW * maxIndex) - screenWidth;
            cardW = cardW - (ovrW / maxIndex);
        }

        float posOffset = cardW * currentIndex - (cardW * maxIndex / 2) + (cardW / 2);
        return new Vector2(posOffset, -100);
    }

    /// <summary>
    /// 清空卡片
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

    #region 事件

    /// <summary>
    /// 没有足够的魔力
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
    /// 事件-选择卡片
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(FightCreatureBean targetData)
    {

    }

    /// <summary>
    /// 事件-取消选择的卡片
    /// </summary>
    public void EventForGameFightLogicUnSelectCard(FightCreatureBean targetData)
    {

    }

    /// <summary>
    /// 事件-放置卡片
    /// </summary>
    public void EventForGameFightLogicPutCard(FightCreatureBean targetData)
    {
        RefreshUIData();
    }
    #endregion
}
