
//献祭卡片选择
public partial class UIViewCreatureCardItemForCreatureSacrifice : UIViewCreatureCardItem
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
            case CardStateEnum.CreatureSacrificeNoSelect:
                break;
            case CardStateEnum.CreatureSacrificeSelect:
                ui_SelectBg.gameObject.SetActive(true);
                break;
        }
    }
    #endregion

}
