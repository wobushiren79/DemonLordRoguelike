

public partial class UIPopupAbyssalBlessingInfo : PopupShowCommonView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(object data)
    {
        AbyssalBlessingEntityBean abyssalBlessingEntityBean = (AbyssalBlessingEntityBean)data;
        SetIcon(abyssalBlessingEntityBean.abyssalBlessingInfo.icon_res);
        SetName(abyssalBlessingEntityBean.abyssalBlessingInfo.name_language);
        SetDetails(abyssalBlessingEntityBean.abyssalBlessingInfo.details_language);
    }

    /// <summary>
    /// 设置图像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetUIIcon(iconName, ui_Icon);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = $"{name}";
    }
    
    /// <summary>
    /// 设置详情
    /// </summary>
    public void SetDetails(string details)
    {
        ui_DetailsText.text = $"{details}";
    }
}