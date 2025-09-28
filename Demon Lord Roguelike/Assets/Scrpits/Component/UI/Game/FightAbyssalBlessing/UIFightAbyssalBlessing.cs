

using System;
using UnityEngine.UI;

public partial class UIFightAbyssalBlessing : BaseUIComponent
{
    //选择回调
    public Action actionForSelect;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData()
    {
        
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_SkipBtn)
        {
            OnClickForSelect();
        }
    }
    
    /// <summary>
    /// 点击选择
    /// </summary>
    public void OnClickForSelect()
    {
        actionForSelect?.Invoke();
        actionForSelect = null;
    }
}