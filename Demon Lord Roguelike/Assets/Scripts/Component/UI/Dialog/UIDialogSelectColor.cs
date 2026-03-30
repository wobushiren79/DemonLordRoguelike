

using UnityEngine.UI;
using UnityEngine;

public partial class UIDialogSelectColor : DialogView
{
    public Color showColor;

    public override void InitData()
    {
        base.InitData();
        ui_ColorR.onValueChanged.AddListener((value) => { OnColorChange(ui_ColorR, value); });
        ui_ColorG.onValueChanged.AddListener((value) => { OnColorChange(ui_ColorG, value); });
        ui_ColorB.onValueChanged.AddListener((value) => { OnColorChange(ui_ColorB, value); });
    }

    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        
        var dialogSelectData = dialogData as DialogSelectColorBean;
        InitColor(dialogSelectData.color);
    }

    public void OnColorChange(Slider targetView, float value)
    {
        if (targetView == ui_ColorR)
        {
            showColor = new Color(value, showColor.g, showColor.b, showColor.a);
        }
        else if (targetView == ui_ColorG)
        {
            showColor = new Color(showColor.r, value, showColor.b, showColor.a);
        }
        else if (targetView == ui_ColorB)
        {
            showColor = new Color(showColor.r, showColor.g, value, showColor.a);
        }
        SetShowColor(showColor);
    }

    public void InitColor(Color showColor)
    {
        this.showColor = showColor;
        ui_ColorR.value = showColor.r;
        ui_ColorG.value = showColor.g;
        ui_ColorB.value = showColor.b;  
        SetShowColor(showColor);
    }

    public void SetShowColor(Color showColor)
    {
        ui_ColorShow.color = showColor;
    }

    public override void SubmitOnClick()
    {
        var dialogSelectData = dialogData as DialogSelectColorBean;
        dialogSelectData.color = showColor;
        base.SubmitOnClick();
    }
}