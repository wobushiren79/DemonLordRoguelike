using UnityEngine;
using UnityEngine.UI;

public partial class UIViewLanguageItem : BaseUIView
{
    /// <summary>
    /// 当前 item 对应的语言
    /// </summary>
    protected LanguageEnum language;

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ItemLanguage)
        {
            OnClickForSelectLanguage();
        }
    }

    #region 数据设置
    /// <summary>
    /// 设置语言数据，文本格式为 英文简称/该语言自称（如 cn/中文）
    /// </summary>
    /// <param name="language"></param>
    public void SetData(LanguageEnum language)
    {
        this.language = language;
        ui_ItemText.text = LanguageCfg.GetLanguageShowName(language);
    }
    #endregion

    #region 点击事件
    /// <summary>
    /// 点击切换多语言
    /// </summary>
    public void OnClickForSelectLanguage()
    {
        var gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //相同语言不重复切换
        if (gameConfig.GetLanguage() == language)
            return;
        gameConfig.SetLanguage(language);
        TextHandler.Instance.ChangeLanguageEnum(language);
        GameDataHandler.Instance.manager.SaveGameConfig();
        UIHandler.Instance.RefreshAllUI();

        //刷新一下当前UI的文本
        var openUI = UIHandler.Instance.GetOpenUI();
        openUI.gameObject.SetActive(false);
        openUI.gameObject.SetActive(true);
    }
    #endregion
}
