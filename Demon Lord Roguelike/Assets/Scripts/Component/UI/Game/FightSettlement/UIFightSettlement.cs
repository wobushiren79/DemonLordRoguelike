using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIFightSettlement : BaseUIComponent
{
    protected List<FightRecordsCreatureBean> listRecordsCreatureData;
    protected FightBean fightData;

    protected Action actionForNext;

    //当前筛选排序的优先级列表(index0=最高优先级;默认按伤害)
    protected List<OrderFilterTypeEnum> currentFilterTypes = new List<OrderFilterTypeEnum> { OrderFilterTypeEnum.Damage };
    //当前是否正序(false=倒序;默认伤害倒序,高伤害在前)
    protected bool currentAscending = false;

    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChangeForItem);
        //排序筛选按钮的悬浮详情:筛选排序
        ui_OrderBtn_PopupButtonCommonView.SetData(TextHandler.Instance.GetTextById(2000014), PopupEnum.Text);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        //结算界面打开时停止战斗音乐的播放
        AudioHandler.Instance.StopMusic();
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
        //初始化排序(按当前筛选排序设置,默认伤害倒序)
        OrderListData(currentFilterTypes, currentAscending, false);
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

    #region 筛选排序
    /// <summary>
    /// 打开筛选排序弹窗(在排序按钮处弹出;结算仅开放 伤害/击杀/承伤/经验)
    /// </summary>
    protected void ShowOrderFilterDialog()
    {
        //结算列表开放的筛选类型(战斗统计相关)
        List<OrderFilterTypeEnum> listFilterType = new List<OrderFilterTypeEnum>
        {
            OrderFilterTypeEnum.Damage,
            OrderFilterTypeEnum.Kill,
            OrderFilterTypeEnum.DamageReceived,
            OrderFilterTypeEnum.Exp,
        };
        UIHandler.Instance.ShowDialogOrderFilter(
            ui_OrderBtn_Button.transform as RectTransform,
            OnConfirmOrderFilter,
            listFilterType,
            new List<OrderFilterTypeEnum>(currentFilterTypes));
    }

    /// <summary>
    /// 筛选排序弹窗确认回调
    /// </summary>
    /// <param name="filterTypes">已选筛选类型(按优先级从高到低,index0最高)</param>
    /// <param name="isAscending">是否正序</param>
    protected void OnConfirmOrderFilter(List<OrderFilterTypeEnum> filterTypes, bool isAscending)
    {
        currentFilterTypes = filterTypes ?? new List<OrderFilterTypeEnum>();
        currentAscending = isAscending;
        OrderListData(currentFilterTypes, currentAscending);
    }

    /// <summary>
    /// 按筛选类型优先级列表 + 正/倒序排序结算列表。
    /// 按 filterTypes 顺序依次作为主/次排序键(index0=主键),isAscending 作用于全部键。
    /// </summary>
    /// <param name="filterTypes">筛选类型(按优先级从高到低;为空则不重排)</param>
    /// <param name="isAscending">true正序/false倒序</param>
    /// <param name="isRefreshUI">是否刷新UI</param>
    public void OrderListData(List<OrderFilterTypeEnum> filterTypes, bool isAscending, bool isRefreshUI = true)
    {
        if (listRecordsCreatureData == null)
            return;
        if (filterTypes != null && filterTypes.Count > 0)
        {
            IOrderedEnumerable<FightRecordsCreatureBean> ordered = null;
            for (int i = 0; i < filterTypes.Count; i++)
            {
                Func<FightRecordsCreatureBean, IComparable> keySelector = GetOrderKeySelector(filterTypes[i]);
                if (i == 0)
                    ordered = isAscending
                        ? listRecordsCreatureData.OrderBy(keySelector)
                        : listRecordsCreatureData.OrderByDescending(keySelector);
                else
                    ordered = isAscending
                        ? ordered.ThenBy(keySelector)
                        : ordered.ThenByDescending(keySelector);
            }
            listRecordsCreatureData = ordered.ToList();
        }
        if (isRefreshUI)
            ui_List.RefreshAllCells();
    }

    /// <summary>
    /// 获取指定筛选类型对应的排序键选择器(结算:伤害/击杀/承伤/经验)
    /// </summary>
    /// <param name="filterType">筛选类型</param>
    /// <returns>排序键选择器</returns>
    protected Func<FightRecordsCreatureBean, IComparable> GetOrderKeySelector(OrderFilterTypeEnum filterType)
    {
        switch (filterType)
        {
            case OrderFilterTypeEnum.Damage:
                return itemData => itemData.damage;
            case OrderFilterTypeEnum.Kill:
                return itemData => itemData.killNum;
            case OrderFilterTypeEnum.DamageReceived:
                return itemData => itemData.damageReceived;
            case OrderFilterTypeEnum.Exp:
                return itemData => itemData.exp;
        }
        return itemData => 0;
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
        else if (viewButton == ui_OrderBtn_Button)
        {
            ShowOrderFilterDialog();
        }
    }
    #endregion
}
