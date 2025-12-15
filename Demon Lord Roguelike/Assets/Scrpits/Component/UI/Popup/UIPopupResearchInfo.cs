

public partial class UIPopupResearchInfo : PopupShowCommonView
{
    protected ResearchInfoBean researchInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(object data)
    {
        researchInfo = (ResearchInfoBean)data;
        SetName(researchInfo.name_language);
        SetIcon(researchInfo.icon_res);
        SetPayCrystal(researchInfo.pay_crystal);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = $"{name}";
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
    public void SetPayCrystal(int payCrystal)
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
        if (userUnlock.CheckIsUnlock(researchInfo.unlock_id))
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