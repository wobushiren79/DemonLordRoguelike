using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-炼狱火瓶发射（深渊馈赠「瓶装炼狱火」）
/// <para>每 trigger_time(10) 秒触发一轮：触发瞬间快照全场存活敌人（敌人出生即在道路内，"有存活敌人"即满足「进入道路范围才开始丢」），
/// 不放回随机抽取 class_entity_data[0] 个主目标（一轮内多瓶首目标互不重复——同轮不瞄同一目标；
/// 敌人少于瓶数时只丢同等数量的瓶，如仅1敌、Lv3也只丢1瓶），第1瓶立即投掷，后续每瓶间隔 launchInterval(0.2) 秒由 UpdateBuffTime 驱动。</para>
/// <para>每瓶 = 发射一个 AttackModeRangedArcGround：抛物线飞向该目标当前位置（固定落点、不追踪），
/// 落地燃放地形火焰每1秒对半径内存活敌人跳伤（伤害/半径/时长由攻击模块自身处理）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value(0.2) 倍率（每瓶投掷时实时取，保底1），不暴击；场上无存活敌人本轮不触发。</para>
/// <para>class_entity_data 格式："瓶数,攻击模块ID"（如 "3,300101"）。</para>
/// </summary>
public class BuffEntityPeriodicAttackFireBottle : BuffEntityPeriodic
{
    #region 常量
    /// <summary>多瓶投掷间隔（秒）</summary>
    protected const float LaunchInterval = 0.2f;
    #endregion

    #region 字段
    /// <summary>本轮发射的攻击模块ID（触发时从 class_entity_data 解析缓存）</summary>
    protected long attackModeId;
    /// <summary>待投掷的目标队列（触发瞬间一次性不放回抽取）</summary>
    protected Queue<FightCreatureEntity> queuePendingLaunch = new Queue<FightCreatureEntity>();
    /// <summary>投掷间隔计时器</summary>
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
    /// buff持续时间增加（base 维持周期触发；此处额外驱动待投掷队列按间隔逐个发射）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (queuePendingLaunch.Count == 0)
            return;
        timeIntervalCurrent += buffTime;
        if (timeIntervalCurrent >= LaunchInterval)
        {
            timeIntervalCurrent = 0;
            LaunchFireBottle(queuePendingLaunch.Dequeue());
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制：快照存活敌人 → 不放回随机抽取瓶数个目标 → 第1瓶立即投掷，其余入队按间隔投掷
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;

        //触发瞬间一次性检测全场存活敌人（后续连发不再检测，间隔期新刷敌人不补入本轮）
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
        //场上无存活敌人则本轮不触发（需求：敌人进入道路范围才开始丢瓶子）
        if (listAliveEnemy.Count == 0) return false;

        //解析参数 "瓶数,攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"炼狱火瓶BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"瓶数,攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }
        int bottleCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);

        //不放回随机抽取主目标（一轮内多瓶首目标互不重复；敌人少于瓶数时只丢同等数量的瓶）：第1瓶立即投掷，其余入队按间隔投掷
        queuePendingLaunch.Clear();
        timeIntervalCurrent = 0;
        List<FightCreatureEntity> listCandidate = new List<FightCreatureEntity>(listAliveEnemy);
        int launchNum = Mathf.Min(bottleCount, listCandidate.Count);
        for (int i = 0; i < launchNum; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, listCandidate.Count);
            var targetEnemy = listCandidate[randomIndex];
            listCandidate.RemoveAt(randomIndex);
            if (i == 0)
            {
                LaunchFireBottle(targetEnemy);
            }
            else
            {
                queuePendingLaunch.Enqueue(targetEnemy);
            }
        }
        return true;
    }
    #endregion

    #region 火瓶投掷
    /// <summary>
    /// 向单个目标投掷1瓶（纯数据发射路径）：注入攻击者快照、伤害、起终点（固定落点不追踪）
    /// </summary>
    private void LaunchFireBottle(FightCreatureEntity targetEnemy)
    {
        //目标在间隔期内死亡则跳过本瓶
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead())
            return;
        if (attackModeId == 0)
        {
            LogUtil.LogError($"炼狱火瓶BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }

        //伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每瓶投掷时实时取，保底1点）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null) return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = Mathf.Max(1, (int)(coreATK * buffEntityData.GetTriggerValue()));

        //起点：魔王生物位置 + CreatureInfo 攻击起始位置 + 攻击模块偏移（与回旋镖/冲击波发射点对齐）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        Vector3 startPos = coreCreature.creatureObj.transform.position + startPosOffset;
        //落点：目标当前位置（固定落点，火瓶只朝一开始的位置飞、不跟随目标）
        Vector3 targetPos = targetEnemy.creatureObj.transform.position;

        //纯数据发射路径（照闪电/冲击波先例）：注入攻击者快照与起终点
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        //不暴击（保持闪电/矿车/回旋镖的设计）
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
