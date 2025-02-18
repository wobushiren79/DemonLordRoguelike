using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class UIFightSettlement : BaseUIComponent
{
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {

    }
}