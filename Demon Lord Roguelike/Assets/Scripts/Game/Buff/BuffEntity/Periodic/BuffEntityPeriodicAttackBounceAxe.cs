using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-弹跳斧头发射（深渊馈赠「跳跳斧」）
/// <para>每 trigger_time 秒触发一轮：从防守核心（魔王）位置扔出斧头，
/// 目标 = 随机一排的最远敌人（同路按 x 降序=离魔王最远优先）；
/// 不放回抽取（一轮内多斧首目标互不重复；敌人少于斧数时只扔同等数量的斧），
/// 每轮斧头数量 = class_entity_data[0]（1~5 对应 Lv1~5）。</para>
/// <para>第 1 斧立即扔出，后续每斧间隔 launchInterval 秒由 UpdateBuffTime 驱动发射（不在同帧连发）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每斧发射时实时取），不暴击；
/// 命中后的弹跳（抛物线分段）与逐目标伤害减半由 AttackModeRangedArcBounce 自身处理。</para>
/// <para>class_entity_data 格式："斧数,攻击模块ID,弹跳次数"（如 "3,300061,2"）。场上无存活敌人时本轮不触发。</para>
/// </summary>
public class BuffEntityPeriodicAttackBounceAxe : BuffEntityPeriodic
{
    #region 字段
    /// <summary>本轮发射的攻击模块ID（触发时从 class_entity_data 解析缓存）</summary>
    protected long attackModeId;
    /// <summary>斧头弹跳次数（发射时注入弹道 bounceMax）</summary>
    protected int bounceMax;
    /// <summary>多斧发射间隔（秒）</summary>
    protected float launchInterval = 0.2f;
    /// <summary>待发射的目标队列（触发瞬间一次性不放回抽取）</summary>
    protected Queue<FightCreatureEntity> queuePendingLaunch = new Queue<FightCreatureEntity>();
    /// <summary>发射间隔计时器</summary>
    protected float timeIntervalCurrent;
    /// <summary>按道路分组的候选（key=roadIndex，value=该路存活敌人按 x 降序，抽取即移除表头）</summary>
    protected readonly Dictionary<int, List<FightCreatureEntity>> dicRoadCandidate = new Dictionary<int, List<FightCreatureEntity>>();
    /// <summary>还有剩余候选的道路列表（复用缓冲）</summary>
    protected readonly List<int> listRoadAlive = new List<int>();
    #endregion

    #region 数据相关
    /// <summary>
    /// 清理数据（对象池复用前清空攻击模块ID/弹跳次数/队列/计时器/候选缓冲，防残留）
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        attackModeId = 0;
        bounceMax = 0;
        queuePendingLaunch.Clear();
        timeIntervalCurrent = 0;
        dicRoadCandidate.Clear();
        listRoadAlive.Clear();
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
            LaunchAxe(queuePendingLaunch.Dequeue());
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制：收集存活敌人按道路分组（同路 x 降序）→ 不放回抽取（随机路取最远，首目标互不重复）→ 第1斧立即发射，其余入队按间隔发射
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;

        // 检测场上存活敌人（无敌人跳过本轮）
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listEnemy = gameFightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy.IsNull()) return false;

        // 按道路分组收集存活敌人（同路按 x 降序=离魔王最远优先）
        dicRoadCandidate.Clear();
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy == null || enemy.IsDead() || enemy.fightCreatureData == null || enemy.creatureObj == null)
                continue;
            int road = enemy.fightCreatureData.roadIndex;
            if (!dicRoadCandidate.TryGetValue(road, out var list))
            {
                list = new List<FightCreatureEntity>();
                dicRoadCandidate[road] = list;
            }
            list.Add(enemy);
        }
        if (dicRoadCandidate.Count == 0) return false;
        foreach (var kv in dicRoadCandidate)
        {
            kv.Value.Sort((a, b) => b.creatureObj.transform.position.x.CompareTo(a.creatureObj.transform.position.x));
        }

        // 解析参数 "斧数,攻击模块ID,弹跳次数"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 3)
        {
            LogUtil.LogError($"弹跳斧头BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"斧数,攻击模块ID,弹跳次数\"：{buffInfo.class_entity_data}");
            return false;
        }
        int axeCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);
        bounceMax = int.Parse(arrEntityData[2]);

        // 不放回抽取（首目标互不重复；敌人少于斧数时只扔同等数量的斧）：第1个立即发射，其余入队按间隔发射
        queuePendingLaunch.Clear();
        timeIntervalCurrent = 0;
        int launchTotal = 0;
        for (int i = 0; i < axeCount; i++)
        {
            FightCreatureEntity target = PickNextTarget();
            if (target == null) break;
            launchTotal++;
            if (i == 0)
            {
                LaunchAxe(target);
            }
            else
            {
                queuePendingLaunch.Enqueue(target);
            }
        }
        return launchTotal > 0;
    }

    /// <summary>
    /// 不放回抽取下一个首目标：随机选一条还有未分配敌人的道路，取该路当前最远（x 最大）的敌人；无候选返回 null
    /// </summary>
    private FightCreatureEntity PickNextTarget()
    {
        listRoadAlive.Clear();
        foreach (var kv in dicRoadCandidate)
        {
            if (kv.Value.Count > 0)
                listRoadAlive.Add(kv.Key);
        }
        if (listRoadAlive.Count == 0)
            return null;
        int road = listRoadAlive[UnityEngine.Random.Range(0, listRoadAlive.Count)];
        var list = dicRoadCandidate[road];
        FightCreatureEntity target = list[0];
        list.RemoveAt(0);
        return target;
    }
    #endregion

    #region 斧头发射
    /// <summary>
    /// 向单个目标扔出1把斧头（纯数据发射路径）：注入攻击者快照、伤害、起终点，并在 StartAttack 前写入弹跳次数。
    /// </summary>
    private void LaunchAxe(FightCreatureEntity targetEnemy)
    {
        // 目标在间隔期内死亡则跳过本斧
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead())
            return;
        if (attackModeId == 0)
        {
            LogUtil.LogError($"弹跳斧头BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }

        // 伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每斧发射时实时取）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null) return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * buffEntityData.GetTriggerValue());
        if (attackDamage <= 0) return;

        // 起点：魔王生物位置 + CreatureInfo 攻击起始位置 + 攻击模块偏移（与回旋镖对齐）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        Vector3 startPos = coreCreature.creatureObj.transform.position + startPosOffset;
        // 锁定点：目标生物当前位置（抛物线末端追踪目标，不再固定高度）
        Vector3 targetPos = targetEnemy.creatureObj.transform.position;

        // 纯数据发射路径（照回旋镖先例）：注入攻击者快照与起终点
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        // 不暴击（保持闪电/矿车/回旋镖的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = startPos;
        attackModeData.targetPos = targetPos;
        attackModeData.attackedId = targetEnemy.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        attackModeData.attackDirection = (targetPos - startPos).normalized;

        int bounceMaxCache = bounceMax;
        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            // 弹跳次数须在 StartAttack 之前写入（照 targetRoad/filterCreatureIds 先例）
            if (attackMode is AttackModeRangedArcBounce bounceAxe)
                bounceAxe.bounceMax = bounceMaxCache;
            attackMode.StartAttack();
        });
    }
    #endregion
}
