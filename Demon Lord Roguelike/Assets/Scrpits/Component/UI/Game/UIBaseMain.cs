using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIBaseMain : BaseUIComponent
{

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    /// <summary>
    /// ˢ��UI����
    /// </summary>
    public void RefreshUIData()
    {

    }
}
