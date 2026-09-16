using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 群体治疗-自身周围（牧师技能 500006，经 attack_mode_ext trigger_scene=1[释放技能意图] 挂载，由 NPC ai_param 的 skill_update 事件驱动「释放技能」意图在出手点发射）。
/// <para>瞬发无弹道无伤害：当帧遍历同阵营存活友军（含自己），筛 距自己≤collider_area_size[0](=5) 且 HP不满 者，
/// 逐个 RegainHP（治疗量=施法瞬间 ATK×damage_add_rate 快照，配置0=1倍）+ 各播一个一次性治疗粒子（ShowEffect 独立实例通道，多目标同帧不互顶）。</para>
/// <para>空放预检：重写 CheckCanTriggerSkill，「释放技能」意图切入前判定范围内无血量不满友军时不施法（不播动作/音效/粒子，保持就绪下帧再试）。</para>
/// <para>出手音效走 sound_start（FightManager.GetAttackModePrefab 创建时自动播放）；单目标回血音效走 sound_hit（RegainHP 内真实回血>0 才播）。</para>
/// </summary>
public class AttackModeRegainHPArea : BaseAttackMode
{
    #region 技能触发预检
    /// <summary>
    /// 技能触发预检（重写基类）：治疗半径内无血量不满的友军时不允许施放——空放=白播施法动作+出手音效且无粒子
    /// </summary>
    public override bool CheckCanTriggerSkill(FightCreatureEntity attacker, AttackModeInfoBean attackModeInfo)
    {
        if (attacker == null || attacker.IsDead() || attacker.creatureObj == null || attackModeInfo == null)
            return false;
        var listAlly = GetAllyList(attacker);
        if (listAlly.IsNull())
            return false;
        Vector3 center = attacker.creatureObj.transform.position;
        float sqrRadius = GetSqrHealRadius(attackModeInfo);
        for (int i = 0; i < listAlly.Count; i++)
        {
            if (IsValidHealTarget(listAlly[i], center, sqrRadius))
                return true;
        }
        return false;
    }
    #endregion

    #region 攻击流程
    /// <summary>
    /// 开始攻击-生物：治疗自身周围友军 → 回收并回调（attacked 实参忽略，自身施法不使用）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && !attacker.IsDead() && attacker.creatureObj != null)
        {
            RegainHPForArea(attacker, attacker.creatureObj.transform.position);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region 群体治疗
    /// <summary>
    /// 治疗中心点 collider_area_size[0] 半径内同阵营存活且 HP不满 的友军（含自己），每个目标各播一个一次性治疗粒子
    /// </summary>
    protected virtual void RegainHPForArea(FightCreatureEntity attacker, Vector3 center)
    {
        var listAlly = GetAllyList(attacker);
        if (listAlly.IsNull())
            return;
        float sqrRadius = GetSqrHealRadius(attackModeInfo);
        long effectId = attackModeInfo.GetEffectHitId(0);
        for (int i = 0; i < listAlly.Count; i++)
        {
            var itemTarget = listAlly[i];
            if (!IsValidHealTarget(itemTarget, center, sqrRadius))
                continue;
            Vector3 targetPos = itemTarget.creatureObj.transform.position;
            itemTarget.RegainHP(this);
            //一次性治疗粒子：独立实例通道，多目标同帧各播各的（全局单例通道会互相顶替）
            if (effectId != 0)
                EffectHandler.Instance.ShowEffect(effectId, targetPos);
        }
    }
    #endregion

    #region 目标判定（技能触发预检与 StartAttack 共用同一份）
    /// <summary>
    /// 取攻击者同阵营友军列表（仅支援进攻/防守阵营，其余返回 null）
    /// </summary>
    private static List<FightCreatureEntity> GetAllyList(FightCreatureEntity attacker)
    {
        var fightType = attacker.fightCreatureData.creatureFightType;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        return fightType == CreatureFightTypeEnum.FightAttack
            ? gameFightLogic?.fightData?.dlAttackCreatureEntity?.List
            : fightType == CreatureFightTypeEnum.FightDefense
                ? gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List
                : null;
    }

    /// <summary>
    /// 治疗半径平方（配置 collider_area_size[0]，默认5）
    /// </summary>
    private static float GetSqrHealRadius(AttackModeInfoBean attackModeInfo)
    {
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        float radius = (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0) ? arrAreaSize[0] : 5f;
        return radius * radius;
    }

    /// <summary>
    /// 目标是否为有效治疗对象：存活、距中心≤半径、HP不满（满血回血为0且白播特效/跳字）
    /// </summary>
    private static bool IsValidHealTarget(FightCreatureEntity itemTarget, Vector3 center, float sqrRadius)
    {
        if (itemTarget == null || itemTarget.IsDead() || itemTarget.creatureObj == null)
            return false;
        var fightData = itemTarget.fightCreatureData;
        if (fightData == null || fightData.HPCurrent >= fightData.GetAttribute(CreatureAttributeTypeEnum.HP))
            return false;
        return (itemTarget.creatureObj.transform.position - center).sqrMagnitude <= sqrRadius;
    }
    #endregion
}
