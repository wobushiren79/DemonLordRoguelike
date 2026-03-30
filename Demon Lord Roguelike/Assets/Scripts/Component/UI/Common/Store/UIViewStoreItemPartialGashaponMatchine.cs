

using System;

public partial class UIViewStoreItem
{
    private StoreGashaponMachineInfoBean storeGashaponMachineInfoData;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(int storeIndex, StoreGashaponMachineInfoBean itemData, Action<int> actionForOnClickBuy)
    {
        this.storeIndex = storeIndex;
        this.storeGashaponMachineInfoData = itemData;
        this.actionForOnClickBuy = actionForOnClickBuy;

        string name = storeGashaponMachineInfoData.name_language;
        SetName(name);
        SetPrice(storeGashaponMachineInfoData.pay_crystal);
        SetIcon(storeGashaponMachineInfoData.icon_res);
    }
}