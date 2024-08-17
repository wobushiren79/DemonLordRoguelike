using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent
{
    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpack);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        InitBackpackData();
    }

    /// <summary>
    /// item滚动变化
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForBackpack(ScrollGridCell itemCell)
    {

    }

    /// <summary>
    /// 初始化背包卡片数据
    /// </summary>
    public void InitBackpackData()
    {
        ui_BackpackContent.SetCellCount(100);
        //ui_BackpackContent.RefreshAllCells();
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
}
