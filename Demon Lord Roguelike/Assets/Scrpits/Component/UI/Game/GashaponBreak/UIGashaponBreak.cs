using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponBreak : BaseUIComponent
{
    public GameObject objTargetEgg;
    public GashaponItemBean gashaponItemData;


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
        else if (viewButton == ui_BtnNext)
        {
            OnClickForNext();
        }
        else if (viewButton == ui_BtnReset)
        {
            OnClickForReset();
        }
        else if (viewButton == ui_BtnEnd)
        {
            OnClickForEnd();
        }
    }

    /// <summary>
    /// 初始化-可点击
    /// </summary>
    public void InitForClick(GameObject objTargetEgg, GashaponItemBean gashaponItemData)
    {
        this.objTargetEgg = objTargetEgg;
        this.gashaponItemData = gashaponItemData;
        ui_UIBtn.ShowObj(false);
        ui_ButtonClick.ShowObj(true);
        ui_UIShow.ShowObj(false);
    }

    /// <summary>
    /// 初始化-UI展示
    /// </summary>
    public void InitForBreak(GameObject objTargetEgg, GashaponItemBean gashaponItemData)
    {
        this.objTargetEgg = objTargetEgg;
        this.gashaponItemData = gashaponItemData;
        ui_UIBtn.ShowObj(true);
        ui_ButtonClick.ShowObj(false);
        ui_UIShow.ShowObj(true);

        ui_BtnReset.ShowObj(false);
        ui_BtnNext.ShowObj(true);
        ui_BtnEnd.ShowObj(false);

        InitCardData();
    }

    /// <summary>
    /// 初始化-最后一个蛋
    /// </summary>
    public void InitFoEnd()
    {
        ui_UIBtn.ShowObj(true);
        ui_ButtonClick.ShowObj(false);
        ui_UIShow.ShowObj(true);

        ui_BtnReset.ShowObj(true);
        ui_BtnNext.ShowObj(false);
        ui_BtnEnd.ShowObj(true);


        InitCardData();
    }

    /// <summary>
    /// 初始化-不可操作
    /// </summary>
    public void InitForNone()
    {
        ui_UIBtn.ShowObj(false);
        ui_ButtonClick.ShowObj(false);
        ui_UIShow.ShowObj(false);
    }

    /// <summary>
    /// 初始化卡片数据
    /// </summary>
    public void InitCardData()
    {
        ui_CreatureCardItem.SetData(gashaponItemData.creatureData, CardUseStateEnum.Show);
        ui_ViewCreatureCardDetails.SetData(gashaponItemData.creatureData);
    }

    /// <summary>
    /// 点击破碎
    /// </summary>
    public void OnClickForBreak()
    {
        //设置为不可操作
        InitForNone();
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickBreak, objTargetEgg, gashaponItemData);
    }

    /// <summary>
    /// 点击下一个
    /// </summary>
    public void OnClickForNext()
    {
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickNext);
    }

    /// <summary>
    /// 点击重置
    /// </summary>
    public void OnClickForReset()
    {
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickReset);
    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd() 
    {
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickEnd);
    }

}