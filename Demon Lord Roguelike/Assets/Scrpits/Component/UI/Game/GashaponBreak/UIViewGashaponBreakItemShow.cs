

using UnityEngine.UI;

public partial class UIViewGashaponBreakItemShow : BaseUIView
{
    protected CreatureBean creatureData;
    protected CardUseStateEnum cardUseState;
    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        this.cardUseState = cardUseState;
        this.creatureData = creatureData;
        ui_CreatureCardItem.SetData(creatureData, cardUseState);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BtnRename)
        {
            OnClickForRename();
        }
    }

    /// <summary>
    /// 点击重命名
    /// </summary>
    public void OnClickForRename()
    {
        DialogRenameBean dialogData = new DialogRenameBean();
        dialogData.inputContent = creatureData.creatureName;
        dialogData.actionSubmit = (view, data) =>
        {
            creatureData.creatureName = dialogData.inputContent;
            ui_CreatureCardItem.SetData(creatureData, cardUseState);
            this.TriggerEvent(EventsInfo.Creature_Rename, creatureData);

            //保存数据
            GameDataHandler.Instance.manager.SaveUserData();
        };
        UIHandler.Instance.ShowDialogRename(dialogData);
    }
}