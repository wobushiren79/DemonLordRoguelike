using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class UIFightSettlement : BaseUIComponent
{
    protected List<FightRecordsCreatureBean> listRecordsCreatureData;
    protected FightRecordsBean fightRecordsData;
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
    public void SetData(FightRecordsBean fightRecordsData)
    {
        this.fightRecordsData = fightRecordsData;
        var listRecordsCreatureData = fightRecordsData.GetRecordsForCreatureData();
        SetListData(listRecordsCreatureData);
    }

    /// <summary>
    /// 设置列表数据
    /// </summary>
    /// <param name="listCreatureData"></param>
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
        itemView.SetData(fightRecordsData, itemData);
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
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        switch (fightLogic.fightData.gameFightType)
        {
            case GameFightTypeEnum.Test:
                UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                WorldHandler.Instance.EnterGameForBaseScene(userData, true);
                break;
            case GameFightTypeEnum.Infinite:
                break;
            case GameFightTypeEnum.Conquer:
                break;
        }
    }
}