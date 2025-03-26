

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

        SetName(storeGashaponMachineInfoData.GetName());
        SetPrice(storeGashaponMachineInfoData.pay_crystal);
    }
}