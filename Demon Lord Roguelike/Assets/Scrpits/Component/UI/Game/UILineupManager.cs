using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent
{
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpack);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
        this.RegisterEvent<CreatureBean>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<CreatureBean>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        InitBackpackData();
        InitLineupData();
    }


    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForBackpack(ScrollGridCell itemCell)
    {
        var itemData = listBackpackCreature[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.SetData(itemData, CardUseState.LineupBackpack);
    }

    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitBackpackData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        listBackpackCreature.Clear();
        listBackpackCreature.AddRange(userData.listBackpackCreature);
        //设置数量
        ui_BackpackContent.SetCellCount(userData.listBackpackCreature.Count);
    }

    /// <summary>
    /// 初始化阵容内容
    /// </summary>
    public void InitLineupData()
    {
       UserDataBean userData =  GameDataHandler.Instance.manager.GetUserData();
    }


    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback); 
        if(inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 事件-焦点选中卡片
    /// </summary>
    public void EventForCardPointerEnter(CreatureBean creatureData)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(true);
        ui_ViewCreatureCardDetails.SetData(creatureData);
    }

    public void EventForCardPointerExit(CreatureBean creatureData)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
    }
}
