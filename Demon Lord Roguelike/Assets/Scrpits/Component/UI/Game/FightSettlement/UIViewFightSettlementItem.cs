

public partial class UIViewFightSettlementItem : BaseUIView
{
    protected FightRecordsCreatureBean fightRecordsCreatureData;

    /// <summary>
    /// ��������
    /// </summary>
    public void SetData(FightRecordsBean fightRecordsData, FightRecordsCreatureBean fightRecordsCreatureData)
    {
        this.fightRecordsCreatureData = fightRecordsCreatureData;
    }

    /// <summary>
    /// ������������
    /// </summary>
    /// <param name="creatureName"></param>
    public void SetCreatureName(string creatureName)
    {

    }

    /// <summary>
    /// ��������ͼ��
    /// </summary>
    public void SetCreatureIcon(CreatureBean creatureData)
    {
        //GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }
}