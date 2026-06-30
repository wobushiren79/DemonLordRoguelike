/// <summary>
/// 传送门详情气泡中的单个信息项(标题+内容), 用于展示名字/线路数/关卡数等
/// </summary>
public partial class UIViewPopupPortalDetailsItem : BaseUIView
{
    #region 数据设置
    /// <summary>
    /// 设置详情项数据
    /// </summary>
    /// <param name="title">标题文本</param>
    /// <param name="content">内容文本</param>
    /// <param name="isShow">是否显示该项(无尽模式下关卡数项不显示)</param>
    public void SetData(string title, string content, bool isShow = true)
    {
        gameObject.SetActive(isShow);
        if (!isShow)
            return;
        SetTitle(title);
        SetContent(content);
    }

    /// <summary>
    /// 设置标题文本
    /// </summary>
    /// <param name="title">标题文本</param>
    public void SetTitle(string title)
    {
        if (ui_Title != null)
            ui_Title.text = title;
    }

    /// <summary>
    /// 设置内容文本
    /// </summary>
    /// <param name="content">内容文本</param>
    public void SetContent(string content)
    {
        if (ui_Content != null)
            ui_Content.text = content;
    }
    #endregion
}
