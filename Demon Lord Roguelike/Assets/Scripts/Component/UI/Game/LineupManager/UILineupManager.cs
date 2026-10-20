using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent, IRadioGroupCallBack
{
    //阵容预制体
    public UIViewCreatureCardItem modelLineupItem;
    //阵容标签
    public List<RadioButtonView> listLineupTag = new List<RadioButtonView>();

    // 缓存池里的阵容卡片
    public Queue<UIViewCreatureCardItem> queuePoolCardLineup = new Queue<UIViewCreatureCardItem>();
    // 展示中的阵容卡片
    public List<UIViewCreatureCardItem> listShowCardLineup = new List<UIViewCreatureCardItem>();

    //阵容卡片重排移动时长(增删/拖拽后归位)
    protected float timeForLineupCardMove = 0.2f;
    //上阵新卡从被点击背包卡位置飞入槽位的时长
    protected float timeForLineupCardAddFly = 0.3f;
    //阵容卡片出现动画时长(原地淡入+放大)
    protected float timeForLineupCardMoveInit = 0.25f;
    //阵容卡片出现动画的淡入时长(与放大过冲峰值对齐)
    protected float timeForLineupCardFadeInit = 0.15f;
    //阵容卡片出现动画的起始缩放(小于1,从缩小状态弹出)
    protected float scaleForLineupCardShowStart = 0.85f;
    //阵容卡片出现动画的过冲缩放峰值(大于1,随后回落至1)
    protected float scaleForLineupCardShowOvershoot = 1.08f;
    //卡片动画错开间隔
    protected float timeForLineupCardStagger = 0.04f;
    //卡片动画错开等待总上限(卡片多时延迟封顶,避免末尾卡片等待过久)
    protected float timeForLineupCardStaggerMax = 0.5f;
    //重排时位移卡的缩放脉冲峰值(不额外延长动画时长)
    protected float scaleForLineupCardMovePulse = 1.06f;
    //位置判定阈值:与目标位差小于该值视为未位移(仅过滤浮点/拖拽噪声)
    protected float epsForLineupCardPos = 0.1f;
    //当前阵容的序号
    protected int currentLineupIndex = 1;
    //当前正在拖拽的阵容卡片(非空时屏蔽悬停层级变化,保证拖拽卡始终最上)
    protected UIViewCreatureCardItem currentDragCard;
    public override void Awake()
    {
        base.Awake();
        ui_LineupIndexTitle.SetCallBack(this);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_LineupIndexBtn.gameObject.SetActive(false);

        //重命名按钮仅在已解锁「阵容重命名」研究后显示,并挂悬停气泡
        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        bool isUnlockRename = userUnlock.CheckIsUnlock(UnlockEnum.LineupRename);
        ui_RenameBtn_Button.gameObject.SetActive(isUnlockRename);
        if (isUnlockRename)
        {
            ui_RenameBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(30008), PopupEnum.Text);
        }

        //清空按钮常驻显示,挂悬停气泡
        ui_CleanBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(30010), PopupEnum.Text);

        //默认打开第一套阵容
        currentLineupIndex = 1;
        //防御性清理拖拽标记(拖拽中意外关闭UI时 currentDragCard 可能残留,导致悬停置顶失效)
        currentDragCard = null;
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnBeginDrag, EventForCardBeginDrag);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnEndDrag, EventForCardEndDrag);
        InitLineupTag();
        InitCreatureData();
        InitLineupData();
        //初始化设置标题
        ui_LineupIndexTitle.SetPosition(0, false);
        //刷新UI数据
        RefreshUIData();
    }

    public override void CloseUI()
    {
        ui_UIViewCreatureCardList.CloseUI();
        while (queuePoolCardLineup.Count > 0)
        {
            var targetView = queuePoolCardLineup.Dequeue();
            DestroyImmediate(targetView.gameObject);
        }
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var targetView = listShowCardLineup[i];
            DestroyImmediate(targetView.gameObject);
        }
        listShowCardLineup.Clear();
        //清除所有title
        ui_LineupIndexTitle.DestroyAllChild();
        //保存一下用户数据
        GameDataHandler.Instance.manager.SaveUserData();
        base.CloseUI();
    }

    #region  初始化
    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitCreatureData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var listBackpackCreature = GetSortedBackpackCreature(userData);
        ui_UIViewCreatureCardList.SetData(listBackpackCreature, CardUseStateEnum.LineupBackpack, OnCellChangeForBackpackCreature);
    }

    /// <summary>
    /// 获取阵容管理背包列表的默认排序副本:当前阵容内优先(按当前阵容槽位序号升序,阵容外排后) → 稀有度降序(高→低) → 等级降序(高→低)。
    /// <para>返回新列表,不改动底层存档的 listBackpackCreature 原始顺序;该顺序会作为筛选排序弹窗的稳定基序(次级 tiebreaker)。</para>
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <returns>按默认规则排序后的生物列表副本</returns>
    private List<CreatureBean> GetSortedBackpackCreature(UserDataBean userData)
    {
        var listSource = userData.GetUserBackpackCreatureData().listBackpackCreature;
        var listSorted = new List<CreatureBean>(listSource);
        //当前阵容槽位序号:在当前阵容内取其槽位序号(>=0),否则置最大值排到阵容外
        int LineupOrder(CreatureBean itemData)
        {
            int posIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemData.creatureUUId);
            return posIndex >= 0 ? posIndex : int.MaxValue;
        }
        listSorted.Sort((a, b) =>
        {
            //当前阵容内优先:按槽位序号升序(阵容外统一为最大值排后)
            int lineupCompare = LineupOrder(a).CompareTo(LineupOrder(b));
            if (lineupCompare != 0) return lineupCompare;
            //稀有度降序(高稀有度置前)
            int rarityCompare = b.GetRarityValue().CompareTo(a.GetRarityValue());
            if (rarityCompare != 0) return rarityCompare;
            //等级降序(高等级置前)
            return b.level.CompareTo(a.level);
        });
        return listSorted;
    }

    /// <summary>
    /// 初始化阵容内容(整组建好后只播一次出现动画,避免逐卡添加触发多次全量动画)
    /// </summary>
    public void InitLineupData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        List<CreatureBean> listLineupCreature = userData.GetLineupCreature(currentLineupIndex);
        int totalCard = listLineupCreature.Count;
        for (int i = 0; i < totalCard; i++)
        {
            var creatureData = listLineupCreature[i];
            var itemView = GetOrCreateLineupCardView(creatureData);
            //直接落位到槽位(出现动画为原地淡入放大,不需要起始位置)
            int posIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, creatureData.creatureUUId);
            itemView.transform.localPosition = GetLineupPostion(totalCard, posIndex);
            listShowCardLineup.Add(itemView);
            itemView.gameObject.SetActive(true);
        }
        //整组一次性播出现动画(无卡时内部直接回调)
        AnimForAllLineupCardPosReset(1, null);
    }

    /// <summary>
    /// 初始化阵容标签
    /// </summary>
    public void InitLineupTag()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //获取阵容解锁数量
        int unlockLineupNum = userUnlock.GetUnlockLineupNum();
        //清除所有title
        ui_LineupIndexTitle.DestroyAllChild();
        listLineupTag.Clear();
        ui_LineupIndexTitle.listButton.Clear();
        for (int i = 0; i < unlockLineupNum; i++)
        {
            GameObject objItemTitle = Instantiate(ui_LineupIndexTitle.gameObject, ui_LineupIndexBtn.gameObject);
            RadioButtonView radioButton = objItemTitle.GetComponent<RadioButtonView>();
            SetLineupTagText(radioButton, i + 1);
            listLineupTag.Add(radioButton);
            ui_LineupIndexTitle.AddRadioButton(radioButton);
        }
    }

    /// <summary>
    /// 设置阵容选择标签文本
    /// </summary>
    public void SetLineupTagText(RadioButtonView radioButtonView, int index)
    {
        var titleTex = radioButtonView.GetComponentInChildren<TextMeshProUGUI>();
        titleTex.text = GetLineupShowName(index);
    }

    /// <summary>
    /// 获取阵容显示名字:已改名用自定义名,未改名用默认「阵容 {序号}」多语言文本(委托 UserDataBean 同名单一真实源)
    /// </summary>
    public string GetLineupShowName(int index)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        return userData.GetLineupShowName(index);
    }
    #endregion

    #region  其他
    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        int creatureNumMax = userUnlock.GetUnlockLineupCreatureNum();

        var listCreatureIds = userData.GetLineupCreatureIds(currentLineupIndex);
        //阵容魔物数量文本：满员时数量显示为通用警示红
        string countStr = ColorUtil.WrapLimitFull($"{listCreatureIds.Count}/{creatureNumMax}", listCreatureIds.Count >= creatureNumMax);
        ui_LineupHint.text = string.Format(TextHandler.Instance.GetTextById(30007), countStr);
    }

    /// <summary>
    /// 改变队伍序号
    /// </summary>
    public void ChangeLineupIndex(int indexLineup)
    {
        currentLineupIndex = indexLineup;
        //移除所有展示中的阵容卡片
        RemoveLineupCardShow();
        //初始化阵容卡片
        InitLineupData();
        //重排并刷新背包卡片(默认排序以当前阵容为准,切换阵容后需重排)
        InitCreatureData();
        //刷新UI数据
        RefreshUIData();
    }

    /// <summary>
    /// 获取阵容槽位间距：卡片少时按单卡宽度排布；卡片多到整行放不下时压缩间距，卡片尺寸保持不变
    /// </summary>
    /// <param name="totalNum">当前阵容卡片数量</param>
    /// <returns>相邻卡片的中心间距</returns>
    protected float GetLineupItemWidth(int totalNum)
    {
        float cardW = modelLineupItem.rectTransform.rect.width;
        float wView = ui_LineupContent.rect.width;
        //单张卡无相邻间距概念,其它情况数据异常时按原尺寸
        if (totalNum <= 1 || cardW <= 0f || wView <= 0f)
            return cardW;
        //卡片中心可用范围 = 内容区宽度 - 单卡宽度(首尾卡外沿贴边)，均分到 totalNum-1 个间隔
        float pitch = (wView - cardW) / (totalNum - 1);
        return Mathf.Clamp(pitch, 0f, cardW);
    }

    /// <summary>
    /// 获取阵容卡片位置：首卡左沿贴住 LineupContent 左边界，按槽位间距依次排列(间距未压缩时与按卡宽均分一致)
    /// </summary>
    public Vector3 GetLineupPostion(int maxLineupNum, int lineupPosIndex)
    {
        float wView = ui_LineupContent.rect.width;
        float cardW = modelLineupItem.rectTransform.rect.width;
        float itemW = GetLineupItemWidth(maxLineupNum);
        return new Vector3(-wView / 2f + cardW / 2f + itemW * lineupPosIndex, 0, 0);
    }

    /// <summary>
    /// 由拖拽落点的横坐标反解目标槽位序号(GetLineupPostion 的逆运算)，夹回 [0, 总数-1]
    /// </summary>
    public int GetLineupDropPosIndex(float localX, int maxLineupNum)
    {
        float wView = ui_LineupContent.rect.width;
        float cardW = modelLineupItem.rectTransform.rect.width;
        float itemW = GetLineupItemWidth(maxLineupNum);
        if (itemW <= 0f)
            return 0;
        int posIndex = Mathf.RoundToInt((localX + wView / 2f - cardW / 2f) / itemW);
        return Mathf.Clamp(posIndex, 0, maxLineupNum - 1);
    }

        /// <summary>
    /// 背包列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        //设置选中和未选中状态
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData.CheckIsLineup(currentLineupIndex, itemData.creatureUUId))
        {
            itemView.SetCardState(CardStateEnum.LineupSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.LineupNoSelect);
        }
    }
    
    #endregion
    #region 动画相关
    /// <summary>
    /// 参与阵容重排动画的卡片(仅位置真正发生变化的卡)
    /// </summary>
    private struct LineupCardMoveInfo
    {
        /// <summary>卡片视图</summary>
        public UIViewCreatureCardItem view;
        /// <summary>落位目标本地坐标</summary>
        public Vector3 targetPos;
        /// <summary>错开延迟(秒)</summary>
        public float delay;
        /// <summary>本卡移动时长(秒)</summary>
        public float moveTime;
    }

    /// <summary>
    /// 播放所有lineup卡片重置位置动画
    /// </summary>
    /// <param name="animType">0增删/拖拽后重排 1初始化出现</param>
    /// <param name="actionForComplete">全部动画完成回调(无动画卡时立即回调)</param>
    /// <param name="focusPosIndex">操作点槽位序号(错开延迟的起点,离它越远越晚动);小于0时按最右槽位处理</param>
    /// <param name="moveTimeForFocus">操作点卡片的移动时长(上阵飞入用);小于0时统一用 timeForLineupCardMove</param>
    public void AnimForAllLineupCardPosReset(int animType, Action actionForComplete, int focusPosIndex = -1, float moveTimeForFocus = -1f)
    {
        int totalCard = listShowCardLineup.Count;
        //如果没有卡片，直接回调
        if (totalCard == 0)
        {
            actionForComplete?.Invoke();
            return;
        }
        //统一按槽位排序:列表顺序=视觉顺序,层级与错开方向都以它为准
        SortShowCardLineupByPos();
        if (animType == 1)
        {
            //初始化动画:原地淡入+放大(不再从下方弹入,避免遮挡背包列表)
            AnimForAllLineupCardShow(totalCard, actionForComplete);
            return;
        }

        var userData = GameDataHandler.Instance.manager.GetUserData();
        int focusIndex = focusPosIndex >= 0 ? focusPosIndex : totalCard - 1;
        //第一遍:未位移卡瞬间吸附(不建tween/不发声),位移卡收集目标位置与延迟
        List<LineupCardMoveInfo> listMoveInfo = new List<LineupCardMoveInfo>(totalCard);
        for (int i = 0; i < totalCard; i++)
        {
            var itemCardView = listShowCardLineup[i];
            //层级按槽位从左到右置顶(与是否播动画解耦),拖拽/悬停后还原正确层级
            itemCardView.transform.SetAsLastSibling();
            //杀残留序列并复位缩放/透明度(不改坐标)
            itemCardView.ResetCardAnimStateForReuse();
            var itemLineupPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemCardView.cardData.creatureData.creatureUUId);
            Vector3 itemLineupPos = GetLineupPostion(totalCard, itemLineupPosIndex);
            if ((itemCardView.transform.localPosition - itemLineupPos).sqrMagnitude <= epsForLineupCardPos * epsForLineupCardPos)
            {
                //位置没变:瞬间归位,不参与动画
                itemCardView.transform.localPosition = itemLineupPos;
                continue;
            }
            //错开延迟 = 离操作点的槽位距离 × 间隔,总等待封顶
            float delay = Mathf.Min(Mathf.Abs(itemLineupPosIndex - focusIndex) * timeForLineupCardStagger, timeForLineupCardStaggerMax);
            //操作点卡片(上阵新卡)可用单独时长,如从背包卡飞入槽位
            float itemMoveTime = moveTimeForFocus >= 0f && itemLineupPosIndex == focusIndex ? moveTimeForFocus : timeForLineupCardMove;
            listMoveInfo.Add(new LineupCardMoveInfo
            {
                view = itemCardView,
                targetPos = itemLineupPos,
                delay = delay,
                moveTime = itemMoveTime,
            });
        }

        int animNum = listMoveInfo.Count;
        if (animNum == 0)
        {
            //没有位移卡(如删掉最右卡且间距未压缩):直接回调
            actionForComplete?.Invoke();
            return;
        }

        //第二遍:只有位移卡建序列(缩放脉冲用 Join+Insert 铺满移动时长,序列总时长仍为 delay+timeForMove)
        int completeNum = 0;
        for (int i = 0; i < animNum; i++)
        {
            LineupCardMoveInfo moveInfo = listMoveInfo[i];
            Transform cardTransform = moveInfo.view.transform;
            float delay = moveInfo.delay;
            float timeForMove = moveInfo.moveTime;
            DOTween.Sequence()
                .SetTarget(cardTransform)
                .AppendInterval(delay)
                .Append(cardTransform.DOLocalMove(moveInfo.targetPos, timeForMove).SetEase(Ease.OutBack))
                .Join(cardTransform.DOScale(scaleForLineupCardMovePulse, timeForMove * 0.5f).SetEase(Ease.OutQuad))
                .Insert(delay + timeForMove * 0.5f, cardTransform.DOScale(1f, timeForMove * 0.5f).SetEase(Ease.InOutQuad))
                .OnComplete(() =>
                {
                    completeNum++;
                    if (completeNum == animNum)
                    {
                        actionForComplete?.Invoke();
                    }
                });
        }
    }

    /// <summary>
    /// 播放整组阵容卡片出现动画(原地淡入+放大,按槽位左到右错开;整组只播一遍)
    /// </summary>
    /// <param name="totalCard">当前展示卡片数量</param>
    /// <param name="actionForComplete">全部动画完成回调</param>
    protected void AnimForAllLineupCardShow(int totalCard, Action actionForComplete)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        int completeNum = 0;
        for (int i = 0; i < totalCard; i++)
        {
            var itemCardView = listShowCardLineup[i];
            itemCardView.transform.SetAsLastSibling();
            itemCardView.ResetCardAnimStateForReuse();
            var itemLineupPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, itemCardView.cardData.creatureData.creatureUUId);
            Vector3 itemLineupPos = GetLineupPostion(totalCard, itemLineupPosIndex);
            //i 已按槽位排序=左到右级联;错开延迟在方法内按上限封顶
            itemCardView.AnimForCardShowByFadeScale(itemLineupPos, i, timeForLineupCardStagger,
                timeForLineupCardMoveInit, timeForLineupCardFadeInit, scaleForLineupCardShowStart, scaleForLineupCardShowOvershoot, () =>
                {
                    completeNum++;
                    if (completeNum == totalCard)
                    {
                        actionForComplete?.Invoke();
                    }
                }, timeForLineupCardStaggerMax);
        }
    }
    #endregion
    #region 阵容里卡片相关处理
    /// <summary>
    /// 从池中取(或新建)一张阵容卡片并设置数据(不加入展示列表、不播动画)
    /// </summary>
    /// <param name="creatureData">生物数据</param>
    /// <returns>设置好数据的卡片视图</returns>
    protected UIViewCreatureCardItem GetOrCreateLineupCardView(CreatureBean creatureData)
    {
        UIViewCreatureCardItem lineupView;
        if (queuePoolCardLineup.Count > 0)
        {
            lineupView = queuePoolCardLineup.Dequeue();
        }
        else
        {
            //BaseMonoBehaviour.Instantiate(父容器, 模板):把卡片模板实例化到 lineup 容器下
            GameObject ObjNewCard = Instantiate(ui_LineupContent.gameObject, modelLineupItem.gameObject);
            lineupView = ObjNewCard.GetComponent<UIViewCreatureCardItem>();
        }
        lineupView.SetData(creatureData, CardUseStateEnum.Lineup);
        //池复用时先复位缩放/透明度,避免上一轮动画残留
        lineupView.ResetCardAnimStateForReuse();
        return lineupView;
    }

    /// <summary>
    /// 增加阵容里面的卡片
    /// </summary>
    /// <param name="creatureData">生物数据</param>
    /// <param name="startPos">起始位置(上阵时=被点击背包卡在阵容容器里的局部坐标,新卡从这里飞入槽位)</param>
    /// <param name="animType">0默认(飞入) 1初始化出现</param>
    public void AddLineupCard(CreatureBean creatureData, Vector3 startPos, int animType = 0)
    {
        UIViewCreatureCardItem lineupView = GetOrCreateLineupCardView(creatureData);
        lineupView.transform.localPosition = startPos;
        listShowCardLineup.Add(lineupView);
        lineupView.gameObject.SetActive(true);
        //操作点=新卡自己的目标槽位:新卡零延迟先飞入(单独时长),其余位移卡按距离依次跟上
        int focusPosIndex = GameDataHandler.Instance.manager.GetUserData()
            .GetLineupCreaturePosIndex(currentLineupIndex, creatureData.creatureUUId);
        AnimForAllLineupCardPosReset(animType, null, focusPosIndex, timeForLineupCardAddFly);
    }

    /// <summary>
    /// 移除阵容里的卡片
    /// </summary>
    /// <param name="targetView">被移除的卡片</param>
    /// <param name="animType">0默认 1初始化</param>
    /// <param name="focusPosIndex">操作点槽位序号(被移除卡片的原槽位);小于0时按最右槽位处理</param>
    public void RemoveLineupCard(UIViewCreatureCardItem targetView, int animType = 0, int focusPosIndex = -1)
    {
        //入池前复位动画状态(杀残留序列+缩放/透明度回满),避免隐藏卡片继续播放动画/音效
        targetView.ResetCardAnimStateForReuse();
        targetView.gameObject.SetActive(false);
        queuePoolCardLineup.Enqueue(targetView);
        listShowCardLineup.Remove(targetView);

        AnimForAllLineupCardPosReset(animType, null, focusPosIndex);
    }

    /// <summary>
    /// 移除所有展示的阵容卡片
    /// </summary>
    public void RemoveLineupCardShow()
    {
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            var targetView = listShowCardLineup[i];
            //入池前复位动画状态(杀残留序列+缩放/透明度回满),避免快速切换页签时旧阵容动画/音效继续播放
            targetView.ResetCardAnimStateForReuse();
            targetView.gameObject.SetActive(false);
            queuePoolCardLineup.Enqueue(targetView);
        }
        listShowCardLineup.Clear();
    }

    /// <summary>
    /// 按当前阵容槽位序号重排展示中的卡片列表(列表顺序=从左到右的视觉顺序,作为层级与动画错开顺序的基准)
    /// </summary>
    protected void SortShowCardLineupByPos()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        listShowCardLineup.Sort((a, b) =>
        {
            int posA = userData.GetLineupCreaturePosIndex(currentLineupIndex, a.cardData.creatureData.creatureUUId);
            int posB = userData.GetLineupCreaturePosIndex(currentLineupIndex, b.cardData.creatureData.creatureUUId);
            return posA.CompareTo(posB);
        });
    }

    /// <summary>
    /// 重排阵容卡片层级:按槽位从左到右依次置顶(最右卡层级最高),用于悬停结束后还原层级
    /// </summary>
    protected void RefreshLineupCardLayer()
    {
        SortShowCardLineupByPos();
        for (int i = 0; i < listShowCardLineup.Count; i++)
        {
            listShowCardLineup[i].transform.SetAsLastSibling();
        }
    }
    #endregion
    #region  点击相关

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_RenameBtn_Button)
        {
            OnClickForRename();
        }
        else if (viewButton == ui_CleanBtn_Button)
        {
            OnClickForClean();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 点击重命名:打开重命名弹窗,改名当前选中阵容(提交空名=清除自定义名,恢复默认显示)
    /// </summary>
    public void OnClickForRename()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        DialogRenameBean dialogRenameData = new DialogRenameBean();
        //输入框默认填入当前阵容显示名(自定义名优先,否则默认「阵容 {序号}」)
        dialogRenameData.inputContent = GetLineupShowName(currentLineupIndex);
        dialogRenameData.characterLimit = 10;
        dialogRenameData.actionSubmit = (view, data) =>
        {
            string newName = dialogRenameData.inputContent.IsNull() ? "" : dialogRenameData.inputContent.Trim();
            userData.SetLineupName(currentLineupIndex, newName);
            //刷新当前页签文本
            SetLineupTagText(listLineupTag[currentLineupIndex - 1], currentLineupIndex);
            GameDataHandler.Instance.manager.SaveUserData();
            //弹出提示
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000003), 1);
        };
        UIHandler.Instance.ShowDialogRename(dialogRenameData);
    }

    /// <summary>
    /// 点击清空:阵容非空时弹确认框,确认后清空当前阵容的所有魔物
    /// </summary>
    public void OnClickForClean()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //阵容为空时不处理(无可清空内容,不弹确认框)
        if (userData.GetLineupCreatureIds(currentLineupIndex).Count == 0)
            return;
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(30011);
        dialogData.actionSubmit = (view, data) =>
        {
            CleanCurrentLineup();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 清空当前阵容的所有魔物(与单卡移除一致,由 CloseUI 统一落盘)
    /// </summary>
    public void CleanCurrentLineup()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //复制列表后再逐个移除(边遍历边删会改原集合)
        var listLineupCreatureIds = new List<string>(userData.GetLineupCreatureIds(currentLineupIndex));
        foreach (var creatureUUId in listLineupCreatureIds)
        {
            userData.RemoveLineupCreature(currentLineupIndex, creatureUUId);
        }
        //移除所有展示中的阵容卡片
        RemoveLineupCardShow();
        //刷新背包卡片选中状态
        ui_UIViewCreatureCardList.RefreshAllCard();
        //刷新UI数据
        RefreshUIData();
    }
    #endregion

    #region 回调相关
    /// <summary>
    /// 事件-焦点选中卡片(悬停卡置顶;拖拽中屏蔽,保证拖拽卡始终最上)
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {
        //仅阵容行卡片响应悬停层级(背包列表卡片不参与,避免打乱列表顺序)
        if (targetView.cardData.cardUseState != CardUseStateEnum.Lineup)
            return;
        if (currentDragCard != null)
            return;
        targetView.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 事件-焦点离开(还原为槽位层级;拖拽中屏蔽,划过时其他卡的进出事件不生效)
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {
        if (targetView.cardData.cardUseState != CardUseStateEnum.Lineup)
            return;
        if (currentDragCard != null)
            return;
        //悬停结束,按槽位重排所有卡片层级
        RefreshLineupCardLayer();
    }

    /// <summary>
    /// 事件-点击
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {
        //点击阵容卡片时播放卡片音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_card_1);
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (targetView.cardData.cardUseState == CardUseStateEnum.LineupBackpack && targetView.cardData.cardState == CardStateEnum.LineupNoSelect)
        {
            //如果已经超过阵容生物上限
            var userUnlock = userData.GetUserUnlockData();
            int creatureNumMax = userUnlock.GetUnlockLineupCreatureNum();
            var listLineupCreatureId = userData.GetLineupCreature(currentLineupIndex);
            if (listLineupCreatureId.Count >= creatureNumMax)
            {       
                //弹出提示
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(30006));
                return;
            }
            userData.AddLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId);
            //刷新背包里的卡片
            ui_UIViewCreatureCardList.RefreshCardByIndex(targetView.cardData.indexList);
            //增加阵容卡
            Vector3 posStart = UGUIUtil.GetRootPos(ui_LineupContent.transform, targetView.transform);
            AddLineupCard(targetView.cardData.creatureData, posStart);
            //刷新UI数据
            RefreshUIData();
        }
        else if (targetView.cardData.cardUseState == CardUseStateEnum.Lineup)
        {
            //操作点=被移除卡片的原槽位(删除后数据中已查不到,必须先取)
            int removedPosIndex = userData.GetLineupCreaturePosIndex(currentLineupIndex, targetView.cardData.creatureData.creatureUUId);
            userData.RemoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId);
            //刷新背包里的卡片
            ui_UIViewCreatureCardList.RefreshCardByCreatureUUId(targetView.cardData.creatureData.creatureUUId);
            //移除阵容卡
            RemoveLineupCard(targetView, 0, removedPosIndex);
            //刷新UI数据
            RefreshUIData();
        }
    }

    /// <summary>
    /// 事件-开始拖拽阵容卡片(卡片自身已置顶跟手,此处记录拖拽卡以屏蔽悬停层级变化)
    /// </summary>
    public void EventForCardBeginDrag(UIViewCreatureCardItem targetView)
    {
        currentDragCard = targetView;
    }

    /// <summary>
    /// 事件-结束拖拽阵容卡片：按落点横坐标反解目标槽位换位，随后全部卡片吸附归位(夹回)并按新槽位重排层级
    /// </summary>
    public void EventForCardEndDrag(UIViewCreatureCardItem targetView)
    {
        currentDragCard = null;
        int totalCard = listShowCardLineup.Count;
        //操作点=拖拽落点槽位(拖拽卡自身距离0先吸附,其余位移卡按距离依次跟上)
        int focusPosIndex = -1;
        if (totalCard > 1)
        {
            focusPosIndex = GetLineupDropPosIndex(targetView.transform.localPosition.x, totalCard);
            var userData = GameDataHandler.Instance.manager.GetUserData();
            userData.MoveLineupCreature(currentLineupIndex, targetView.cardData.creatureData.creatureUUId, focusPosIndex);
            //换位后按新槽位重排展示列表,让层级跟随视觉位置(最右最上),避免拖拽卡残留旧层级
            SortShowCardLineupByPos();
        }
        //所有卡吸附到新槽位(含夹回)
        AnimForAllLineupCardPosReset(0, null, focusPosIndex);
    }

    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
        for (int i = 0; i < listLineupTag.Count; i++)
        {
            var itemLineupTag = listLineupTag[i];
            if (rbview == itemLineupTag)
            {
                ChangeLineupIndex(position + 1);
            }
        }
    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
    #endregion
}
