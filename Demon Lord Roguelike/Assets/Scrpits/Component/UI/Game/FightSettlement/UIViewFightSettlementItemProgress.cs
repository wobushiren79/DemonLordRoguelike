

public partial class UIViewFightSettlementItemProgress : BaseUIView
{
    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(string title, int maxData, int data)
    {
        float progress = (float)data / maxData;
        SetTitle(title);
        SetProgress(data, progress);
    }

    /// <summary>
    /// ���ñ���
    /// </summary>
    public void SetTitle(string title)
    {
        ui_ProgressName.text = $"{title}";
    }

    /// <summary>
    /// ���ý���
    /// </summary>
    public void SetProgress(int data, float progress)
    {
        int percentage = MathUtil.GetPercentage(data, 2);
        ui_ProgressContent.text = $"{data}({percentage}%)";
        ui_ProgressValue.fillAmount = progress;
    }
}