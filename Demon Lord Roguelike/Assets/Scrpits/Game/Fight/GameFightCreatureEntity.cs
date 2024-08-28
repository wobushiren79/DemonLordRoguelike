using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;

public class GameFightCreatureEntity
{
    public string creatureId;
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;
    public CreatureStateEnum creatureState = CreatureStateEnum.None;
    //spine����
    public SkeletonAnimation creatureSkeletionAnimation;
    //��������ʾ
    public SpriteRenderer creatureLifeShow;
    //��ɫ����-�ܵ�����
    Tween animForUnderAttackColor;
    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureId = $"Creature_{SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N)}";
        this.creatureState = CreatureStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = creatureId;
        //��ȡ��������
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
        //��ȡ����ֵ��ʾ
        creatureLifeShow = creatureObj.transform.Find("LifeShow")?.GetComponent<SpriteRenderer>();
        creatureLifeShow?.ShowObj(false);

        ChangeSkin(fightCreatureData.creatureData);
    }

    /// <summary>
    /// �޸�Ƥ�� �������������޸�
    /// </summary>
    public void ChangeSkin(CreatureBean creatureData)
    {
        if (creatureSkeletionAnimation == null)
            return;
        if (creatureData == null)
            return;
        string[] skinArray = creatureData.GetSkinArray();
        SpineHandler.Instance.ChangeSkeletonSkin(creatureSkeletionAnimation.skeleton, skinArray);
    }

    /// <summary>
    /// ���Ŷ���
    /// </summary>
    /// <param name="animationCreatureState"></param>
    public TrackEntry PlayAnim(SpineAnimationStateEnum animationCreatureState, bool isLoop, float mixDuration = -1)
    {
        if (creatureSkeletionAnimation == null)
            return null;
        var animData =  SpineHandler.Instance.PlayAnim(creatureSkeletionAnimation, animationCreatureState, isLoop);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    /// <summary>
    /// ���Ӷ���
    /// </summary>
    public void AddAnim(int trackIndex, SpineAnimationStateEnum animationCreatureState, bool isLoop, float delay)
    {
        if (creatureSkeletionAnimation == null)
            return;
        creatureSkeletionAnimation.AnimationState.AddAnimation(trackIndex, animationCreatureState.GetEnumName(), isLoop, delay);
    }

    /// <summary>
    /// ������
    /// </summary>
    public void ClearAnim()
    {
        if (creatureSkeletionAnimation == null)
            return;
        //���������
        //creatureSkeletionAnimation.AnimationState.ClearTracks();
        var trackEntry = PlayAnim(SpineAnimationStateEnum.Idle, true, 0);
       //��������
        if (animForUnderAttackColor != null && animForUnderAttackColor.IsPlaying())
        {
            animForUnderAttackColor.Complete();
        }
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
    public void UnderAttack(int attDamage, Vector3 attDirection, out int leftLife, out int leftArmor)
    {
        //�ȿ۳�����
        leftArmor = fightCreatureData.ChangeArmor(-attDamage, out int outArmorChangeData);
        //��������Ѿ�ȫ������ �ٿ۳�����
        if (outArmorChangeData != 0)
        {
            fightCreatureData.ChangeLife(outArmorChangeData);
        }
        leftLife = fightCreatureData.liftCurrent;
        //�����������ֵ 
        if (leftLife > 0)
        {
            //������������Ч
            if (creatureSkeletionAnimation != null)
            {
                //����
                creatureSkeletionAnimation.transform.DOShakePosition(0.06f, strength: 0.05f, vibrato: 10, randomness: 180);
                //��Ѫ
                EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), attDirection);
                //��ɫ�仯����
                AnimForUnderAttackColor();
                //չʾѪ��
                creatureLifeShow?.ShowObj(true);
                //����Ѫ������
                creatureLifeShow?.material.SetFloat("_Progress_1", leftLife / (float)fightCreatureData.liftMax);
                //���û��ܽ���
                creatureLifeShow?.material.SetFloat("_Progress_2", leftArmor / (float)fightCreatureData.armorMax);
            }
        }
        else
        {   //������
            FightHandler.Instance.CreateDropCoin(creatureObj.transform.position);
            //����������������� 
            SetCreatureDead();
            //��Ѫ
            EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), attDirection);
            //��ɫ�仯����
            AnimForUnderAttackColor();
            //����Ѫ��
            creatureLifeShow?.ShowObj(false);
        }
    }


    /// <summary>
    /// ��ɫ����-�ܵ�����
    /// </summary>
    public void AnimForUnderAttackColor()
    {        
        // �����ʼ��ɫ��Ŀ����ɫ
        Color startColor = Color.white;
        Color endColor = Color.red;
        // ������ɫ���䶯��
        if (animForUnderAttackColor != null && animForUnderAttackColor.IsPlaying())
        {
            animForUnderAttackColor.Complete();
        }
        animForUnderAttackColor = DOTween
            .To(() => startColor, x => startColor = x, endColor, 0.2f)
            .OnUpdate(() => 
            {
                // ��ÿ֡����ʱִ�еĲ���
                creatureSkeletionAnimation.skeleton.SetColor(startColor);
            })
            .OnComplete(() => {

                creatureSkeletionAnimation.skeleton.SetColor(Color.white);
                animForUnderAttackColor = null;
            });
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
