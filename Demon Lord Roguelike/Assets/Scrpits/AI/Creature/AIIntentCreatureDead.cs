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
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Dead, false);

        var selfFightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadStart, selfFightCreatureData);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForDead += Time.deltaTime;
        if (timeUpdateForDead >= timeUpdateForDeadCD)
        {
            timeUpdateForDead = 0;
            var selfFightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
            CreatureHandler.Instance.RemoveFightCreatureEntity(selfAIEntity.selfCreatureEntity, creatureFightType);
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureData);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}