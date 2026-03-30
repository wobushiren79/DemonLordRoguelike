using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefenseCreatureIdle : AIBaseIntent
{
    AIDefenseCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public float timeUpdateForFindTargetCD = 0.2f;
    FightCreatureBean fightCreatureData;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateForFindTarget = 0;
        //初始化相关数据
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //播放起始动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > timeUpdateForFindTargetCD)
        {
            timeUpdateForFindTarget = 0;
            timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
            //搜索目标
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSinge(DirectionEnum.Right);
            if (selfAIEntity.targetCreatureEntity != null)
            {
                //如果攻击模式是防守则进入防守状态
                if (fightCreatureData.creatureData.creatureInfo.attack_mode == 0)
                {
                    ChangeIntent(AIIntentEnum.DefenseCreatureDefend);
                }
                //其他情况进入攻击状态
                else
                {
                    ChangeIntent(AIIntentEnum.DefenseCreatureAttack);
                }
            }
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.2f;
    }

}
