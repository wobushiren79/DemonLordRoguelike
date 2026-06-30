public partial class UIViewItemBackpack : UIViewItem
{
    #region 数据
    public CreatureBean creatureData;
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="itemData">道具数据</param>
    /// <param name="creatureData">生物数据（用于判断道具是否可装备）</param>
    public void SetData(ItemBean itemData, CreatureBean creatureData = null)
    {
        this.creatureData = creatureData;
        SetData(itemData);
    }
    #endregion

    #region 点击
    /// <summary>
    /// 点击选择（触发背包道具选中事件）
    /// </summary>
    public override void OnClickForSelect()
    {
        this.TriggerEvent(EventsInfo.UIViewItemBackpack_OnClickSelect, this);
    }
    #endregion
}
