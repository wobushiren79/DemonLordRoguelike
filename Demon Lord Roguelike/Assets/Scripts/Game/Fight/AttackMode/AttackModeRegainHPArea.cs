using System;
using UnityEngine;

/// <summary>
/// 群体治疗-自身周围（牧师技能 500006，经 attack_mode_ext trigger_scene=1[释放技能意图] 挂载，由 NPC ai_param 的 skill_update 事件驱动「释放技能」意图在出手点发射）。
/// <para>瞬发无弹道无伤害：当帧遍历同阵营存活友军（含自己），筛 距自己≤collider_area_size[0](=5) 且 HP不满 者，
/// 逐个 RegainHP（治疗量=施法瞬间 ATK×damage_add_rate 快照，配置0=1倍）+ 各播一个一次性治疗粒子（ShowEffect 独立实例通道，多目标同帧不互顶）。</para>
/// <para>出手音效走 sound_start（FightManager.GetAttackModePrefab 创建时自动播放）；单目标回血音效走 sound_hit（RegainHP 内真实回血>0 才播）。</para>
/// </summary>
public class AttackModeRegainHPArea : BaseAttackMode
{
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
        var fightType = attacker.fightCreatureData.creatureFightType;
        //只支援进攻/防守阵营（魔王核心等不触发）
        if (fightType != CreatureFightTypeEnum.FightAttack && fightType != CreatureFightTypeEnum.FightDefense)
            return;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var listAlly = fightType == CreatureFightTypeEnum.FightAttack
            ? gameFightLogic?.fightData?.dlAttackCreatureEntity?.List
            : gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        if (listAlly.IsNull())
            return;
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        float radius = (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0) ? arrAreaSize[0] : 5f;
        float sqrRadius = radius * radius;
        long effectId = attackModeInfo.GetEffectHitId(0);
        for (int i = 0; i < listAlly.Count; i++)
        {
            var itemTarget = listAlly[i];
            if (itemTarget == null || itemTarget.IsDead() || itemTarget.creatureObj == null)
                continue;
            var fightData = itemTarget.fightCreatureData;
            //血量不满才治疗（满血回血为0且白播特效）
            if (fightData == null || fightData.HPCurrent >= fightData.GetAttribute(CreatureAttributeTypeEnum.HP))
                continue;
            Vector3 targetPos = itemTarget.creatureObj.transform.position;
            if ((targetPos - center).sqrMagnitude > sqrRadius)
                continue;
            itemTarget.RegainHP(this);
            //一次性治疗粒子：独立实例通道，多目标同帧各播各的（全局单例通道会互相顶替）
            if (effectId != 0)
                EffectHandler.Instance.ShowEffect(effectId, targetPos);
        }
    }
    #endregion
}
