using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AIIntentDefCreatureAttack : AIBaseIntent
{
    //����׼��ʱ��
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttacking = 0;

    //Ŀ��AI
    public AIDefCreatureEntity selfAIEntity;
    //����״̬ 0׼���� 1������
    public int attackState = 0;

    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        timeUpdateAttackPre = 0;
        attackState = 0;
        selfAIEntity = aiEntity as AIDefCreatureEntity;
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Idle, false);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //����׼����
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;
            float attCD = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttCD();
            if (timeUpdateAttackPre >= attCD)
            {
                timeUpdateAttackPre = 0;
                AttackAttCreature();
            }
        }
        //������
        else if (attackState == 1)
        {
            timeUpdateAttacking += Time.deltaTime;
            float attAnimCastTime = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetAttAnimCastTime();
            if (timeUpdateAttacking >= attAnimCastTime)
            {
                timeUpdateAttacking = 0;
                AttackDefCreatureStartEnd();
            }
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
        //���Ź�������
        selfAIEntity.selfDefCreatureEntity.PlayAnim(AnimationCreatureStateEnum.Attack, false);
        selfAIEntity.selfDefCreatureEntity.AddAnim(0, AnimationCreatureStateEnum.Idle, true, 1);
    }

    /// <summary>
    /// ��������
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        var creatureInfo = selfAIEntity.selfDefCreatureEntity.fightCreatureData.GetCreatureInfo();
        //��ȡ������ʽ
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
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
