using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public partial class UIFightMain : BaseUIComponent
{
    //所有生成的卡片
    public List<UIViewCreatureCardItemForFight> listCreatureCard = new List<UIViewCreatureCardItemForFight>();

    public override void Awake()
    {
        base.Awake();
        ui_CreatureCardItem.gameObject.SetActive(false);
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
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_SelectCard, EventForGameFightLogicSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_UnSelectCard, EventForGameFightLogicUnSelectCard);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.GameFightLogic_PutCard, EventForGameFightLogicPutCard);

        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);

        //新增防御生物(如深渊馈赠增殖复制)：增量添加卡片，不重建整个列表
        RegisterEvent<CreatureBean>(EventsInfo.Buff_DefenseCreatureAdd, EventForDefenseCreatureAdd);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnRemoveCreature)
        {
            OnClickForRemoveCreature();
        }
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listCreatureData = gameFightLogic.fightData.dlDefenseCreatureData.List;

        //初始所有卡片
        SetCreatureCardList(listCreatureData);
        //初始进攻进度
        ui_UIViewFightMainAttCreateProgress.SetProgress(0);
        //刷新一次UI
        RefreshUIData();
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        //征服模式专属UI显隐控制(关卡进度文本 / 进攻进度条)
        bool isConquer = gameFightLogic.fightData.gameFightType == GameFightTypeEnum.Conquer;
        SetConquerUIShow(isConquer);
        //仅征服模式刷新当前关卡进度
        if (isConquer)
        {
            SetFightLevel(gameFightLogic.fightData.fightNum, gameFightLogic.fightData.figthNumMax);
        }

        float progressAttack = gameFightLogic.fightData.fightAttackData.GetAttackProgress();
        float progressAttackTime = 0;
        var currentAttackDetail = gameFightLogic.fightData.fightAttackData.GetCurrentAttackDetailData();
        if (currentAttackDetail != null) progressAttackTime = currentAttackDetail.timeNextAttack;
        SetAttCreateProgress(progressAttack, progressAttackTime);
    }

    /// <summary>
    /// 设置当前关卡进度文本(仅征服模式)
    /// 文本格式：当前征程：{0}/{1}
    /// </summary>
    /// <param name="currentLevel">当前关卡数</param>
    /// <param name="maxLevel">最大关卡数</param>
    public void SetFightLevel(int currentLevel, int maxLevel)
    {
        if (ui_FightLevel == null)
            return;
        ui_FightLevel.text = string.Format(TextHandler.Instance.GetTextById(50005), currentLevel, maxLevel);
    }

    /// <summary>
    /// 设置征服模式专属UI显隐(关卡进度文本 / 进攻进度条)
    /// 当前仅征服模式展示，其余模式隐藏
    /// </summary>
    /// <param name="isShow">是否显示</param>
    public void SetConquerUIShow(bool isShow)
    {
        if (ui_FightLevel != null)
            ui_FightLevel.gameObject.SetActive(isShow);
        if (ui_UIViewFightMainAttCreateProgress != null)
            ui_UIViewFightMainAttCreateProgress.gameObject.SetActive(isShow);
    }

    /// <summary>
    /// 设置进攻数据进度
    /// </summary>
    public void SetAttCreateProgress(float progress, float progressAnimTime)
    {
        ui_UIViewFightMainAttCreateProgress.SetProgress(progress, animTime: progressAnimTime);
    }

    /// <summary>
    /// 初始化卡片列表
    /// </summary>
    public void SetCreatureCardList(List<CreatureBean> listCreatureData)
    {
        //先清空一下卡片
        ClearCardList();
        if (listCreatureData.IsNull())
        {
            LogUtil.LogError($"初始化卡片列表失败，卡片数据为null");
            return;
        }
        //排个序
        listCreatureData = listCreatureData
            .OrderBy((itemData) =>
            {
                return itemData.order;
            })
            .ToList();

        for (int i = 0; i < listCreatureData.Count; i++)
        {
            var itemData = listCreatureData[i];
            var itemCardObj = Instantiate(ui_CardContent.gameObject, ui_CreatureCardItem.gameObject);
            var itemCardView = itemCardObj.GetComponent<UIViewCreatureCardItemForFight>();
            var posTarget = GetCardItemPos(i, listCreatureData.Count);
            //设置数据
            itemCardView.SetData(itemData, CardUseStateEnum.Fight, posTarget);

            listCreatureCard.Add(itemCardView);
        }

        //展示卡片创建动画
        int animTypeRandom = Random.Range(1, 3);
        ShowCardCreateAnim(animTypeRandom);
    }

    /// <summary>
    /// 增量新增一张防御生物卡片(不重建整个列表，保留其它卡片的Rest/Fighting等状态)
    /// 已存在对应UUID的卡片时跳过，保证幂等(事件与同步双通道不会重复添加)
    /// </summary>
    /// <param name="creatureData">新增的防御生物数据</param>
    public void AddCreatureCard(CreatureBean creatureData)
    {
        if (creatureData == null)
            return;
        //幂等：已存在对应卡片则跳过
        for (int i = 0; i < listCreatureCard.Count; i++)
        {
            var existCardData = listCreatureCard[i].cardData;
            if (existCardData != null && existCardData.creatureData != null
                && existCardData.creatureData.creatureUUId.Equals(creatureData.creatureUUId))
            {
                return;
            }
        }

        //实例化单张卡片(位置先给0，随后统一重排)
        var itemCardObj = Instantiate(ui_CardContent.gameObject, ui_CreatureCardItem.gameObject);
        var itemCardView = itemCardObj.GetComponent<UIViewCreatureCardItemForFight>();
        itemCardView.SetData(creatureData, CardUseStateEnum.Fight, Vector2.zero);
        listCreatureCard.Add(itemCardView);

        //数量变化后重新计算所有卡片位置
        RefreshCardListPos();

        //新卡片单独播放创建动画
        int animTypeRandom = Random.Range(1, 3);
        itemCardView.AnimForCreateShow(animTypeRandom, 0);
    }

    /// <summary>
    /// 同步卡片列表与防御生物数据
    /// 用于补齐在UI关闭期间新增的生物(如选择深渊馈赠增殖时，UIFightMain已被馈赠界面关闭，事件无法送达)
    /// </summary>
    public void SyncCreatureCardList()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listCreatureData = gameFightLogic.fightData.dlDefenseCreatureData.List;
        if (listCreatureData.IsNull())
            return;
        for (int i = 0; i < listCreatureData.Count; i++)
        {
            AddCreatureCard(listCreatureData[i]);
        }
    }

    /// <summary>
    /// 重新计算并应用所有卡片的布局位置(卡片数量变化后调用)
    /// </summary>
    public void RefreshCardListPos()
    {
        int count = listCreatureCard.Count;
        for (int i = 0; i < count; i++)
        {
            var itemView = listCreatureCard[i];
            var posTarget = GetCardItemPos(i, count);
            itemView.RefreshCardPos(posTarget, i);
        }
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
        return new Vector2(posOffset, -80);
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

    #region 点击按钮
    public void OnClickForRemoveCreature()
    {
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        gameFightLogic.SelectCreatureDestroy();
    }
    #endregion

    #region 事件
    /// <summary>
    /// 事件-选择卡片
    /// </summary>
    /// <param name="targetData"></param>
    public void EventForGameFightLogicSelectCard(UIViewCreatureCardItem targetView)
    {
        
    }

    /// <summary>
    /// 事件-取消选择的卡片
    /// </summary>
    public void EventForGameFightLogicUnSelectCard(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-放置卡片
    /// </summary>
    public void EventForGameFightLogicPutCard(UIViewCreatureCardItem targetView)
    {
        RefreshUIData();
    }

    /// <summary>
    /// 事件-新增防御生物(如深渊馈赠增殖复制)
    /// UI处于打开状态时通过该事件实时增量加卡；若UI关闭期间触发(事件丢失)，由重开时的SyncCreatureCardList补齐
    /// </summary>
    public void EventForDefenseCreatureAdd(CreatureBean creatureData)
    {
        AddCreatureCard(creatureData);
    }

    /// <summary>
    /// 事件-焦点选中卡片
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-焦点离开
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {

    }

    /// <summary>
    /// 事件-点击
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        //战斗中的卡片不能点击
        if (targetView.cardData.cardState == CardStateEnum.Fighting)
            return;
        //CD中的卡片不能点击
        if (targetView.cardData.cardState == CardStateEnum.FightRest)
            return;
        GameFightLogic fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        fightLogic.SelectCard(targetView);
    }
    #endregion
}
