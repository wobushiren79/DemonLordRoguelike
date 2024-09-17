using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIHandler
{
    public void ShowMask(float maskTime, Action acionForStart, Action acionForComplete)
    {
        var maskUI = OpenUIAndCloseOther<UICommonMask>();
        maskUI.StartMask(maskTime, acionForStart, acionForComplete);
    }

    public void HideMask(float maskTime, Action acionForStart, Action acionForComplete, bool isCloseSelf = true)
    {
        var maskUI = OpenUIAndCloseOther<UICommonMask>();
        maskUI.EndMask(maskTime, acionForStart, acionForComplete, isCloseSelf);
    }

}
