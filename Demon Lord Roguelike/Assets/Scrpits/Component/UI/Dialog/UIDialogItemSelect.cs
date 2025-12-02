

using UnityEngine.UI;

public partial class UIDialogItemSelect : DialogView
{
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
        //打开选项
    }
    #endregion

}