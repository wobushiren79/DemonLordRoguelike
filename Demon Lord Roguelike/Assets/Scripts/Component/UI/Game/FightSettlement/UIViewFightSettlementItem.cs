using UnityEngine;

public partial class UIViewFightSettlementItem : BaseUIView
{
    protected FightRecordsCreatureBean fightRecordsCreatureData;

    #region 进度条颜色

    /// <summary>
    /// 击杀进度条颜色（深红）
    /// </summary>
    protected static readonly Color KillProgressColor = ColorUtil.ParseHtmlString("#C0392B");

    /// <summary>
    /// 伤害进度条颜色（黄）
    /// </summary>
    protected static readonly Color DamageProgressColor = ColorUtil.ParseHtmlString("#F1C40F");

    /// <summary>
    /// 受到的伤害进度条颜色（蓝）
    /// </summary>
    protected static readonly Color DamageReceivedProgressColor = ColorUtil.ParseHtmlString("#3498DB");

    /// <summary>
    /// 经验进度条颜色（青绿）
    /// </summary>
    protected static readonly Color ExpProgressColor = ColorUtil.ParseHtmlString("#1ABC9C");

    /// <summary>
    /// 输出治疗量进度条颜色（亮绿）
    /// </summary>
    protected static readonly Color HealingDoneProgressColor = ColorUtil.ParseHtmlString("#2ECC71");

    /// <summary>
    /// 接收治疗量进度条颜色（浅绿）
    /// </summary>
    protected static readonly Color HealingReceivedProgressColor = ColorUtil.ParseHtmlString("#58D68D");

    #endregion

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
            SetCardData(creatureData);
            SetProgressForKill(fightRecordsData.totalKillNumForDef, fightRecordsCreatureData.killNum);
            SetProgressForDamage(fightRecordsData.totalDamageForDef, fightRecordsCreatureData.damage);
            SetProgressForDamageReceived(fightRecordsData.totalDamageReceivedForDef, fightRecordsCreatureData.damageReceived);
            SetProgressForHealingDone(fightRecordsData.totalRegainHPForDef, fightRecordsCreatureData.regainHP);
            SetProgressForHealingReceived(fightRecordsData.totalRegainHPReceivedForDef, fightRecordsCreatureData.regainHPReceived);
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
        ui_Name_TextMeshProUGUI.text = creatureName;
    }

    /// <summary>
    /// 设置卡片数据(与其他场景卡片一致,由卡片自管 图标/稀有度/等级/MP/职业图标/悬浮详情)
    /// </summary>
    public void SetCardData(CreatureBean creatureData)
    {
        ui_UIViewCreatureCardItem.SetData(creatureData, CardUseStateEnum.Show);
    }

    /// <summary>
    /// 设置击杀数
    /// </summary>
    public void SetProgressForKill(long maxKill, long kill)
    {
        string title = TextHandler.Instance.GetTextById(50002);
        ui_UIViewFightSettlementItemProgress_Kill.SetProgressColor(KillProgressColor);
        ui_UIViewFightSettlementItemProgress_Kill.SetData(title, maxKill, kill);
    }

    /// <summary>
    /// 设置伤害
    /// </summary>
    public void SetProgressForDamage(long maxDamage, long damage)
    {
        string title = TextHandler.Instance.GetTextById(50001);
        ui_UIViewFightSettlementItemProgress_Damage.SetProgressColor(DamageProgressColor);
        ui_UIViewFightSettlementItemProgress_Damage.SetData(title, maxDamage, damage);
    }

    /// <summary>
    /// 设置受到的伤害
    /// </summary>
    public void SetProgressForDamageReceived(long maxDamageReceived, long damageReceived)
    {
        string title = TextHandler.Instance.GetTextById(50004);
        ui_UIViewFightSettlementItemProgress_DamageReceived.SetProgressColor(DamageReceivedProgressColor);
        ui_UIViewFightSettlementItemProgress_DamageReceived.SetData(title, maxDamageReceived, damageReceived);
    }

    /// <summary>
    /// 设置输出治疗量（治疗别人）
    /// </summary>
    public void SetProgressForHealingDone(long maxHealingDone, long healingDone)
    {
        string title = TextHandler.Instance.GetTextById(50007);
        ui_UIViewFightSettlementItemProgress_HealingDone.SetProgressColor(HealingDoneProgressColor);
        ui_UIViewFightSettlementItemProgress_HealingDone.SetData(title, maxHealingDone, healingDone);
    }

    /// <summary>
    /// 设置接收治疗量（被别人治疗）
    /// </summary>
    public void SetProgressForHealingReceived(long maxHealingReceived, long healingReceived)
    {
        string title = TextHandler.Instance.GetTextById(50008);
        ui_UIViewFightSettlementItemProgress_HealingReceived.SetProgressColor(HealingReceivedProgressColor);
        ui_UIViewFightSettlementItemProgress_HealingReceived.SetData(title, maxHealingReceived, healingReceived);
    }

    /// <summary>
    /// 设置经验
    /// </summary>
    public void SetPrgoressForExp(long maxExp, long exp)
    {
        string title = TextHandler.Instance.GetTextById(50003);
        ui_UIViewFightSettlementItemProgress_Exp.SetProgressColor(ExpProgressColor);
        ui_UIViewFightSettlementItemProgress_Exp.SetData(title, maxExp, exp);
    }
}