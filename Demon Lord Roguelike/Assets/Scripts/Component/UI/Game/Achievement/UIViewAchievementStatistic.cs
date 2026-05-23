/// <summary>
/// 成就统计列表单元(一行一条)
/// </summary>
public partial class UIViewAchievementStatistic : BaseUIView
{
    public AchievementStatisticItemBean data;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(AchievementStatisticItemBean data)
    {
        this.data = data;
        if (data == null) return;
        if (ui_TxtLabel != null) ui_TxtLabel.text = data.label;
        if (ui_TxtValue != null) ui_TxtValue.text = data.value;
    }
}
