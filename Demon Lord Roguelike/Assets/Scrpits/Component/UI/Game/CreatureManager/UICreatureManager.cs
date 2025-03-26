

using System.Collections.Generic;
using UnityEngine.UI;

public partial class UICreatureManager : BaseUIComponent
{
    public List<ItemBean> listBackpackItem = new List<ItemBean>();
    public override void OpenUI()
    {
        base.OpenUI();
        InitCreaturekData();
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
    }

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpackItem);
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
        var listItems = userData.listBackpackItems;
        // List<ItemBean> listShowItems = new List<ItemBean>();
        // for (int i = 0; i < listItems.Count; i++)
        // {

        // }
        listBackpackItem = listItems;
        ui_BackpackContent.SetCellCount(listBackpackItem.Count);
    }

    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreature(UIViewCreatureCardItem itemView, CreatureBean itemData)
    {

    }

    /// <summary>
    /// 背包道具变化
    /// </summary>
    public void OnCellChangeForBackpackItem(ScrollGridCell itemCell)
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