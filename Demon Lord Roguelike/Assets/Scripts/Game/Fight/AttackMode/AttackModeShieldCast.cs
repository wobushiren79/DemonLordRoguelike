using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 援护护盾施法（大盾战士BOSS技能 500003，经 attack_mode_ext trigger_scene=1[释放技能意图] 挂载，由 NPC ai_param 的 skill_update 事件驱动「释放技能」意图在出手点发射）。
/// <para>瞬发无弹道无伤害：当帧搜索攻击方阵营最前排 N 个存活友军（排除自己；FightAttack 前排=x最小=最靠近敌方阵地，FightDefense 前排=x最大），
/// 逐个套上 other_data 键 shield_buff 指定的护盾BUFF（applier=BOSS自己，伤害转移护盾 2000700001）。</para>
/// <para>出手特效 effect_hit 播在 BOSS 出手点（自身位置+攻击模块 start_pos_offset 偏移，空=0,0,0），出手音效走 sound_start（FightManager.GetAttackModePrefab 创建时自动播放），均为纯配置。</para>
/// <para>other_data 格式：shield_count:3&amp;shield_buff:2000700001（最前排目标数量,护盾BUFF的ID）。</para>
/// </summary>
public class AttackModeShieldCast : BaseAttackMode
{
    #region 攻击流程
    /// <summary>
    /// 开始攻击-生物：给最前排友军套盾 → 播出手特效 → 回收并回调
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && !attacker.IsDead())
        {
            CastShieldForFrontRow(attacker);
            //出手特效：BOSS 出手点（攻击者位置 + 攻击模块 start_pos_offset 偏移，空=0,0,0 播在脚下；不用生物 attack_start_position，避免牵连普攻命中特效取点）
            if (attacker.creatureObj != null)
            {
                Vector3 castPos = attacker.creatureObj.transform.position + attackModeInfo.GetStartPosOffset();
                PlayEffectForHit(castPos);
            }
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region 套盾
    /// <summary>
    /// 搜索最前排 N 个同阵营存活友军（排除自己）并逐个套上护盾BUFF；无存活友军则本次空放（动作/特效照播）
    /// </summary>
    protected virtual void CastShieldForFrontRow(FightCreatureEntity attacker)
    {
        attackModeInfo.GetShieldCastConfig(out int shieldCount, out long shieldBuffId);
        if (shieldBuffId == 0)
        {
            LogUtil.LogError($"援护护盾施法攻击模块[{attackModeInfo.id}]未配置 shield_buff（other_data 格式：shield_count:3&shield_buff:2000700001）");
            return;
        }
        var fightType = attacker.fightCreatureData.creatureFightType;
        //只支援进攻/防守阵营（魔王核心等不触发）
        if (fightType != CreatureFightTypeEnum.FightAttack && fightType != CreatureFightTypeEnum.FightDefense)
            return;
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var listAlly = fightType == CreatureFightTypeEnum.FightAttack
            ? gameFightLogic?.fightData?.dlAttackCreatureEntity?.List
            : gameFightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        //选取最前排（前排方向由阵营决定），排除自己
        var listFrontRow = FightCreatureSearchUtil.FindFrontRowCreatures(listAlly, shieldCount, attacker, fightType);
        if (listFrontRow.IsNull())
            return;
        string selfUUId = attacker.fightCreatureData.creatureData.creatureUUId;
        for (int i = 0; i < listFrontRow.Count; i++)
        {
            var itemTarget = listFrontRow[i];
            BuffHandler.Instance.AddFightCreatureBuff(new List<BuffBean>() { new BuffBean(shieldBuffId) },
                selfUUId, itemTarget.fightCreatureData.creatureData.creatureUUId);
        }
    }
    #endregion
}
