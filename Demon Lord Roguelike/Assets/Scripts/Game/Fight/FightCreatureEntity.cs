using System;
using System.Collections.Generic;
using DG.Tweening;
using Spine;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 战斗生物实体（通用部分）
/// <para>按生物类型拆分为多个 partial 文件：</para>
/// <para>FightCreatureEntityForAttack.cs      - 进攻生物专属逻辑（换路诱导、死亡意图）</para>
/// <para>FightCreatureEntityForDefense.cs     - 防守生物专属逻辑（死亡意图）</para>
/// <para>FightCreatureEntityForDefenseCore.cs - 魔王（防守核心）专属逻辑（魔力MPShow显示、死亡意图）</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 数据
    /// <summary>
    /// 生物游戏物体
    /// </summary>
    public GameObject creatureObj;
    /// <summary>
    /// 战斗生物数据
    /// </summary>
    public FightCreatureBean fightCreatureData;
    /// <summary>
    /// AI实体
    /// </summary>
    public AIBaseEntity aiEntity;
    /// <summary>
    /// 生物战斗状态
    /// </summary>
    public CreatureFightStateEnum creatureFightState = CreatureFightStateEnum.None;
    /// <summary>
    /// spine骨骼动画
    /// </summary>
    public SkeletonAnimation creatureSkeletionAnimation;
    /// <summary>
    /// 生命条显示
    /// </summary>
    public SpriteRenderer creatureLifeShow;

    /// <summary>
    /// 动画-受到攻击颜色渐变
    /// </summary>
    Tween animForUnderAttackColor;
    /// <summary>
    /// 动画-受到攻击抖动
    /// </summary>
    Tween animForUnderAttackShake;
    #endregion

    #region 生命周期
    /// <summary>
    /// 构造函数
    /// </summary>
    public FightCreatureEntity(GameObject creatureObj, FightCreatureBean fightCreatureData)
    {
        SetData(creatureObj, fightCreatureData);
    }

    /// <summary>
    /// 设置数据（通用初始化 + 各类型专属初始化）
    /// </summary>
    public void SetData(GameObject creatureObj, FightCreatureBean fightCreatureData)
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
        //魔王（防守核心）专属初始化（MPShow魔力显示挂接 非核心生物预制下无该节点自动跳过）
        SetDataForDefenseCore();
        //设置spine
        CreatureHandler.Instance.SetCreatureData(creatureSkeletionAnimation, fightCreatureData.creatureData, isSetSkeletonDataAsset: false);
        //动画播放速度跟随当前游戏速度（2倍速开启期间新建的生物也要以2倍播放）
        SetAnimTimeScale(GameFightLogic.GetCurrentGameSpeed());
        //设置身体颜色
        RefreshBodyColor();
    }

    /// <summary>
    /// 设置动画播放速度（跟随游戏速度，2倍速时全动画2倍播放；与各 PlayAnim 的 animSpeed 相乘叠加）
    /// </summary>
    /// <param name="timeScale">动画时间倍率（1=原速, 2=2倍速）</param>
    public void SetAnimTimeScale(float timeScale)
    {
        if (creatureSkeletionAnimation != null)
        {
            creatureSkeletionAnimation.timeScale = timeScale;
        }
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
    #endregion

    #region  身体相关
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

    /// <summary>
    /// 刷新身体颜色
    /// </summary>
    public void RefreshBodyColor()
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
        Vector3 curScale = creatureSkeletionAnimation.transform.localScale;
        float size = Mathf.Abs(curScale.x);
        float targetX = direction2DEnum == Direction2DEnum.Left ? -size : size;
        //朝向未变则跳过：避免每次攻击循环重复写 localScale（防守生物可能每次出手都调用）
        if (curScale.x == targetX)
            return;
        curScale.x = targetX;
        creatureSkeletionAnimation.transform.localScale = curScale;
    }
    #endregion

    #region 状态相关
    /// <summary>
    /// 增加BUFF
    /// </summary>
    public void AddBuff(BaseAttackMode baseAttackMode)
    {
        //触发buff
        var listBuffData = baseAttackMode.attackModeInfo.GetListBuff();
        if (!listBuffData.IsNull())
        {
            bool isAdd = BuffHandler.Instance.AddFightCreatureBuff(listBuffData, baseAttackMode.attackModeData.attackerId, fightCreatureData.creatureData.creatureUUId);
            if (isAdd)
            {
                //刷新一下身体颜色
                RefreshBodyColor();
            }
        }
    }

    /// <summary>
    /// 回复HP
    /// </summary>
    public void RegainHP(BaseAttackMode baseAttackMode)
    {
        RegainHP(baseAttackMode.attackModeData.attackerId, fightCreatureData.creatureData.creatureUUId, baseAttackMode.attackModeData.attackerDamage,
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
    public void RegainHP(string attackerId, string attackedId, int hpChangeData,
        Action<int> actionForNoDead = null, Action<int> actionForDead = null)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;
        fightCreatureData.ChangeHP(hpChangeData, out int curHP, out int changeHPReal);

        //记录数据
        fightRecordsData.AddCreatureRegainHP(attackerId, changeHPReal);
        fightRecordsData.AddCreatureRegainHPReceived(attackedId, changeHPReal);

        //回复HP事件：仅真实回血(截断后>0)时派发，供治疗类前置条件BUFF累积(借用FightUnderAttackBean承载)
        if (changeHPReal > 0)
        {
            FightUnderAttackBean regainData = new FightUnderAttackBean();
            regainData.attackerId = attackerId;
            regainData.attackedId = attackedId;
            regainData.attackerDamage = changeHPReal;
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_RegainHP, regainData);
        }

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
        RegainDR(baseAttackMode.attackModeData.attackerId, fightCreatureData.creatureData.creatureUUId, baseAttackMode.attackModeData.attackerDamage,
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
    /// 回复护甲
    /// </summary>
    public void RegainDR(string attackerId, string attackedId, int drChangeData,
        Action<int> actionForNoDead = null, Action<int> actionForDead = null)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightRecordsData = gameLogic.fightData.fightRecordsData;
        fightCreatureData.ChangeDR(drChangeData, out int leftDR, out int changeDRReal);

        //记录数据
        fightRecordsData.AddCreatureRegainDR(attackerId, changeDRReal);
        fightRecordsData.AddCreatureRegainDRReceived(attackedId, changeDRReal);
        //检测一下是否死亡
        CheckDead
        (
            //没有死亡
            actionForNoDead: () =>
            {
                actionForNoDead?.Invoke(changeDRReal);
            },
            //死亡之后
            actionForDead: () =>
            {
                actionForDead?.Invoke(changeDRReal);
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
        FightUnderAttackBean fightUnderAttackData = FightHandler.Instance.GetFightUnderAttackData(baseAttackMode, fightCreatureData.creatureData.creatureUUId);
        UnderAttack(fightUnderAttackData,
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
                        //护甲：特效位置偏移读 CreatureInfo.shield_effect_position(空则默认 0,0.5,0)
                        EffectHandler.Instance.ShowShieldHitEffect(creatureObj.transform.position + fightCreatureData.creatureData.creatureInfo.GetShieldEffectPosition(), baseAttackMode.attackModeData.attackDirection);

                    }
                    //如果是打击到护甲
                    else
                    {
                        //流血
                        EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackModeData.attackDirection);
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
                fightRecordsData.AddCreatureKillNum(baseAttackMode.attackModeData.attackerId, 1);
                //流血
                EffectHandler.Instance.ShowBloodEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), baseAttackMode.attackModeData.attackDirection);
            });
    }

    /// <summary>
    /// 受到攻击
    /// </summary>
    public void UnderAttack(FightUnderAttackBean fightUnderAttackData, Action<int, int> actionForNoDead = null, Action<int, int> actionForDead = null)
    {
        var gameLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightData = gameLogic.fightData;
        var fightRecordsData = fightData.fightRecordsData;
        //判断是否闪避
        float evaRate = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.EVA);
        float randomEVA = UnityEngine.Random.Range(0f, 1f);
        if (randomEVA <= evaRate)
        {
            //显示数字
            EffectHandler.Instance.ShowTextNumEffect(creatureObj.transform.position + new Vector3(0, 0.5f, 0), 0, 1);
            //播放闪避音效
            AudioHandler.Instance.PlaySound(fightUnderAttackData.soundMissId);
            return;
        }
        //判断是否受到暴击
        float randomCRT = UnityEngine.Random.Range(0f, 1f);
        if (randomCRT <= fightUnderAttackData.attackerCRT)
        {
            fightUnderAttackData.attackerDamage = (int)(1.5f * fightUnderAttackData.attackerDamage);
        }
        //先扣除护甲 再扣除生命
        fightCreatureData.ChangeDRAndHP(-fightUnderAttackData.attackerDamage,
        out int curDR, out int curHP,
        out int changeDRReal, out int changeHPReal);
        //真实造成的伤害
        int damageReal = Mathf.Abs(changeDRReal + changeHPReal);
        //记录数据
        fightRecordsData.AddCreatureDamage(fightUnderAttackData.attackerId, damageReal);
        fightRecordsData.AddCreatureDamageReceived(fightUnderAttackData.attackedId, damageReal);

        //是否暴击
        if (randomCRT <= fightUnderAttackData.attackerCRT)
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
        AudioHandler.Instance.PlaySound(fightUnderAttackData.soundHitId);

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
                EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnderAttack_Dead, fightUnderAttackData);
            }
        );
        //事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_UnderAttack, fightUnderAttackData);
        //攻击结束 回收攻击数据
        FightHandler.Instance.RemoveFightUnderAttackData(fightUnderAttackData);
    }
    #endregion

    #region  动画相关
    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="animationCreatureState"></param>
    public TrackEntry PlayAnim(SpineAnimationStateEnum animationCreatureState, bool isLoop, float mixDuration = -1, float animSpeed = 1)
    {
        var trackEntry = SpineHandler.Instance.PlayAnim(creatureSkeletionAnimation, animationCreatureState, fightCreatureData.creatureData, isLoop, mixDuration: mixDuration, animSpeed: animSpeed);
        return trackEntry;
    }

    /// <summary>
    /// 增加动画
    /// </summary>
    public TrackEntry AddAnim(int trackIndex, SpineAnimationStateEnum animationCreatureState, bool isLoop, float delay, string animNameAppoint = null, float animSpeed = 1)
    {
        if (creatureSkeletionAnimation == null)
            return null;
        var animData = SpineHandler.Instance.AddAnimation(creatureSkeletionAnimation, trackIndex, animationCreatureState, isLoop, delay, animNameAppoint, animSpeed: animSpeed);
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
        //creatureSkeletionAnimation.AnimationState.ClearTracks();;
        var trackEntry = PlayAnim(SpineAnimationStateEnum.Idle, true, 0);
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
                RefreshBodyColor();
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
            float HPMax = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
            if (HPMax > 0)
            {
                creatureLifeShow?.material.SetFloat("_Progress_1", fightCreatureData.HPCurrent / HPMax);
            }
            else
            {
                creatureLifeShow?.material.SetFloat("_Progress_1", 0);
            }
            float DRMax = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.DR);
            //设置护盾进度
            if (DRMax > 0)
            {
                creatureLifeShow?.material.SetFloat("_Progress_2", fightCreatureData.DRCurrent / DRMax);
            }
            else
            {
                creatureLifeShow?.material.SetFloat("_Progress_2", 0);
            }
        }
        else
        {
            //如果被攻击对象死亡
            SetCreatureDead();
            //死亡回调触发
            actionForDead?.Invoke();
        }
    }


    /// <summary>
    /// 掉落水晶（跨类型共用 按state过滤生物类型）
    /// </summary>
    /// <param name="state">0:所有生物掉落水晶 1:只有进攻生物才掉落水晶 2只有防守生物才掉落水晶</param>
    public void DropCrystal(int state)
    {
        Action actionForDrop = () =>
        {
            int dropCrystal = 1;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            if (gameFightLogic.fightData.gameFightType == GameFightTypeEnum.Conquer)
            {
                FightBeanForConquer conquerFightData = gameFightLogic.fightData as FightBeanForConquer;
                dropCrystal = conquerFightData.fightTypeConquerInfo.drop_crystal;
            }

            FightDropCrystalBean fightDropCrystal = FightHandler.Instance.manager.GetFightDropCrystalBean(dropCrystal, creatureObj.transform.position);
            //存在时长 = 基础时长 + 研究加成(魔晶掉落时长 每级+5秒)；显式赋值避免对象池复用残留的旧时长
            var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
            fightDropCrystal.lifeTime = FightDropCrystalBean.BASE_LIFE_TIME + userUnlock.GetUnlockDropCrystalAddLifeTime();
            //标记掉落者 BUFF事件回调按此过滤 区分真实生物掉落与BUFF追加掉落
            fightDropCrystal.dropperCreatureUUId = fightCreatureData?.creatureData?.creatureUUId;
            //掉落水晶
            FightHandler.Instance.CreateDropCrystal(fightDropCrystal);
            //事件通知
            EventHandler.Instance.TriggerEvent(EventsInfo.GameFightLogic_CreatureDeadDropCrystal, fightDropCrystal);
        };
        //只有进攻生物才掉落水晶
        if (state == 0)
        {
            actionForDrop?.Invoke();
        }
        else if (state == 1 && fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightAttack)
        {
            actionForDrop?.Invoke();
        }
        else if (state == 2 && fightCreatureData.creatureFightType == CreatureFightTypeEnum.FightDefense)
        {
            actionForDrop?.Invoke();
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

    #region 死亡相关
    /// <summary>
    /// 设置生物死亡（通用处理 + 分发各类型的死亡意图切换）
    /// </summary>
    public void SetCreatureDead()
    {
        //死亡掉落水晶
        DropCrystal(1);
        //颜色变化动画
        AnimForUnderAttackColor();
        //隐藏血条
        creatureLifeShow?.ShowObj(false);

        creatureFightState = CreatureFightStateEnum.Dead;
        //各类型死亡意图切换（在各自的partial文件中处理 aiEntity只会命中其中一种类型）
        SetCreatureDeadForAttack();
        SetCreatureDeadForDefense();
        SetCreatureDeadForDefenseCore();
    }
    #endregion
}
