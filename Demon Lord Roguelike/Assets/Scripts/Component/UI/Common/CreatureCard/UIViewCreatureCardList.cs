

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewCreatureCardList : BaseUIView
{
    //生物数据
    protected List<CreatureBean> listCreatureData = new List<CreatureBean>();
    //卡片的使用地方
    protected CardUseStateEnum cardUseState;
    //卡片变化回调
    protected Action<int, UIViewCreatureCardItem, CreatureBean> actionForOnCellChange;
    //当前筛选排序的优先级列表(index0=最高优先级)
    protected List<OrderFilterTypeEnum> currentFilterTypes = new List<OrderFilterTypeEnum> { OrderFilterTypeEnum.Rarity };
    //当前是否正序(false=倒序;默认稀有度倒序,高稀有度在前)
    protected bool currentAscending = false;

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
        listCreatureData.Clear();
        listCreatureData.AddRange(listData);
        //初始化排序(按当前筛选排序设置)
        OrderListCreature(currentFilterTypes, currentAscending, false);
        //设置数量
        ui_CreatureListContent.SetCellCount(listCreatureData.Count);
        //刷新空列表提示
        RefreshNullText();
    }

    /// <summary>
    /// 刷新空列表提示文本：列表无魔物时显示"没有相关魔物"
    /// </summary>
    protected void RefreshNullText()
    {
        bool isEmpty = listCreatureData == null || listCreatureData.Count == 0;
        ui_UIViewNullText_UITextLanguageView.textId = 2000016;
        ui_UIViewNullText_UITextLanguageView.RefreshUI();
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
            new List<OrderFilterTypeEnum>(currentFilterTypes));
    }

    /// <summary>
    /// 筛选排序弹窗确认回调
    /// </summary>
    /// <param name="filterTypes">已选筛选类型(按优先级从高到低,index0最高)</param>
    /// <param name="isAscending">是否正序</param>
    protected void OnConfirmOrderFilter(List<OrderFilterTypeEnum> filterTypes, bool isAscending)
    {
        currentFilterTypes = filterTypes ?? new List<OrderFilterTypeEnum>();
        currentAscending = isAscending;
        OrderListCreature(currentFilterTypes, currentAscending);
    }

    /// <summary>
    /// 按筛选类型优先级列表 + 正/倒序排序生物列表。
    /// 按 filterTypes 顺序依次作为主/次排序键(index0=主键),isAscending 作用于全部键。
    /// </summary>
    /// <param name="filterTypes">筛选类型(按优先级从高到低;为空则不重排)</param>
    /// <param name="isAscending">true正序/false倒序</param>
    /// <param name="isRefreshUI">是否刷新UI</param>
    public void OrderListCreature(List<OrderFilterTypeEnum> filterTypes, bool isAscending, bool isRefreshUI = true)
    {
        if (filterTypes != null && filterTypes.Count > 0)
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            //阵容索引:在阵容内取其序号,否则置最大值排到最后
            Func<CreatureBean, int> lineupOrder = itemData =>
            {
                int lineupIndex = userData.GetLinupIndex(itemData.creatureUUId);
                return lineupIndex > 0 ? lineupIndex : int.MaxValue;
            };

            IOrderedEnumerable<CreatureBean> ordered = null;
            for (int i = 0; i < filterTypes.Count; i++)
            {
                Func<CreatureBean, IComparable> keySelector = GetOrderKeySelector(filterTypes[i], lineupOrder);
                if (i == 0)
                    ordered = isAscending
                        ? listCreatureData.OrderBy(keySelector)
                        : listCreatureData.OrderByDescending(keySelector);
                else
                    ordered = isAscending
                        ? ordered.ThenBy(keySelector)
                        : ordered.ThenByDescending(keySelector);
            }
            listCreatureData = ordered.ToList();
        }
        if (isRefreshUI)
            RefreshAllCard();
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
