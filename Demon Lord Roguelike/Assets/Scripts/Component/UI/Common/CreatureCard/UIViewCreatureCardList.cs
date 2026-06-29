

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewCreatureCardList : BaseUIView
{
    //生物数据(派生的"已筛选+已排序"展示列表)
    protected List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //生物数据主列表(未筛选的全量数据,作为筛选排序的数据源)
    protected List<CreatureBean> listCreatureDataAll = new List<CreatureBean>();
    //卡片的使用地方
    protected CardUseStateEnum cardUseState;
    //卡片变化回调
    protected Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange;
    //当前条件(默认空=无条件;名字/等级/稀有度命中项置顶,排序键 Lineup/Class 次级固定正序;不删行、全部展示)
    protected OrderFilterResultBean currentFilter = new OrderFilterResultBean();

    public override void Awake()
    {
        base.Awake();
        ui_CreatureListContent.AddCellListener(OnCellChangeForCreatrue);
        //排序筛选按钮的悬浮详情:筛选排序
        ui_OrderBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000014), PopupEnum.Text);
    }

    public override void OpenUI()
    {
        base.OpenUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_CreatureListContent.SetCellCount(0);
        ui_CreatureListContent.ClearAllCell();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_OrderBtn_Button)
        {
            ShowOrderFilterDialog();
        }
    }

    /// <summary>
    /// 刷新指定卡片
    /// </summary>
    public void RefreshCardByIndex(int index)
    {
        ui_CreatureListContent.RefreshCell(index);
    }

    /// <summary>
    /// 刷新指定卡片
    /// </summary>
    public void RefreshCardByCreatureUUId(string creatureUUId)
    {
        listCreatureData.ForEach((index, itemData) =>
        {
            if (creatureUUId.Equals(itemData.creatureUUId))
            {
                RefreshCardByIndex(index);
            }
        });
    }

    /// <summary>
    /// 刷新所有卡片
    /// </summary>
    public void RefreshAllCard()
    {
        ui_CreatureListContent.RefreshAllCells();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<CreatureBean> listData, CardUseStateEnum cardUseState, Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange = null)
    {
        gameObject.SetActive(true);
        this.cardUseState = cardUseState;
        this.actionForOnCellChange = actionForOnCellChange;
        //存全量主列表副本(筛选排序的数据源)
        listCreatureDataAll.Clear();
        listCreatureDataAll.AddRange(listData);
        //按当前筛选排序生成展示列表,并刷新数量/空提示
        RefreshFilterSortList();
    }

    /// <summary>
    /// 刷新空列表提示文本：列表无魔物时显示"没有相关魔物"
    /// </summary>
    protected void RefreshNullText()
    {
        bool isEmpty = listCreatureData == null || listCreatureData.Count == 0;
        ui_UIViewNullText_UITextLanguageView.gameObject.SetActive(isEmpty);
    }

    /// <summary>
    /// 获取列表单个数据
    /// </summary>
    public CreatureBean GetItemData(int index)
    {
        if (index >= listCreatureData.Count)
        {
            LogUtil.LogError($"获取单个生物数据失败 超过下标 index_{index} Count_{listCreatureData.Count}");
            return null;
        }
        return listCreatureData[index];
    }

    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForCreatrue(ScrollGridCell itemCell)
    {
        var itemData = listCreatureData[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.cardData.indexList = itemCell.index;
        itemView.SetData(itemData, cardUseState);
        actionForOnCellChange?.Invoke(itemCell.index, itemView, itemData);
    }

    #region 筛选排序
    /// <summary>
    /// 打开筛选排序弹窗(在排序按钮处弹出,可多选筛选类型并按选择顺序定优先级,确认后排序)
    /// </summary>
    protected void ShowOrderFilterDialog()
    {
        //生物列表开放的筛选类型
        List<OrderFilterTypeEnum> listFilterType = new List<OrderFilterTypeEnum>
        {
            OrderFilterTypeEnum.Rarity,
            OrderFilterTypeEnum.Level,
            OrderFilterTypeEnum.Lineup,
            OrderFilterTypeEnum.Name,
            OrderFilterTypeEnum.Class,
        };
        UIHandler.Instance.ShowDialogOrderFilter(
            ui_OrderBtn_Button.transform as RectTransform,
            OnConfirmOrderFilter,
            listFilterType,
            new List<OrderFilterTypeEnum>(currentFilter.sortTypes),
            currentFilter.nameFilter,
            currentFilter.levelMin,
            currentFilter.levelMax,
            new List<RarityEnum>(currentFilter.rarities));
    }

    /// <summary>
    /// 筛选排序弹窗确认回调
    /// </summary>
    /// <param name="result">弹窗回传结果(排序键 + 名字/等级/稀有度筛选)</param>
    protected void OnConfirmOrderFilter(OrderFilterResultBean result)
    {
        currentFilter = result ?? new OrderFilterResultBean();
        RefreshFilterSortList();
    }

    /// <summary>
    /// 按当前条件重排主列表(不删行、全部展示):命中项(名字/等级/稀有度)置顶,再按排序键固定正序次级排序,刷新UI。
    /// </summary>
    protected void RefreshFilterSortList()
    {
        listCreatureData = OrderListCreature(listCreatureDataAll, currentFilter);
        //数据不删行,展示数量恒等于主列表;刷新数量/空提示/卡片
        ui_CreatureListContent.SetCellCount(listCreatureData.Count);
        RefreshNullText();
        RefreshAllCard();
    }

    /// <summary>
    /// 生成展示列表:以「是否命中(名字/等级/稀有度)」为主键把命中项置顶,再以各排序键固定正序为次键,全部数据保留(与入参等量)。
    /// </summary>
    /// <param name="listData">主列表(全量)</param>
    /// <param name="filter">当前条件</param>
    /// <returns>重排后的新列表</returns>
    private List<CreatureBean> OrderListCreature(List<CreatureBean> listData, OrderFilterResultBean filter)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //阵容索引:在阵容内取其序号,否则置最大值排到最后
        Func<CreatureBean, int> lineupOrder = itemData =>
        {
            int lineupIndex = userData.GetLinupIndex(itemData.creatureUUId);
            return lineupIndex > 0 ? lineupIndex : int.MaxValue;
        };
        //主键:命中项置顶(命中=true 排在前)
        IOrderedEnumerable<CreatureBean> ordered = listData.OrderByDescending(c => IsMatch(c, filter));
        //次键:各排序键固定正序(index0 优先级最高)
        if (filter.sortTypes != null)
            for (int i = 0; i < filter.sortTypes.Count; i++)
                ordered = ordered.ThenBy(GetOrderKeySelector(filter.sortTypes[i], lineupOrder));
        return ordered.ToList();
    }

    /// <summary>
    /// 判断生物是否命中当前条件(名字模糊 + 等级区间 + 稀有度多选;对应条件为空即视为命中)
    /// </summary>
    /// <param name="itemData">生物数据</param>
    /// <param name="filter">当前条件</param>
    /// <returns>是否命中</returns>
    private bool IsMatch(CreatureBean itemData, OrderFilterResultBean filter)
    {
        return filter.MatchName(itemData.creatureName)
            && filter.MatchLevel(itemData.level)
            && filter.MatchRarity(itemData.rarity);
    }

    /// <summary>
    /// 获取指定筛选类型对应的排序键选择器
    /// </summary>
    /// <param name="filterType">筛选类型</param>
    /// <param name="lineupOrder">阵容索引取值器</param>
    /// <returns>排序键选择器</returns>
    protected Func<CreatureBean, IComparable> GetOrderKeySelector(OrderFilterTypeEnum filterType, Func<CreatureBean, int> lineupOrder)
    {
        switch (filterType)
        {
            case OrderFilterTypeEnum.Rarity:
                return itemData => itemData.rarity;
            case OrderFilterTypeEnum.Level:
                return itemData => itemData.level;
            case OrderFilterTypeEnum.Lineup:
                return itemData => lineupOrder(itemData);
            case OrderFilterTypeEnum.Name:
                return itemData => itemData.creatureName;
            case OrderFilterTypeEnum.Class:
                return itemData => itemData.creatureId;
        }
        return itemData => 0;
    }
    #endregion
}
