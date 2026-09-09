using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 近战多段攻击（一次攻击造成 N 段伤害，段间间隔由配置驱动、不同帧结算）
/// <para>第 1 段随 StartAttack 当帧结算并立即回调 actionForAttackEnd（保持 AI 攻击循环节奏与单段近战一致），
/// 剩余段由 Update 按 hit_interval 累计战斗帧时间(GetFightDeltaTime,随游戏倍速/暂停缩放)逐段静默追加，打满或目标死亡即回收。</para>
/// <para>每段独立走 UnderAttack 管线：暴击/闪避/无敌各自独立掷骰，段间沿用 StartAttack 时写入的伤害快照(不随攻击者属性变化)。</para>
/// <para>配置：other_data 键 hit_times(总段数,默认1=退化为单段)&amp;hit_interval(段间隔秒,默认0.1)，解析见 AttackModeInfoBeanPartial.GetMultiHitConfig()。</para>
/// <para>使用者：难度4进攻敌人 盗贼104001 用 100002(配 hit_times:2&amp;hit_interval:0.1)。</para>
/// </summary>
public class AttackModeMeleeMulti : AttackModeMelee
{
    #region 多段状态（对象池复用安全：OnMeleeStartAttack 发射时重置 + Destroy 回收时清空，双保险）
    /// <summary>剩余待结算段数（0=无待结算段，Update 直接跳过）</summary>
    protected int hitTimesLeft = 0;
    /// <summary>段间隔配置（秒）</summary>
    protected float hitInterval = 0.1f;
    /// <summary>段间隔计时器（秒，累计 GetFightDeltaTime）</summary>
    protected float timeToNextHit = 0;
    /// <summary>首帧跳过标记：保证第 2 段不与第 1 段同帧（低帧率+高倍速下单帧 dt 可能直接满间隔）</summary>
    protected bool hasPassedFirstFrame = false;
    /// <summary>缓存的被攻击目标（段间隔期间持续命中同一目标）</summary>
    protected FightCreatureEntity cachedAttacked;
    /// <summary>缓存的攻击者（仅用于命中特效定位；伤害快照在 attackModeData 中，与其存活无关）</summary>
    protected FightCreatureEntity cachedAttacker;
    #endregion

    #region 攻击发起
    /// <summary>
    /// 近战出手结算：第 1 段立即命中 + 立即回调攻击结束，剩余段挂起由 Update 按间隔追加
    /// </summary>
    protected override void OnMeleeStartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        //读取多段配置（Bean 侧缓存解析结果）并重置多段状态（对象池复用安全）
        attackModeInfo.GetMultiHitConfig(out int hitTimes, out float interval);
        hitInterval = Mathf.Max(0f, interval);
        hitTimesLeft = Mathf.Max(1, hitTimes);
        timeToNextHit = 0;
        hasPassedFirstFrame = false;
        cachedAttacked = attacked;
        cachedAttacker = attacker;
        //第 1 段当帧结算
        MeleeHit(attacker, attacked);
        hitTimesLeft--;
        //攻击结束回调立即触发（与单段近战同节奏：AI 立刻重新索敌回准备阶段，后续段静默追加）
        actionForAttackEnd?.Invoke(this);
        //无剩余段（hit_times<=1）则与父类一致立即回收
        if (hitTimesLeft <= 0)
        {
            Destroy();
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// 驱动剩余段按 hit_interval 逐段结算（满间隔一段；目标死亡/失效放弃剩余段并回收；打满回收）
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (!isValid || hitTimesLeft <= 0)
            return;
        //保证第 2 段不与第 1 段同帧：首个 Update 帧只标记不计时
        if (!hasPassedFirstFrame)
        {
            hasPassedFirstFrame = true;
            return;
        }
        timeToNextHit += GameFightLogic.GetFightDeltaTime();
        if (timeToNextHit < hitInterval)
            return;
        timeToNextHit -= hitInterval;
        //目标在段间隔期间死亡/失效：放弃剩余段并回收
        if (cachedAttacked == null || cachedAttacked.IsDead())
        {
            Destroy();
            return;
        }
        MeleeHit(cachedAttacker, cachedAttacked);
        hitTimesLeft--;
        if (hitTimesLeft <= 0)
        {
            Destroy();
        }
    }
    #endregion

    #region 清理
    /// <summary>
    /// 清理状态（对象池复用前清空多段状态与缓存目标，防残留；照 AttackModeInstantArea 清 filterCreatureIds 先例）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        hitTimesLeft = 0;
        timeToNextHit = 0;
        hasPassedFirstFrame = false;
        cachedAttacked = null;
        cachedAttacker = null;
        base.Destroy(isPermanently);
    }
    #endregion
}
