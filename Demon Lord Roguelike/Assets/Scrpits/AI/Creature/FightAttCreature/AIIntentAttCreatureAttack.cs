using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentAttCreatureAttack : AIBaseIntent
{
    //����׼��ʱ��
    public float timeUpdateAttactPre = 0;
    //Ŀ��AI
    public AIAttCreatureEntity selfAIEntity;
    //����״̬ 0׼���� 1������
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttactPre = 0;
        attackState = 0;
        selfAIEntity = aiEntity as AIAttCreatureEntity;
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //����׼����
        if (attackState == 0)
        {
            timeUpdateAttactPre += Time.deltaTime;
            float attCD = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttactPre >= attCD)
            {
                timeUpdateAttactPre = 0;
                AttackDefCreature();
            }
        }
        //������
        else if (attackState == 1)
        {

        }
    }

    public override void IntentLeaving(AIBaseEntity aiEntity)
    {

    }

    /// <summary>
    /// ������������
    /// </summary>
    public virtual void AttackDefCreature()
    {
        attackState = 1;
        //���Ŀ�������Ѿ�����
        if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
    }

}
