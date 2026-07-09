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

    //当前条件(名字模糊+稀有度多选+道具类型多选命中项置顶,道具无等级、无排序键,次按稀有度倒序;不删行、全部展示)
    protected OrderFilterResultBean currentFilter = new OrderFilterResultBean();

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
        //过滤+排序并刷新列表(装备资格过滤 -> 名字/稀有度筛选 -> 稀有度倒序)
        RefreshFilterSortList();
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
    /// 打开筛选弹窗(在排序按钮处弹出;道具开放 稀有度多选 + 名字模糊,无排序键、无等级;
    /// 有生物上下文时额外开放「道具类型」多选筛选,选项为当前魔物的可装备类型,无生物时不显示该区)
    /// </summary>
    protected void ShowOrderFilterDialog()
    {
        //道具列表开放的筛选类型(稀有度 + 名字始终可用)
        List<OrderFilterTypeEnum> listFilterType = new List<OrderFilterTypeEnum>
        {
            OrderFilterTypeEnum.Rarity,
            OrderFilterTypeEnum.Name,
        };
        //有生物上下文(如生物管理界面)才开放道具类型筛选,选项取该魔物的可装备类型
        List<ItemTypeEnum> itemTypeOptions = null;
        CreatureInfoBean creatureInfo = creatureData != null ? creatureData.creatureInfo : null;
        if (creatureInfo != null)
        {
            itemTypeOptions = new List<ItemTypeEnum>(creatureInfo.GetEquipItemsType());
            if (itemTypeOptions.Count > 0)
                listFilterType.Add(OrderFilterTypeEnum.ItemType);
        }
        UIHandler.Instance.ShowDialogOrderFilter(
            ui_OrderBtn_Button.transform as RectTransform,
            OnConfirmOrderFilter,
            listFilterType,
            //道具无排序键(Combat/Other 不适用),默认不选中任何排序项
            new List<OrderFilterTypeEnum>(),
            currentFilter.nameFilter,
            //不传等级默认值(道具无等级),仅回传当前已选名字/稀有度/道具类型
            defaultRarities: new List<RarityEnum>(currentFilter.rarities),
            itemTypes: itemTypeOptions,
            defaultItemTypes: new List<ItemTypeEnum>(currentFilter.itemTypes));
    }

    /// <summary>
    /// 筛选弹窗确认回调:接住名字+稀有度筛选结果,重过滤并刷新列表
    /// </summary>
    /// <param name="result">弹窗回传的筛选结果(可空)</param>
    protected void OnConfirmOrderFilter(OrderFilterResultBean result)
    {
        currentFilter = result ?? new OrderFilterResultBean();
        RefreshFilterSortList();
    }

    /// <summary>
    /// 执行装备资格过滤 + 重排 + 刷新流程(道具不按名字/稀有度/道具类型删行,全部展示):
    /// ① 装备资格硬过滤(FilterItems 从 listBackpackItems 构建,上下文相关,保留为第一阶段);
    /// ② 名字/稀有度/道具类型命中项置顶(不删行);
    /// ③ 次按稀有度倒序(高稀有度在前);
    /// ④ 设置 Cell 数量、刷新空列表提示与卡片。
    /// </summary>
    public void RefreshFilterSortList()
    {
        //① 装备资格硬过滤:重建 listFilterItems(上下文相关,保留为第一阶段)
        FilterItems();
        //② 名字/稀有度/道具类型命中项置顶 ③ 次按稀有度倒序(道具无等级)
        listFilterItems = listFilterItems
            .OrderByDescending(i => currentFilter.MatchName(i.itemsInfo.name_language)
                && currentFilter.MatchRarity(i.rarity)
                && currentFilter.MatchItemType((int)i.GetItemType()))
            .ThenByDescending(i => i.rarity)
            .ToList();
        //④ 刷新 UI:Cell 数量 -> 空列表提示 -> 卡片
        ui_BackpackContent.SetCellCount(listFilterItems.Count);
        RefreshNullText();
        RefreshAllItem();
    }
    #endregion
}
