

using UnityEngine.UI;

public partial class UIViewItemEquip : BaseUIView
{
    public ItemTypeEnum itemTypeEnum;
    public ItemBean itemData;

    /// <summary>
    /// 点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewItemEquip_Button)
        {
            OnClickForSelect();
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemTypeEnum itemTypeEnum)
    {
        this.itemTypeEnum = itemTypeEnum;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemBean itemData)
    {
        if (itemData != null && this.itemData == itemData)
            return;
        this.itemData = itemData;
        //设置为null
        if (itemData == null)
        {
            SetIcon(itemTypeEnum, 0);
            SetItemPopup(itemTypeEnum, null);
        }
        //设置显示道具
        else
        {
            this.itemData = itemData;
            SetIcon(itemTypeEnum, itemData.itemId);
            SetItemPopup(itemTypeEnum, itemData);
        }
    }

    /// <summary>
    /// 设置弹窗信息
    /// </summary>
    public void SetItemPopup(ItemTypeEnum itemType, ItemBean itemData)
    {
        if (itemData == null)
        {
            var itemsTypeInfo = ItemsTypeCfg.GetItemData(itemType);
            string itemsTypeName = itemsTypeInfo.name_language;
            ui_UIViewItemEquip_PopupButtonCommonView.SetData(itemsTypeName, PopupEnum.Text);
        }
        else
        {
            ui_UIViewItemEquip_PopupButtonCommonView.SetData(itemData, PopupEnum.ItemInfo);
        }
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(ItemTypeEnum itemType, long itemId)
    {
        if (itemId <= 0)
        {
            var itemsTypeInfo = ItemsTypeCfg.GetItemData(itemType);
            IconHandler.Instance.SetUIIcon(itemsTypeInfo.icon_res, ui_ItemIcon);
        }
        else
        {
            IconHandler.Instance.SetItemIcon(itemId, ui_ItemIcon);
        }
    }

    /// <summary>
    /// 点击
    /// </summary>
    public void OnClickForSelect()
    {
        this.TriggerEvent(EventsInfo.UIViewItemEquip_OnClickSelect,this);
    }
}