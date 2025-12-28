using DG.Tweening;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGashaponBreak : BaseUIComponent
{
    public List<UIViewGashaponBreakItemShow> listViewGashaponBreakItemShow;
    public GameObject objTargetEgg;
    public GashaponItemBean gashaponItemData;
    //UI状态 0:Null 1:展示 2:破碎
    public int uiState = 0;

    /// <summary>
    /// 所有点击事件
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnBreak)
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
        else if (viewButton == ui_BtnShowAll)
        {
            OnClickForShowAll();
        }
        else if (viewButton == ui_BGClick)
        {
            switch (uiState)
            {
                case 1:
                    OnClickForBreak();
                    break;
                case 2:
                    OnClickForNext();
                    break;
            }
        }
    }

    protected void InitForDefault()
    {
        ui_AllList.ShowObj(false);
        ui_BGClick.ShowObj(false);
        ui_UIShow.ShowObj(false);

        ui_BtnReset.ShowObj(false);
        ui_BtnNext.ShowObj(false);
        ui_BtnEnd.ShowObj(false);
        ui_BtnShowAll.ShowObj(false);
        ui_BtnBreak.ShowObj(false);
    }

    /// <summary>
    /// 初始化-不可操作
    /// </summary>
    public void InitForNone()
    {
        uiState = 0;
        InitForDefault();
        RefreshUILayout();
    }

    /// <summary>
    /// 初始化-可点击
    /// </summary>
    public void InitForShow(GameObject objTargetEgg, GashaponItemBean gashaponItemData)
    {
        this.uiState = 1;
        InitForDefault();
        this.objTargetEgg = objTargetEgg;
        this.gashaponItemData = gashaponItemData;
        ui_BGClick.ShowObj(true);
        ui_BtnShowAll.ShowObj(true);
        ui_BtnBreak.ShowObj(true);
        RefreshUILayout();
    }

    /// <summary>
    /// 初始化-UI展示
    /// </summary>
    public void InitForBreak(GameObject objTargetEgg, GashaponItemBean gashaponItemData)
    {
        this.uiState = 2;
        InitForDefault();
        this.objTargetEgg = objTargetEgg;
        this.gashaponItemData = gashaponItemData;
        ui_BGClick.ShowObj(true);
        ui_UIShow.ShowObj(true);
        ui_BtnShowAll.ShowObj(true);
        ui_BtnNext.ShowObj(true);
        InitCardData();
        RefreshUILayout();
    }

    /// <summary>
    /// 初始化-最后一个蛋
    /// </summary>
    public void InitForEnd(List<GashaponItemBean> listGashaponData)
    {
        this.uiState = 3;
        InitForDefault();
        ui_AllList.ShowObj(true);

        ui_BtnReset.ShowObj(true);
        ui_BtnEnd.ShowObj(true);

        InitAllCardData(listGashaponData);
        RefreshUILayout();
    }

    /// <summary>
    /// 初始化卡片数据
    /// </summary>
    public void InitCardData()
    {
        ui_UIViewGashaponBreakItemShow.SetData(gashaponItemData.creatureData, CardUseStateEnum.ShowNoPopup);
        ui_ViewCreatureCardDetails.SetData(gashaponItemData.creatureData);
    }

    /// <summary>
    /// 初始化所有卡片数据
    /// </summary>
    public void InitAllCardData(List<GashaponItemBean> listGashaponData)
    {
        if (listViewGashaponBreakItemShow == null)
        {
            listViewGashaponBreakItemShow = new List<UIViewGashaponBreakItemShow>();
        }
        for (int i = 0; i < listViewGashaponBreakItemShow.Count; i++)
        {
            var itemShow = listViewGashaponBreakItemShow[i];
            itemShow.gameObject.SetActive(false);
        }
        for (int i = 0; i < listGashaponData.Count; i++)
        {
            GashaponItemBean itemData = listGashaponData[i];
            UIViewGashaponBreakItemShow itemShow = null;
            if (i < listViewGashaponBreakItemShow.Count)
            {
                itemShow = listViewGashaponBreakItemShow[i];
            }
            else
            {
                var targetObj = Instantiate(ui_AllList.gameObject, ui_UIViewGashaponBreakItemShow_Model.gameObject);
                itemShow = targetObj.GetComponent<UIViewGashaponBreakItemShow>();
                listViewGashaponBreakItemShow.Add(itemShow);
            }
            itemShow.gameObject.SetActive(true);
            itemShow.SetData(itemData.creatureData, CardUseStateEnum.Show);
        }
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

    /// <summary>
    /// 点击显示所有
    /// </summary>
    public void OnClickForShowAll()
    {
        //触发事件
        this.TriggerEvent(EventsInfo.GashaponMachine_ClickShowAll);
    }

    /// <summary>
    /// 刷新UI
    /// </summary>
    public void RefreshUILayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_AllList);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ui_UIBtn);
    }
}