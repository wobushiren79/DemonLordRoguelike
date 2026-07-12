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
        SetItemBG(itemData);
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
        // 图标回退成未知图标(缺图/加载失败)时输出日志,方便定位有问题的道具(GetSprite从图集取回名字可能带"(Clone)"后缀,故用Contains判定)
        if (ui_ItemIcon.sprite != null && ui_ItemIcon.sprite.name.Contains(IconHandler.IconNameUnKnow))
        {
            var itemInfo = ItemsInfoCfg.GetItemData(itemId);
            LogUtil.LogError($"道具图标加载失败,回退未知图标：itemId={itemId}，icon_res={(itemInfo == null ? "null" : itemInfo.icon_res)}");
        }
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
    /// 设置背景颜色（按稀有度取道具专用单色 ui_board_color_item，空槽位/缺配置回退白色）
    /// </summary>
    public virtual void SetItemBG(ItemBean itemData)
    {
        if (ui_ItemBG == null)
            return;
        if (itemData == null)
        {
            ui_ItemBG.color = Color.white;
            return;
        }
        var rarityInfo = RarityInfoCfg.GetItemData(itemData.rarity);
        if (rarityInfo != null && !string.IsNullOrEmpty(rarityInfo.ui_board_color_item))
            ui_ItemBG.color = ColorUtil.ParseHtmlString(rarityInfo.ui_board_color_item);
        else
            ui_ItemBG.color = Color.white;
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
