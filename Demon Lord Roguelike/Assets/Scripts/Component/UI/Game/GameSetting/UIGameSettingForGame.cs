using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForGame : UIGameSettingBase
{
    //多语言选择已迁移到主界面 UIMainStart 的语言列表（UIViewLanguageItem），此处暂时注释
    //protected UIViewGameSettingSelect selectForLanguage;

    /// <summary>
    /// 按键提示显示开关
    /// </summary>
    protected UIViewGameSettingCheckBox checkboxForPressKeyTip;

    //public List<string> listLanguageSelect = new List<string>()
    //{
    //     "中文",
    //     "English",
    //     "日本語",
    //     "한국어",
    //     "繁體中文",
    //     "Deutsch",
    //     "Français",
    //     "Русский"
    //};

    public UIGameSettingForGame(GameObject objListContainer) : base(objListContainer)
    {

    }


    public override void Open()
    {
        base.Open();
        //selectForLanguage = CreatureItemForSelect("", listLanguageSelect);
        //selectForLanguage.SetSelcet((int)gameConfig.GetLanguage());

        //按键提示显示开关（默认开启，关闭后所有 UIViewPressCommon 不显示）
        checkboxForPressKeyTip = CreatureItemForCheckBox("");
        checkboxForPressKeyTip.SetSelect(gameConfig.pressKeyTipShow);

        RefreshUIText();
    }

    public void RefreshUIText()
    {
        //string textLanguageTitle = TextHandler.Instance.GetTextById(41001);
        //selectForLanguage.SetTitle(textLanguageTitle);

        string textPressKeyTipTitle = TextHandler.Instance.GetTextById(41002);
        checkboxForPressKeyTip.SetTitle(textPressKeyTipTitle);
    }

    //public override void ActionForSelectValueChange(UIViewGameSettingSelect targetView, int index)
    //{
    //    base.ActionForSelectValueChange(targetView, index);
    //    if (targetView == selectForLanguage)
    //    {
    //        LanguageEnum language = (LanguageEnum)index;
    //        gameConfig.SetLanguage(language);
    //        TextHandler.Instance.ChangeLanguageEnum(language);
    //        UIHandler.Instance.RefreshAllUI();

    //        //刷新一下当前UI的文本
    //        var openUI = UIHandler.Instance.GetOpenUI();
    //        openUI.gameObject.SetActive(false);
    //        openUI.gameObject.SetActive(true);

    //        RefreshUIText();
    //    }
    //}

    public override void ActionForCheckBoxValueChange(UIViewGameSettingCheckBox targetView, bool isCheck)
    {
        base.ActionForCheckBoxValueChange(targetView, isCheck);
        if (targetView == checkboxForPressKeyTip)
        {
            //保存设置并广播，让已显示的 UIViewPressCommon 实时刷新显隐
            gameConfig.pressKeyTipShow = isCheck;
            EventHandler.Instance.TriggerEvent(EventsInfo.GameSetting_PressKeyTipShowChange);
        }
    }
}
