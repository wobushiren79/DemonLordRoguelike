using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForGame : UIGameSettingBase
{
    protected UIViewGameSettingSelect selectForLanguage;

    public List<string> listLanguageSelect = new List<string>()
    {
         "中文",
         "English"
    };

    public UIGameSettingForGame(GameObject objListContainer) : base(objListContainer)
    {

    }


    public override void Open()
    {
        base.Open();
        selectForLanguage = CreatureItemForSelect("", listLanguageSelect);
        selectForLanguage.SetSelcet((int)gameConfig.GetLanguage());
        RefreshUIText();
    }

    public void RefreshUIText()
    {
        string textLanguageTitle = TextHandler.Instance.GetTextById(41001);
        selectForLanguage.SetTitle(textLanguageTitle);
    }

    public override void ActionForSelectValueChange(UIViewGameSettingSelect targetView, int index)
    {
        base.ActionForSelectValueChange(targetView, index);
        if (targetView == selectForLanguage)
        {
            LanguageEnum language = (LanguageEnum)index;
            gameConfig.SetLanguage(language);
            TextHandler.Instance.ChangeLanguageEnum(language);
            UIHandler.Instance.RefreshAllUI();

            //刷新一下当前UI的文本
            var openUI = UIHandler.Instance.GetOpenUI();
            openUI.gameObject.SetActive(false);
            openUI.gameObject.SetActive(true);

            RefreshUIText();
        }
    }
}
