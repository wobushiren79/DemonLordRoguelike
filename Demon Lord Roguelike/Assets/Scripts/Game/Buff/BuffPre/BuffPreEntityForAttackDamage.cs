using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class BuffPreEntityForAttackDamage : BuffBasePreEntity
{
    /// <summary>
    /// 该前置在 UnderAttack 事件中关注"BUFF目标作为攻击者"
    /// </summary>
    public override BuffPreEventRole GetEventRole() => BuffPreEventRole.Attacker;

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        FightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creatureEntity == null)
        {
            return false;
        }
        //受到的伤害总量 是否满足
        if (buffEntityData.conditionalValue >= preValue)
        {
            return true;
        }
        return false;
    }
}
