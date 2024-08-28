using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIViewGameSettingRange : BaseUIView
{
    public Action<UIViewGameSettingRange,float> actionForValueChanged;

    public override void Awake()
    {
        base.Awake();
        ui_Slider.onValueChanged.AddListener(OnValueChanged);
    }

    /// <summary>
    /// 回调
    /// </summary>
    public void OnValueChanged(float progress)
    {
        actionForValueChanged?.Invoke(this,progress);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_RangeText.text = title;
    }

    /// <summary>
    /// 设置进度
    /// </summary>
    public void SetProgress(float progress)
    {
        ui_Slider.value = progress;
    }

    /// <summary>
    /// 设置进度文字
    /// </summary>
    public void SetProgressText(string progressText)
    {
        ui_RangeProgressText.text = progressText;
    }

    /// <summary>
    /// 设置范围
    /// </summary>
    public void SetMinAndMax(float min,float max)
    {
        ui_Slider.minValue = min;
        ui_Slider.maxValue = max;
    }

    /// <summary>
    /// 设置回调
    /// </summary>
    public void SetCallBack(Action<UIViewGameSettingRange, float> actionForValueChanged)
    {
        this.actionForValueChanged = actionForValueChanged;
    }
}
