using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-多次瞬时攻击（深渊馈赠「闪电」落雷）
/// <para>每 trigger_time 秒触发一轮：触发瞬间一次性检测全场存活敌人（此后不再检测，间隔期新刷敌人不补入也不受波及），
/// 有放回随机抽取 class_entity_data[0] 个目标（可重复，如场上仅1个敌人则多道雷全落它身上）；</para>
/// <para>第 1 次瞬时攻击立即执行，后续每次间隔 multiAttackInterval 秒由 UpdateBuffTime 驱动执行（不在同帧连发）；
/// 场上无敌人时本轮不触发。</para>
/// <para>每次瞬时攻击：在目标位置播放全局单例雷电粒子，并对落点 class_entity_data[1] 半径内的快照敌人造成
/// 「BUFF目标(魔王)实时攻击力 × trigger_value 倍率」的伤害（走标准 UnderAttack 流程：闪避/护甲/飘字/死亡）；
/// 单次落雷命中目标数受 class_entity_data[2] 上限限制（0或缺省=不限），超上限时按距落点近者优先（保证落点主目标必中）。</para>
/// <para>class_entity_data 格式："攻击次数,AOE半径,单雷命中目标上限(可选,0=不限)"（如 "3,0.6,3"）。</para>
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
    //本轮瞬时攻击的AOE半径(触发时从 class_entity_data 解析缓存)
    protected float attackAreaRadius = 0;
    //单次落雷命中目标数上限(触发时从 class_entity_data 解析缓存, 0=不限)
    protected int attackHitMax = 0;

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
        attackAreaRadius = 0;
        attackHitMax = 0;
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
            ExecuteInstantAttackOnTarget(buffEntityData, targetEnemy);
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

        //解析参数 "攻击次数,AOE半径,单雷命中目标上限(可选,0=不限)"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        int attackCount = int.Parse(arrEntityData[0]);
        attackAreaRadius = float.Parse(arrEntityData[1]);
        attackHitMax = arrEntityData.Length >= 3 ? int.Parse(arrEntityData[2]) : 0;

        //快照本轮敌人名单(供AOE过滤间隔期新刷敌人)
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
                ExecuteInstantAttackOnTarget(buffEntityData, targetEnemy);
            }
            else
            {
                queuePendingAttack.Enqueue(targetEnemy);
            }
        }
        return true;
    }
    #endregion

    #region 瞬时攻击执行
    /// <summary>
    /// 对单个目标执行一次瞬时攻击（播雷电粒子 + 落点AOE伤害）
    /// </summary>
    protected void ExecuteInstantAttackOnTarget(BuffEntityBean buffEntityData, FightCreatureEntity targetEnemy)
    {
        //目标在间隔期内死亡则跳过本次
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead())
            return;
        Vector3 strikePosition = targetEnemy.creatureObj.transform.position;

        //播放全局单例雷电粒子
        EffectHandler.Instance.ShowThunderEffect(strikePosition);

        //伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * buffEntityData.GetTriggerValue());
        if (attackDamage <= 0)
            return;

        //落点AOE搜索敌方生物，按距落点近远升序（命中上限截断时保证落点主目标必中）
        var targetColliders = RayUtil.OverlapToSphere(strikePosition, attackAreaRadius, 1 << LayerInfo.CreatureAtt);
        if (targetColliders.IsNull())
            return;
        System.Array.Sort(targetColliders, (a, b) =>
            (a.transform.position - strikePosition).sqrMagnitude.CompareTo((b.transform.position - strikePosition).sqrMagnitude));
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        int hitNum = 0;
        for (int i = 0; i < targetColliders.Length; i++)
        {
            //单次落雷命中目标数达上限则停止
            if (attackHitMax > 0 && hitNum >= attackHitMax)
                break;
            string creatureId = targetColliders[i].gameObject.name;
            //仅快照名单内敌人受伤(间隔期新刷敌人不受波及)
            if (!setSnapshotCreatureId.Contains(creatureId))
                continue;
            var hitCreature = gameFightLogic.fightData.GetCreatureById(creatureId, CreatureFightTypeEnum.FightAttack);
            if (hitCreature == null || hitCreature.IsDead())
                continue;
            FightUnderAttackBean fightUnderAttackData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, attackDamage);
            hitCreature.UnderAttack(fightUnderAttackData);
            hitNum++;
        }
    }
    #endregion
}
