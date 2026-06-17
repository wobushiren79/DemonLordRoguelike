

using UnityEngine.InputSystem;

public partial class UIDoomCouncilMain : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        //开启控制
        GameControlHandler.Instance.SetBaseControl();
        //开启摄像头
        CameraHandler.Instance.SetCameraForControl(CinemachineCameraEnum.Base);
    }

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        RefreshUIData();
    }

    /// <summary>
    /// 刷新UI数据
    /// </summary>
    public void RefreshUIData()
    {
        //刷新当前议案通过率
        RefreshSuccessRate();
    }

    /// <summary>
    /// 刷新当前议案通过率显示
    /// </summary>
    public void RefreshSuccessRate()
    {
        if (ui_SuccessText == null)
        {
            return;
        }
        var doomCouncilLogic = GameHandler.Instance.manager.GetGameLogic<DoomCouncilLogic>();
        if (doomCouncilLogic == null || doomCouncilLogic.doomCouncilData == null || doomCouncilLogic.doomCouncilData.doomCouncilInfo == null)
        {
            ui_SuccessText.text = "";
            return;
        }
        //当前议案通过率(百分比,保留2位小数)
        float percentage = MathUtil.GetPercentage(doomCouncilLogic.doomCouncilData.doomCouncilInfo.success_rate, 2);
        ui_SuccessText.text = string.Format(TextHandler.Instance.GetTextById(53014), percentage);
    }
}