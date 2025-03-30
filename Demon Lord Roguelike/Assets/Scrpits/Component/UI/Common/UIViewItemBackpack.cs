

public partial class UIViewItemBackpack : BaseUIView
{
    protected ItemBean itemData;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemBean itemData)
    {
        this.itemData = itemData;
        SetNum(itemData.itemNum);
        SetIcon(itemData.itemId);
        SetItemPopup(itemData);
    }

    /// <summary>
    /// 设置弹窗信息
    /// </summary>
    public void SetItemPopup(ItemBean itemData)
    {
        ui_UIViewItemBackpack.SetData(itemData, PopupEnum.ItemInfo);
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(long itemId)
    {
        IconHandler.Instance.SetItemIcon(itemId, ui_ItemIcon);
    }

    /// <summary>
    /// 设置数量
    /// </summary>
    public void SetNum(int num)
    {
        if (num <= 1)
        {
            ui_ItemNumBg.gameObject.SetActive(false);
        }
        else
        {
            ui_ItemNumBg.gameObject.SetActive(true);
            ui_ItemNum.text = $"{num}";
        }
    }
}