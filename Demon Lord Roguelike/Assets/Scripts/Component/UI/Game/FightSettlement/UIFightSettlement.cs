using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public partial class UIFightSettlement : BaseUIComponent
{
    protected List<FightRecordsCreatureBean> listRecordsCreatureData;
    protected FightBean fightData;

    protected Action actionForNext;

    //当前排序类型
    protected int currentOrderType = 1;

    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChangeForItem);
        //排序按钮提示
        ui_OrderBtn_Damage_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(50001), PopupEnum.Text);
        ui_OrderBtn_Kill_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(50002), PopupEnum.Text);
        ui_OrderBtn_Exp_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(50003), PopupEnum.Text);
        ui_OrderBtn_DamageReceived_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(50004), PopupEnum.Text);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_List.SetCellCount(0);
        ui_List.ClearAllCell();
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(FightBean fightData, Action actionForNext = null)
    {
        this.fightData = fightData;
        this.actionForNext = actionForNext;
        listRecordsCreatureData = fightData.fightRecordsData.GetRecordsForCreatureData();
        //默认按伤害降序
        OrderListData(currentOrderType, false);
        ui_List.SetCellCount(listRecordsCreatureData.Count);
    }

    /// <summary>
    /// 设置列表数据
    /// </summary>
    public void SetListData(List<FightRecordsCreatureBean> listRecordsCreatureData)
    {
        if (listRecordsCreatureData == null)
            return;
        this.listRecordsCreatureData = listRecordsCreatureData;
        ui_List.SetCellCount(listRecordsCreatureData.Count);
    }

    /// <summary>
    ///  list数据
    /// </summary>
    public void OnCellChangeForItem(ScrollGridCell itemCell)
    {
        if (listRecordsCreatureData == null)
            return;
        var itemData = listRecordsCreatureData[itemCell.index];
        if (itemData == null)
            return;
        var itemView = itemCell.GetComponent<UIViewFightSettlementItem>();
        itemView.SetData(fightData.fightRecordsData, itemData);
    }

    #region 排序
    /// <summary>
    /// 排序列表
    /// </summary>
    /// <param name="orderType">1-伤害 2-击杀 3-受到伤害 4-经验</param>
    public void OrderListData(int orderType, bool isRefreshUI = true)
    {
        if (listRecordsCreatureData == null)
            return;
        currentOrderType = orderType;
        switch (orderType)
        {
            case 1://按伤害降序
                listRecordsCreatureData = listRecordsCreatureData
                    .OrderByDescending((itemData) => itemData.damage)
                    .ThenByDescending((itemData) => itemData.killNum)
                    .ToList();
                break;
            case 2://按击杀降序
                listRecordsCreatureData = listRecordsCreatureData
                    .OrderByDescending((itemData) => itemData.killNum)
                    .ThenByDescending((itemData) => itemData.damage)
                    .ToList();
                break;
            case 3://按受到伤害降序
                listRecordsCreatureData = listRecordsCreatureData
                    .OrderByDescending((itemData) => itemData.damageReceived)
                    .ThenByDescending((itemData) => itemData.damage)
                    .ToList();
                break;
            case 4://按经验降序
                listRecordsCreatureData = listRecordsCreatureData
                    .OrderByDescending((itemData) => itemData.exp)
                    .ThenByDescending((itemData) => itemData.damage)
                    .ToList();
                break;
        }
        if (isRefreshUI)
            ui_List.RefreshAllCells();
    }
    #endregion

    #region 按钮
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnNext)
        {
            actionForNext?.Invoke();
            actionForNext = null;
        }
        else if (viewButton == ui_OrderBtn_Damage_Button)
        {
            OrderListData(1);
        }
        else if (viewButton == ui_OrderBtn_Kill_Button)
        {
            OrderListData(2);
        }
        else if (viewButton == ui_OrderBtn_DamageReceived_Button)
        {
            OrderListData(3);
        }
        else if (viewButton == ui_OrderBtn_Exp_Button)
        {
            OrderListData(4);
        }
    }
    #endregion
}
