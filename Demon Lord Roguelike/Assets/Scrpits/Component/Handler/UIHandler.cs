using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIHandler
{
    /// <summary>
    /// 展示遮罩UI
    /// </summary>
    public void ShowMask(float maskTime, Action acionForStart, Action acionForComplete, bool isCloseOther)
    {
        UICommonMask maskUI;
        if (isCloseOther)
        {
            maskUI = OpenUIAndCloseOther<UICommonMask>(layer: 99);
        }
        else
        {
            maskUI = OpenUI<UICommonMask>(layer: 99);
        }
        maskUI.StartMask(maskTime, acionForStart, acionForComplete);
    }

    public void HideMask(float maskTime, Action acionForStart, Action acionForComplete, bool isCloseSelf = true)
    {
        var maskUI = OpenUIAndCloseOther<UICommonMask>();
        maskUI.EndMask(maskTime, acionForStart, acionForComplete, isCloseSelf);
    }

    /// <summary>
    /// 展示默认普通
    /// </summary>
    public UIDialogNormal ShowDialogNormal(DialogBean dialogData)
    {
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogNormal>(dialogData);
    }

    /// <summary>
    /// 展示传送门
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogPortalDetails ShowDialogPortalDetails(DialogBean dialogData)
    {
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogPortalDetails>(dialogData);
    }
}
