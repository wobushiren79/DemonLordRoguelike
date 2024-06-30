using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureMove : AIBaseIntent
{
    //Ŀ��AI
    public AIAttCreatureEntity selfAIEntity;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;
        //�����ƶ�����
        selfAIEntity.selfAttCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Walk, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //���Ŀ����ħ��
        if (selfAIEntity.targetMovePos.x <= 0)
        {
            //����Ƿ񿿽�Ŀ��
            if (CheckIsCloseTarget())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureDead);
                return;
            }
        }
        //�������ħ��
        else
        {
            //���Ŀ���Ѿ�����
            if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureIdle);
                return;
            }
            //����Ƿ��ڹ�����Χ��
            if (CheckIsAttRange())
            {
                selfAIEntity.ChangeIntent(AIIntentEnum.AttCreatureAttack);
                return;
            }
        }

        float moveSpeed = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetMoveSpeed();
        Transform selfTF = selfAIEntity.selfAttCreatureEntity.creatureObj.transform;
        selfTF.Translate(Vector3.Normalize(selfAIEntity.targetMovePos - selfTF.transform.position) * Time.deltaTime * moveSpeed);
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

    /// <summary>
    /// ����Ƿ��ڹ�����Χ��
    /// </summary>
    public bool CheckIsAttRange()
    {
        var currentPosition = selfAIEntity.selfAttCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetCreatureInfo();
        if (dis <= creatureInfo.att_range)
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// ����Ƿ񿿽���Ŀ��
    /// </summary>
    /// <returns></returns>
    public bool CheckIsCloseTarget()
    {
        var currentPosition = selfAIEntity.selfAttCreatureEntity.creatureObj.transform.position;
        var targetMovePos = selfAIEntity.targetMovePos;
        float dis = Vector3.Distance(currentPosition, targetMovePos);
        if (dis <= 0.05f)
        {
            return true;
        }
        return false;
    }
}
