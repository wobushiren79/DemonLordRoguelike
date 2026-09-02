using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForDisplay : UIGameSettingBase
{

    protected UIViewGameSettingSelect selectForScreen;
    protected UIViewGameSettingCheckBox checkboxForVSync;
    protected UIViewGameSettingCheckBox checkboxForFrameLock;
    protected UIViewGameSettingRange rangeForFrame;
    //分辨率选项列表（预设列表 + 拖动窗口产生的自定义分辨率）
    protected List<string> listResolutionData;

    public UIGameSettingForDisplay(GameObject objListContainer) : base(objListContainer)
    {

    }


    public override void Open()
    {
        base.Open();

        //屏幕分辨率（拖动窗口产生的自定义分辨率不在预设列表时，插入首位展示）
        string textScreenTitle = TextHandler.Instance.GetTextById(42001);
        listResolutionData = new List<string>(GameSystemInfo.ListScreenResolutionData);
        int indexResolution = listResolutionData.IndexOf(gameConfig.screenResolution);
        if (indexResolution < 0 && !gameConfig.screenResolution.IsNull())
        {
            listResolutionData.Insert(0, gameConfig.screenResolution);
            indexResolution = 0;
        }
        selectForScreen = CreatureItemForSelect(textScreenTitle, listResolutionData);
        selectForScreen.SetSelcet(indexResolution);

        //垂直同步
        string textVSyncTitle = TextHandler.Instance.GetTextById(42004);
        checkboxForVSync = CreatureItemForCheckBox(textVSyncTitle);
        checkboxForVSync.SetSelect(gameConfig.vsync);

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
            gameConfig.screenResolution = listResolutionData[index];
            gameConfig.GetScreenResolution(out int w, out int h);
            //通过分辨率Handler设置（更新锚定比例并标记来源，避免被等比缩放逻辑二次修正）
            ScreenResolutionHandler.Instance.SetResolutionByCode(w, h);
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
        else if (targetView == checkboxForVSync)
        {
            gameConfig.vsync = isCheck;
            FPSHandler.Instance.SetSyncCount(isCheck ? 1 : 0);
        }
    }
}
