using UnityEngine;

/// <summary>
/// 进攻生物-释放技能意图（attack_mode_ext 中 trigger_scene=CastSkillIntent(1) 的技能释放载体）。
/// <para>由 NPC ai_param 的 skill_update 事件（AICreatureEntity 通用 Update 事件）到点切入，复刻攻击意图的单次出手流程：
/// 播一次攻击动作（anim_attack，按攻速换算倍速与攻击意图同口径）→ 出手点（anim_attack_time 换算后）用技能攻击模块发射
/// → 攻击结束回调回闲置（自然接回 移动/攻击 流程）。</para>
/// <para>切入前须由 SetupCastSkill 写入本次技能配置（照击退意图 SetupKnockback 的先写后切先例）；
/// 事件计时在切入时即清零——抬手期被击退/魅惑打断则本轮技能丢失，等下个周期（契约取舍：保证 Update 事件系统通用简单）。</para>
/// </summary>
public class AIIntentAttackCreatureCastSkill : AIBaseIntent
{
    #region 字段
    /// <summary>攻击动作出手时长的保底值(秒)：配置 anim_attack_time 缺省时使用，保证技能动作可见</summary>
    private const float CastTimeFallback = 0.5f;
    /// <summary>发射后等回调的兜底余量(秒)：超时未回调强制回闲置，防攻击模块异常把生物卡死在本意图</summary>
    private const float CallbackTimeoutBuffer = 3f;
    /// <summary>所属进攻生物AI实体</summary>
    private AIAttackCreatureEntity selfAIEntity;
    /// <summary>本次释放的技能配置（切入前 SetupCastSkill 写入）</summary>
    private AttackModeExtInfoBean extInfo;
    /// <summary>出手阶段已计时(秒)</summary>
    private float timeCasting;
    /// <summary>出手阶段时长(秒)：到点发射技能攻击模块（攻速换算同攻击意图）</summary>
    private float timeCastingCD;
    /// <summary>是否已发射（发射后等攻击结束回调回闲置）</summary>
    private bool hasFired;
    #endregion

    #region 意图生命周期
    /// <summary>
    /// 写入本次释放的技能配置（ChangeIntent 切入前调用）
    /// </summary>
    public void SetupCastSkill(AttackModeExtInfoBean extInfo)
    {
        this.extInfo = extInfo;
    }

    /// <summary>
    /// 进入意图：换算出手时长并播放一次攻击动作；无实体/无技能配置则直接回闲置
    /// </summary>
    public override void IntentEntering(AIBaseEntity aiEntity)
    {
        selfAIEntity = aiEntity as AIAttackCreatureEntity;
        timeCasting = 0;
        hasFired = false;
        var selfCreatureEntity = selfAIEntity?.selfCreatureEntity;
        if (selfCreatureEntity == null || extInfo == null)
        {
            selfAIEntity?.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
            return;
        }
        //出手时长：攻击动画出手时间按攻速换算（与攻击意图 RefreshData 同口径，缺省保底）
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        fightCreatureData.GetAttackTimeData(out _, out float timeAttacking, out _);
        timeCastingCD = timeAttacking > 0 ? timeAttacking : CastTimeFallback;
        //播放攻击动作（非循环）：按基础动画时长与出手CD等比例换算播放速度（同攻击意图）
        float animTimeBase = fightCreatureData.creatureData.GetAttackAnimTime();
        float animSpeed = animTimeBase > 0 && timeCastingCD > 0 ? animTimeBase / timeCastingCD : 1f;
        selfCreatureEntity.PlayAnim(SpineAnimationStateEnum.Attack, false, animSpeed: animSpeed);
    }

    /// <summary>
    /// 每帧更新：到出手点发射技能攻击模块；发射后等回调，超时兜底回闲置
    /// </summary>
    public override void IntentUpdate(AIBaseEntity aiEntity)
    {
        if (selfAIEntity == null || selfAIEntity.selfCreatureEntity == null) return;
        timeCasting += GameFightLogic.GetFightDeltaTime();
        if (!hasFired)
        {
            if (timeCasting >= timeCastingCD)
            {
                hasFired = true;
                //目标仅作攻击模块的可选实参（援护护盾这类自身施法不使用）；目标已死/缺失传 null
                var target = selfAIEntity.targetCreatureEntity;
                if (target != null && target.IsDead()) target = null;
                FightHandler.Instance.StartCreateAttackMode(selfAIEntity.selfCreatureEntity, target, ActionForCastEnd, customAttackModeId: extInfo.attack_mode_id);
            }
            return;
        }
        //兜底：回调超时未归强制回闲置
        if (timeCasting >= timeCastingCD + CallbackTimeoutBuffer)
        {
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
        }
    }

    /// <summary>
    /// 离开意图：重置计时与发射标记
    /// </summary>
    public override void IntentLeaving(AIBaseEntity aiEntity)
    {
        timeCasting = 0;
        hasFired = false;
        extInfo = null;
    }
    #endregion

    #region 回调
    /// <summary>
    /// 技能攻击结束回调：回闲置意图（Idle→Move/Attack 自然续接原行为）；被击退等外力已切走时不抢意图
    /// </summary>
    private void ActionForCastEnd(BaseAttackMode attackMode)
    {
        if (selfAIEntity != null && selfAIEntity.currentIntentEnum == AIIntentEnum.AttackCreatureCastSkill)
        {
            selfAIEntity.ChangeIntent(AIIntentEnum.AttackCreatureIdle);
        }
    }
    #endregion
}
