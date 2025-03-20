

public partial class UIPopupCreatureCardDetails : PopupShowCommonView
{
    public override void SetData(object data)
    {
        var targetData =  (CreatureBean)data;
        ui_UIViewCreatureCardDetails.SetData(targetData);
    }
}