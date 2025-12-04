
using System;
using UnityEngine;
using UnityEngine.UI;

public partial class UIViewColorShow : BaseUIView
{
    public Color showColor;
    public Action<Color> actionForColorChange;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewColorShow)
        {
            OnClickForSelect();
        }
    }

    public void SetData(string title, Color showColor, Action<Color> actionForColorChange)
    {
        this.actionForColorChange = actionForColorChange;
        SetTitle(title);
        SetShowColor(showColor);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_SelectText.text = $"{title}";
    }

    /// <summary>
    /// 设置显示颜色
    /// </summary>
    public void SetShowColor(Color showColor)
    {
        this.showColor = showColor;
        ui_SelectColor.color = showColor;
    }

    /// <summary>
    /// 点击打开颜色选择
    /// </summary>
    public void OnClickForSelect()
    {
        if (actionForColorChange == null)
            return;
        DialogSelectColorBean dialogData = new DialogSelectColorBean();
        dialogData.color = showColor;
        dialogData.actionSubmit = (view, data) =>
        {
            actionForColorChange?.Invoke(dialogData.color);
            SetShowColor(dialogData.color);
        };
        UIHandler.Instance.ShowDialogSelectColor(dialogData);
    }
}