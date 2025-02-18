using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIGameSetting : BaseUIComponent, IRadioGroupCallBack
{
    protected UIGameSettingForGame gameSettingForGame;
    protected UIGameSettingForDisplay gameSettingForDisplay;
    protected UIGameSettingForAudio gameSettingForAudio;

    public int currentSettingType = 1;
    public int enterType = 0;
    public override void Awake()
    {
        base.Awake();
        ui_TitleRadioGroup.SetCallBack(this);
        ui_TitleRadioGroup.SetPosition(0, false);

        gameSettingForGame = new UIGameSettingForGame(ui_List.gameObject);
        gameSettingForDisplay = new UIGameSettingForDisplay(ui_List.gameObject);
        gameSettingForAudio = new UIGameSettingForAudio(ui_List.gameObject);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_TitleRadioGroup.SetPosition(0, true);
    }

    public override void CloseUI()
    {
        base.CloseUI();
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
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        //���������
        if (enterType == 0)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
        }
        //���ؽ���
        else if (enterType == 1)
        {
            UIHandler.Instance.OpenUIAndCloseOther<UIGameSystem>();
        }
    }

    /// <summary>
    /// ������������
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
        }
    }


    #region �¼��ص�
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
    }

    public void RadioButtonUnSelected(RadioGroupView rgView, int position, RadioButtonView rbview)
    {

    }
    #endregion
}
