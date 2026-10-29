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
    /// 遮罩的 RaycastTarget 只挡鼠标射线、挡不住键盘输入，统一走 UIManager 的按来源输入锁封堵；
    /// 解锁统一收在 OnDisable（CloseUI/被 OpenUIAndCloseOther 关掉/销毁等一切隐藏路径都经此处）
    /// </summary>
    public override void OpenUI()
    {
        base.OpenUI();
        UIHandler.Instance.manager.SetUIInputLock(UIInputLockSourceEnum.Mask, true);
    }

    /// <summary>
    /// 遮罩隐藏：解掉遮罩自己的那把 UI 输入锁
    /// <para>按来源解锁:只移除 Mask 来源,不会解开故事演出/屏幕锁等其它来源的锁(旧实现硬置 true 会把别人的锁一并放掉)</para>
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        UIHandler.Instance.manager.SetUIInputLock(UIInputLockSourceEnum.Mask, false);
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
