using UnityEngine.UI;

public partial class UIViewItemBackpack : BaseUIView
{
    public ItemBean itemData;
    public CreatureBean creatureData;

    /// <summary>
    /// 点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewItemBackpack_Button)
        {
            OnClickForSelect();
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="itemData">道具数据</param>
    /// <param name="creatureData">生物数据（用于判断道具是否可装备）</param>
    public void SetData(ItemBean itemData, CreatureBean creatureData = null)
    {
        this.itemData = itemData;
        this.creatureData = creatureData;
        SetNum(itemData.itemNum);
        SetIcon(itemData.itemId);
        SetItemPopup(itemData);
    }

    /// <summary>
    /// 设置弹窗信息
    /// </summary>
    public void SetItemPopup(ItemBean itemData)
    {
        ui_UIViewItemBackpack_PopupButtonCommonView.SetData(itemData, PopupEnum.ItemInfo);
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

    /// <summary>
    /// 点击
    /// </summary>
    public void OnClickForSelect()
    {
        this.TriggerEvent(EventsInfo.UIViewItemBackpack_OnClickSelect,this);
    }
}
