using System.Globalization;

public class BuffEntityConditionalDeadAttack : BuffEntityConditionalDead
{
    #region 死亡引爆
    /// <summary>
    /// 死亡时用持有者的攻击快照引爆，词条随机倍率参与爆炸伤害。
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var targetFightCreatureEntity = GetFightCreatureEntityForTarget();
        if (targetFightCreatureEntity == null)
            return false;
        if (!long.TryParse(buffEntityData.GetBuffInfo().class_entity_data, NumberStyles.Integer, CultureInfo.InvariantCulture, out long attackModeId) || attackModeId <= 0)
            return false;
        targetFightCreatureEntity.CreateAttackModeForDeadExplosion(attackModeId, buffEntityData.GetTriggerValueRate());
        return true;
    }
    #endregion
}
