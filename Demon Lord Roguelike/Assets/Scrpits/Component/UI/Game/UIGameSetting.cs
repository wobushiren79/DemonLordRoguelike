using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIGameSetting : BaseUIComponent, IRadioGroupCallBack
{
    public override void Awake()
    {
        base.Awake();
        ui_TitleRadioGroup.SetCallBack(this);
        ui_TitleRadioGroup.SetPosition(0, false);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_TitleRadioGroup.SetPosition(0, true);
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
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIGameSystem>();
    }

    #region 事件回调
    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
    #endregion
}
