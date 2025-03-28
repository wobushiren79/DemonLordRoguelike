

using System.Collections.Generic;
using UnityEngine.UI;

public partial class UICreatureManager : BaseUIComponent
{

    public override void OpenUI()
    {
        base.OpenUI();
        InitCreaturekData();
        InitBackpackItemsData();
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_UIViewCreatureCardList.CloseUI();
        ui_UIViewItemBackpackList.CloseUI();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitCreaturekData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        ui_UIViewCreatureCardList.SetData(userData.listBackpackCreature, CardUseState.CreatureManager, OnCellChangeForBackpackCreature);
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
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {

    }

    /// <summary>
    /// 背包道具变化
    /// </summary>
    public void OnCellChangeForBackpackItem(int index, UIViewItemBackpack itemView, ItemBean itemData)
    {

    }

    #region 点击事件
    /// <summary>
    /// 点击返回
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }
    #endregion

    #region  回调事件
    /// <summary>
    /// 卡片点击
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem targetView)
    {
        ui_UIViewCreatureCardDetails.SetData(targetView.cardData.creatureData);
    }
    #endregion
}