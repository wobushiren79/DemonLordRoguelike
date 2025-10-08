

using UnityEngine.UI;

public partial class UIViewFightAbyssalBlessingItem : BaseUIView
{
    public AbyssalBlessingInfoBean abyssalBlessingInfo;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        this.abyssalBlessingInfo = abyssalBlessingInfo;
        SetName(abyssalBlessingInfo.name_language);
        SetDetails(abyssalBlessingInfo.details_language);
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