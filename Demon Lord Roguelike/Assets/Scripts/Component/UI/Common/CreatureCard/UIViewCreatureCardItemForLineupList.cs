

//阵容管理列表卡片特殊设置(列表卡片不带拖拽,拖拽换位由阵容行卡片 UIViewCreatureCardItemForLineup 负责;
//若列表cell误用带拖拽接口的卡片,拖拽事件会被卡片截获导致列表无法滚动)
public partial class UIViewCreatureCardItemForLineupList : UIViewCreatureCardItem
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
            //阵容选中:显示遮罩表示该生物已在当前阵容中
            case CardStateEnum.LineupSelect:
                ui_Mask.gameObject.SetActive(true);
                break;
            case CardStateEnum.LineupNoSelect:
                break;
        }
    }
    #endregion

}
