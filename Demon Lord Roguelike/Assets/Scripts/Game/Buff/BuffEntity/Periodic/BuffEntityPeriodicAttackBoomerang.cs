using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-回旋镖发射（深渊馈赠「死亡回旋」）
/// <para>每 trigger_time 秒触发一轮：从防守核心（魔王）位置发射回旋镖，
/// 不放回随机抽取场上存活敌人作为目标（一轮内多镖目标互不重复；敌人少于镖数时只发同等数量的镖），
/// 每轮回旋镖数量 = class_entity_data[0]（1~5 对应 Lv1~5）。</para>
/// <para>第 1 镖立即发射，后续每镖间隔 launchInterval 秒由 UpdateBuffTime 驱动发射（不在同帧连发）。</para>
/// <para>锁定点 = 目标生物位置，高度取魔王发射点高度（部分敌人无 attack_start_position 参数，不用它）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每镖发射时实时取），不暴击；
/// 逐目标伤害递减由 AttackModeRangedBoomerang 自身处理。</para>
/// <para>class_entity_data 格式："镖数,攻击模块ID"（如 "3,300051"）。场上无存活敌人时本轮不触发。</para>
/// </summary>
public class BuffEntityPeriodicAttackBoomerang : BuffEntityPeriodic
{
    #region 字段
    /// <summary>本轮发射的攻击模块ID（触发时从 class_entity_data 解析缓存）</summary>
    protected long attackModeId;
    /// <summary>多镖发射间隔（秒）</summary>
    protected float launchInterval = 0.2f;
    /// <summary>待发射的目标队列（触发瞬间一次性不放回抽取）</summary>
    protected Queue<FightCreatureEntity> queuePendingLaunch = new Queue<FightCreatureEntity>();
    /// <summary>发射间隔计时器</summary>
    protected float timeIntervalCurrent;
    #endregion

    #region 数据相关
    /// <summary>
    /// 清理数据（对象池复用前清空攻击模块ID/队列/计时器，防残留）
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        attackModeId = 0;
        queuePendingLaunch.Clear();
        timeIntervalCurrent = 0;
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加（base 维持周期触发；此处额外驱动待发队列按间隔逐个发射）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (queuePendingLaunch.Count == 0)
            return;
        timeIntervalCurrent += buffTime;
        if (timeIntervalCurrent >= launchInterval)
        {
            timeIntervalCurrent = 0;
            LaunchBoomerang(queuePendingLaunch.Dequeue());
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制：检测存活敌人 → 不放回随机抽取目标（一轮内目标互不重复）→ 第1镖立即发射，其余入队按 0.2 秒间隔发射
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;

        // 检测场上存活敌人（无敌人跳过本轮）
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listEnemy = gameFightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy.IsNull()) return false;
        List<FightCreatureEntity> listAliveEnemy = new List<FightCreatureEntity>(listEnemy.Count);
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy != null && !enemy.IsDead())
                listAliveEnemy.Add(enemy);
        }
        if (listAliveEnemy.Count == 0) return false;

        // 解析参数 "镖数,攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"回旋镖BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"镖数,攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }
        int boomerangCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);

        // 不放回随机抽取目标（敌人少于镖数时只发同等数量的镖）：第1个立即发射，其余入队按间隔发射
        queuePendingLaunch.Clear();
        timeIntervalCurrent = 0;
        List<FightCreatureEntity> listCandidate = new List<FightCreatureEntity>(listAliveEnemy);
        int launchCount = Mathf.Min(boomerangCount, listCandidate.Count);
        for (int i = 0; i < launchCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, listCandidate.Count);
            var targetEnemy = listCandidate[randomIndex];
            listCandidate.RemoveAt(randomIndex);
            if (i == 0)
            {
                LaunchBoomerang(targetEnemy);
            }
            else
            {
                queuePendingLaunch.Enqueue(targetEnemy);
            }
        }
        return true;
    }
    #endregion

    #region 回旋镖发射
    /// <summary>
    /// 向单个目标发射1枚回旋镖（纯数据发射路径）：注入攻击者快照、伤害、起终点，走 AttackModeRangedBoomerang 三段式飞行。
    /// </summary>
    private void LaunchBoomerang(FightCreatureEntity targetEnemy)
    {
        // 目标在间隔期内死亡则跳过本镖
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead())
            return;
        if (attackModeId == 0)
        {
            LogUtil.LogError($"回旋镖BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }

        // 伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每镖发射时实时取）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null) return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * buffEntityData.GetTriggerValue());
        if (attackDamage <= 0) return;

        // 起点：魔王生物位置 + CreatureInfo 攻击起始位置（与 BaseAttackMode 常规攻击对齐）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        Vector3 startPos = coreCreature.creatureObj.transform.position + startPosOffset;
        // 锁定点：目标生物位置，高度取魔王发射点高度（部分敌人无 attack_start_position 参数，弹道保持水平飞行）
        Vector3 targetPos = targetEnemy.creatureObj.transform.position;
        targetPos.y = startPos.y;

        // 纯数据发射路径（照矿车/闪电先例）：注入攻击者快照与起终点
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        // 不暴击（保持闪电/矿车的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = startPos;
        attackModeData.targetPos = targetPos;
        attackModeData.attackedId = targetEnemy.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        attackModeData.attackDirection = (targetPos - startPos).normalized;

        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
    }
    #endregion
}
