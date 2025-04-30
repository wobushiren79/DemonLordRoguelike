
public partial class UIViewFightSettlementItem : BaseUIView
{
    protected FightRecordsCreatureBean fightRecordsCreatureData;

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(FightRecordsBean fightRecordsData, FightRecordsCreatureBean fightRecordsCreatureData)
    {
        this.fightRecordsCreatureData = fightRecordsCreatureData;
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();

        var creatureData = gameFightLogic.fightData.GetCreatureDataById(fightRecordsCreatureData.creatureId);
        if (creatureData != null)
        {
            SetCreatureName(creatureData.creatureName);
            SetCreatureIcon(creatureData);
            SetProgressForKill(fightRecordsData.totalKillNumForDef, fightRecordsCreatureData.killNum);
            SetProgressForDamage(fightRecordsData.totalDamageForDef, fightRecordsCreatureData.damage);
            SetProgressForDamageReceived(fightRecordsData.totalDamageReceivedForDef, fightRecordsCreatureData.damageReceived);
            SetPrgoressForExp(fightRecordsData.totalAddExp, fightRecordsCreatureData.exp);
        }
        else
        {
            LogUtil.LogError($"设置记录数据失败，UIViewFightSettlementItem {fightRecordsCreatureData.creatureId}");
        }
    }

    /// <summary>
    /// 设置生物名字
    /// </summary>
    public void SetCreatureName(string creatureName)
    {
        ui_Name.text = creatureName;
    }

    /// <summary>
    /// 设置生物图标
    /// </summary>
    public void SetCreatureIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }

    /// <summary>
    /// 设置击杀数
    /// </summary>
    public void SetProgressForKill(long maxKill, long kill)
    {
        string title = TextHandler.Instance.GetTextById(50002);
        ui_UIViewFightSettlementItemProgress_Kill.SetData(title, maxKill, kill);
    }

    /// <summary>
    /// 设置伤害
    /// </summary>
    public void SetProgressForDamage(long maxDamage, long damage)
    {
        string title = TextHandler.Instance.GetTextById(50001);
        ui_UIViewFightSettlementItemProgress_Damage.SetData(title, maxDamage, damage);
    }

    /// <summary>
    /// 设置受到的伤害
    /// </summary>
    public void SetProgressForDamageReceived(long maxDamageReceived, long damageReceived)
    {
        string title = TextHandler.Instance.GetTextById(50004);
        ui_UIViewFightSettlementItemProgress_DamageReceived.SetData(title, maxDamageReceived, damageReceived);
    }

    /// <summary>
    /// 设置经验
    /// </summary>
    public void SetPrgoressForExp(long maxExp, long exp)
    {
        string title = TextHandler.Instance.GetTextById(50003);
        ui_UIViewFightSettlementItemProgress_Exp.SetData(title, maxExp, exp);
    }
}