using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIGameSystem : BaseUIComponent
{
    /// <summary>进入类型-基地(默认，显示设置/返回/退出按钮)</summary>
    public const int EnterTypeBase = 0;
    /// <summary>进入类型-战斗(显示结束战斗按钮，支持重开的战斗模式额外显示重新挑战按钮)</summary>
    public const int EnterTypeFight = 1;

    /// <summary>当前进入类型，决定按钮显隐与退出逻辑</summary>
    public int enterType = EnterTypeBase;

    /// <summary>打开界面前的原始时间缩放，用于关闭时恢复</summary>
    protected float timeScaleOrigin = 1f;

    public override void OpenUI()
    {
        base.OpenUI();
        //打开系统界面时暂停游戏时间，记录原始时间缩放
        timeScaleOrigin = Time.timeScale;
        Time.timeScale = 0f;
        //打开系统界面时暂停背景音乐播放
        AudioHandler.Instance.PauseMusic();
        //战斗模式保持战斗控制不变；基地模式禁用基础移动控制
        if (enterType != EnterTypeFight)
            GameControlHandler.Instance.SetBaseControl(false, false);
        //根据进入类型刷新按钮显隐
        RefreshButtonShow();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        //关闭系统界面时恢复打开前的时间缩放
        Time.timeScale = timeScaleOrigin;
        //关闭系统界面时恢复背景音乐播放
        AudioHandler.Instance.RestoreMusic();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnSetting)
        {
            OnClickForGameSetting();
        }
        else if (viewButton == ui_BtnBack)
        {
            OnClickForBackMain();
        }
        else if (viewButton == ui_BtnExit)
        {
            OnClickForExitGame();
        }
        else if (viewButton == ui_BtnExitFight)
        {
            OnClickForExitFight();
        }
        else if (viewButton == ui_BtnRestartFight)
        {
            OnClickForRestartFight();
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
    /// 根据进入类型刷新按钮显隐
    /// 战斗模式：显示结束战斗按钮(支持重开的模式额外显示重新挑战)；基地模式：显示设置/返回/退出，隐藏结束战斗/重新挑战
    /// </summary>
    public void RefreshButtonShow()
    {
        bool isFight = enterType == EnterTypeFight;
        if (ui_BtnExitFight != null)
            ui_BtnExitFight.gameObject.SetActive(isFight);
        //重新挑战按钮: 仅战斗模式且当前战斗支持重开时显示(无尽=只能挑战一次/终焉议会=暴力说服一次性, 均不显示)
        bool isCanRestart = false;
        if (isFight)
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            isCanRestart = gameFightLogic != null && gameFightLogic.IsCanRestartFight();
        }
        if (ui_BtnRestartFight != null)
            ui_BtnRestartFight.gameObject.SetActive(isCanRestart);
        if (ui_BtnSetting != null)
            ui_BtnSetting.gameObject.SetActive(!isFight);
        if (ui_BtnBack != null)
            ui_BtnBack.gameObject.SetActive(!isFight);
        if (ui_BtnExit != null)
            ui_BtnExit.gameObject.SetActive(!isFight);
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        //战斗模式：返回战斗界面
        if (enterType == EnterTypeFight)
        {
            GameControlHandler.Instance.SetFightControl();
            UIHandler.Instance.OpenUIAndCloseOther<UIFightMain>();
        }
        //基地模式：返回基地主界面
        else
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
        }
    }

    /// <summary>
    /// 点击结束战斗
    /// 弹出确认弹窗，确认后结束当前战斗返回基地(当前战斗进度不保留)
    /// </summary>
    public void OnClickForExitFight()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(503);
        dialogData.actionSubmit = (view, data) =>
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameFightLogic?.ExitFightAndReturnToBase();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击重新挑战
    /// 弹出确认弹窗，确认后放弃当前战斗进度，以同一世界/同一配置从头重新挑战(不返回基地)
    /// </summary>
    public void OnClickForRestartFight()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(504);
        dialogData.actionSubmit = (view, data) =>
        {
            var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            gameFightLogic?.RestartFight();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击返回主界面
    /// </summary>
    public void OnClickForBackMain()
    {
        GameDataHandler.Instance.manager.SaveUserData();
        WorldHandler.Instance.EnterMainForBaseScene();
    }

    /// <summary>
    /// 点击进入游戏设置
    /// </summary>
    public void OnClickForGameSetting()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameSetting>();
        targetUI.enterType = 1;
    }

    /// <summary>
    /// 离开游戏
    /// </summary>
    public void OnClickForExitGame()
    {
        GameDataHandler.Instance.manager.SaveUserData();
        GameUtil.ExitGame();
    }
}
