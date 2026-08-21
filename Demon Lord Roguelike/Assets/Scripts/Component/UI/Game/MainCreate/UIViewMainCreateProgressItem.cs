using System;
using UnityEngine;

public partial class UIViewMainCreateProgressItem : BaseUIView
{
    //标题文本
    public string titleStr;
    //数值变化回调
    public Action<UIViewMainCreateProgressItem, float> actionForValueChange;

    public override void Awake()
    {
        base.Awake();
        ui_ItemSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ui_ItemSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="minValue">滑条最小值</param>
    /// <param name="maxValue">滑条最大值</param>
    /// <param name="value">当前值（不触发回调）</param>
    /// <param name="actionForValueChange">数值变化回调</param>
    public void SetData(string title, float minValue, float maxValue, float value, Action<UIViewMainCreateProgressItem, float> actionForValueChange)
    {
        this.titleStr = title;
        this.actionForValueChange = actionForValueChange;
        ui_ItemSlider.minValue = minValue;
        ui_ItemSlider.maxValue = maxValue;
        SetValue(value);
    }

    /// <summary>
    /// 设置当前值（不触发回调，用于外部同步）
    /// </summary>
    public void SetValue(float value)
    {
        ui_ItemSlider.SetValueWithoutNotify(value);
        RefreshTitle(value);
    }

    /// <summary>
    /// 滑条数值变化
    /// </summary>
    protected void OnSliderValueChanged(float value)
    {
        RefreshTitle(value);
        actionForValueChange?.Invoke(this, value);
    }

    /// <summary>
    /// 刷新标题（标题 + 百分比数值，如 身高 105%）
    /// </summary>
    protected void RefreshTitle(float value)
    {
        ui_Title.text = $"{titleStr} {Mathf.RoundToInt(value * 100)}%";
    }
}
