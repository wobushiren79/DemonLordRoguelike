

public partial class UIViewFightSettlementItemProgress : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(string title, int maxData, int data)
    {
        float progress = (float)data / maxData;
        SetTitle(title);
        SetProgress(data, progress);
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
    public void SetProgress(int data, float progress)
    {
        int percentage = MathUtil.GetPercentage(data, 2);
        ui_ProgressContent.text = $"{data}({percentage}%)";
        ui_ProgressValue.fillAmount = progress;
    }
}