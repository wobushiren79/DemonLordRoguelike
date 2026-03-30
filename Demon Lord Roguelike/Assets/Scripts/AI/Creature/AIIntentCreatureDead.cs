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
            CreatureHandler.Instance.RemoveFightCreatureEntity(selfAIEntity.selfCreatureEntity, creatureFightType);
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadEnd, selfFightCreatureEntity);
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}