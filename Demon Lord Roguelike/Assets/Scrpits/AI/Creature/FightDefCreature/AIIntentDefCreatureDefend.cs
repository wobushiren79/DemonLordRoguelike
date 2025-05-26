using UnityEngine;

public class AIIntentDefCreatureDefend : AIBaseIntent
{
    //目标AI
    public AIDefCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public float timeUpdateForFindTargetCD = 0.2f;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        var creatureData = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData;
        //初始化相关数据
        timeUpdateForFindTargetCD = creatureData.GetAttackSearchTime();
        string animNameAppoint = creatureData.creatureInfo.anim_idle;
        //播放待机动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > timeUpdateForFindTargetCD)
        {
            timeUpdateForFindTarget = 0;
            //搜索敌人
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Right);
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
        timeUpdateForFindTargetCD = 0.2f;
    }
}
