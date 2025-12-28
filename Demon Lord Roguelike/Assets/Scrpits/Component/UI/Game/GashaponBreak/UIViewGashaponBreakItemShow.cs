

using UnityEngine.UI;

public partial class UIViewGashaponBreakItemShow : BaseUIView
{
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        ui_CreatureCardItem.SetData(creatureData, cardUseState);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton==ui_BtnRename)
        {
            OnClickForRename();
        }
    }

    /// <summary>
    /// 点击重命名
    /// </summary>
    public void OnClickForRename()
    {
        
    }
}