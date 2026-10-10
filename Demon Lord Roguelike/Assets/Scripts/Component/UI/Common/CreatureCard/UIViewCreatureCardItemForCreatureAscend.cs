
//进阶卡片选择
public partial class UIViewCreatureCardItemForCreatureAscend : UIViewCreatureCardItem
{
    #region 重写
    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        base.SetData(creatureData, cardUseState);
        //阵容标记(通用控件):逐行展示生物所在的全部阵容名字,不在任何阵容时隐藏
        ui_UIViewCreatureCardItemLineUpMark.SetData(creatureData);
    }

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
