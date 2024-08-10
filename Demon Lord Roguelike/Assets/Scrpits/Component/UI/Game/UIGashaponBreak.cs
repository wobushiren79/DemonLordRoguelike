using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponBreak : BaseUIComponent
{
    public GameObject objTargetEgg;
    /// <summary>
    /// 所有点击事件
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ButtonClick)
        {
            OnClickForBreak();
        }
    }

    /// <summary>
    /// 初始化-可点击
    /// </summary>
    /// <param name="objTarget"></param>
    public void InitForClick(GameObject objTargetEgg)
    {
        this.objTargetEgg = objTargetEgg;
        ui_ButtonClick.ShowObj(true);
        ui_UIShow.ShowObj(false);
    }

    /// <summary>
    /// 点击破碎
    /// </summary>
    public void OnClickForBreak()
    {
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickBreak, objTargetEgg);
    }
}