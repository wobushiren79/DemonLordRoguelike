using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UICommonMask : BaseUIComponent
{
    public Color colorStartMask;
    public Color colorEndMask;

    /// <summary>
    /// 打开遮罩：遮罩拉起期间锁定全部 UI 输入（按钮点击 + ESC/数字键等快捷键）
    /// 遮罩的 RaycastTarget 只挡鼠标射线、挡不住键盘输入，统一拨现有总开关封堵；
    /// 恢复统一收在 OnDisable（CloseUI/被 OpenUIAndCloseOther 关掉/销毁等一切隐藏路径都经此处）
    /// </summary>
    public override void OpenUI()
    {
        base.OpenUI();
        UIHandler.Instance.manager.CanClickUIButtons = false;
        UIHandler.Instance.manager.CanInputActionStarted = false;
    }

    /// <summary>
    /// 遮罩隐藏：恢复 UI 输入总开关
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        //屏幕锁(UIScreenLock)仍在展示时让位——那是独立的锁屏流程，由 HideScreenLock 负责解锁，避免抢解锁提前放行输入
        var uiList = UIHandler.Instance.manager.uiList;
        for (int i = 0; i < uiList.Count; i++)
        {
            var itemUI = uiList[i];
            if (itemUI is UIScreenLock && itemUI.gameObject.activeSelf)
                return;
        }
        UIHandler.Instance.manager.CanClickUIButtons = true;
        UIHandler.Instance.manager.CanInputActionStarted = true;
    }

    public void StartMask(float maskTime, Action acionForStart, Action acionForComplete)
    {
        acionForStart?.Invoke();
        ui_BG.ShowObj(true);
        ui_BG.color = colorStartMask;
        ui_BG.DOColor(colorEndMask, maskTime).SetUpdate(true).OnComplete(() =>
        {
            ui_BG.color = colorEndMask;
            acionForComplete?.Invoke();
        });
    }

    public void EndMask(float maskTime, Action acionForStart, Action acionForComplete,bool isCloseSelf = true)
    {
        acionForStart?.Invoke();
        ui_BG.ShowObj(true);
        ui_BG.color = colorEndMask;
        ui_BG.DOColor(colorStartMask, maskTime).SetUpdate(true).OnComplete(() =>
        {
            ui_BG.ShowObj(false);
            ui_BG.color = colorStartMask;
      
            if (isCloseSelf)
            {
                UIHandler.Instance.CloseUI<UICommonMask>();
            }
            acionForComplete?.Invoke();
        });
    }
}
