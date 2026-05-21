using UnityEngine;

public class AIIntentCreatureDead : AIBaseIntent
{
    public float timeUpdateForDead = 0f;
    public float timeUpdateForDeadCD = 1.1f;
    //当前AIEntity
    public AICreatureEntity selfAIEntity;
    public CreatureFightTypeEnum creatureFightType;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateForDead = 0;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false);

        var selfFightCreatureEntity = selfAIEntity.selfCreatureEntity;
        selfFightCreatureEntity.fightCreatureData.positionDead = selfFightCreatureEntity.creatureObj.transform.position;
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadStart, selfFightCreatureEntity);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForDead += Time.deltaTime;
        if (timeUpdateForDead >= timeUpdateForDeadCD)
        {
            timeUpdateForDead = 0;
            var selfFightCreatureEntity = selfAIEntity.selfCreatureEntity;
            //先派发死亡结束事件 让 BuffEntityConditionalDead 系列BUFF有机会执行触发逻辑（重生/死亡爆发/死亡掉水晶/死亡范围伤害等）
            //再清理生物 否则 RemoveFightCreatureEntity 会先清空BUFF导致上述BUFF全部失效
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureEntity);
            CreatureHandler.Instance.RemoveFightCreatureEntity(selfAIEntity.selfCreatureEntity, creatureFightType);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}