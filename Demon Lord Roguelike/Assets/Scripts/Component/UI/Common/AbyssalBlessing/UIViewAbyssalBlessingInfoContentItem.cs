

public partial class UIViewAbyssalBlessingInfoContentItem : BaseUIView
{
    protected AbyssalBlessingEntityBean abyssalBlessingEntityBean;

    public override void Awake()
    {
        base.Awake();

    }

    public void SetData(AbyssalBlessingEntityBean abyssalBlessingEntityBean)
    {
        this.abyssalBlessingEntityBean = abyssalBlessingEntityBean;

        ui_Icon_PopupButtonCommonView.SetData(abyssalBlessingEntityBean, PopupEnum.AbyssalBlessingInfo);

        SetIcon(abyssalBlessingEntityBean.abyssalBlessingInfo.icon_res);
    }

    /// <summary>
    /// 设置图像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetUIIcon(iconName, ui_Icon_Image);
    }
    
}