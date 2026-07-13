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
    /// <summary>是否允许搜索身后敌人（配置门控，IntentEntering 缓存，避免每次搜索重复读配置）</summary>
    private bool isAttackSearchBack = false;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIDefenseCreatureEntity;
        fightCreatureData = selfAIEntity.selfCreatureEntity.fightCreatureData;
        timeUpdateForFindTarget = 0;
        //初始化相关数据
        timeUpdateForFindTargetCD = fightCreatureData.creatureData.GetAttackSearchTime();
        //缓存身后搜索开关
        isAttackSearchBack = fightCreatureData.creatureData.creatureInfo.IsAttackSearchBack();
        //回到待机即恢复朝正面(右)：上一轮若曾转身打身后，此处转回来（SetFaceDirection 已对朝向未变去重）
        if (isAttackSearchBack)
        {
            selfAIEntity.selfCreatureEntity.SetFaceDirection(Direction2DEnum.Right);
        }
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
            //搜索目标（正面优先，正面无目标且开启身后搜索时才向背后补搜）
            selfAIEntity.targetCreatureEntity = selfAIEntity.FindCreatureEntityForSingeFrontThenBack(DirectionEnum.Right, isAttackSearchBack);
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
