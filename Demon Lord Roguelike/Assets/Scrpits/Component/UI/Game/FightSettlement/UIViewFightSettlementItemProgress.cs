

using DG.Tweening;

public partial class UIViewFightSettlementItemProgress : BaseUIView
{

    public override void OnDisable()
    {
        base.OnDisable();
        if (ui_ProgressValue != null)
        {
            ui_ProgressValue.DOKill();
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(string title, int maxData, int data, bool isAnim = true)
    {
        float progress = 0;
        if (maxData != 0)
            progress = (float)data / maxData;

        SetTitle(title);
        SetProgress(data, progress, isAnim);
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_ProgressName.text = $"{title}";
    }

    /// <summary>
    /// 设置进度
    /// </summary>
    public void SetProgress(int data, float progress, bool isAnim)
    {
        ui_ProgressValue.DOKill();
        int percentage = (int)MathUtil.GetPercentage(progress, 2);
        ui_ProgressContent.text = $"{data}({percentage}%)";
        ui_ProgressValue.fillAmount = progress;
        if (isAnim)
        {
            ui_ProgressValue.fillAmount = 0;
            ui_ProgressValue.DOFillAmount(progress, 1f);
        }
    }
}