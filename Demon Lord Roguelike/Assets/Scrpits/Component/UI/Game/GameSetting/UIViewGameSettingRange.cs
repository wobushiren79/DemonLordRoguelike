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
    /// �ص�
    /// </summary>
    public void OnValueChanged(float progress)
    {
        actionForValueChanged?.Invoke(this,progress);
    }

    /// <summary>
    /// ���ñ���
    /// </summary>
    public void SetTitle(string title)
    {
        ui_RangeText.text = title;
    }

    /// <summary>
    /// ���ý���
    /// </summary>
    public void SetProgress(float progress)
    {
        ui_Slider.value = progress;
    }

    /// <summary>
    /// ���ý�������
    /// </summary>
    public void SetProgressText(string progressText)
    {
        ui_RangeProgressText.text = progressText;
    }

    /// <summary>
    /// ���÷�Χ
    /// </summary>
    public void SetMinAndMax(float min,float max)
    {
        ui_Slider.minValue = min;
        ui_Slider.maxValue = max;
    }

    /// <summary>
    /// ���ûص�
    /// </summary>
    public void SetCallBack(Action<UIViewGameSettingRange, float> actionForValueChanged)
    {
        this.actionForValueChanged = actionForValueChanged;
    }
}
