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
        selfAIEntity = aiEntity as AIAttCreatureEntity;

        timeUpdateAttactPre = 0;
        attackState = 0;

        //���ô�������
        selfAIEntity.selfAttCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, true);
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
        //����Լ�����
        if (selfAIEntity.selfAttCreatureEntity == null || selfAIEntity.selfAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureDead);
            return;
        }
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.GetCreatureInfo();
        //��ȡ������ʽ
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
            //�����ƶ�����
            selfAIEntity.selfAttCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
            //��ʼ����
            targetAttackMode.StartAttack(selfAIEntity.selfAttCreatureEntity, selfAIEntity.targetDefCreatureEntity, ActionForAttackEnd);
        });
    }


    /// <summary>
    /// ���������ص�
    /// </summary>
    public void ActionForAttackEnd()
    {
        //���Ŀ�������Ѿ����� ������Ѱ��Ŀ��
        if (selfAIEntity.targetDefCreatureEntity == null || selfAIEntity.targetDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.AttCreatureIdle);
            return;
        }
        //��������
        else
        {
            attackState = 0;
        }
    }
}
