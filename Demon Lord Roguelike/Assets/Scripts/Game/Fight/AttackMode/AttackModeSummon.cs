using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 召唤（骷髅召唤师：普攻 500004[自己当前位置召唤1只骷髅战士,5秒一次] / BOSS技能 500005[自己所在路+上下相邻路每路3只,每只独立随机骷髅战士/投手,共最多9只,经 ext 100008 trigger_scene=0 攻击意图内30秒一发顶替当次普攻,大魔法师BOSS同款]）。
/// <para>瞬发无弹道无伤害：当帧按 other_data 键 summon_npc_ids(逗号分隔NPC池,每只独立随机)&amp;summon_count(每路数量)&amp;road_spread(上下扩展路数,越界自然衰减跳过不clamp)
/// 经 CreatureHandler.CreateAttackCreature 在攻击者当前位置(x±0.25抖动)生成进攻生物；强度倍率透传 attacker.fightCreatureData.intensityRate 保证召唤物随关卡难度递增。</para>
/// <para>召唤物为独立进攻生物：无 owner 关联(召唤者死亡不影响存活)、计入场上进攻生物(未清完不结算胜利)；召唤生成即置 isSummoned 标记,死亡不掉魔晶(防挂机刷取)；出场走出土冒出动画(通用意图 AIIntentCreatureEmerge,从地底冒出)。</para>
/// <para>出手特效 effect_hit 播在攻击者位置+攻击模块 start_pos_offset(空=0,0,0播在脚下)，出手音走 sound_start(FightManager.GetAttackModePrefab 创建时自动播放)，均为纯配置。</para>
/// <para>【对象池安全】当帧完成全部召唤、无 Update 路径、无实例状态，天然安全，无需重写 Destroy 清状态。</para>
/// </summary>
public class AttackModeSummon : BaseAttackMode
{
    #region 攻击流程
    /// <summary>
    /// 开始攻击-默认（防御性废路径：召唤永不走纯数据发射，立即回收防池化实例带残留状态挂场；照 AttackModeFalluponChainMulti 先例）
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物：按配置召唤 → 播出手特效 → 回收并回调（照 AttackModeShieldCast 瞬发范式）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && !attacker.IsDead() && attacker.creatureObj != null)
        {
            SummonCreatures(attacker);
            //出手特效：攻击者位置 + 攻击模块 start_pos_offset 偏移（空=0,0,0 播在脚下）
            PlayEffectForHit(attacker.creatureObj.transform.position + attackModeInfo.GetStartPosOffset());
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
    #endregion

    #region 召唤
    /// <summary>
    /// 按 other_data 配置以攻击者所在路为中心向上下扩展逐路召唤；npcIds 空=报错空放（动作/特效照播，照 ShieldCast 空放语义）
    /// </summary>
    protected virtual void SummonCreatures(FightCreatureEntity attacker)
    {
        attackModeInfo.GetSummonConfig(out List<long> npcIds, out int countPerRoad, out int roadSpread);
        if (npcIds.IsNull())
        {
            LogUtil.LogError($"召唤攻击模块[{attackModeInfo.id}]未配置 summon_npc_ids（other_data 格式：summon_npc_ids:20010001,20020001&summon_count:3&road_spread:1），本次空放");
            return;
        }
        var gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameFightLogic?.fightData == null)
            return;
        int sceneRoadNum = gameFightLogic.fightData.sceneRoadNum;
        int selfRoad = attacker.fightCreatureData.roadIndex;
        float createPosX = attacker.creatureObj.transform.position.x;
        //强度倍率透传：召唤物与召唤者同难度递增（征服模式按关卡对 HP/护甲/攻击力 整体倍率）
        float intensityRate = attacker.fightCreatureData.intensityRate;
        for (int roadOffset = -roadSpread; roadOffset <= roadSpread; roadOffset++)
        {
            int targetRoad = selfRoad + roadOffset;
            //越界自然衰减：超出道路范围的路直接跳过（不 clamp，避免边界路叠加双倍数量）
            if (targetRoad < 1 || targetRoad > sceneRoadNum)
                continue;
            for (int i = 0; i < countPerRoad; i++)
            {
                //每只独立随机NPC（骷髅战士/骷髅投手）
                long npcId = npcIds[UnityEngine.Random.Range(0, npcIds.Count)];
                GameObject summonedObj = CreatureHandler.Instance.CreateAttackCreature(npcId, sceneRoadNum, targetRoad: targetRoad, createPosX: createPosX, intensityRate: intensityRate);
                MarkSummoned(gameFightLogic, summonedObj);
            }
        }
    }

    /// <summary>
    /// 给召唤生成的生物置召唤物标记并发起出土冒出（CreateAttackCreature 返回的 obj.name 即 creatureUUId，见 FightCreatureEntity.SetData；isSummoned 当前消费：DropCrystal 跳过不掉魔晶）
    /// </summary>
    protected virtual void MarkSummoned(GameFightLogic gameFightLogic, GameObject summonedObj)
    {
        if (summonedObj == null)
            return;
        var summonedEntity = gameFightLogic.fightData.GetCreatureById(summonedObj.name, CreatureFightTypeEnum.FightAttack);
        if (summonedEntity != null)
        {
            summonedEntity.fightCreatureData.isSummoned = true;
            //出场动画：切换通用出土冒出意图从地底冒出（CreateAIEntity 已同步 StartAIEntity 进闲置，此处干净切走）
            if (summonedEntity.aiEntity is AICreatureEntity aiCreatureEntity)
                aiCreatureEntity.StartEmerge();
        }
    }
    #endregion
}
