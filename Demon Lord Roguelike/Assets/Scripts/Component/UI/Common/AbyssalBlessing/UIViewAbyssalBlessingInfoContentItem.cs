

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
        SetLevelColor(abyssalBlessingEntityBean.abyssalBlessingInfo.level);
    }

    /// <summary>
    /// 按等级为背景着色（Lv1-5 共 5 种颜色），与战斗馈赠卡片角标配色逻辑一致。
    /// </summary>
    public void SetLevelColor(int level)
    {
        if (ui_BG != null)
            ui_BG.color = AbyssalBlessingInfoCfg.GetLevelColor(level);
    }

    /// <summary>
    /// 设置图像
    /// </summary>
    public void SetIcon(string iconName)
    {
        IconHandler.Instance.SetAbyssalBlessingIcon(iconName, ui_Icon_Image);
    }
    
}