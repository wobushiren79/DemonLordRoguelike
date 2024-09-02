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
    /// ˢ��UI����
    /// </summary>
    public void RefreshUIData()
    {

    }

    /// <summary>
    /// �����ʼ��Ϸ
    /// </summary>
    public void OnClickForStartGame()
    {

    }

    /// <summary>
    /// ����뿪��Ϸ
    /// </summary>
    public void OnClickForExitGame()
    {
        GameUtil.ExitGame();
    }

    /// <summary>
    /// �����Ϸ����
    /// </summary>
    public void OnClickForGameSettting()
    {
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGameSetting>();
        targetUI.enterType = 0;
    }

    /// <summary>
    /// ���������Ϸ������
    /// </summary>
    public void OnClickForMaker()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIMainMaker>();
    }
}
