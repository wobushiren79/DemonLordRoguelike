using System;
using System.Collections.Generic;
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

    //动画-受到攻击
    Tween animForUnderAttackColor;
    Tween animForUnderAttackShake;

    public GameFightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        this.creatureFightState = CreatureFightStateEnum.Live;
        this.creatureObj = creatureObj;
        this.fightCreatureData = fightCreatureData;
        this.creatureObj.name = fightCreatureData.creatureData.creatureUUId;
        //获取骨骼数据
        creatureSkeletionAnimation = creatureObj.transform.Find("Spine")?.GetComponent<SkeletonAnimation>();
        //获取生命值显示
        creatureLifeShow = creatureObj.transform.Find("LifeShow")?.GetComponent<SpriteRenderer>();
        creatureLifeShow?.ShowObj(false);
        //设置皮肤
        ChangeSkin(fightCreatureData.creatureData);
        //设置buff数据
        long[] creatureBuffs = fightCreatureData.creatureData.creatureInfo.GetCreatureBuff();
        BuffHandler.Instance.AddBuff(creatureBuffs, fightCreatureData.creatureData.creatureUUId, fightCreatureData.creatureData.creatureUUId);  
        //设置身体颜色
        SetBodyColor();
    }

    /// <summary>
    /// 更新
    /// </summary>
    public void Update(float updateTime)
    {

    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="isPermanently">是否永久删除</param>
    public void Destory(bool isPermanently)
    {
        if (isPermanently)
        {
            if (creatureObj != null)
            {
                GameObject.Destroy(creatureObj);
            }
        }
    }

    #region  身体相关
    /// <summary>
    /// 修改皮肤 根据生物数据修改
    /// </summary>
    public void ChangeSkin(CreatureBean creatureData)
    {
        if (creatureSkeletionAnimation == null)
            return;
        if (creatureData == null)
            return;
        //设置spine
        CreatureHandler.Instance.SetCreatureData(creatureSkeletionAnimation, creatureData, isSetSkeletonDataAsset: false);
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
    #endregion

    #region 状态相关
    //改变路线
    public void ChangeRoad(int targetRoadIndex)
    {
        fightCreatureData.roadIndex = targetRoadIndex;
        var creatureType = fightCreatureData.creatureData.creatureInfo.GetCreatureType();
        switch (creatureType)
        {
            case CreatureTypeEnum.FightAttack:
                aiEntity.ChangeIntent(AIIntentEnum.AttackCreatureLured);
                break;
        }
    }

    /// <summary>
    /// 增加BUFF
    /// </summary>
    public void AddBuff(BaseAttackMode baseAttackMode)
    {
        //触发buff
        var buffIds = baseAttackMode.attackModeInfo.GetBuffIds();
        bool isAdd = BuffHandler.Instance.AddBuff(buffIds, baseAttackMode.attackerId, fightCreatureData.creatureData.creatureUUId);
        if (isAdd)
        {
            //刷新一下身体颜色
            SetBodyColor();
        }
    }

    /// <summary>
    /// 回复HP
    /// </summary>
    public void RegainHP(BaseAttackMode baseAttackMode)
    {
        RegainHP(baseAttackMode.attackerId, fightCreatureData.creatureData.creatureUUId, baseAttackMode.attackerDamage,
            actionForNoDead: (changeHPReal) =>
            {
                //增加BUFF
                AddBuff(baseAttackMode);
            },
            actionForDead: (changeHPReal) =>
            {

            }
        );
    }

    /// <summary>
    /// 回复HP
    /// </summary>
    public void RegainHP(string attackerId, string attackedId, int hpChangeData, Action<int> actionForNoDead = null, Action<int> actionForDead = null)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;
        fightCreatureData.ChangeHP(hpChangeData, out int curHP, out int changeHPReal);

        //记录数据
        fightRecordsData.AddCreatureRegainHP(attackerId, changeHPReal);
        fightRecordsData.AddCreatureRegainHPReceived(attackedId, changeHPReal);

        //检测一下是否死亡
        CheckDead
        (
            //没有死亡
            actionForNoDead: () =>
            {
                actionForNoDead?.Invoke(changeHPReal);
            },
            //死亡之后
            actionForDead: () =>
            {
                actionForDead?.Invoke(changeHPReal);
            }
        );

        //显示数字
        EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), changeHPReal, 3);
    }

    /// <summary>
    /// 回复护甲
    /// </summary>
    public void RegainDR(BaseAttackMode baseAttackMode)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;
        fightCreatureData.ChangeDR(baseAttackMode.attackerDamage, out int leftDR, out int changeDRReal);

        //记录数据
        fightRecordsData.AddCreatureRegainDR(baseAttackMode.attackerId, changeDRReal);
        fightRecordsData.AddCreatureRegainDRReceived(fightCreatureData.creatureData.creatureUUId, changeDRReal);
        //检测一下是否死亡
        CheckDead
        (
            //没有死亡
            actionForNoDead: () =>
            {
                //增加BUFF
                AddBuff(baseAttackMode);
            },
            //死亡之后
            actionForDead: () =>
            {

            }
        );
        //显示数字
        EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), changeDRReal, 4);
    }

    /// <summary>
    /// 受到攻击
    /// </summary>
    public void UnderAttack(BaseAttackMode baseAttackMode)
    {
        FightUnderAttackStruct fightUnderAttackStruct = new FightUnderAttackStruct(baseAttackMode,fightCreatureData.creatureData.creatureUUId);
        UnderAttack(fightUnderAttackStruct,
            actionForNoDead: (changeDRReal, changeHPReal) =>
            {
                //触发被攻击特效
                AnimForAnimForUnderAttackShake();
                //增加BUFF
                AddBuff(baseAttackMode);
                //播放受伤特效
                if (baseAttackMode.attackModeInfo.effect_damage.IsNull())
                {
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
                }
                //不播放受伤特效
                else if (baseAttackMode.attackModeInfo.effect_damage.Equals("0"))
                {

                }
            },
            actionForDead: (changeDRReal, changeHPReal) =>
            {
                var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
                var fightRecordsData = gameLogic.fightData.fightRecordsData;
                //记录数据
                fightRecordsData.AddCreatureKillNum(baseAttackMode.attackerId, 1);
                //流血
                EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackDirection);
            });
    }

    /// <summary>
    /// 受到攻击
    /// </summary>
    public void UnderAttack(FightUnderAttackStruct fightUnderAttackStruct, Action<int, int> actionForNoDead = null, Action<int, int> actionForDead = null)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightData = gameLogic.fightData;
        var fightRecordsData = fightData.fightRecordsData;
        //判断是否闪避
        float evaRate = fightCreatureData.GetEVA();
        float randomEVA = UnityEngine.Random.Range(0f, 1f);
        if (randomEVA <= evaRate)
        {
            //显示数字
            EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), 0, 1);
            //播放闪避音效
            AudioHandler.Instance.PlaySound(fightUnderAttackStruct.soundMissId);
            return;
        }
        //判断是否受到暴击
        float randomCRT = UnityEngine.Random.Range(0f, 1f);
        if (randomCRT <= fightUnderAttackStruct.attackerCRT)
        {
            fightUnderAttackStruct.attackerDamage = (int)(1.5f * fightUnderAttackStruct.attackerDamage);
        }
        //先扣除护甲 再扣除生命
        fightCreatureData.ChangeDRAndHP(-fightUnderAttackStruct.attackerDamage,
        out int curDR, out int curHP,
        out int changeDRReal, out int changeHPReal);
        //真实造成的伤害
        int damageReal = Mathf.Abs(changeDRReal + changeHPReal);
        //记录数据
        fightRecordsData.AddCreatureDamage(fightUnderAttackStruct.attackerId, damageReal);
        fightRecordsData.AddCreatureDamageReceived(fightUnderAttackStruct.attackedId, damageReal);

        //是否暴击
        if (randomCRT <= fightUnderAttackStruct.attackerCRT)
        {
            //显示数字
            EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), damageReal, 2);
        }
        else
        {
            //显示数字
            EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), damageReal, 0);
        }
        //播放闪避音效
        AudioHandler.Instance.PlaySound(fightUnderAttackStruct.soundHitId);

        //检测一下是否死亡
        CheckDead
        (
            //没有死亡
            actionForNoDead: () =>
            {
                actionForNoDead?.Invoke(changeDRReal, changeHPReal);
            },
            //死亡之后
            actionForDead: () =>
            {
                actionForDead?.Invoke(changeDRReal, changeHPReal);
            }
        );
    }
    #endregion

    #region  动画相关
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
    /// 抖动动画-受到攻击
    /// </summary>
    public void AnimForAnimForUnderAttackShake()
    {
        // 创建颜色渐变动画
        if (animForUnderAttackShake != null && animForUnderAttackShake.IsPlaying())
        {
            animForUnderAttackShake.Complete();
        }
        //触发被攻击特效
        if (creatureSkeletionAnimation != null)
        {
            creatureSkeletionAnimation.transform.localPosition = Vector3.zero;
            //颤抖
            animForUnderAttackShake = creatureSkeletionAnimation.transform.DOShakePosition(0.06f, strength: 0.05f, vibrato: 10, randomness: 180);
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
    #endregion

    #region  检测相关

    /// <summary>
    /// 检测死亡
    /// </summary>
    public void CheckDead(Action actionForNoDead = null, Action actionForDead = null)
    {
        //如果还有生命值 
        if (fightCreatureData.HPCurrent > 0)
        {
            actionForNoDead?.Invoke();
            //颜色变化动画
            AnimForUnderAttackColor();
            //展示血条
            creatureLifeShow?.ShowObj(true);

            //设置血条进度
            if (fightCreatureData.HPMax > 0)
            {
                creatureLifeShow?.material.SetFloat("_Progress_1", fightCreatureData.HPCurrent / (float)fightCreatureData.HPMax);
            }
            else
            {
                creatureLifeShow?.material.SetFloat("_Progress_1", 0);
            }

            //设置护盾进度
            if (fightCreatureData.DRMax > 0)
            {
                creatureLifeShow?.material.SetFloat("_Progress_2", fightCreatureData.DRCurrent / (float)fightCreatureData.DRMax);
            }
            else
            {
                creatureLifeShow?.material.SetFloat("_Progress_2", 0);
            }
        }
        else
        {
            //死掉之后设置BUFF信息为无效
            BuffHandler.Instance.SetCreatureBuffsActivieIsValid(fightCreatureData.creatureData.creatureUUId, false);
            //死亡掉落水晶
            DeadDropCrystal();
            //如果被攻击对象死亡 
            SetCreatureDead();
            //颜色变化动画
            AnimForUnderAttackColor();
            //隐藏血条
            creatureLifeShow?.ShowObj(false);
            //死亡回调触发
            actionForDead?.Invoke();
        }
    }

    /// <summary>
    /// 死亡掉落水晶
    /// </summary>
    public void DeadDropCrystal()
    {
        //只有进攻生物才掉落水晶
        if (fightCreatureData.creatureData.creatureInfo.GetCreatureType() == CreatureTypeEnum.FightAttack)
        {
            FightDropCrystalBean fightDropCrystal = new FightDropCrystalBean();
            fightDropCrystal.crystalNum = 1;
            fightDropCrystal.lifeTime = 30;
            fightDropCrystal.dropPos = creatureObj.transform.position;
            //掉落水晶
            FightHandler.Instance.CreateDropCrystal(fightDropCrystal);
            //事件通知
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadDropCrystal, fightDropCrystal);
        }
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
    #endregion

    /// <summary>
    /// 设置生物死亡
    /// </summary>
    public void SetCreatureDead()
    {
        creatureFightState = CreatureFightStateEnum.Dead;
        if (aiEntity is AIAttackCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.AttackCreatureDead);
        }
        else if (aiEntity is AIDefenseCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCreatureDead);
        }
        else if (aiEntity is AIDefenseCoreCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCoreCreatureDead);
        }
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadStart, fightCreatureData);
    }
}
