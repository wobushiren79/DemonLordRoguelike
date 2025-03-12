

using System;
using UnityEngine.UI;

public partial class UIViewStoreItem : BaseUIView
{

    //商品下标
    public int storeIndex;

    //回调点击
    public Action<int> actionForOnClickBuy;

    /// <summary>
    /// 按钮处理
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BTBuy)
        {
            OnClickForBuy();
        }
    }


    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon()
    {

    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_ItemName.text = $"{name}";
    }

    /// <summary>
    /// 设置价格
    /// </summary>
    public void SetPrice(int price)
    {
        ui_BTTextBuy.text = $"{price}";
    }

    /// <summary>
    /// 点击购买
    /// </summary>
    public void OnClickForBuy()
    {
        actionForOnClickBuy?.Invoke(storeIndex);
    }
}