

public partial class UIPopupItemInfo : PopupShowCommonView
{

    public override void SetData(object data)
    {
        ItemBean itemData = (ItemBean)data;
        var itemInfo = ItemsInfoCfg.GetItemData(itemData.itemId);
        string itemName = itemInfo.GetName();
        SetIcon(itemData.itemId);
        SetName(itemName);
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(long itemId)
    {
        IconHandler.Instance.SetItemIcon(itemId, ui_Icon);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = $"{name}";
    }
}