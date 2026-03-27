

using UnityEngine.UI;

public partial class UIViewFightAbyssalBlessingItem : BaseUIView
{
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="resolvedBuffInfo">有等级的BUFF时，传入已解析的下一级BuffInfo用于展示；无等级时传null</param>
    public void SetData(AbyssalBlessingInfoBean abyssalBlessingInfo, BuffInfoBean resolvedBuffInfo = null)
    {
        this.abyssalBlessingInfo = abyssalBlessingInfo;
        if (resolvedBuffInfo != null)
        {
            SetName(resolvedBuffInfo.name_language);
            SetDetails(resolvedBuffInfo.content_language);
        }
        else
        {
            SetName(abyssalBlessingInfo.name_language);
            SetDetails(abyssalBlessingInfo.details_language);
        }
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    public void SetName(string name)
    {
        ui_NameText.text = name;
    }

    /// <summary>
    /// 设置详情
    /// </summary>
    /// <param name="details"></param>
    public void SetDetails(string details)
    {
        ui_DetailsText.text = details;
    }

    #region 点击
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_Content)
        {
            OnClickForSelect();
        }
    }

    public void OnClickForSelect()
    {
        var targetUI = UIHandler.Instance.GetUI<UIFightAbyssalBlessing>();
        targetUI.OnClickForSelect(abyssalBlessingInfo);
    }
    #endregion
}