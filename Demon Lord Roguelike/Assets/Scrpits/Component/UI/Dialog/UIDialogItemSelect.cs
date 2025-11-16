

using UnityEngine.UI;

public partial class UIDialogItemSelect : DialogView
{
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewExit || viewButton == ui_Background)
        {
            OnClickForExit();
        }
    }

    public override void OpenUI()
    {
        base.OpenUI();
        InitBackpackItemsData();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewItemBackpackList.CloseUI();
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
    
    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        DestroyDialog();
    }
}