using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-多次瞬时攻击（深渊馈赠「闪电」落雷）
/// <para>每 trigger_time 秒触发一轮：触发瞬间一次性检测全场存活敌人（此后不再检测，间隔期新刷敌人不补入也不受波及），
/// 有放回随机抽取 class_entity_data[0] 个目标（可重复，如场上仅1个敌人则多道雷全落它身上）；</para>
/// <para>第 1 次瞬时攻击立即执行，后续每次间隔 multiAttackInterval 秒由 UpdateBuffTime 驱动执行（不在同帧连发）；
/// 场上无敌人时本轮不触发。</para>
/// <para>每次瞬时攻击 = 发射一个 class_entity_data[1] 指定的攻击模块（AttackModeInstantAreaThunder 落雷），
/// 粒子/AOE/伤害由攻击模式框架处理：AOE半径与单雷命中上限配在攻击模块表（collider_area_size/hit_max），
/// 本类注入「BUFF目标(魔王)实时攻击力 × trigger_value 倍率」的伤害快照与触发瞬间的敌人快照名单。</para>
/// <para>class_entity_data 格式："攻击次数,攻击模块ID"（如 "3,300033"；次数是本BUFF的调度参数，攻击模块本身只是单道雷）。</para>
/// </summary>
public class BuffEntityPeriodicMultiInstantAttack : BuffEntityPeriodic
{
    //多次瞬时攻击的执行间隔(秒)
    protected float multiAttackInterval = 0.1f;
    //待执行的瞬时攻击目标队列(触发瞬间一次性快照抽取)
    protected Queue<FightCreatureEntity> queuePendingAttack = new Queue<FightCreatureEntity>();
    //触发瞬间快照的敌人名单(AOE只作用于名单内，间隔期新刷敌人不受波及)
    protected HashSet<string> setSnapshotCreatureId = new HashSet<string>();
    //瞬时攻击间隔计时器
    protected float timeIntervalCurrent = 0;
    //本轮发射的攻击模块ID(触发时从 class_entity_data 解析缓存)
    protected long attackModeId = 0;

    #region 数据相关
    /// <summary>
    /// 清理数据（对象池复用前清空队列/快照/计时器，防残留）
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        queuePendingAttack.Clear();
        setSnapshotCreatureId.Clear();
        timeIntervalCurrent = 0;
        attackModeId = 0;
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加（base 维持周期触发；此处额外驱动待执行的多次瞬时攻击按间隔逐个落地）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (queuePendingAttack.Count == 0)
            return;
        timeIntervalCurrent += buffTime;
        if (timeIntervalCurrent >= multiAttackInterval)
        {
            timeIntervalCurrent = 0;
            var targetEnemy = queuePendingAttack.Dequeue();
            LaunchStrike(targetEnemy);
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制（一轮多次瞬时攻击的起点）
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //触发瞬间一次性检测全场存活敌人(后续多次攻击不再检测)
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listEnemy = gameFightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy.IsNull()) return false;
        List<FightCreatureEntity> listAliveEnemy = new List<FightCreatureEntity>(listEnemy.Count);
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var itemEnemy = listEnemy[i];
            if (itemEnemy != null && !itemEnemy.IsDead())
                listAliveEnemy.Add(itemEnemy);
        }
        //场上无敌人则本轮不触发
        if (listAliveEnemy.Count == 0) return false;

        //解析参数 "攻击次数,攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"多次瞬时攻击BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"攻击次数,攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }
        int attackCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);

        //快照本轮敌人名单(供攻击模块AOE过滤间隔期新刷敌人)
        setSnapshotCreatureId.Clear();
        for (int i = 0; i < listAliveEnemy.Count; i++)
        {
            setSnapshotCreatureId.Add(listAliveEnemy[i].fightCreatureData.creatureData.creatureUUId);
        }

        //有放回随机抽取目标：第1个立即执行，其余入队按间隔执行
        queuePendingAttack.Clear();
        timeIntervalCurrent = 0;
        for (int i = 0; i < attackCount; i++)
        {
            var targetEnemy = listAliveEnemy[UnityEngine.Random.Range(0, listAliveEnemy.Count)];
            if (i == 0)
            {
                LaunchStrike(targetEnemy);
            }
            else
            {
                queuePendingAttack.Enqueue(targetEnemy);
            }
        }
        return true;
    }
    #endregion

    #region 瞬时攻击发射
    /// <summary>
    /// 对单个目标发射一次瞬时攻击（落雷攻击模块；粒子/AOE/伤害全部由攻击模式框架处理）
    /// </summary>
    protected void LaunchStrike(FightCreatureEntity targetEnemy)
    {
        //目标在间隔期内死亡则跳过本次
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead())
            return;
        if (attackModeId == 0)
        {
            LogUtil.LogError($"多次瞬时攻击BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }
        //伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每发落地时实时取）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * buffEntityData.GetTriggerValue());
        if (attackDamage <= 0)
            return;
        Vector3 strikePosition = targetEnemy.creatureObj.transform.position;

        //纯数据发射路径（照分裂弹发射器先例）：注入攻击者快照与落点，攻击者快照不随弹道存活期变化
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        //不暴击（保持闪电不暴击的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = strikePosition;
        attackModeData.targetPos = strikePosition;
        attackModeData.attackedId = targetEnemy.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        //血液等受击特效朝向（核心→落点）
        attackModeData.attackDirection = (strikePosition - coreCreature.creatureObj.transform.position).normalized;
        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            //把触发瞬间的敌人快照名单注入攻击模块（当帧同步发射即用完Destroy，引用共享安全）
            if (attackMode is AttackModeInstantArea instantArea)
            {
                instantArea.filterCreatureIds = setSnapshotCreatureId;
            }
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
    }
    #endregion
}
