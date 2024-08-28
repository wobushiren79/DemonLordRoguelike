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
    //spine数据
    public SkeletonAnimation creatureSkeletionAnimation;
    //生命条显示
    public SpriteRenderer creatureLifeShow;
    //颜色动画-受到攻击
    Tween animForUnderAttackColor;
    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureId = $"Creature_{SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N)}";
        this.creatureState = CreatureStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = creatureId;
        //获取骨骼数据
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
        //获取生命值显示
        creatureLifeShow = creatureObj.transform.Find("LifeShow")?.GetComponent<SpriteRenderer>();
        creatureLifeShow?.ShowObj(false);

        ChangeSkin(fightCreatureData.creatureData);
    }

    /// <summary>
    /// 修改皮肤 根据生物数据修改
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
    /// 播放动画
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
    /// 增加动画
    /// </summary>
    public void AddAnim(int trackIndex, SpineAnimationStateEnum animationCreatureState, bool isLoop, float delay)
    {
        if (creatureSkeletionAnimation == null)
            return;
        creatureSkeletionAnimation.AnimationState.AddAnimation(trackIndex, animationCreatureState.GetEnumName(), isLoop, delay);
    }

    /// <summary>
    /// 清理动画
    /// </summary>
    public void ClearAnim()
    {
        if (creatureSkeletionAnimation == null)
            return;
        //再清除动画
        //creatureSkeletionAnimation.AnimationState.ClearTracks();
        var trackEntry = PlayAnim(SpineAnimationStateEnum.Idle, true, 0);
       //清理数据
        if (animForUnderAttackColor != null && animForUnderAttackColor.IsPlaying())
        {
            animForUnderAttackColor.Complete();
        }
    }

    /// <summary>
    /// 设置脸的朝向
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
    /// 收到攻击
    /// </summary>
    public void UnderAttack(int attDamage, Vector3 attDirection, out int leftLife, out int leftArmor)
    {
        //先扣除护甲
        leftArmor = fightCreatureData.ChangeArmor(-attDamage, out int outArmorChangeData);
        //如果护甲已经全部扣完 再扣除生命
        if (outArmorChangeData != 0)
        {
            fightCreatureData.ChangeLife(outArmorChangeData);
        }
        leftLife = fightCreatureData.liftCurrent;
        //如果还有生命值 
        if (leftLife > 0)
        {
            //触发被攻击特效
            if (creatureSkeletionAnimation != null)
            {
                //颤抖
                creatureSkeletionAnimation.transform.DOShakePosition(0.06f, strength: 0.05f, vibrato: 10, randomness: 180);
                //流血
                EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), attDirection);
                //颜色变化动画
                AnimForUnderAttackColor();
                //展示血条
                creatureLifeShow?.ShowObj(true);
                //设置血条进度
                creatureLifeShow?.material.SetFloat("_Progress_1", leftLife / (float)fightCreatureData.liftMax);
                //设置护盾进度
                creatureLifeShow?.material.SetFloat("_Progress_2", leftArmor / (float)fightCreatureData.armorMax);
            }
        }
        else
        {   //掉落金币
            FightHandler.Instance.CreateDropCoin(creatureObj.transform.position);
            //如果被攻击对象死亡 
            SetCreatureDead();
            //流血
            EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), attDirection);
            //颜色变化动画
            AnimForUnderAttackColor();
            //隐藏血条
            creatureLifeShow?.ShowObj(false);
        }
    }


    /// <summary>
    /// 颜色动画-受到攻击
    /// </summary>
    public void AnimForUnderAttackColor()
    {        
        // 定义初始颜色和目标颜色
        Color startColor = Color.white;
        Color endColor = Color.red;
        // 创建颜色渐变动画
        if (animForUnderAttackColor != null && animForUnderAttackColor.IsPlaying())
        {
            animForUnderAttackColor.Complete();
        }
        animForUnderAttackColor = DOTween
            .To(() => startColor, x => startColor = x, endColor, 0.2f)
            .OnUpdate(() => 
            {
                // 在每帧更新时执行的操作
                creatureSkeletionAnimation.skeleton.SetColor(startColor);
            })
            .OnComplete(() => {

                creatureSkeletionAnimation.skeleton.SetColor(Color.white);
                animForUnderAttackColor = null;
            });
    }

    /// <summary>
    /// 是否已经死亡
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        if (creatureState == CreatureStateEnum.Dead)
            return true;
        //如果目标生物已经无了
        //if (creatureObj == null || fightCreatureData == null || fightCreatureData.liftCurrent <= 0)
        //{
        //    return true;
        //}
        return false;
    }

    /// <summary>
    /// 设置生物死亡
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
