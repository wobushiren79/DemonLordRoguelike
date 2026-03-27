using System;
using System.Collections.Generic;
using System.Linq;
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

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForCreatrue);
        ui_OrderBtn_Rarity_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000004), PopupEnum.Text);
        ui_OrderBtn_Name_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000007), PopupEnum.Text);
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
        ui_BackpackContent.SetCellCount(listFilterItems.Count);
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
        if (viewButton == ui_OrderBtn_Rarity_Button)
        {
            OrderListItem(1);
        }
        else if (viewButton == ui_OrderBtn_Name_Button)
        {
            OrderListItem(2);
        }
    }

        /// <summary>
    /// 排序背包里的生物
    /// </summary>
    /// <param name="orderType"></param>
    public void OrderListItem(int orderType, bool isRefreshUI = true)
    {
        switch (orderType)
        {
            case 1://按稀有度排序
                listFilterItems = listFilterItems
                    .OrderByDescending((itemData) => itemData.rarity)
                    .ThenBy((itemData) => itemData.itemsInfo.name_language)
                    .ToList();
                break;
            case 2://名字排序
                listFilterItems = listFilterItems
                    .OrderBy((itemData) => itemData.itemsInfo.name_language)
                    .ThenByDescending((itemData) => itemData.rarity)
                    .ToList();
                break;
        }
        if (isRefreshUI)
            RefreshAllItem();
    }
}