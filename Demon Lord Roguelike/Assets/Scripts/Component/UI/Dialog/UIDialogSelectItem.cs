

using System;
using UnityEngine.UI;

public partial class UIDialogSelectItem : DialogView
{
    public override void InitData()
    {
        base.InitData();
        RegisterEvent<UIViewItemBackpack>(EventsInfo.UIViewItemBackpack_OnClickSelect, EventForItemBackpackClickSelect);
        InitItemSelect();
    }

    /// <summary>
    /// 初始化背包道具数据
    /// </summary>
    public void InitBackpackItemsData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        ui_UIViewItemBackpackList.SetData(userData.GetUserBackpackItemsData().listBackpackItems, OnCellChangeForBackpackItem);
    }

    /// <summary>
    /// 初始化道具选项控件：选项显隐由 DialogSelectItemBean 传入的回调决定（未传回调的选项不显示），点击后的业务逻辑由各回调自行处理
    /// </summary>
    public void InitItemSelect()
    {
        var dialogItemSelect = dialogData as DialogSelectItemBean;
        //回调为空则对应选项不显示（包装后的回调固定非空，故需按原回调判空再包装）
        Action<ItemBean> actionForGift = null;
        if (dialogItemSelect.actionForSelectGift != null)
            actionForGift = itemData => dialogItemSelect.actionForSelectGift.Invoke(this, itemData);
        Action<ItemBean> actionForDelete = null;
        if (dialogItemSelect.actionForSelectDelete != null)
            actionForDelete = itemData => dialogItemSelect.actionForSelectDelete.Invoke(this, itemData);
        ui_UIViewItemSelect.SetData(actionForGift, actionForDelete);
    }

    /// <summary>
    /// 背包道具变化
    /// </summary>
    public void OnCellChangeForBackpackItem(int index, UIViewItemBackpack itemView, ItemBean itemData)
    {

    }

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewExit)
        {
            DestroyDialog();
        }
    }
    #endregion

    #region 回调事件
    /// <summary>
    /// 背包道具点击：打开道具选项并定位到目标道具
    /// </summary>
    public void EventForItemBackpackClickSelect(UIViewItemBackpack itemView)
    {
        ui_UIViewItemSelect.ShowSelect(itemView.itemData, itemView.transform);
    }
    #endregion

}
