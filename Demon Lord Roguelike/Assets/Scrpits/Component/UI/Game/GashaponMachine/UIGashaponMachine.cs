using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponMachine : BaseUIComponent
{
    public List<StoreGashaponMachineInfoBean> listStoreData;

    public override void Awake()
    {
        base.Awake();
        ui_List.AddCellListener(OnCellChangeForItem);
    }

    public override void OpenUI()
    {
        base.OpenUI();

        GameControlHandler.Instance.SetBaseControl(false);
        CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue, true);

        InitStoreListData();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ui_List.ClearAllCell();
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
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
        var itemView = itemCell.GetComponent<UIViewStoreItem>();
        itemView.SetData(itemCell.index, listStoreData[itemCell.index], CallBackForItemOnClickyBuy);
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
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        InitUIText();
    }

    /// <summary>
    /// 设置卡组列表
    /// </summary>
    public void InitStoreListData()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userLockData = userData.GetUserUnlockData();
        listStoreData = new List<StoreGashaponMachineInfoBean>();
        var allData = StoreGashaponMachineInfoCfg.GetAllData();
        foreach (var item in allData)
        {
            var itemData = item.Value;
            if (itemData.unlock_id != 0)
            {
                //检测是否解锁普通
                if (!userLockData.CheckIsUnlock(itemData.unlock_id))
                {
                    continue;
                }
                //检测是否解锁生物
                var listCreatureIds = itemData.GetCreatureIds();
                if(!userLockData.CheckIsUnlockForCreature(listCreatureIds))
                {
                    continue;
                }
            }
            listStoreData.Add(itemData);
        }
        ui_List.SetCellCount(listStoreData.Count);
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 回调-item购买
    /// </summary>
    public void CallBackForItemOnClickyBuy(int storeIndex)
    {
        var itemStore = listStoreData[storeIndex];
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //检测是否有足够的魔晶
        if (userData.CheckHasCrystal(itemStore.pay_crystal, isHint: true, isAddCrystal: true))
        {
            StartGashaponMachine(itemStore);
        }
    }

    /// <summary>
    /// 开始扭蛋游戏
    /// </summary>
    /// <param name="num"></param>
    public void StartGashaponMachine(StoreGashaponMachineInfoBean storeGashaponMachineInfoData)
    {        
        List<GashaponMachineCreatureStruct> listCreatureRandomData = new List<GashaponMachineCreatureStruct>();
        GashaponMachineBean gashaponMachine = new GashaponMachineBean();
        //设置扭蛋数量
        gashaponMachine.gashaponNum = storeGashaponMachineInfoData.buy_num;
        gashaponMachine.listCreatureRandomData = listCreatureRandomData;

        //获取所有生物ID
        var listCreatureId = storeGashaponMachineInfoData.GetCreatureIds();

        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userLockData = userData.GetUserUnlockData();

        for (int i = 0; i < listCreatureId.Count; i++)
        {
            long itemCreatureId = listCreatureId[i];
            //检测是否解锁该生物
            if(!userLockData.CheckIsUnlockForCreature(itemCreatureId))
                continue;
                
            CreatureInfoRandomBean creatureInfoRandomData = CreatureInfoRandomCfg.GetItemData(itemCreatureId);

            if (creatureInfoRandomData != null)
            {
                GashaponMachineCreatureStruct gashaponMachineCreature = new GashaponMachineCreatureStruct();
                gashaponMachineCreature.creatureId = itemCreatureId;
                //获取所有随机的身体部件
                gashaponMachineCreature.randomCreatureMode = creatureInfoRandomData.GetAllRandomData();

                listCreatureRandomData.Add(gashaponMachineCreature);
            }
        }
        GameHandler.Instance.StartGashaponMachine(gashaponMachine);
    }

}
