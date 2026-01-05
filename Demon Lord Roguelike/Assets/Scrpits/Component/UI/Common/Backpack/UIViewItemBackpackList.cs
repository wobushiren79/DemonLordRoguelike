

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public partial class UIViewItemBackpackList : BaseUIView
{
    //卡片变化回调
    protected Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange;

    public List<ItemBean> listBackpackItems;

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
    public void SetData(List<ItemBean> listBackpackItems, Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange)
    {
        gameObject.SetActive(true);
        this.listBackpackItems = listBackpackItems;
        this.actionForOnCellChange = actionForOnCellChange;
        ui_BackpackContent.SetCellCount(listBackpackItems.Count);
    }

    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForCreatrue(ScrollGridCell itemCell)
    {
        var itemData = listBackpackItems[itemCell.index];
        UIViewItemBackpack itemView = itemCell.GetComponent<UIViewItemBackpack>();
        itemView.SetData(itemData);
        actionForOnCellChange?.Invoke(itemCell.index, itemView, itemData);
    }

    /// <summary>
    /// 刷新所有Item
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
                listBackpackItems = listBackpackItems
                    .OrderByDescending((itemData) => itemData.rarity)
                    .ThenBy((itemData) => itemData.itemsInfo.name_language)
                    .ToList();
                break;
            case 2://名字排序
                listBackpackItems = listBackpackItems
                    .OrderBy((itemData) => itemData.itemsInfo.name_language)
                    .ThenByDescending((itemData) => itemData.rarity)
                    .ToList();
                break;
        }
        if (isRefreshUI)
            RefreshAllItem();
    }
}