
//阵容卡片特殊设置
public partial class UIViewCreatureCardItemForSelectCreature : UIViewCreatureCardItem
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
            case CardStateEnum.SelectCreatureSelect:    
                ui_SelectBg.gameObject.SetActive(true);
                break;
            case CardStateEnum.SelectCreatureNoSelect:
                break;
        }
    }
    #endregion

}
