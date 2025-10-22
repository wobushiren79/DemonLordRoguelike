
//献祭卡片选择
public partial class UIViewCreatureCardItemForCreatureAscend : UIViewCreatureCardItem
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
            case CardStateEnum.CreatureAscendNoSelect:
                break;
            case CardStateEnum.CreatureAscendSelect:
                ui_SelectBg.gameObject.SetActive(true);
                break;
        }
    }
    #endregion

}
