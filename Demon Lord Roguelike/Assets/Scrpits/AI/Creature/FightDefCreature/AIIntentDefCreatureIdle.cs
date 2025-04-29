using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureIdle : AIBaseIntent
{
    AIDefCreatureEntity selfAIEntity;
    public float timeUpdateForFindTarget = 0;
    public float timeUpdateForFindTargetCD = 0.2f;
    FightCreatureBean fightCreatureData;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateForFindTarget = 0;
        //初始化相关数据
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        string animNameAppoint = fightCreatureData.creatureData.creatureInfo.anim_idle;
        //播放起始动作
        selfAIEntity.selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true, animNameAppoint: animNameAppoint);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        timeUpdateForFindTarget += Time.deltaTime;
        if (timeUpdateForFindTarget > timeUpdateForFindTargetCD)
        {
            timeUpdateForFindTarget = 0;
            timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
            //搜索目标
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForDis(Vector3.right);
            if (selfAIEntity.targetCreatureEntity != null)
            {
                //如果攻击模式是防守则进入防守状态
                if (fightCreatureData.creatureData.creatureInfo.attack_mode == 0)
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
        timeUpdateForFindTarget = 0;
        timeUpdateForFindTargetCD = 0.2f;
    }

}
