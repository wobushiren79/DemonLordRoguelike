
//阵容卡片特殊设置
public partial class UIViewCreatureCardItemForCreatureManager : UIViewCreatureCardItem
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
            case CardStateEnum.CreatureManagerNoSelect:
                break;
            case CardStateEnum.CreatureManagerSelect:
                ui_SelectBg.gameObject.SetActive(true);
                break;
        }
    }
    #endregion

}
