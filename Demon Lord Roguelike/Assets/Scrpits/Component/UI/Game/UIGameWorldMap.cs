using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameWorldMap : BaseUIComponent
{

    /// <summary>
    /// 按钮点击
    /// </summary>
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击-退出
    /// </summary>
    public void OnClickForExit()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content= TextHandler.Instance.GetTextById(501);
        dialogData.submitStr = TextHandler.Instance.GetTextById(1000001);
        dialogData.cancelStr = TextHandler.Instance.GetTextById(1000002);
        dialogData.actionSubmit = (view, data) =>
        {
            UIHandler.Instance.ShowMask(2f, 
                () =>
                {
                    AnimForHideUI(2);
                }, 
                () =>
                {
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    userData.ClearGameWorldMapData();
                    WorldHandler.Instance.EnterGameForBaseScene(userData, true);
                },false);
        };
        var targetDialog = UIHandler.Instance.ShowDialog<UIDialogNormal>(dialogData);
    }

    /// <summary>
    /// 动画展开动画
    /// </summary>
    public void AnimForShowUI(float animTime)
    {
        ui_MaskClick.ShowObj(true);
        Vector2 targetSizeDelta = new Vector2(1920,1080);
        ui_Content.sizeDelta = new Vector2(200, 1080);
        ui_Content
            .DOSizeDelta(targetSizeDelta, animTime)
            .OnComplete(() =>
            {
                ui_MaskClick.ShowObj(false);
                ui_Content.sizeDelta = targetSizeDelta;
            });
    }

    /// <summary>
    /// 动画关闭
    /// </summary>
    public void AnimForHideUI(float animTime)
    {
        ui_MaskClick.ShowObj(true);
        Vector2 targetSizeDelta = new Vector2(200, 1080);
        ui_Content.sizeDelta = new Vector2(1920, 1080);
        ui_Content
            .DOSizeDelta(targetSizeDelta, animTime)
            .OnComplete(() =>
            {
                ui_MaskClick.ShowObj(false);
                ui_Content.sizeDelta = targetSizeDelta;
            });
    }

}
