using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForDisplay : UIGameSettingBase
{

    protected UIViewGameSettingSelect selectForScreen;
    protected UIViewGameSettingCheckBox checkboxForFrameLock;
    protected UIViewGameSettingRange rangeForFrame;

    public UIGameSettingForDisplay(GameObject objListContainer) : base(objListContainer)
    {

    }


    public override void Open()
    {
        base.Open();

        //屏幕分辨率
        string textScreenTitle = TextHandler.Instance.GetTextById(42001);
        selectForScreen = CreatureItemForSelect(textScreenTitle, GameSystemInfo.ListScreenResolutionData);
        selectForScreen.SetSelcet(GameSystemInfo.ListScreenResolutionData.IndexOf(gameConfig.screenResolution));

        //帧数锁定
        string textFrameLockTitle = TextHandler.Instance.GetTextById(42002);
        checkboxForFrameLock = CreatureItemForCheckBox(textFrameLockTitle);
        checkboxForFrameLock.SetSelect(gameConfig.stateForFrames == 1 ? true : false);

        //帧数
        string textFrameTitle = TextHandler.Instance.GetTextById(42003);
        rangeForFrame = CreatureItemForRange(textFrameTitle, 30, 120);
        rangeForFrame.SetProgress((float)gameConfig.frames);
    }

    public override void ActionForRangeValueChange(UIViewGameSettingRange targetView, float progress)
    {
        base.ActionForRangeValueChange(targetView, progress);
        if (targetView == rangeForFrame)
        {
            int targetFrame = Mathf.RoundToInt(progress);
            targetView.SetProgressText($"{targetFrame}");
            gameConfig.frames = targetFrame;
            FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        }
    }

    public override void ActionForSelectValueChange(UIViewGameSettingSelect targetView, int index)
    {
        base.ActionForSelectValueChange(targetView, index);
        if (targetView == selectForScreen)
        {
            gameConfig.screenResolution = GameSystemInfo.ListScreenResolutionData[index];
            gameConfig.GetScreenResolution(out int w, out int h);
            //只有全屏模式才使用固定分辨率，窗口模式时使用自己的分辨率
            Screen.SetResolution(w, h, false);
        }
    }

    public override void ActionForCheckBoxValueChange(UIViewGameSettingCheckBox targetView, bool isCheck)
    {
        base.ActionForCheckBoxValueChange(targetView, isCheck);
        if (targetView == checkboxForFrameLock)
        {
            gameConfig.stateForFrames = isCheck ? 1 : 0;
            FPSHandler.Instance.SetData(gameConfig.stateForFrames, gameConfig.frames);
        }
    }
}
