

using System;
using System.Collections.Generic;

public partial class UIViewItemBackpackList : BaseUIView
{
    //卡片变化回调
    protected Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange;

    public List<ItemBean> listBackpackItems;

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForCreatrue);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_BackpackContent.SetCellCount(0);
        ui_BackpackContent.ClearAllCell();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<ItemBean> listBackpackItems, Action<int, UIViewItemBackpack, ItemBean> actionForOnCellChange)
    {
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
}