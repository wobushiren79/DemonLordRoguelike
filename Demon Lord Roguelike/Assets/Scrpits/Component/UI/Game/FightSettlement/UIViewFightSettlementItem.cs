

public partial class UIViewFightSettlementItem : BaseUIView
{
    protected FightRecordsCreatureBean fightRecordsCreatureData;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(FightRecordsBean fightRecordsData, FightRecordsCreatureBean fightRecordsCreatureData)
    {
        this.fightRecordsCreatureData = fightRecordsCreatureData;
    }

    /// <summary>
    /// 设置生物名字
    /// </summary>
    /// <param name="creatureName"></param>
    public void SetCreatureName(string creatureName)
    {

    }

    /// <summary>
    /// 设置生物图标
    /// </summary>
    public void SetCreatureIcon(CreatureBean creatureData)
    {
        //GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }
}