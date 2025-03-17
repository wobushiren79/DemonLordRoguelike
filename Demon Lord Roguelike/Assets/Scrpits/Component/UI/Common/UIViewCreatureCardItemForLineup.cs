
//阵容卡片特殊设置
public partial class UIViewCreatureCardItemForLineup : UIViewCreatureCardItem
{
   
    #region 重写
    /// <summary>
    /// 刷新状态
    /// </summary>
    public override void RefreshCardState(CardStateEnum cardState)
    {
        base.RefreshCardState(cardState);
        switch (cardState)
        {
            case CardStateEnum.LineupSelect:
                maskUI.ShowMask();
                break;
            case CardStateEnum.LineupNoSelect:
                break;
        }
    }
    #endregion

}
