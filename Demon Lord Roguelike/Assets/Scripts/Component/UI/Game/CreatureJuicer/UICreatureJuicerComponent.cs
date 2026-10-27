using UnityEngine;
using UnityEngine.UI;
using TMPro;


public partial class UICreatureJuicer
{
    public Button ui_BtnStart;

    public UITextLanguageView ui_BtnStartText;

    public UIViewBaseInfoContent ui_UIViewBaseInfoContent;

    public UIViewCreatureCardList ui_UIViewCreatureCardList_Target;

    public TextMeshProUGUI ui_LimmitText;

    public Button ui_ViewExit;

    public Image ui_JuicerWater;

    public TextMeshProUGUI ui_JuicerText;

    /// <summary>魔晶加减整行容器(研究未解锁时隐藏)</summary>
    public Transform ui_CrystalAddRoot;

    /// <summary>添加魔晶按钮(与 LongPressButton 同物体)</summary>
    public Button ui_BtnAddCrystal_Button;

    /// <summary>添加魔晶长按组件</summary>
    public LongPressButton ui_BtnAddCrystal_LongPressButton;

    /// <summary>当前已添加魔晶颗数文本</summary>
    public TextMeshProUGUI ui_CrystalAddNumText;

    /// <summary>减少魔晶按钮(与 LongPressButton 同物体)</summary>
    public Button ui_BtnReduceCrystal_Button;

    /// <summary>减少魔晶长按组件</summary>
    public LongPressButton ui_BtnReduceCrystal_LongPressButton;
}