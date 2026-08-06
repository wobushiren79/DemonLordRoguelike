using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIMainStart : BaseUIComponent
{
    public override void OpenUI()
    {
        base.OpenUI();
        //设置基地场景视角
        CameraHandler.Instance.SetGameStartCamera(int.MaxValue, true);
        InitLanguageList();
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);

    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_GameTitle_Button)
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
    /// 初始化多语言选择列表，按 LanguageEnum 的数量实时生成 ItemLanguage
    /// </summary>
    public void InitLanguageList()
    {
        //先隐藏模板（RemoveChildsByActive 只清理 active 的实例，保留模板）
        ui_ItemLanguage.gameObject.SetActive(false);
        CptUtil.RemoveChildsByActive(ui_ListLanguage.gameObject);
        var languageArray = Enum.GetValues(typeof(LanguageEnum));
        foreach (LanguageEnum itemLanguage in languageArray)
        {
            GameObject objItem = Instantiate(ui_ListLanguage.gameObject, ui_ItemLanguage.gameObject);
            var itemView = objItem.GetComponent<UIViewLanguageItem>();
            itemView.SetData(itemLanguage);
        }
    }

    /// <summary>
    /// 点击开始游戏
    /// </summary>
    public void OnClickForStartGame()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIMainLoad>();
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
