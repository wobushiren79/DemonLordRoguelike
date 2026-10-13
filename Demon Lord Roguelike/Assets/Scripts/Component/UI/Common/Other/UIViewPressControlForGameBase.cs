using UnityEngine;

/// <summary>
/// 场景操作按键提示组：按显示模式区分两套按键布局——
/// 基地/终焉议会(GameBase)：W/A/S/D=移动(常驻)、E=互动(可交互时才显示)、Space=空格突进(研究 UnlockEnum.SpaceDash 解锁后才显示,冷却中展示 CD 遮罩)；
/// 战斗(GameFight)：W/A/S/D=移动镜头(常驻)、F1=2倍速、F2=加快进攻节奏、Space=0倍速(三者显隐由 UIFightMain 按研究与战斗模式门控推送)。
/// 整体显隐受游戏设置「按键提示显示」开关控制——由每个 UIViewPressCommon 子项
/// 自行响应 GameSetting_PressKeyTipShowChange,本组只负责各键的业务显隐条件。
/// 每个按键均配悬浮文本气泡提示该键用途(UIViewPressCommon.SetPopupTip)。
/// </summary>
public partial class UIViewPressControlForGameBase : BaseUIView
{
    /// <summary>
    /// 显示模式：基地/战斗 两套按键布局与提示
    /// </summary>
    public enum ShowModeEnum
    {
        GameBase = 0,//基地/终焉议会
        GameFight = 1,//战斗
    }

    #region 多语言文本ID
    /// <summary>提示文本ID-移动(基地WASD)</summary>
    protected const long TEXT_ID_MOVE = 2000026;
    /// <summary>提示文本ID-移动镜头(战斗WASD)</summary>
    protected const long TEXT_ID_MOVE_CAMERA = 2000027;
    /// <summary>提示文本ID-互动(基地E)</summary>
    protected const long TEXT_ID_INTERACTION = 2000028;
    /// <summary>提示文本ID-突进(基地Space)</summary>
    protected const long TEXT_ID_DASH = 2000029;
    /// <summary>提示文本ID-0倍速(战斗Space)</summary>
    protected const long TEXT_ID_SPEED_0 = 2000030;
    /// <summary>提示文本ID-2倍速(战斗F1)</summary>
    protected const long TEXT_ID_SPEED_2 = 2000031;
    /// <summary>提示文本ID-加快进攻节奏(战斗F2)</summary>
    protected const long TEXT_ID_QUICK_ATTACK = 2000032;
    #endregion

    /// <summary>当前显示模式(默认基地,战斗由 UIFightMain.InitData 切换)</summary>
    protected ShowModeEnum showMode = ShowModeEnum.GameBase;
    /// <summary>E互动键当前是否展示中(仅状态变更时才切换子项,避免每帧重复 SetActive)</summary>
    protected bool isInteractionKeyShowing;
    /// <summary>空格突进是否已解锁(研究 UnlockEnum.SpaceDash 解锁状态缓存)</summary>
    protected bool isDashUnlock;

    #region 生命周期

    /// <summary>
    /// 初始化：默认按基地模式布局(战斗模式由 UIFightMain.InitData 调用 SetShowMode 切换)，并注册研究解锁变更事件
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        SetShowMode(ShowModeEnum.GameBase);
        //研究解锁变更时实时刷新 Space 显隐(仅基地模式的突进)
        RegisterEvent<long>(EventsInfo.User_AddUnlock, EventForUserAddUnlock);
    }

    /// <summary>
    /// 逐帧轮询基地控制状态：刷新 E 互动键显隐与 Space 突进 CD 遮罩(仅基地模式需要)
    /// </summary>
    public void Update()
    {
        if (showMode != ShowModeEnum.GameBase)
            return;
        RefreshInteractionKey();
        RefreshSpaceDashCDMask();
    }

    #endregion

    #region 模式切换

    /// <summary>
    /// 设置显示模式：GameBase=基地(WASD移动/E互动/Space突进)、GameFight=战斗(WASD移动镜头/F1 2倍速/F2 加快进攻节奏/Space 0倍速)
    /// </summary>
    /// <param name="targetMode">目标模式</param>
    public void SetShowMode(ShowModeEnum targetMode)
    {
        showMode = targetMode;
        //W/A/S/D 两种模式都常驻,仅悬浮提示文案不同(基地=移动,战斗=移动镜头)
        long textIdForWasd = targetMode == ShowModeEnum.GameFight ? TEXT_ID_MOVE_CAMERA : TEXT_ID_MOVE;
        string tipForWasd = TextHandler.Instance.GetTextById(textIdForWasd);
        ui_UIViewPressCommon_W.SetData(KeyCode.W);
        ui_UIViewPressCommon_A.SetData(KeyCode.A);
        ui_UIViewPressCommon_S.SetData(KeyCode.S);
        ui_UIViewPressCommon_D.SetData(KeyCode.D);
        ui_UIViewPressCommon_W.SetPopupTip(tipForWasd);
        ui_UIViewPressCommon_A.SetPopupTip(tipForWasd);
        ui_UIViewPressCommon_S.SetPopupTip(tipForWasd);
        ui_UIViewPressCommon_D.SetPopupTip(tipForWasd);
        //F1/F2 仅战斗模式使用,基地恒隐藏
        ui_UIViewPressCommon_F1.HideForNoKey();
        ui_UIViewPressCommon_F2.HideForNoKey();
        if (targetMode == ShowModeEnum.GameFight)
        {
            //战斗:E恒隐藏;Space(0倍速)默认隐藏,等 UIFightMain 按研究门控推送显隐;关闭基地突进CD遮罩
            ui_UIViewPressCommon_E.HideForNoKey();
            ui_UIViewPressCommon_Space.HideForNoKey();
            ui_UIViewPressCommon_Space.SetMaskCD(0, 0);
            //悬浮提示提前配好,后续显隐切换不影响提示内容
            ui_UIViewPressCommon_Space.SetPopupTip(TextHandler.Instance.GetTextById(TEXT_ID_SPEED_0));
            ui_UIViewPressCommon_F1.SetPopupTip(TextHandler.Instance.GetTextById(TEXT_ID_SPEED_2));
            ui_UIViewPressCommon_F2.SetPopupTip(TextHandler.Instance.GetTextById(TEXT_ID_QUICK_ATTACK));
        }
        else
        {
            //基地:E可交互时才显示(悬浮提示提前配好);Space按突进研究解锁显隐
            isInteractionKeyShowing = false;
            ui_UIViewPressCommon_E.HideForNoKey();
            ui_UIViewPressCommon_E.SetPopupTip(TextHandler.Instance.GetTextById(TEXT_ID_INTERACTION));
            ui_UIViewPressCommon_Space.SetPopupTip(TextHandler.Instance.GetTextById(TEXT_ID_DASH));
            RefreshSpaceDashUnlock();
        }
    }

    #endregion

    #region 战斗按键显隐

    /// <summary>
    /// 设置 F1(2倍速)按键提示显隐(仅战斗模式)：与2倍速按钮同门控——仅征服模式且已解锁当前世界当前难度「2倍速游戏」研究时显示
    /// </summary>
    /// <param name="isShow">是否显示</param>
    public void SetSpeed2KeyShow(bool isShow)
    {
        if (showMode != ShowModeEnum.GameFight)
            return;
        if (isShow)
            ui_UIViewPressCommon_F1.SetData(KeyCode.F1);
        else
            ui_UIViewPressCommon_F1.HideForNoKey();
    }

    /// <summary>
    /// 设置 F2(加快进攻节奏)按键提示显隐(仅战斗模式)：与Quick按钮同门控——仅征服模式且已解锁当前世界当前难度「加快进攻节奏」研究时显示
    /// </summary>
    /// <param name="isShow">是否显示</param>
    public void SetQuickKeyShow(bool isShow)
    {
        if (showMode != ShowModeEnum.GameFight)
            return;
        if (isShow)
            ui_UIViewPressCommon_F2.SetData(KeyCode.F2);
        else
            ui_UIViewPressCommon_F2.HideForNoKey();
    }

    /// <summary>
    /// 设置 Space(0倍速)按键提示显隐(仅战斗模式)：与0速按钮同门控——仅征服模式且已解锁当前世界当前难度「0倍速游戏」研究时显示
    /// </summary>
    /// <param name="isShow">是否显示</param>
    public void SetSpeed0KeyShow(bool isShow)
    {
        if (showMode != ShowModeEnum.GameFight)
            return;
        if (isShow)
            ui_UIViewPressCommon_Space.SetData(KeyCode.Space);
        else
            ui_UIViewPressCommon_Space.HideForNoKey();
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 事件-研究解锁变更：空格突进(UnlockEnum.SpaceDash)解锁/升级时刷新 Space 键显隐(仅基地模式)
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    protected void EventForUserAddUnlock(long unlockId)
    {
        if (showMode != ShowModeEnum.GameBase)
            return;
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
