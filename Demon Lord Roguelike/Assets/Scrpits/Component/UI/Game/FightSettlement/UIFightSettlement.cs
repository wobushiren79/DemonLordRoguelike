using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class UIFightSettlement : BaseUIComponent
{
    protected List<FightRecordsCreatureBean> listRecordsCreatureData;
    protected FightBean fightData;

    public Action actionForNext;


    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChangeForItem);
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
    public void SetData(FightBean fightData)
    {
        this.fightData = fightData;
        var listRecordsCreatureData = fightData.fightRecordsData.GetRecordsForCreatureData();
        SetListData(listRecordsCreatureData);
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

    #region 按钮
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnNext)
        {
            actionForNext?.Invoke();
            actionForNext = null;
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        switch (fightLogic.fightData.gameFightType)
        {
            case GameFightTypeEnum.Test:
                OnClickForExitTest();
                break;
            case GameFightTypeEnum.Infinite:
                OnClickForExitInfinite();
                break;
            case GameFightTypeEnum.Conquer:
                OnClickForExitConquer();
                break;
        }
    }

    public void OnClickForExitTest()
    {
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }

    public void OnClickForExitInfinite()
    {
        //展示mask
        UIHandler.Instance.ShowMask(1, null, () =>
        {
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            WorldHandler.Instance.EnterGameForBaseScene(userData);
        }, false);
    }

    public void OnClickForExitConquer()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (fightData.gameIsWin)
        {
            if (fightData.fightNum == fightData.figthNumMax)
            {

            }
            //TODO 结算发奖
            else
            {

            }
        }
        else
        {   
            //展示mask
            UIHandler.Instance.ShowMask(1, null, () =>
            {
                WorldHandler.Instance.EnterGameForBaseScene(userData);  
            }, false);
        }
    }
    #endregion
}