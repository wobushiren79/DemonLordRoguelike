using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.TestTools.CodeCoverage;
using UnityEngine;

public class GameFightCreatureEntity
{
    public string creatureId;
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;
    public CreatureStateEnum creatureState = CreatureStateEnum.None;
    public SkeletonAnimation creatureSkeletionAnimation;

    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureId = $"Creature_{SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N)}";
        this.creatureState = CreatureStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = creatureId;
        //��ȡ��������
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
    }

    /// <summary>
    /// ���Ŷ���
    /// </summary>
    /// <param name="animationCreatureState"></param>
    public void PlayAnim(AnimationCreatureStateEnum animationCreatureState, bool isLoop)
    {
        if (creatureSkeletionAnimation == null)
            return;
        creatureSkeletionAnimation.AnimationState.SetAnimation(0, animationCreatureState.GetEnumName(), isLoop);
    }

    /// <summary>
    /// �������ĳ���
    /// </summary>
    public void SetFaceDirection(Direction2DEnum direction2DEnum)
    {
        if (creatureSkeletionAnimation == null)
            return;
        float size = Mathf.Abs(creatureSkeletionAnimation.transform.localScale.x);
        if (direction2DEnum == Direction2DEnum.Left)
        {
            creatureSkeletionAnimation.transform.localScale = new Vector3(-size, size, size);
        }
        else
        {
            creatureSkeletionAnimation.transform.localScale = new Vector3(size, size, size);
        }
    }

    /// <summary>
    /// �յ�����
    /// </summary>
    /// <returns></returns>
    public void UnderAttack(int attDamage, out int leftLife, out int leftArmor)
    {
        //�ȿ۳�����
        leftArmor = fightCreatureData.ChangeArmor(-attDamage, out int outArmorChangeData);
        //��������Ѿ�ȫ������ �ٿ۳�����
        if (outArmorChangeData != 0)
        {
            fightCreatureData.ChangeLife(outArmorChangeData);
        }
        leftLife = fightCreatureData.liftCurrent;
    }

    /// <summary>
    /// �Ƿ��Ѿ�����
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        if (creatureState == CreatureStateEnum.Dead)
            return true;
        //���Ŀ�������Ѿ�����
        //if (creatureObj == null || fightCreatureData == null || fightCreatureData.liftCurrent <= 0)
        //{
        //    return true;
        //}
        return false;
    }

    /// <summary>
    /// ������������
    /// </summary>
    public void SetCreatureDead()
    {
        creatureState = CreatureStateEnum.Dead;
        if (aiEntity is AIAttCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.AttCreatureDead);
        }
        else if (aiEntity is AIDefCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefCreatureDead);
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDead, fightCreatureData);
    }
}
