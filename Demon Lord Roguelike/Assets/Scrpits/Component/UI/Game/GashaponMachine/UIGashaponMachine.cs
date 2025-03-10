using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponMachine : BaseUIComponent
{
    //单个卡片所占的比例宽
    protected float itemCardGroupViewW;
    //当前选中的卡住下标
    protected int currentSelectCardGroupIndex = 0;

    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChangeForItem);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        currentSelectCardGroupIndex = -1;
        SetCardGroupList();
        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue, true);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_List.ClearAllCell();
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    public void InitUIText()
    {

    }

    /// <summary>
    /// 列表数据设置
    /// </summary>
    public void OnCellChangeForItem(ScrollGridCell itemCell)
    {

    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BTBuy)
        {
            StartGashaponMachine(1);
        }
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        InitUIText();
    }

    /// <summary>
    /// 开始扭蛋游戏
    /// </summary>
    /// <param name="num"></param>
    public void StartGashaponMachine(int num)
    {
        GashaponMachineBean gashaponMachine = new GashaponMachineBean();
        gashaponMachine.gashaponNum = num;
        GameHandler.Instance.StartGashaponMachine(gashaponMachine);
    }

    /// <summary>
    /// 设置卡组列表
    /// </summary>
    public void SetCardGroupList()
    {
        var allData = StoreGashaponMachineInfoCfg.GetAllData();
        foreach (var item in allData)
        {
            var itemData = item.Value;
            GameObject objItem = Instantiate(ui_Content.gameObject, ui_ViewGashaponCardGroupItem.gameObject);
            var itemView = objItem.GetComponent<UIViewGashaponCardGroupItem>();
            itemView.SetData(itemData);
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }
}
