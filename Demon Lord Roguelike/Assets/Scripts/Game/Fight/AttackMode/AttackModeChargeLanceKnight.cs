using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 长枪骑士 BOSS 技能-沿路冲锋：从当前位置沿本路向 -X 冲到道路左边缘 x=0.5，再抛物线跳回冲锋前位置。
/// <para>冲锋期间按「上一帧 x ~ 本帧 x」区间扫描本路防守生物，每个目标只结算一次 1 倍 ATK 伤害；
/// 由 attack_mode_ext 的 trigger_scene=1（ai_param skill_update）驱动，走 CastSkill 意图。</para>
/// <para>本类会移动施法者本体（非弹道），因此每帧校验施法者存活/UUID/当前意图，失配立即中止，避免与击退/魅惑/死亡抢位置。</para>
/// </summary>
public class AttackModeChargeLanceKnight : BaseAttackMode
{
    #region 常量
    /// <summary>道路左边缘 X（与击退/矿车/AI 切核心阈值同口径）</summary>
    private const float RoadMinX = 0.5f;
    /// <summary>冲锋时长上限(秒)：与跳回合计须小于 CastSkill 的 3 秒回调兜底</summary>
    private const float MaxChargeDuration = 1.8f;
    /// <summary>冲锋阶段走路动画倍速</summary>
    private const float ChargeAnimSpeed = 2f;
    /// <summary>浮点比较容差</summary>
    private const float PositionEpsilon = 0.001f;
    /// <summary>跳回时长缺省值(秒)</summary>
    private const float DefaultJumpDuration = 0.6f;
    /// <summary>跳回弧高缺省值(格)</summary>
    private const float DefaultJumpHeight = 1.5f;
    /// <summary>命中特效相对目标根节点的 Y 偏移</summary>
    private const float HitEffectOffsetY = 0.5f;
    #endregion

    #region 阶段与运行态
    /// <summary>冲锋阶段</summary>
    private enum ChargePhase
    {
        None = 0,
        Charging = 1,
        Jumping = 2,
    }

    /// <summary>当前阶段</summary>
    private ChargePhase phase = ChargePhase.None;
    /// <summary>本次冲锋已命中目标 UUID（每发清空，保证每个目标只受一次伤害）</summary>
    private readonly HashSet<string> setHitCreatureId = new HashSet<string>();
    /// <summary>施法者实体（对象池复用需配合 UUID 校验）</summary>
    private FightCreatureEntity attackerEntity;
    /// <summary>施法者根节点 Transform</summary>
    private Transform attackerTransform;
    /// <summary>施法者 UUID 快照（防实体被对象池复用后误移动新生物）</summary>
    private string attackerUUId;
    /// <summary>施法者所在道路索引（冲锋期间 Z 保持不变）</summary>
    private int roadIndex;
    /// <summary>冲锋前精确位置（跳回终点）</summary>
    private Vector3 originalPos;
    /// <summary>跳回起点（冲锋终点）</summary>
    private Vector3 jumpStartPos;
    /// <summary>跳回已计时(秒)</summary>
    private float jumpElapsed;
    /// <summary>跳回总时长(秒)</summary>
    private float jumpDuration = DefaultJumpDuration;
    /// <summary>跳回弧高(格)</summary>
    private float jumpHeight = DefaultJumpHeight;
    /// <summary>冲锋速度(格/秒)</summary>
    private float chargeSpeed;
    /// <summary>冲锋风特效配置 id（0=不播放）</summary>
    private long chargeFxId;
    /// <summary>跳回落地特效配置 id（0=不播放）</summary>
    private long landFxId;
    /// <summary>冲锋期间跟随施法者的风特效实例（独立实例，支持多 BOSS 同屏）</summary>
    private EffectBase chargeFxEffect;
    /// <summary>攻击结束回调（释放技能意图等待它回 Idle）</summary>
    private Action<BaseAttackMode> actionForAttackEnd;
    #endregion

    #region 攻击生命周期
    /// <summary>
    /// 无参路径（纯数据发射）：无施法者实体，不执行冲锋，直接回收
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击：缓存施法者/原位/道路与速度，播走路动画并进入冲锋阶段
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        this.actionForAttackEnd = actionForAttackEnd;
        ResetRuntimeState();
        if (attacker == null || attacker.IsDead() || attacker.creatureObj == null || attacker.fightCreatureData?.creatureData == null)
        {
            //施法者无效：立即回调让释放技能意图回 Idle，不占用 3 秒兜底
            Finish(true);
            return;
        }
        attackerEntity = attacker;
        attackerTransform = attacker.creatureObj.transform;
        attackerUUId = attacker.fightCreatureData.creatureData.creatureUUId;
        roadIndex = attacker.fightCreatureData.roadIndex;
        originalPos = attackerTransform.position;
        //冲锋固定朝左（敌人向 -X 进攻），保证受击方向与命中特效朝向正确
        attackModeData.attackDirection = Vector3.left;
        attackModeInfo.GetLanceChargeConfig(out jumpDuration, out jumpHeight, out chargeFxId, out landFxId);
        float distance = originalPos.x - RoadMinX;
        if (distance <= PositionEpsilon)
        {
            //已贴道路左缘：不反向冲锋、不原地跳，直接结束
            Finish(true);
            return;
        }
        chargeSpeed = GetMoveSpeed();
        //速度异常/过低时按最长冲锋时长反算，保证「冲锋 + 跳回」小于 CastSkill 的 3 秒兜底
        if (chargeSpeed <= 0f || distance / chargeSpeed > MaxChargeDuration)
            chargeSpeed = distance / MaxChargeDuration;
        phase = ChargePhase.Charging;
        attackerEntity.SetFaceDirection(Direction2DEnum.Left);
        attackerEntity.PlayAnim(SpineAnimationStateEnum.Walk, true, animSpeed: ChargeAnimSpeed);
        PlayChargeFx();
    }

    /// <summary>
    /// 每帧更新：校验施法者有效性后推进冲锋或跳回阶段
    /// </summary>
    public override void Update()
    {
        if (!isValid || phase == ChargePhase.None)
            return;
        if (!ValidateCaster())
        {
            Abort();
            return;
        }
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        //0 倍速/暂停时完全冻结
        if (deltaTime <= 0f)
            return;
        if (phase == ChargePhase.Charging)
            UpdateCharge(deltaTime);
        else if (phase == ChargePhase.Jumping)
            UpdateJump(deltaTime);
    }

    /// <summary>
    /// 销毁：清空运行态并回收（对象池复用时不得残留上一发的目标集合与施法者引用）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        StopChargeFx();
        phase = ChargePhase.None;
        actionForAttackEnd = null;
        attackerEntity = null;
        attackerTransform = null;
        attackerUUId = null;
        chargeFxId = 0;
        landFxId = 0;
        setHitCreatureId.Clear();
        base.Destroy(isPermanently);
    }
    #endregion

    #region 冲锋阶段
    /// <summary>
    /// 冲锋推进：先按「上一帧 x ~ 本帧 x」区间结算命中，再移动到本帧位置；到达左缘切跳回阶段
    /// </summary>
    private void UpdateCharge(float deltaTime)
    {
        float prevX = attackerTransform.position.x;
        float newX = Mathf.Max(RoadMinX, prevX - chargeSpeed * deltaTime);
        //先按本帧跨越区间命中，防高速/低帧率下一帧跨多个单位漏判
        HitTargetsInInterval(newX, prevX);
        attackerTransform.position = new Vector3(newX, originalPos.y, originalPos.z);
        //风特效跟随施法者当前位置，已发射的风线留在世界空间形成拖尾
        UpdateChargeFxPosition();
        if (newX <= RoadMinX + PositionEpsilon)
        {
            StopChargeFx();
            phase = ChargePhase.Jumping;
            jumpStartPos = attackerTransform.position;
            jumpElapsed = 0f;
        }
    }

    /// <summary>
    /// 结算本路位于 [minX, maxX] 区间的所有防守生物（每个目标只结算一次）
    /// </summary>
    private void HitTargetsInInterval(float minX, float maxX)
    {
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameFightLogic == null || gameFightLogic.fightData == null)
            return;
        List<FightCreatureEntity> listTarget = gameFightLogic.fightData.GetDefenseCreatureByRoad(roadIndex);
        for (int i = 0; i < listTarget.Count; i++)
        {
            var target = listTarget[i];
            if (target == null || target.IsDead() || target.creatureObj == null)
                continue;
            var targetData = target.fightCreatureData?.creatureData;
            if (targetData == null)
                continue;
            string targetUUId = targetData.creatureUUId;
            if (targetUUId.IsNull() || setHitCreatureId.Contains(targetUUId))
                continue;
            Vector3 targetPos = target.creatureObj.transform.position;
            if (targetPos.x < minX - PositionEpsilon || targetPos.x > maxX + PositionEpsilon)
                continue;
            setHitCreatureId.Add(targetUUId);
            target.UnderAttack(this);
            PlayEffectForHit(targetPos + new Vector3(0f, HitEffectOffsetY, 0f));
        }
    }
    #endregion

    #region 跳回阶段
    /// <summary>
    /// 跳回推进：x/z 线性插值回原位，y 叠加抛物线弧（与 FightDropCrystalInstanceRenderer.EvalJumpPosition 同式）
    /// </summary>
    private void UpdateJump(float deltaTime)
    {
        jumpElapsed += deltaTime;
        float t = jumpDuration > 0f ? Mathf.Clamp01(jumpElapsed / jumpDuration) : 1f;
        Vector3 pos = Vector3.Lerp(jumpStartPos, originalPos, t);
        pos.y += jumpHeight * 4f * t * (1f - t);
        attackerTransform.position = pos;
        if (t >= 1f)
        {
            //精确回原位，避免浮点误差累积
            attackerTransform.position = originalPos;
            PlayLandFx();
            Finish(true);
        }
    }
    #endregion

    #region 冲锋/落地特效
    /// <summary>
    /// 播放冲锋风特效：创建独立实例并开始跟随施法者（World 模拟，粒子留在身后形成风线拖尾）
    /// </summary>
    private void PlayChargeFx()
    {
        if (chargeFxId == 0)
            return;
        PlayIndependentEffect(chargeFxId, attackerTransform.position, 0f, (effect) =>
        {
            //回调期间冲锋可能已结束：直接回收，避免残留实例
            if (!isValid || phase != ChargePhase.Charging || attackerTransform == null)
            {
                EffectHandler.Instance.manager.DestoryEffect(effect);
                return;
            }
            chargeFxEffect = effect;
            effect.transform.position = attackerTransform.position;
            //World 模拟：发射器随施法者移动，已发射的风线留在世界空间形成拖尾
            var particleSystem = effect.GetParticleSystem();
            if (particleSystem != null)
            {
                var mainModule = particleSystem.main;
                mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            }
        });
    }

    /// <summary>
    /// 每帧同步冲锋风特效到施法者当前位置
    /// </summary>
    private void UpdateChargeFxPosition()
    {
        if (chargeFxEffect != null && attackerTransform != null)
            chargeFxEffect.transform.position = attackerTransform.position;
    }

    /// <summary>
    /// 停止冲锋风特效并回池（不销毁实例，供后续复用）
    /// </summary>
    private void StopChargeFx()
    {
        var effect = chargeFxEffect;
        chargeFxEffect = null;
        if (effect == null)
            return;
        effect.StopEffect();
        EffectHandler.Instance.manager.DestoryEffect(effect);
    }

    /// <summary>
    /// 播放跳回落地特效（一次性独立实例，show_time 到期自动销毁）
    /// </summary>
    private void PlayLandFx()
    {
        if (landFxId == 0)
            return;
        PlayIndependentEffect(landFxId, originalPos, 1f, null);
    }

    /// <summary>
    /// 播放独立粒子实例（不进持久单例字典，支持多个长枪骑士同时冲锋/落地）
    /// </summary>
    /// <param name="effectId">EffectInfo 配置 id</param>
    /// <param name="position">世界坐标</param>
    /// <param name="showTime">自动销毁时长(秒，0=不自动销毁，由调用方回收)</param>
    /// <param name="actionForGet">取到实例后的回调(播放前调用)</param>
    private void PlayIndependentEffect(long effectId, Vector3 position, float showTime, Action<EffectBase> actionForGet)
    {
        var effectInfo = EffectInfoCfg.GetItemData(effectId);
        if (effectInfo == null)
        {
            LogUtil.LogError($"长枪骑士冲锋特效配置错误：EffectInfo[{effectId}] 不存在");
            return;
        }
        EffectBean effectData = new EffectBean();
        effectData.effectName = effectInfo.res_name;
        effectData.effectType = EffectTypeEnum.Visual;
        effectData.effectShowType = EffectShowTypeEnum.Once;
        effectData.effectPosition = position;
        effectData.isPlayInShow = false;
        effectData.isDestoryPlayEnd = showTime > 0f;
        effectData.timeForShow = showTime;
        EffectHandler.Instance.ShowEffect(effectData, (effect) =>
        {
            if (effect == null)
                return;
            actionForGet?.Invoke(effect);
            effect.PlayEffect();
        });
    }
    #endregion

    #region 校验与收尾
    /// <summary>
    /// 校验施法者仍是原生物且仍处于释放技能意图（死亡/击退/魅惑等会切走意图）
    /// </summary>
    private bool ValidateCaster()
    {
        if (!CheckTargetEntityValid(attackerEntity, attackerUUId))
            return false;
        return attackerEntity.aiEntity != null
            && attackerEntity.aiEntity.currentIntentEnum == AIIntentEnum.AttackCreatureCastSkill;
    }

    /// <summary>
    /// 中止冲锋：跳跃中被打断时只把 y 拉回地面（x/z 交给击退/魅惑/死亡意图），不回调
    /// </summary>
    private void Abort()
    {
        if (phase == ChargePhase.Jumping && attackerTransform != null)
        {
            Vector3 pos = attackerTransform.position;
            pos.y = originalPos.y;
            attackerTransform.position = pos;
        }
        Finish(false);
    }

    /// <summary>
    /// 收尾：幂等清空运行态并回收；invokeCallback=true 时通知释放技能意图回 Idle
    /// </summary>
    private void Finish(bool invokeCallback)
    {
        if (!isValid)
            return;
        var callback = actionForAttackEnd;
        actionForAttackEnd = null;
        phase = ChargePhase.None;
        attackerEntity = null;
        attackerTransform = null;
        attackerUUId = null;
        setHitCreatureId.Clear();
        Destroy();
        if (invokeCallback)
            callback?.Invoke(this);
    }

    /// <summary>
    /// 重置本发运行态（对象池复用时调用）
    /// </summary>
    private void ResetRuntimeState()
    {
        phase = ChargePhase.None;
        setHitCreatureId.Clear();
        attackerEntity = null;
        attackerTransform = null;
        attackerUUId = null;
        roadIndex = 0;
        originalPos = Vector3.zero;
        jumpStartPos = Vector3.zero;
        jumpElapsed = 0f;
        jumpDuration = DefaultJumpDuration;
        jumpHeight = DefaultJumpHeight;
        chargeSpeed = 0f;
        chargeFxId = 0;
        landFxId = 0;
        chargeFxEffect = null;
    }
    #endregion
}
