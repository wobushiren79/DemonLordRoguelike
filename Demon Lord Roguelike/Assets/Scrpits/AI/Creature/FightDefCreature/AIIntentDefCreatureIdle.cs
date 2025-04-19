using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureIdle : AIBaseIntent
{
    AIDefCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        timeUpdateForFindTarget = 0;

        string animNameAppoint = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo.anim_idle;
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > 0.25f)
        {
            timeUpdateForFindTarget = 0;
            var selfCreatureInfo = selfAIEntity.selfCreatureEntity.fightCreatureData.creatureData.creatureInfo;
            //搜索目标
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForDis(Vector3.right, CreatureTypeEnum.FightAttack);
            if (selfAIEntity.targetCreatureEntity != null)
            {
                //如果攻击模式是防守则进入防守状态
                if (selfCreatureInfo.attack_mode == 0)
                {
                    ChangeIntent(AIIntentEnum.DefCreatureDefend);
                }
                //其他情况进入攻击状态
                else
                {
                    ChangeIntent(AIIntentEnum.DefCreatureAttack);
                }
            }
        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

}
