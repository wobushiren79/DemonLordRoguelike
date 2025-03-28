

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
        SetIcon(itemData.itemsInfo.icon_res);
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetItemIcon(iconName,ui_ItemIcon);
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