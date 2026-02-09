using UnityEngine;

public class BuffEntityConditionalDeadAttack : BuffEntityConditionalDead
{

    public override void CreatureDeadEnd()
    {
        base.CreatureDeadEnd();
        TriggerBuffConditional(buffEntityData);
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        if (targetFightCreatureEntity == null)
            return false;
        var buffInfo = buffEntityData.GetBuffInfo();
        //获取攻击模块ID 
        long attackModeId = long.Parse(buffInfo.class_entity_data);
        AttackModeBean attackModeData = FightHandler.Instance.manager.GetAttackModeData(attackModeId);
        //获取敌人层级
        attackModeData.attackedLayerTarget =  targetFightCreatureEntity.fightCreatureData.GetCreatrueLayer(true);
        //获取攻击者UUID
        attackModeData.attackerId = targetFightCreatureEntity.fightCreatureData.creatureData.creatureUUId;
        //获取被攻击者UUID
        attackModeData.attackedId = "";
        //获取攻击者伤害
        attackModeData.attackerDamage = (int)targetFightCreatureEntity.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        //获取攻击方向
        attackModeData.attackDirection = targetFightCreatureEntity.fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightAttack ? Vector3.left : Vector3.right;
        //获取攻击位置
        attackModeData.startPos = targetFightCreatureEntity.fightCreatureData.positionDead;
        //开始攻击
        FightHandler.Instance.StartCreateAttackMode(attackModeData);
        return true;
    }
}