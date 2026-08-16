using UnityEngine;

/// <summary>
/// 基地/终焉议会场景的基础操作按键提示组：W/A/S/D=移动(常驻)、E=互动(可交互时才显示)、
/// Space=空格突进(研究 UnlockEnum.SpaceDash 解锁后才显示,冷却中展示 CD 遮罩)。
/// 整体显隐受游戏设置「按键提示显示」开关控制——由每个 UIViewPressCommon 子项
/// 自行响应 GameSetting_PressKeyTipShowChange,本组只负责各键的业务显隐条件。
/// </summary>
public partial class UIViewPressControlForGameBase : BaseUIView
{
    /// <summary>E互动键当前是否展示中(仅状态变更时才切换子项,避免每帧重复 SetActive)</summary>
    protected bool isInteractionKeyShowing;
    /// <summary>空格突进是否已解锁(研究 UnlockEnum.SpaceDash 解锁状态缓存)</summary>
    protected bool isDashUnlock;

    #region 生命周期

    /// <summary>
    /// 初始化：W/A/S/D 常驻、E 默认隐藏、Space 按研究解锁初始化，并注册研究解锁变更事件
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        //W/A/S/D 移动按键常驻显示
        ui_UIViewPressCommon_W.SetData(KeyCode.W);
        ui_UIViewPressCommon_A.SetData(KeyCode.A);
        ui_UIViewPressCommon_S.SetData(KeyCode.S);
        ui_UIViewPressCommon_D.SetData(KeyCode.D);
        //E 互动键默认隐藏,可交互时才显示
        ui_UIViewPressCommon_E.HideForNoKey();
        //Space 突进键按研究解锁状态初始化显隐
        RefreshSpaceDashUnlock();
        //研究解锁变更时实时刷新 Space 显隐
        RegisterEvent<long>(EventsInfo.User_AddUnlock, EventForUserAddUnlock);
    }

    /// <summary>
    /// 逐帧轮询基地控制状态：刷新 E 互动键显隐与 Space 突进 CD 遮罩
    /// </summary>
    public void Update()
    {
        RefreshInteractionKey();
        RefreshSpaceDashCDMask();
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 事件-研究解锁变更：空格突进(UnlockEnum.SpaceDash)解锁/升级时刷新 Space 键显隐
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    protected void EventForUserAddUnlock(long unlockId)
    {
        if (unlockId == (long)UnlockEnum.SpaceDash)
            RefreshSpaceDashUnlock();
    }

    #endregion

    #region 状态刷新

    /// <summary>
    /// 刷新 E 互动键显隐：仅当基地控制当前可交互(场景交互提示展示中)时显示,其余时候隐藏
    /// </summary>
    protected void RefreshInteractionKey()
    {
        var control = GameControlHandler.Instance.manager.controlForGameBase;
        bool isCanInteraction = control != null && control.IsInteractionShowing;
        //仅状态变更时才切换
        if (isCanInteraction == isInteractionKeyShowing)
            return;
        isInteractionKeyShowing = isCanInteraction;
        if (isCanInteraction)
            ui_UIViewPressCommon_E.SetData(KeyCode.E);
        else
            ui_UIViewPressCommon_E.HideForNoKey();
    }

    /// <summary>
    /// 刷新 Space 突进键显隐：已解锁空格突进研究才显示,未解锁隐藏(并确保 CD 遮罩关闭)
    /// </summary>
    protected void RefreshSpaceDashUnlock()
    {
        isDashUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockSpaceDashLevel() > 0;
        if (isDashUnlock)
        {
            ui_UIViewPressCommon_Space.SetData(KeyCode.Space);
        }
        else
        {
            ui_UIViewPressCommon_Space.HideForNoKey();
            //未解锁时确保 CD 遮罩关闭
            ui_UIViewPressCommon_Space.SetMaskCD(0, 0);
        }
    }

    /// <summary>
    /// 刷新 Space 突进 CD 遮罩：冷却中展示 MaskCD 并按 剩余/总冷却 更新填充,冷却结束隐藏
    /// </summary>
    protected void RefreshSpaceDashCDMask()
    {
        //未解锁突进:无 CD 可展示
        if (!isDashUnlock)
            return;
        var control = GameControlHandler.Instance.manager.controlForGameBase;
        if (control == null)
            return;
        float cdRemain = control.DashCdRemain;
        if (cdRemain > 0)
        {
            //总冷却按当前研究等级实时读取(突进CD研究可缩短)
            float cdTotal = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockSpaceDashCD();
            ui_UIViewPressCommon_Space.SetMaskCD(cdRemain, cdTotal);
        }
        else
        {
            ui_UIViewPressCommon_Space.SetMaskCD(0, 0);
        }
    }

    #endregion
}
