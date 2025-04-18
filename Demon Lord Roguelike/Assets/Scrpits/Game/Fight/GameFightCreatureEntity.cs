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
    //spine数据
    public SkeletonAnimation creatureSkeletionAnimation;
    //生命条显示
    public SpriteRenderer creatureLifeShow;
    //颜色动画-受到攻击
    Tween animForUnderAttackColor;
    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureFightState = CreatureFightStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = fightCreatureData.creatureData.creatureId;
        //获取骨骼数据
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
        //获取生命值显示
        creatureLifeShow = creatureObj.transform.Find("LifeShow")?.GetComponent<SpriteRenderer>();
        creatureLifeShow?.ShowObj(false);

        ChangeSkin(fightCreatureData.creatureData);
        //设置身体颜色
        SetBodyColor();
    }

    /// <summary>
    /// 更新
    /// </summary>
    public void Update(float updateTime)
    {
        UpdateForBuffs(updateTime);
    }

    /// <summary>
    /// 更新buffs
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
            itemBuff.AddBuffTime(updateTime, out bool isRemove, actionForCompleteRemove: CallBackForRemoveBuff);
            if (isRemove)
            {
                i--;
            }
        }
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
    ///  设置身体颜色
    /// </summary>
    public void SetBodyColor(Color bodyColor)
    {
        //初始化身体颜色
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
    /// 播放动画
    /// </summary>
    /// <param name="animationCreatureState"></param>
    public TrackEntry PlayAnim(SpineAnimationStateEnum animationCreatureState, bool isLoop, float mixDuration = -1, string animNameAppoint = null)
    {
        if (creatureSkeletionAnimation == null)
            return null;
        var animData = SpineHandler.Instance.PlayAnim(creatureSkeletionAnimation, animationCreatureState, isLoop, animNameAppoint);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    /// <summary>
    /// 增加动画
    /// </summary>
    public TrackEntry AddAnim(int trackIndex, SpineAnimationStateEnum animationCreatureState, bool isLoop, float delay, string animNameAppoint = null)
    {
        if (creatureSkeletionAnimation == null)
            return null;
        var animData = SpineHandler.Instance.AddAnimation(creatureSkeletionAnimation, trackIndex, animationCreatureState, isLoop, delay, animNameAppoint);
        return animData;
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
        string animNameAppoint = fightCreatureData.creatureData.creatureInfo.anim_idle;
        var trackEntry = PlayAnim(SpineAnimationStateEnum.Idle, true, 0, animNameAppoint: animNameAppoint);
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
    /// 增加BUFF
    /// </summary>
    public void AddBuff(BaseAttackMode baseAttackMode)
    {
        //触发buff
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
    /// 收到攻击
    /// </summary>
    public void UnderAttack(BaseAttackMode baseAttackMode)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;

        //先扣除护甲 再扣除生命
        fightCreatureData.ChangeDRAndHP(-baseAttackMode.attackerDamage,
        out int curDR, out int curHP,
        out int changeDRReal, out int changeHPReal);
        //真实造成的伤害
        int damageReal = Mathf.Abs(changeDRReal + changeHPReal);
        //记录数据
        fightRecordsData.AddCreatureDamage(baseAttackMode.attackerId, damageReal);
        fightRecordsData.AddCreatureDamageReceived(baseAttackMode.attackedId, damageReal);

        //如果还有生命值 
        if (curHP > 0)
        {
            //增加BUFF
            AddBuff(baseAttackMode);
            //触发被攻击特效
            if (creatureSkeletionAnimation != null)
            {
                //颤抖
                creatureSkeletionAnimation.transform.DOShakePosition(0.06f, strength: 0.05f, vibrato: 10, randomness: 180);

                //如果是打到肉
                if (changeHPReal == 0)
                {
                    //护甲
                    EffectHandler.Instance.ShowShieldHitEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
            
                }
                //如果是打击到护甲
                else
                {
                    //流血
                    EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
                }

                //颜色变化动画
                AnimForUnderAttackColor();
                //展示血条
                creatureLifeShow?.ShowObj(true);
                //设置血条进度
                creatureLifeShow?.material.SetFloat("_Progress_1", curHP / (float)fightCreatureData.HPMax);
                //设置护盾进度
                creatureLifeShow?.material.SetFloat("_Progress_2", curDR / (float)fightCreatureData.DRMax);
            }
        }
        else
        {
            //记录数据
            fightRecordsData.AddCreatureKillNum(baseAttackMode.attackerId, 1);

            //掉落水晶
            FightHandler.Instance.CreateDropCrystal(creatureObj.transform.position);
            //如果被攻击对象死亡 
            SetCreatureDead();
            //流血
            EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
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
        Color startColor = fightCreatureData.GetBodyColor();
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
                SetBodyColor(startColor);
            })
            .OnComplete(() =>
            {
                SetBodyColor();
                animForUnderAttackColor = null;
            });
    }

    /// <summary>
    /// 是否已经死亡
    /// </summary>
    /// <returns></returns>
    public bool IsDead()
    {
        if (creatureFightState == CreatureFightStateEnum.Dead)
            return true;
        //如果目标生物已经无了
        //if (creatureObj == null || fightCreatureData == null || fightCreatureData.lifeCurrent <= 0)
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
    /// 回调移除BUFF
    /// </summary>
    public void CallBackForRemoveBuff()
    {
        SetBodyColor();
    }

    /// <summary>
    /// 回调增加BUFF
    /// </summary>
    public void CallBackForAddBuff()
    {
        SetBodyColor();
    }
}
