using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIHandler
{

    /// <summary>
    /// toast提示
    /// </summary>
    /// <param name="hintContent"></param>
    /// <param name="state">0失败 1成功</param>
    public void ToastHintText(string hintContent, int state = 0)
    {
        string iconRes = "icon_unknow";
        Color iconColor = Color.white;
        switch (state)
        {
            case 0:
                iconRes = "ui_other_3";
                ColorUtility.TryParseHtmlString("#E32626",out iconColor);
                break;
            case 1:
                iconRes = "ui_other_6";
                ColorUtility.TryParseHtmlString("#25BC29",out iconColor);
                break;
        }
        IconHandler.Instance.GetIconSprite(SpriteAtlasTypeEnum.UI, iconRes, (sprite) =>
        {
            ToastHint<ToastView>(sprite, iconColor, hintContent);
        });
    }

    /// <summary>
    /// 清理所有主界面UI-用于进游戏
    /// </summary>
    public void DestoryAllMainUI()
    {
        for (int i = 0; i < manager.uiList.Count; i++)
        {
            var itemUI =  manager.uiList[i];
            if (itemUI.name.Contains("UIMain"))
            {
                i--;
                itemUI.SetUICloseType(UICloseTypeEnum.Destory);
                itemUI.CloseUI();
            }
        }
    }

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
        if (maskTime <= 0)
        {
            maskUI.CloseUI();
        }
        else
        {
            maskUI.EndMask(maskTime, acionForStart, acionForComplete, isCloseSelf);
        }
    }

    /// <summary>
    /// 生物选择
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogSelectCreature ShowDialogSelectCreature(DialogSelectCreatureBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectCreature;
        if (dialogData.content.IsNull())
        {
            dialogData.content = TextHandler.Instance.GetTextById(1005001);
        }
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogSelectCreature>(dialogData);
    }

    /// <summary>
    /// 文本修改
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogRename ShowDialogRename(DialogRenameBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Rename;
        if (dialogData.characterLimit <= 0)
        {
            dialogData.characterLimit = 10;
        }
        dialogData.content = TextHandler.Instance.GetTextById(1004001);   
        dialogData.inputHint = TextHandler.Instance.GetTextById(1004002);
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogRename>(dialogData);
    }

    /// <summary>
    /// 展示颜色选择
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogSelectColor ShowDialogSelectColor(DialogSelectColorBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectColor;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        return ShowDialog<UIDialogSelectColor>(dialogData);
    }

    /// <summary>
    /// 展示道具选择弹窗
    /// </summary>
    public UIDialogSelectItem ShowDialogItemSelect(DialogSelectItemBean dialogData)
    {
        dialogData.dialogType = DialogEnum.SelectItem;
        var dialogView = ShowDialog<UIDialogSelectItem>(dialogData);
        dialogView.InitBackpackItemsData();
        return dialogView;
    }

    /// <summary>
    /// 展示默认普通
    /// </summary>
    public UIDialogNormal ShowDialogNormal(DialogBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Normal;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogNormal>(dialogData);
    }
    
    /// <summary>
    /// 展示选项提示
    /// </summary>
    public UIDialogSelect ShowDialogSelect(DialogSelectBean dialogData)
    {
        dialogData.dialogType = DialogEnum.Select;
        return ShowDialog<UIDialogSelect>(dialogData);
    }

    /// <summary>
    /// 展示传送门
    /// </summary>
    /// <param name="dialogData"></param>
    /// <returns></returns>
    public UIDialogPortalDetails ShowDialogPortalDetails(DialogBean dialogData)
    {
        dialogData.dialogType = DialogEnum.PortalDetails;
        if (dialogData.submitStr.IsNull())
            dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        if (dialogData.cancelStr.IsNull())
            dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        return ShowDialog<UIDialogPortalDetails>(dialogData);
    }
}
