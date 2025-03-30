

public partial class UIViewItemEquip : BaseUIView
{
    protected ItemTypeEnum itemTypeEnum;
    protected ItemBean itemData;

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
        this.itemData = itemData;
        SetIcon(itemData.itemId);
        SetItemPopup(itemData);
    }

    /// <summary>
    /// 设置弹窗信息
    /// </summary>
    public void SetItemPopup(ItemBean itemData)
    {
        ui_UIViewItemEquip.SetData(itemData, PopupEnum.ItemInfo);
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(long itemId)
    {
        IconHandler.Instance.SetItemIcon(itemId, ui_ItemIcon);
    }

}