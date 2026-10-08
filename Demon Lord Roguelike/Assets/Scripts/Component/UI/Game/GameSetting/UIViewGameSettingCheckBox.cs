using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIViewGameSettingCheckBox : BaseUIView,IRadioButtonCallBack
{
    public Action<UIViewGameSettingCheckBox, bool> actionForCallBack;
    public string selectStr;
    public string unselectStr;

    public override void Awake()
    {
        base.Awake();
        ui_Check.SetCallBack(this);
    }

    public void RadioButtonSelected(RadioButtonView view, bool isSelect)
    {
        actionForCallBack?.Invoke(this, isSelect);
        if (isSelect)
        {
            ui_CheckText.text = selectStr;
        }
        else
        {
            ui_CheckText.text = unselectStr;
        }
    }

    /// <summary>
    /// 设置是否选中（触发回调）
    /// </summary>
    public void SetSelect(bool isSelect)
    {
        ui_Check.SetStates(isSelect);
    }

    /// <summary>
    /// 设置是否选中（不触发回调；用于互斥联动时同步对端勾选状态，避免回调回环）
    /// </summary>
    public void SetSelectNoCallback(bool isSelect)
    {
        ui_Check.ChangeStates(isSelect);
        ui_CheckText.text = isSelect ? selectStr : unselectStr;
    }

    /// <summary>
    /// 设置回调
    /// </summary>
    public void SetCallBack(Action<UIViewGameSettingCheckBox, bool> actionForCallBack)
    {
        this.actionForCallBack = actionForCallBack;
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_CheckBoxText.text = title;
    }

    /// <summary>
    /// 设置开关文本
    /// </summary>
    public void SetCheckStr(string selectStr, string unselectStr)
    {
        this.selectStr = selectStr;
        this.unselectStr = unselectStr;
    }


}
