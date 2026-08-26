using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 道具选项通用控件：右键（或点击）道具时弹出的操作选项（送礼/丢弃/装备）。
/// <para>各选项显隐由 SetData 传入的回调决定（传入即显示、为空即隐藏），点击后的业务逻辑全部由使用方在各自回调里处理。</para>
/// </summary>
public partial class UIViewItemSelect : BaseUIView
{
    //当前选中的道具
    public ItemBean selectItem;

    //各选项点击回调（为空则对应按钮不显示）
    protected Action<ItemBean> actionForGift;
    protected Action<ItemBean> actionForDelete;
    protected Action<ItemBean> actionForEquip;

    #region 数据设置
    /// <summary>
    /// 设置各选项点击回调：传入回调的选项显示、未传入的隐藏（业务逻辑由使用方在回调里各自处理）
    /// </summary>
    /// <param name="actionForGift">送礼回调（空则不显示送礼按钮）</param>
    /// <param name="actionForDelete">丢弃回调（空则不显示丢弃按钮）</param>
    /// <param name="actionForEquip">装备回调（空则不显示装备按钮）</param>
    public void SetData(Action<ItemBean> actionForGift = null, Action<ItemBean> actionForDelete = null, Action<ItemBean> actionForEquip = null)
    {
        this.actionForGift = actionForGift;
        this.actionForDelete = actionForDelete;
        this.actionForEquip = actionForEquip;
        ui_UIViewDialogItemSelectChild_Gift.gameObject.SetActive(actionForGift != null);
        ui_UIViewDialogItemSelectChild_Delete.gameObject.SetActive(actionForDelete != null);
        ui_UIViewDialogItemSelectChild_Equip.gameObject.SetActive(actionForEquip != null);
    }

    /// <summary>
    /// 显示选项：记录选中道具并把选项列表定位到目标道具处
    /// </summary>
    /// <param name="itemData">选中的道具</param>
    /// <param name="targetTF">目标道具的Transform（选项列表定位到其位置）</param>
    public void ShowSelect(ItemBean itemData, Transform targetTF)
    {
        if (itemData == null)
            return;
        selectItem = itemData;
        gameObject.SetActive(true);
        ui_SelectList.localPosition = UGUIUtil.GetRootPos(rectTransform, targetTF);
    }

    /// <summary>
    /// 关闭选项
    /// </summary>
    public void CloseSelect()
    {
        selectItem = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 是否展示中
    /// </summary>
    public bool IsShowing()
    {
        return gameObject.activeSelf;
    }
    #endregion

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        //点击空白背景：关闭选项
        if (viewButton == ui_UIViewItemSelect)
        {
            CloseSelect();
        }
        else if (viewButton == ui_UIViewDialogItemSelectChild_Gift)
        {
            OnClickForChild(actionForGift);
        }
        else if (viewButton == ui_UIViewDialogItemSelectChild_Delete)
        {
            OnClickForChild(actionForDelete);
        }
        else if (viewButton == ui_UIViewDialogItemSelectChild_Equip)
        {
            OnClickForChild(actionForEquip);
        }
    }

    /// <summary>
    /// 点击选项按钮：先关闭选项再回调使用方（回传入参为点击时选中的道具）
    /// </summary>
    protected void OnClickForChild(Action<ItemBean> action)
    {
        var itemData = selectItem;
        CloseSelect();
        action?.Invoke(itemData);
    }
    #endregion
}
