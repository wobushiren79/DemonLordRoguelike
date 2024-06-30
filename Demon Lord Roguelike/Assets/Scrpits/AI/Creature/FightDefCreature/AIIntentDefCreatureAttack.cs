using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIIntentDefCreatureAttack : AIBaseIntent
{
    //����׼��ʱ��
    public float timeUpdateAttactPre = 0;
    //Ŀ��AI
    public AIDefCreatureEntity selfAIEntity;
    //����״̬ 0׼���� 1������
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttactPre = 0;
        attackState = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //����׼����
        if (attackState == 0)
        {
            timeUpdateAttactPre += Time.deltaTime;
            float attCD = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttactPre >= attCD)
            {
                timeUpdateAttactPre = 0;
                AttackAttCreature();
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
    /// ��������
    /// </summary>
    public virtual void AttackAttCreature()
    {
        attackState = 1;
        //���Ŀ�������Ѿ�����
        if (selfAIEntity.targetAttCreatureEntity == null || selfAIEntity.targetAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
            return;
        }
        //����Լ�����
        if (selfAIEntity.selfDefCreatureEntity == null || selfAIEntity.selfDefCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureDead);
            return;
        }
        var creatureInfo = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetCreatureInfo();
        //��ȡ������ʽ
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
            selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
            //��ʼ����
            targetAttackMode.StartAttack(selfAIEntity.selfDefCreatureEntity, selfAIEntity.targetAttCreatureEntity, ActionForAttackEnd);
        });
    }


    /// <summary>
    /// ���������ص�
    /// </summary>
    public void ActionForAttackEnd()
    {
        //���Ŀ�������Ѿ����� ������Ѱ��Ŀ��
        if (selfAIEntity.targetAttCreatureEntity == null || selfAIEntity.targetAttCreatureEntity.IsDead())
        {
            ChangeIntent(AIIntentEnum.DefCreatureIdle);
            return;
        }
        //��������
        else
        {
            attackState = 0;
        }
    }
}
