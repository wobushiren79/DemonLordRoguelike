using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIGameSetting : BaseUIComponent, IRadioGroupCallBack
{
    /// <summary>进入类型-主界面(退出后返回主界面)</summary>
    public const int EnterTypeMainStart = 0;
    /// <summary>进入类型-基地(退出后返回基地系统界面)</summary>
    public const int EnterTypeBase = 1;
    /// <summary>进入类型-战斗(打开期间暂停战斗时间，退出后返回战斗系统界面)</summary>
    public const int EnterTypeFight = 2;

    protected UIGameSettingForGame gameSettingForGame;
    protected UIGameSettingForDisplay gameSettingForDisplay;
    protected UIGameSettingForAudio gameSettingForAudio;
    protected UIGameSettingForMods gameSettingForMods;

    public int currentSettingType = 1;
    public int enterType = EnterTypeMainStart;
    /// <summary>战斗进入前的原始时间缩放，用于关闭时恢复</summary>
    protected float timeScaleOrigin = 1f;

    public override void Awake()
    {
        base.Awake();
        ui_TitleRadioGroup.SetCallBack(this);
        ui_TitleRadioGroup.SetPosition(0, false);

        gameSettingForGame = new UIGameSettingForGame(ui_List.gameObject);
        gameSettingForDisplay = new UIGameSettingForDisplay(ui_List.gameObject);
        gameSettingForAudio = new UIGameSettingForAudio(ui_List.gameObject);
        gameSettingForMods = new UIGameSettingForMods(ui_List.gameObject);
    }

    /// <summary>
    /// 打开设置界面
    /// 战斗进入时自行暂停游戏时间(打开时系统界面已被关闭并恢复了时间缩放)
    /// </summary>
    public override void OpenUI()
    {
        base.OpenUI();
        if (enterType == EnterTypeFight)
        {
            timeScaleOrigin = Time.timeScale;
            Time.timeScale = 0f;
        }
        ui_TitleRadioGroup.SetPosition(0, true);
    }

    /// <summary>
    /// 关闭设置界面
    /// 战斗进入时恢复打开前的时间缩放，并保存配置
    /// </summary>
    public override void CloseUI()
    {
        base.CloseUI();
        if (enterType == EnterTypeFight)
            Time.timeScale = timeScaleOrigin;
        GameDataHandler.Instance.manager.SaveGameConfig();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// 按进入类型返回对应界面(主界面/基地系统界面/战斗系统界面)
    /// </summary>
    public void OnClickForExit()
    {
        //主界面进入
        if (enterType == EnterTypeMainStart)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
        }
        //基地进入
        else if (enterType == EnterTypeBase)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIGameSystem>((ui) => ui.enterType = UIGameSystem.EnterTypeBase);
        }
        //战斗进入
        else if (enterType == EnterTypeFight)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIGameSystem>((ui) => ui.enterType = UIGameSystem.EnterTypeFight);
        }
    }

    /// <summary>
    /// 设置设置类型
    /// </summary>
    /// <param name="type"></param>
    public void SetSettingType(int type)
    {
        currentSettingType = type;
        switch (currentSettingType)
        {
            case 1:
                gameSettingForGame.Open();
                break;
            case 2:
                gameSettingForDisplay.Open();
                break;
            case 3:
                gameSettingForAudio.Open();
                break;
            case 4:
                gameSettingForMods.Open();
                break;
        }
    }


    #region 事件回调
    public void RadioButtonSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {
        if (rbview == ui_GameSetttingLabel_Game)
        {
            SetSettingType(1);
        }
        else if (rbview == ui_GameSetttingLabel_Display)
        {
            SetSettingType(2);
        }
        else if (rbview == ui_GameSetttingLabel_Audio)
        {
            SetSettingType(3);
        }
        else if (rbview == ui_GameSetttingLabel_Mods)
        {
            SetSettingType(4);
        }
    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
    #endregion
}
