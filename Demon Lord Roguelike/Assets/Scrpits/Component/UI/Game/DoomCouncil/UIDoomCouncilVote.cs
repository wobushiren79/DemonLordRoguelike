

public partial class UIDoomCouncilVote : BaseUIComponent
{
    protected DoomCouncilBean doomCouncilData;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(DoomCouncilBean doomCouncilData)
    {
        this.doomCouncilData = doomCouncilData;
    }
    
    /// <summary>
    /// 设置标题
    /// </summary>
    public void SetTitle(string title)
    {
        ui_TitleText.text = title;
    }
}