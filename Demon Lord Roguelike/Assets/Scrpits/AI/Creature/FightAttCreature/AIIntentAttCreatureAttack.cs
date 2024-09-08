using UnityEngine;

public class AIIntentAttCreatureAttack : AIBaseIntent
{
    //����׼��ʱ��
    public float timeUpdateAttackPre = 0;
    public float timeUpdateAttacking = 0;
    //Ŀ��AI
    public AIAttCreatureEntity selfAIEntity;
    //����״̬ 0׼���� 1������
    public int attackState = 0;
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttCreatureEntity;

        timeUpdateAttackPre = 0;
        timeUpdateAttacking = 0;
        attackState = 0;

        //���ô�������
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Idle, true);
    }

    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        //����׼����
        if (attackState == 0)
        {
            timeUpdateAttackPre += Time.deltaTime;
            float attCD = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.GetAttCD();
            if (timeUpdateAttackPre >= attCD)
            {
                timeUpdateAttackPre = 0;
                AttackDefCreatureStart();
            }
        }
        //������
        else if (attackState == 1)
        {
            timeUpdateAttacking += Time.deltaTime;
            float attAnimCastTime = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.GetAttAnimCastTime();
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
    /// ������ʼ
    /// </summary>
    public virtual void AttackDefCreatureStart()
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
        //���Ź�������
        selfAIEntity.selfAttCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false);
    }

    /// <summary>
    /// ��������
    /// </summary>
    public virtual void AttackDefCreatureStartEnd()
    {
        attackState = 2;
        var creatureInfo = selfAIEntity.selfAttCreatureEntity.fightCreatureData.creatureData.creatureInfo;
        //��ȡ������ʽ
        FightHandler.Instance.CreateAttackModePrefab(creatureInfo.att_mode, (targetAttackMode) =>
        {
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
