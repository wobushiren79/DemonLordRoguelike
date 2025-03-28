

public partial class UIViewItemBackpack : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(ItemBean itemData)
    {
        SetNum(itemData.itemNum);
    }

    /// <summary>
    /// 设置头像
    /// </summary>
    public void SetIcon()
    {

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