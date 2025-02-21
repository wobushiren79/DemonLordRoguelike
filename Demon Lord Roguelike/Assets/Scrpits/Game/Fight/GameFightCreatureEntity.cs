using DG.Tweening;
using Spine;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;

public class GameFightCreatureEntity
{
    public GameObject creatureObj;
    public FightCreatureBean fightCreatureData;
    public AIBaseEntity aiEntity;
    public CreatureFightStateEnum creatureFightState = CreatureFightStateEnum.None;
    //spine����
    public SkeletonAnimation creatureSkeletionAnimation;
    //��������ʾ
    public SpriteRenderer creatureLifeShow;
    //��ɫ����-�ܵ�����
    Tween animForUnderAttackColor;
    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureFightState = CreatureFightStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = fightCreatureData.creatureData.creatureId;
        //��ȡ��������
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
        //��ȡ����ֵ��ʾ
        creatureLifeShow = creatureObj.transform.Find("LifeShow")?.GetComponent<SpriteRenderer>();
        creatureLifeShow?.ShowObj(false);

        ChangeSkin(fightCreatureData.creatureData);
        //����������ɫ
        SetBodyColor();
    }

    /// <summary>
    /// ����
    /// </summary>
    public void Update(float updateTime)
    {
        UpdateForBuffs(updateTime);
    }

    /// <summary>
    /// ����buffs
    /// </summary>
    public void UpdateForBuffs(float updateTime)
    {
        if (IsDead())
            return;
        if (fightCreatureData == null)
            return;
        if (fightCreatureData.listBuff.IsNull())
            return;
        for (int i = 0; i < fightCreatureData.listBuff.Count; i++)
        {
            var itemBuff = fightCreatureData.listBuff[i];
            itemBuff.AddBuffTime(updateTime, out bool isRemove, actionForCompleteRemove : CallBackForRemoveBuff);
            if (isRemove)
            {
                i--;
            }
        }
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
    ///  ����������ɫ
    /// </summary>
    public void SetBodyColor(Color bodyColor)
    {
        //��ʼ��������ɫ
        if (creatureSkeletionAnimation != null && creatureSkeletionAnimation.skeleton != null)
        {
            creatureSkeletionAnimation.skeleton.SetColor(bodyColor);
        }
    }
    public void SetBodyColor()
    {
        Color bodyColor = fightCreatureData.GetBodyColor();
        SetBodyColor(bodyColor);
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
    /// ����BUFF
    /// </summary>
    public void AddBuff(BaseAttackMode baseAttackMode)
    {
        //����buff
        var buffs = baseAttackMode.attackModeInfo.GetBuff();
        if (!buffs.IsNull())
        {
            var buffsTrigger = FightBuffBean.GetTriggerFightBuff(buffs, fightCreatureData.creatureData.creatureId);
            if (!buffsTrigger.IsNull())
            {
                fightCreatureData.AddBuff(buffsTrigger, actionForComplete: CallBackForAddBuff);
            }
        }
    }

    /// <summary>
    /// �յ�����
    /// </summary>
    public void UnderAttack(BaseAttackMode baseAttackMode)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;

        //�ȿ۳����� �ٿ۳�����
        fightCreatureData.ChangeArmorAndLife(-baseAttackMode.attackerDamage,
        out int curArmor, out int curLife,
        out int changeArmorReal, out int changeLifeReal);
        //��ʵ��ɵ��˺�
        int damageReal = Mathf.Abs(changeArmorReal + changeLifeReal);
        //��¼����
        fightRecordsData.AddCreatureDamage(baseAttackMode.attackerId, damageReal);
        fightRecordsData.AddCreatureDamageReceived(baseAttackMode.attackedId, damageReal);

        //�����������ֵ 
        if (curLife > 0)
        {
            //����BUFF
            AddBuff(baseAttackMode);
            //������������Ч
            if (creatureSkeletionAnimation != null)
            {
                //����
                creatureSkeletionAnimation.transform.DOShakePosition(0.06f, strength: 0.05f, vibrato: 10, randomness: 180);
                //��Ѫ
                EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
                //��ɫ�仯����
                AnimForUnderAttackColor();
                //չʾѪ��
                creatureLifeShow?.ShowObj(true);
                //����Ѫ������
                creatureLifeShow?.material.SetFloat("_Progress_1", curLife / (float)fightCreatureData.lifeMax);
                //���û��ܽ���
                creatureLifeShow?.material.SetFloat("_Progress_2", curArmor / (float)fightCreatureData.armorMax);
            }
        }
        else
        {
            //��¼����
            fightRecordsData.AddCreatureKillNum(baseAttackMode.attackerId, 1);

            //������
            FightHandler.Instance.CreateDropCoin(creatureObj.transform.position);
            //����������������� 
            SetCreatureDead();
            //��Ѫ
            EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
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
        Color startColor = fightCreatureData.GetBodyColor();
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
                SetBodyColor(startColor);
            })
            .OnComplete(() => 
            {
                SetBodyColor();
                animForUnderAttackColor = null;
            });
    }

    /// <summary>
    /// �Ƿ��Ѿ�����
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        if (creatureFightState == CreatureFightStateEnum.Dead)
            return true;
        //���Ŀ�������Ѿ�����
        //if (creatureObj == null || fightCreatureData == null || fightCreatureData.lifeCurrent <= 0)
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
        creatureFightState = CreatureFightStateEnum.Dead;
        if (aiEntity is AIAttCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.AttCreatureDead);
        }
        else if (aiEntity is AIDefCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefCreatureDead);
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadStart, fightCreatureData);
    }

    /// <summary>
    /// �ص��Ƴ�BUFF
    /// </summary>
    public void CallBackForRemoveBuff()
    {
        SetBodyColor();
    }

    /// <summary>
    /// �ص�����BUFF
    /// </summary>
    public void CallBackForAddBuff()
    {
        SetBodyColor();
    }
}
