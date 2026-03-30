

using UnityEngine;
using UnityEngine.UI;

public partial class UIDialogSelectItem : DialogView
{
    /// <summary>
    /// 选中的道具
    /// </summary>
    public ItemBean selectItem = null;

    public override void InitData()
    {
        base.InitData();
        RegisterEvent<UIViewItemBackpack>(EventsInfo.UIViewItemBackpack_OnClickSelect, EventForItemBackpackClickSelect);
    }

    /// <summary>
    /// 初始化背包道具数据
    /// </summary>
    public void InitBackpackItemsData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        ui_UIViewItemBackpackList.SetData(userData.listBackpackItems, OnCellChangeForBackpackItem);
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
        if (viewButton == ui_UIViewExit || viewButton == ui_Background)
        {
            OnClickForExit();
        }
        else if(viewButton== ui_SelectContent_Button)
        {
            OnClickForCloseSelect();
        }
        else if (viewButton == ui_UIViewDialogItemSelectChild_Delete)
        {
            OnClickForDeleteItem();
        }
        else if (viewButton == ui_UIViewDialogItemSelectChild_Gift)
        {
            OnClickForGiftItem();
        }
    }

    /// <summary>
    /// 点击关闭选项
    /// </summary>
    public void OnClickForCloseSelect()
    {
        selectItem = null;
        ui_SelectContent_RectTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// 点击丢弃道具
    /// </summary>
    public void OnClickForDeleteItem()
    {
        var dialogItemSelect = dialogData as DialogSelectItemBean;
        dialogItemSelect.actionForSelectDelete?.Invoke(this, selectItem);

        OnClickForCloseSelect();
    }

    /// <summary>
    /// 点击送礼
    /// </summary>
    public void OnClickForGiftItem()
    {        
        var dialogItemSelect = dialogData as DialogSelectItemBean;
        dialogItemSelect.actionForSelectGift?.Invoke(this, selectItem);

        OnClickForCloseSelect();
    }

    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        DestroyDialog();
    }
    #endregion

    #region 回调事件

    public void EventForItemBackpackClickSelect(UIViewItemBackpack itemView)
    {
        //选中的道具
        selectItem = itemView.itemData;
        //打开选项
        ui_SelectContent_RectTransform.gameObject.SetActive(true);
        Vector3 targetPos = UGUIUtil.GetRootPos(ui_SelectContent_RectTransform, itemView.transform);
        ui_SelectList.transform.localPosition = targetPos;
    }
    #endregion

}