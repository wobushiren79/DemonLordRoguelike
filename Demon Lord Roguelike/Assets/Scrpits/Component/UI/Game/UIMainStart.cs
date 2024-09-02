using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIMainStart : BaseUIComponent
{
    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);

    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_GameTitle)
        {
            OnClickForMaker();
        }
        else if (viewButton == ui_UIMainStartBtn_StartGame)
        {
            OnClickForStartGame();
        }
        else if (viewButton == ui_UIMainStartBtn_GameSetting)
        {
            OnClickForGameSettting();
        }
        else if (viewButton == ui_UIMainStartBtn_ExitGame)
        {
            OnClickForExitGame();
        }
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

    }

    /// <summary>
    /// 点击开始游戏
    /// </summary>
    public void OnClickForStartGame()
    {

    }

    /// <summary>
    /// 点击离开游戏
    /// </summary>
    public void OnClickForExitGame()
    {
        GameUtil.ExitGame();
    }

    /// <summary>
    /// 点击游戏设置
    /// </summary>
    public void OnClickForGameSettting()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameSetting>();
        targetUI.enterType = 0;
    }

    /// <summary>
    /// 点击进入游戏制作人
    /// </summary>
    public void OnClickForMaker()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIMainMaker>();
    }
}
