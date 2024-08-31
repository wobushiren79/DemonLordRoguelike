using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingForGame : UIGameSettingBase
{
    protected UIViewGameSettingSelect selectForLanguage;

    public List<string> listLanguageSelect = new List<string>()
    {
         "ÖÐÎÄ",
         "English"
    };

    public UIGameSettingForGame(GameObject objListContainer) : base(objListContainer)
    {

    }


    public override void Open()
    {
        base.Open();
        string textLanguageTitle = TextHandler.Instance.GetTextById(41001);
        selectForLanguage = CreatureItemForSelect(textLanguageTitle, listLanguageSelect);
        selectForLanguage.SetSelcet((int)gameConfig.GetLanguage());
    }

    public override void ActionForSelectValueChange(UIViewGameSettingSelect targetView, int index)
    {
        base.ActionForSelectValueChange(targetView, index);
        if (targetView == selectForLanguage)
        {
            gameConfig.SetLanguage((LanguageEnum)index);
        }
    }
}
