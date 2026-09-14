using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天降连锁-三连发发射器：本身不命中、不播特效，只在存活期内按 hit_interval 逐发发射
/// child_attack_mode_id 指向的子雷攻击模块（每发独立随机选取全场存活防守生物为落点）。
/// <para>【为什么是发射器】子雷=完整普攻雷击(800001 AttackModeFalluponChain，落雷+连锁+伤害减半)，
/// 其实体路径 StartAttack(attacker, attacked, cb) 自带完整快照管线，每发调一次
/// <see cref="FightHandler.StartCreateAttackMode(FightCreatureEntity, FightCreatureEntity, Action{BaseAttackMode}, long)"/>
/// 即重新取当时 ATK 快照——正合"0.5秒内3次普通雷击"语义。</para>
/// <para>【为何不继承 FalluponChain】其连锁循环全 private 且与发射器语义冲突；且无参 StartAttack 是废路径，不能作为子雷发射通道。</para>
/// <para>【排程】首发当帧发射并立即回调 actionForAttackEnd（保 AI 攻击循环节奏，照 AttackModeMeleeMulti 先例），
/// 余发由 Update 按战斗帧时间(GetFightDeltaTime,随倍速/暂停缩放)累计到点补发；攻击者死亡/实体池复用则取消余发。</para>
/// <para>【配置】class_name=本类 + child_attack_mode_id=800001 + other_data 键 hit_times(总发数,默认1)&amp;hit_interval(发间隔秒,默认0.1)，
/// 解析见 AttackModeInfoBeanPartial.GetMultiHitConfig()；发射器自身不配伤害/特效/音效（全由子雷行承担）。</para>
/// <para>使用者：雷大魔法师BOSS 1031040001 用 700006（ext 100007，5秒一次）。</para>
/// </summary>
public class AttackModeFalluponChainMulti : BaseAttackMode
{
    #region 字段（对象池复用安全：StartAttack 发射时重置 + Destroy 回收时清空，双保险）
    /// <summary>随机落点最大重试次数（死亡实体偶发残留时换抽，全落空则跳过本发）</summary>
    private const int RandomRetryMax = 3;
    /// <summary>剩余待发射数（0=无待发，Update 直接跳过）</summary>
    protected int launchesLeft = 0;
    /// <summary>发射间隔配置（秒）</summary>
    protected float launchInterval = 0.1f;
    /// <summary>发射间隔计时器（秒，累计 GetFightDeltaTime）</summary>
    protected float timeToNextLaunch = 0;
    /// <summary>首帧跳过标记：保证第 2 发不与首发同帧（低帧率+高倍速下单帧 dt 可能直接满间隔）</summary>
    protected bool hasPassedFirstFrame = false;
    /// <summary>缓存的攻击者（每发雷经其实体路径重新取 ATK 快照）</summary>
    protected FightCreatureEntity cachedAttacker;
    /// <summary>攻击者身份快照（实体池复用后 UUId 会变，防把复用成的新生物误判为存活攻击者继续发雷）</summary>
    protected string cachedAttackerUUId;
    /// <summary>子雷攻击模块ID（child_attack_mode_id，校验后缓存）</summary>
    protected long childAttackModeId = 0;
    #endregion

    #region 攻击发起
    /// <summary>
    /// 开始攻击-默认（防御性废路径：发射器永不走纯数据发射，立即回收防池化实例带残留状态挂场）
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物：校验子雷配置 → 当帧发射首发并立即回调攻击结束 → 余发挂起由 Update 按间隔追加
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.IsDead())
        {
            Destroy();
            return;
        }
        //子雷配置三重校验（照 AttackModeRangedSplit 先例）：未配置/缺行/子行仍是发射器（同步发射会无限递归崩进程）
        childAttackModeId = attackModeInfo.child_attack_mode_id;
        if (childAttackModeId == 0)
        {
            LogUtil.LogError($"天降连锁三连发[{attackModeInfo.id}]未配置 child_attack_mode_id，无法发射子雷");
            actionForAttackEnd?.Invoke(this);
            Destroy();
            return;
        }
        var childAttackModeInfo = AttackModeInfoCfg.GetItemData(childAttackModeId);
        if (childAttackModeInfo == null)
        {
            LogUtil.LogError($"天降连锁三连发[{attackModeInfo.id}]的 child_attack_mode_id={childAttackModeId} 在配置表中不存在");
            actionForAttackEnd?.Invoke(this);
            Destroy();
            return;
        }
        if (childAttackModeInfo.class_name == nameof(AttackModeFalluponChainMulti))
        {
            LogUtil.LogError($"天降连锁三连发[{attackModeInfo.id}]的 child_attack_mode_id={childAttackModeId} 指向的仍是发射器，会无限递归导致崩溃，已阻止发射");
            actionForAttackEnd?.Invoke(this);
            Destroy();
            return;
        }
        //读取多发配置（Bean 侧缓存解析结果）并重置发射状态（对象池复用安全）
        attackModeInfo.GetMultiHitConfig(out int hitTimes, out float interval);
        launchInterval = Mathf.Max(0f, interval);
        timeToNextLaunch = 0;
        hasPassedFirstFrame = false;
        cachedAttacker = attacker;
        cachedAttackerUUId = attacker.fightCreatureData?.creatureData?.creatureUUId;
        //首发当帧发射
        FireOnce();
        launchesLeft = Mathf.Max(1, hitTimes) - 1;
        //攻击结束回调立即触发（AI 立刻回普攻循环重新索敌，后续发静默追加）
        actionForAttackEnd?.Invoke(this);
        //无剩余发（hit_times<=1）则立即回收
        if (launchesLeft <= 0)
        {
            Destroy();
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// 驱动余发按 hit_interval 逐发发射（满间隔一发；攻击者死亡/实体被复用取消余发并回收；打满回收）
    /// </summary>
    public override void Update()
    {
        base.Update();
        if (!isValid || launchesLeft <= 0)
            return;
        //保证第 2 发不与首发同帧：首个 Update 帧只标记不计时
        if (!hasPassedFirstFrame)
        {
            hasPassedFirstFrame = true;
            return;
        }
        //攻击者死亡或实体已被对象池复用（UUId 变了）：取消余发并回收
        if (cachedAttacker == null || cachedAttacker.IsDead()
            || cachedAttacker.fightCreatureData?.creatureData?.creatureUUId != cachedAttackerUUId)
        {
            Destroy();
            return;
        }
        timeToNextLaunch += GameFightLogic.GetFightDeltaTime();
        //while 兜底：单帧跨多个间隔时（高倍速+低帧率）一次补齐
        while (launchesLeft > 0 && timeToNextLaunch >= launchInterval)
        {
            timeToNextLaunch -= launchInterval;
            launchesLeft--;
            FireOnce();
        }
        if (launchesLeft <= 0)
        {
            Destroy();
        }
    }
    #endregion

    #region 发射
    /// <summary>
    /// 发射一发子雷：独立随机选全场存活防守生物为落点，走实体路径发射（重新取 ATK 快照，发射即忘）
    /// </summary>
    private void FireOnce()
    {
        FightCreatureEntity target = GetRandomDefenseCreature();
        //搜不到目标跳过本发，剩余排程继续（照 AIIntentCreatureAttack.FireExtraShot 先例）
        if (target == null)
            return;
        FightHandler.Instance.StartCreateAttackMode(cachedAttacker, target, null, childAttackModeId);
    }
    #endregion

    #region 随机目标
    /// <summary>
    /// 从场上存活防守生物中随机取一只作为落点（不含魔王核心；最多重试 RandomRetryMax 次，全落空返回null跳过本发）
    /// <para>与 AttackModeFalluponAreaRandom.GetRandomDefenseCreature 同逻辑。</para>
    /// </summary>
    private FightCreatureEntity GetRandomDefenseCreature()
    {
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        List<FightCreatureEntity> listDefense = fightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        if (listDefense.IsNull())
        {
            return null;
        }
        int count = listDefense.Count;
        for (int i = 0; i < RandomRetryMax; i++)
        {
            var candidate = listDefense[UnityEngine.Random.Range(0, count)];
            if (candidate != null && !candidate.IsDead() && candidate.creatureObj != null)
            {
                return candidate;
            }
        }
        return null;
    }
    #endregion

    #region 清理
    /// <summary>
    /// 清理状态（对象池复用前清空发射状态与缓存引用，防残留；照 AttackModeMeleeMulti 清理先例）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        launchesLeft = 0;
        timeToNextLaunch = 0;
        hasPassedFirstFrame = false;
        cachedAttacker = null;
        cachedAttackerUUId = null;
        childAttackModeId = 0;
        base.Destroy(isPermanently);
    }
    #endregion
}
