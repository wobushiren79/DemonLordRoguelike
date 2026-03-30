

public partial class UIPopupCreatureCardDetails : PopupShowCommonView
{
    public override void SetData(object data)
    {
        var targetData =  (CreatureBean)data;
        ui_UIViewCreatureCardDetails.SetData(targetData);
    }

    public override void InitPosition()
    {
        base.InitPosition();
        //如果鼠标在左半边区域
        if (mouseAreaLeftRight == Direction2DEnum.Left)
        {
            ui_UIViewCreatureCardDetails.SetDetailsDirection(Direction2DEnum.Right);
        }
        //如果鼠标在右半边区域
        else
        {
            ui_UIViewCreatureCardDetails.SetDetailsDirection(Direction2DEnum.Left);
        }
    }
}