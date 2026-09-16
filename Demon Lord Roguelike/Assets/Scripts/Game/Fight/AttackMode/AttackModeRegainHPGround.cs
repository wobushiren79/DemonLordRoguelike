using System;
using UnityEngine;

/// <summary>
/// 地面持续治疗区域（神官BOSS技能 500007，经 attack_mode_ext trigger_scene=1[释放技能意图] 挂载，由 NPC ai_param 的 skill_update 事件驱动「释放技能」意图在出手点发射）。
/// <para>施法瞬间以自身位置为圆心生成半径 collider_area_size[0](=2) 的治疗区域，持续5秒：每1秒对区域内同阵营存活且 HP不满 友军跳一次 RegainHP
/// （治疗量=施法瞬间 ATK×damage_add_rate(0.1) 快照，与施法者后续存活/位置无关，照地面火焰 AttackModeRangedArcGround 先例）。</para>
/// <para>区域视觉=effect_hit 粒子走 ShowEffect 一次性独立实例通道（配置 show_time=5 到期框架自动销毁；多施法者同场不互顶，不用全局单例通道）。</para>
/// <para>发射当帧即回调结束（生物继续行动，区域自生自灭）；满5秒自毁；战斗结束由 FightManager.ClearAttackModePrefab 永久清理。</para>
/// </summary>
public class AttackModeRegainHPGround : BaseAttackMode
{
    #region 常量
    /// <summary>区域持续时长（秒）</summary>
    private const float AreaDuration = 5f;
    /// <summary>回血间隔（秒）</summary>
    private const float TickInterval = 1f;
    #endregion

    #region 字段
    /// <summary>区域圆心（施法瞬间攻击者位置快照）</summary>
    private Vector3 centerPos;
    /// <summary>施法者阵营快照（区域存续期间按它找友军，与施法者后续状态无关）</summary>
    private CreatureFightTypeEnum allyFightType = CreatureFightTypeEnum.None;
    /// <summary>区域半径（配置 collider_area_size[0]，默认2）</summary>
    private float areaRadius = 2f;
    /// <summary>区域已持续时长（按 GetFightDeltaTime 累积，跟随2倍速）</summary>
    private float areaTime;
    /// <summary>回血计时器（每满 TickInterval 跳一次）</summary>
    private float tickTimer;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击-生物：快照圆心/阵营/半径 → 播区域粒子 → 立即回调（区域由 Update 自持，满5秒自毁）；攻击者无效则直接回收
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker == null || attacker.creatureObj == null)
        {
            //无有效施法者：不生成区域，直接回收
            Destroy();
            actionForAttackEnd?.Invoke(this);
            return;
        }
        centerPos = attacker.creatureObj.transform.position;
        allyFightType = attacker.fightCreatureData.creatureFightType;
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        if (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0)
            areaRadius = arrAreaSize[0];
        //区域视觉：一次性独立实例通道（show_time=5 由配置驱动自动销毁），多施法者同场不互顶
        long effectId = attackModeInfo.GetEffectHitId(0);
        if (effectId != 0)
            EffectHandler.Instance.ShowEffect(effectId, centerPos);
        //发射当帧即回调：生物回闲置继续行动，区域自生自灭
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：按 GetFightDeltaTime 累积时长，每满 TickInterval 对半径内同阵营存活且 HP不满 友军跳一次回血；满 AreaDuration 自毁
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        areaTime += deltaTime;
        tickTimer += deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer = 0;
            RegainHPForTick();
        }
        if (areaTime >= AreaDuration)
        {
            Destroy();
        }
    }

    /// <summary>
    /// 跳一次回血：遍历同阵营存活友军，距圆心≤半径且 HP不满 者 RegainHP（治疗量=施法快照 attackModeData.attackerDamage）
    /// </summary>
    private void RegainHPForTick()
    {
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var listAlly = allyFightType == CreatureFightTypeEnum.FightAttack
            ? gameFightLogic?.fightData?.dlAttackCreatureEntity?.List
            : allyFightType == CreatureFightTypeEnum.FightDefense
                ? gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List
                : null;
        if (listAlly.IsNull())
            return;
        float sqrRadius = areaRadius * areaRadius;
        for (int i = 0; i < listAlly.Count; i++)
        {
            var itemTarget = listAlly[i];
            if (itemTarget == null || itemTarget.IsDead() || itemTarget.creatureObj == null)
                continue;
            var fightData = itemTarget.fightCreatureData;
            if (fightData == null || fightData.HPCurrent >= fightData.GetAttribute(CreatureAttributeTypeEnum.HP))
                continue;
            if ((itemTarget.creatureObj.transform.position - centerPos).sqrMagnitude > sqrRadius)
                continue;
            itemTarget.RegainHP(this);
        }
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清零计时与阵营快照（防对象池复用残留）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        areaTime = 0;
        tickTimer = 0;
        allyFightType = CreatureFightTypeEnum.None;
        base.Destroy(isPermanently);
    }
    #endregion
}
