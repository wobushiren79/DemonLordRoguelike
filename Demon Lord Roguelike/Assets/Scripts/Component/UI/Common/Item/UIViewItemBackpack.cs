public partial class UIViewItemBackpack : UIViewItem
{
    #region 数据
    public CreatureBean creatureData;
    #endregion

    #region 生命周期
    public override void Awake()
    {
        base.Awake();
        //右键经按钮同物体的 PopupButtonCommonView 转发（Button 只响应左键，点击事件在 Button 所在物体即被吞掉，冒泡不到本根节点）
        ui_UIViewItem.AddListenerForRightClick(EventForRightClick);
    }
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
        base.SetData(itemData);
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

    /// <summary>
    /// 右键点击：触发背包道具右键选中事件（隐藏悬浮详情，供打开选项菜单）
    /// </summary>
    protected void EventForRightClick(PopupButtonCommonView view)
    {
        if (itemData == null)
            return;
        ui_UIViewItem.ClearData();
        this.TriggerEvent(EventsInfo.UIViewItemBackpack_OnRightClickSelect, this);
    }
    #endregion
}
