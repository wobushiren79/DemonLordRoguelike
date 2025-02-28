

using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEditor.Experimental.GraphView;

public partial class UIViewFightSettlementItem : BaseUIView
{
    protected FightRecordsCreatureBean fightRecordsCreatureData;

    /// <summary>
    /// ��������
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
            LogUtil.LogError($"���ü�¼����ʧ�ܣ�UIViewFightSettlementItem {fightRecordsCreatureData.creatureId}");
        }
    }

    /// <summary>
    /// ������������
    /// </summary>
    public void SetCreatureName(string creatureName)
    {
        ui_Name.text = creatureName;
    }

    /// <summary>
    /// ��������ͼ��
    /// </summary>
    public void SetCreatureIcon(CreatureBean creatureData)
    {
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData);
    }

    /// <summary>
    /// ���û�ɱ��
    /// </summary>
    public void SetProgressForKill(int maxKill, int kill)
    {
        ui_UIViewFightSettlementItemProgress_Kill.SetData(maxKill, kill);
    }

    /// <summary>
    /// �����˺�
    /// </summary>
    public void SetProgressForDamage(int maxDamage, int damage)
    {
        ui_UIViewFightSettlementItemProgress_Damage.SetData(maxDamage, damage);
    }

    /// <summary>
    /// �����ܵ����˺�
    /// </summary>
    public void SetProgressForDamageReceived(int maxDamageReceived, int damageReceived)
    {
        ui_UIViewFightSettlementItemProgress_DamageReceived.SetData(maxDamageReceived, damageReceived);
    }

    /// <summary>
    /// ���þ���
    /// </summary>
    public void SetPrgoressForExp(int maxExp, int exp)
    {
        ui_UIViewFightSettlementItemProgress_Exp.SetData(maxExp, exp);
    }
}