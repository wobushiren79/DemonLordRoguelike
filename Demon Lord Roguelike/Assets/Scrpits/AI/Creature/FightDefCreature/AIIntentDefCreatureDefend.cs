using UnityEngine;

public class AIIntentDefCreatureDefend : AIBaseIntent
{
    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCreatureEntity;

        string animNameAppoint = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > 0.5f)
        {
            timeUpdateForFindTarget = 0;
            //搜索敌人
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForDis(Vector3.right, CreatureTypeEnum.FightAttack);
            //如果没有敌人了 就进入待机状态  
            if (selfAIEntity.targetCreatureEntity == null)
            {
                ChangeIntent(AIIntentEnum.DefCoreCreatureIdle);
            }
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
    }
}
