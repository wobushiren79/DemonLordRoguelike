using UnityEngine;

public partial class UIViewItemEquip : UIViewItem
{
    #region 数据
    public ItemTypeEnum itemTypeEnum;
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置数据（装备部位类型，用于空槽位占位显示）
    /// </summary>
    public void SetData(ItemTypeEnum itemTypeEnum)
    {
        this.itemTypeEnum = itemTypeEnum;
    }

    /// <summary>
    /// 设置图标（空槽位显示部位占位图标）
    /// </summary>
    public override void SetIcon(long itemId)
    {
        if (itemId <= 0)
        {
            var itemsTypeInfo = ItemsTypeCfg.GetItemData(itemTypeEnum);
            IconHandler.Instance.SetUIIcon(itemsTypeInfo.icon_res, ui_ItemIcon);
            ui_ItemIcon.color = new Color(1, 1, 1, 0.3f);
            return;
        }
        base.SetIcon(itemId);
    }

    /// <summary>
    /// 设置弹窗信息（空槽位显示部位名称）
    /// </summary>
    public override void SetItemPopup(ItemBean itemData)
    {
        if (itemData == null)
        {
            var itemsTypeInfo = ItemsTypeCfg.GetItemData(itemTypeEnum);
            ui_UIViewItem.SetData(itemsTypeInfo.name_language, PopupEnum.Text);
            return;
        }
        base.SetItemPopup(itemData);
    }
    #endregion

    #region 点击
    /// <summary>
    /// 点击选择（触发装备道具选中事件）
    /// </summary>
    public override void OnClickForSelect()
    {
        this.TriggerEvent(EventsInfo.UIViewItemEquip_OnClickSelect, this);
    }
    #endregion
}
