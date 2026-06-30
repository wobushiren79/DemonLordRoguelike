using UnityEngine;
using UnityEngine.UI;

public partial class UIViewItem : BaseUIView
{
    #region 数据
    public ItemBean itemData;
    #endregion

    #region 生命周期/点击
    /// <summary>
    /// 点击（道具按钮与 popup 在同一物体上，命中即视为选中）
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (ui_UIViewItem != null && viewButton.gameObject == ui_UIViewItem.gameObject)
            OnClickForSelect();
    }

    /// <summary>
    /// 点击选择（由子类重写，触发各自类型的选中事件）
    /// </summary>
    public virtual void OnClickForSelect()
    {
    }
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置道具数据（通用流程：图标 + 数量 + 弹窗）
    /// </summary>
    public virtual void SetData(ItemBean itemData)
    {
        if (itemData != null && this.itemData == itemData)
            return;
        this.itemData = itemData;
        long itemId = itemData == null ? 0 : itemData.itemId;
        int itemNum = itemData == null ? 0 : itemData.itemNum;
        SetIcon(itemId);
        SetNum(itemId, itemNum);
        SetItemPopup(itemData);
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public virtual void SetIcon(long itemId)
    {
        if (itemId <= 0)
        {
            ui_ItemIcon.color = new Color(1, 1, 1, 0.3f);
            return;
        }
        IconHandler.Instance.SetItemIcon(itemId, ui_ItemIcon);
        ui_ItemIcon.color = Color.white;
    }

    /// <summary>
    /// 设置数量（上限为1或数量≤1时隐藏，数量背景为数量文本的父物体）
    /// </summary>
    public virtual void SetNum(long itemId, int num)
    {
        if (ui_ItemNum == null)
            return;
        var itemInfo = ItemsInfoCfg.GetItemData(itemId);
        bool isShow = !(itemInfo != null && itemInfo.num_max == 1) && num > 1;
        if (ui_ItemNum.transform.parent != null)
            ui_ItemNum.transform.parent.gameObject.SetActive(isShow);
        if (isShow)
            ui_ItemNum.text = $"{num}";
    }

    /// <summary>
    /// 设置弹窗信息
    /// </summary>
    public virtual void SetItemPopup(ItemBean itemData)
    {
        ui_UIViewItem.SetData(itemData, PopupEnum.ItemInfo);
    }
    #endregion
}
