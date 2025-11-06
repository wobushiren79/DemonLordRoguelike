

using NUnit.Framework.Internal;

public partial class UIPopupDoomCouncilMainDetails : PopupShowCommonView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(object data)
    {
        DoomCouncilInfoBean doomCouncilInfo = (DoomCouncilInfoBean)data;
        SetName(doomCouncilInfo.name_language);
        SetContent(doomCouncilInfo.details_language);
        SetSuccessRate(doomCouncilInfo.success_rate);
    }

    /// <summary>
    /// 设置名称
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = $"{name}";
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    public void SetContent(string content)
    {
        ui_DetailsText.text = $"{content}";
    }
    
    /// <summary>
    /// 设置成功率
    /// </summary>
    public void SetSuccessRate(float rate)
    {
        //保留2位小数
        float percentage = MathUtil.GetPercentage(rate, 2);
        ui_SuccessRate.text = string.Format(TextHandler.Instance.GetTextById(53003), percentage);
    }
}