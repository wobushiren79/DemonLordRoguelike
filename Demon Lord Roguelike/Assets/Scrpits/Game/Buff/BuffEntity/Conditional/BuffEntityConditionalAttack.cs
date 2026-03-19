using UnityEngine;

public class BuffEntityConditionalAttack : BuffEntityConditional
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        var targetFightCreatureEntity = GetFightCreatureEntityForTarget();
        if (targetFightCreatureEntity == null)
            return false;
        StartCreateAttack(buffEntityData, targetFightCreatureEntity);
        return true;
    }

    /// <summary>
    /// 处理检测
    /// </summary>
    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (CheckIsPre(buffEntityData))
        {
            buffEntityData.conditionalValue = 0;
            //触发BUFF
            TriggerBuffConditional(buffEntityData);
        }
    }

    /// <summary>
    /// 开始攻击
    /// </summary>
    public static void StartCreateAttack(BuffEntityBean buffEntityData, FightCreatureEntity targetFightCreatureEntity)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        //获取攻击模块ID 
        long attackModeId;
        if (buffInfo.class_entity_data.IsNull())
        {
            attackModeId = 0;
        }
        else
        {
            attackModeId = long.Parse(buffInfo.class_entity_data);
        }
        //开始攻击
        FightHandler.Instance.StartCreateAttackMode(targetFightCreatureEntity, null, null, customAttackModeId: attackModeId);
    }
}