using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewItemBackpackList : BaseUIView
{
    //卡片变化回调
    protected Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange;

    public List<ItemBean> listBackpackItems;

    /// <summary>
    /// 生物数据（用于判断道具是否可装备）
    /// </summary>
    public CreatureBean creatureData;

    /// <summary>
    /// 过滤后的道具列表（只包含可装备的道具）
    /// </summary>
    public List<ItemBean> listFilterItems = new List<ItemBean>();

    //当前筛选排序的优先级列表(index0=最高优先级)
    protected List<OrderFilterTypeEnum> currentFilterTypes = new List<OrderFilterTypeEnum> { OrderFilterTypeEnum.Rarity };
    //当前是否正序(false=倒序;默认稀有度倒序,高稀有度在前)
    protected bool currentAscending = false;

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForCreatrue);
        //排序筛选按钮的悬浮详情:筛选排序
        ui_OrderBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000014), PopupEnum.Text);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_BackpackContent.SetCellCount(0);
        ui_BackpackContent.ClearAllCell();
    }

    public override void OpenUI()
    {
        base.OpenUI();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="listBackpackItems">背包道具列表</param>
    /// <param name="actionForOnCellChange">Cell 变化回调</param>
    /// <param name="creatureData">生物数据（用于判断道具是否可装备）</param>
    public void SetData(List<ItemBean> listBackpackItems, Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange, CreatureBean creatureData = null)
    {
        gameObject.SetActive(true);
        this.listBackpackItems = listBackpackItems;
        this.actionForOnCellChange = actionForOnCellChange;
        this.creatureData = creatureData;
        // 过滤道具列表
        FilterItems();
        //初始化排序(按当前筛选排序设置)
        OrderListItem(currentFilterTypes, currentAscending, false);
        ui_BackpackContent.SetCellCount(listFilterItems.Count);
        //刷新空列表提示
        RefreshNullText();
    }

    /// <summary>
    /// 刷新空列表提示文本：过滤后无可用道具时显示"没有相关道具"
    /// </summary>
    protected void RefreshNullText()
    {
        bool isEmpty = listFilterItems == null || listFilterItems.Count == 0;
        ui_UIViewNullText_UITextLanguageView.gameObject.SetActive(isEmpty);
    }


    /// <summary>
    /// 过滤道具列表，只展示可装备的道具
    /// </summary>
    public void FilterItems()
    {
        listFilterItems.Clear();

        // 如果没有生物数据，显示所有道具
        if (creatureData == null)
        {
            listFilterItems.AddRange(listBackpackItems);
            return;
        }

        CreatureInfoBean creatureInfo = creatureData.creatureInfo;
        if (creatureInfo == null)
        {
            listFilterItems.AddRange(listBackpackItems);
            return;
        }

        // 过滤出可装备的道具
        for (int i = 0; i < listBackpackItems.Count; i++)
        {
            var itemData = listBackpackItems[i];
            var itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
            if (itemInfo != null && creatureInfo.CanEquipItem(itemInfo))
            {
                listFilterItems.Add(itemData);
            }
        }
    }

    /// <summary>
    /// item 滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForCreatrue(ScrollGridCell itemCell)
    {
        var itemData = listFilterItems[itemCell.index];
        UIViewItemBackpack itemView = itemCell.GetComponent<UIViewItemBackpack>();
        itemView.SetData(itemData, creatureData);
        actionForOnCellChange?.Invoke(itemCell.index, itemView, itemData);
    }

    /// <summary>
    /// 刷新所有 Item
    /// </summary>
    public void RefreshAllItem()
    {
        ui_BackpackContent.RefreshAllCells();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_OrderBtn_Button)
        {
            ShowOrderFilterDialog();
        }
    }

    #region 筛选排序
    /// <summary>
    /// 打开筛选排序弹窗(在排序按钮处弹出;道具仅开放 稀有度/名字 两种筛选)
    /// </summary>
    protected void ShowOrderFilterDialog()
    {
        //道具列表开放的筛选类型(只有稀有度与名字适用于道具)
        List<OrderFilterTypeEnum> listFilterType = new List<OrderFilterTypeEnum>
        {
            OrderFilterTypeEnum.Rarity,
            OrderFilterTypeEnum.Name,
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
        OrderListItem(currentFilterTypes, currentAscending);
    }

    /// <summary>
    /// 按筛选类型优先级列表 + 正/倒序排序道具列表。
    /// 按 filterTypes 顺序依次作为主/次排序键(index0=主键),isAscending 作用于全部键。
    /// </summary>
    /// <param name="filterTypes">筛选类型(按优先级从高到低;为空则不重排)</param>
    /// <param name="isAscending">true正序/false倒序</param>
    /// <param name="isRefreshUI">是否刷新UI</param>
    public void OrderListItem(List<OrderFilterTypeEnum> filterTypes, bool isAscending, bool isRefreshUI = true)
    {
        if (filterTypes != null && filterTypes.Count > 0)
        {
            IOrderedEnumerable<ItemBean> ordered = null;
            for (int i = 0; i < filterTypes.Count; i++)
            {
                Func<ItemBean, IComparable> keySelector = GetOrderKeySelector(filterTypes[i]);
                if (i == 0)
                    ordered = isAscending
                        ? listFilterItems.OrderBy(keySelector)
                        : listFilterItems.OrderByDescending(keySelector);
                else
                    ordered = isAscending
                        ? ordered.ThenBy(keySelector)
                        : ordered.ThenByDescending(keySelector);
            }
            listFilterItems = ordered.ToList();
        }
        if (isRefreshUI)
            RefreshAllItem();
    }

    /// <summary>
    /// 获取指定筛选类型对应的排序键选择器(道具:仅 稀有度/名字)
    /// </summary>
    /// <param name="filterType">筛选类型</param>
    /// <returns>排序键选择器</returns>
    protected Func<ItemBean, IComparable> GetOrderKeySelector(OrderFilterTypeEnum filterType)
    {
        switch (filterType)
        {
            case OrderFilterTypeEnum.Rarity:
                return itemData => itemData.rarity;
            case OrderFilterTypeEnum.Name:
                return itemData => itemData.itemsInfo.name_language;
        }
        return itemData => 0;
    }
    #endregion
}
