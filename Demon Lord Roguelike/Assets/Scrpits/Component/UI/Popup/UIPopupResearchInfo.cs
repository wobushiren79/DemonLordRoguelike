

public partial class UIPopupResearchInfo : PopupShowCommonView
{
    protected ResearchInfoBean researchInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(object data)
    {
        researchInfo = (ResearchInfoBean)data;
        //获取当前研究等级
        int currentLevel = researchInfo.GetResearchLevel();
        long payCrystal = researchInfo.GetPayCrystal(currentLevel + 1);

        SetName(researchInfo.name_language);
        SetIcon(researchInfo.icon_res);
        SetPayCrystal(payCrystal);
        SetLevel(researchInfo.level_max, currentLevel);
        SetResearchState();
        RefreshUILayout();
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = $"{name}";
    }

    /// <summary>
    /// 设置等级
    /// </summary>
    public void SetLevel(int maxLevel, int level)
    {
        if (maxLevel == 1)
        {
            ui_Level.gameObject.SetActive(false);
        }
        else
        {
            ui_Level.gameObject.SetActive(true);
            if (level == maxLevel)
            {
                ui_LevelText.text = TextHandler.Instance.GetTextById(1003002);
            }
            else
            {
                ui_LevelText.text = string.Format(TextHandler.Instance.GetTextById(1003001), level);
            }
        }
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    public void SetIcon(string iconRes)
    {
        IconHandler.Instance.SetUIIcon(iconRes, ui_Icon);
    }

    /// <summary>
    /// 设置支付的魔晶数量
    /// </summary>
    /// <param name="payCrystal"></param>
    public void SetPayCrystal(long payCrystal)
    {
        ui_PayCrystalText.text = $"x{payCrystal}";
    }

    /// <summary>
    /// 设置研究状态
    /// </summary>
    public void SetResearchState()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        int currentLevel = researchInfo.GetResearchLevel();
        //如果已经达到最大研究等级 也不用显示解锁条件
        if (currentLevel == researchInfo.level_max)
        {
            ui_UnlockPre.gameObject.SetActive(false);
        }
        else
        {
            ui_UnlockPre.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 刷新UI布局
    /// </summary>
    public void RefreshUILayout()
    {
        UGUIUtil.RefreshUISize(ui_UnlockPre);
        UGUIUtil.RefreshUISize(transform);
    }
}