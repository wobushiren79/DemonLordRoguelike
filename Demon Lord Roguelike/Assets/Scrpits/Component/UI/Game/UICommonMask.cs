using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UICommonMask : BaseUIComponent
{
    public Color colorStartMask;
    public Color colorEndMask;
    public override void OpenUI()
    {
        base.OpenUI();
    }

    public void StartMask(float maskTime, Action acionForStart, Action acionForComplete)
    {
        acionForStart?.Invoke();
        ui_BG.ShowObj(true);
        ui_BG.color = colorStartMask;
        ui_BG.DOColor(colorEndMask, maskTime).OnComplete(() =>
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
        ui_BG.DOColor(colorStartMask, maskTime).OnComplete(() =>
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
