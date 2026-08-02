using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-随机道路冲撞（深渊馈赠「失控的矿车」）
/// <para>每 trigger_time 秒触发一轮：随机选出若干条道路，每条被选中的道路从最左端驶出1辆矿车（class_entity_data[1] 指定的攻击模块）
/// 沿道路从左向右碾压，驶到道路尽头消失；出车数=class_entity_data[0]（按馈赠等级1~5配）。</para>
/// <para>选路规则：车数≤道路数时无放回随机抽对应条数；车数>道路数时每条路先各出1辆，多出的随机重复分配到已有道路上
/// （同一条路可出多辆），同路的第2辆起按 staggerInterval 秒间隔错开出车防止重叠（由 UpdateBuffTime 驱动）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每辆车发车时实时取），不暴击；逐目标伤害递减由攻击模块自身处理。</para>
/// <para>class_entity_data 格式："车数,攻击模块ID"（如 "3,300041"）。场上无存活敌人时本轮不触发。</para>
/// </summary>
public class BuffEntityPeriodicAttackRoad : BuffEntityPeriodic
{
    //同一条路多辆矿车的出车间隔(秒)
    protected float staggerInterval = 0.5f;
    //待出车的道路批次队列(每批=一轮，同轮内各路各出1辆)
    protected Queue<List<int>> queuePendingRoadBatch = new Queue<List<int>>();
    //出车间隔计时器
    protected float timeIntervalCurrent = 0;
    //本轮发射的攻击模块ID(触发时从 class_entity_data 解析缓存)
    protected long attackModeId = 0;

    #region 数据相关
    /// <summary>
    /// 清理数据（对象池复用前清空队列/计时器/攻击模块ID，防残留）
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        queuePendingRoadBatch.Clear();
        timeIntervalCurrent = 0;
        attackModeId = 0;
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加（base 维持周期触发；此处额外驱动待出车批次按间隔逐轮落地）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (queuePendingRoadBatch.Count == 0)
            return;
        timeIntervalCurrent += buffTime;
        if (timeIntervalCurrent >= staggerInterval)
        {
            timeIntervalCurrent = 0;
            var listRoad = queuePendingRoadBatch.Dequeue();
            for (int i = 0; i < listRoad.Count; i++)
            {
                LaunchCart(listRoad[i]);
            }
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制（一轮随机选路出车的起点）
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightData = gameFightLogic.fightData;
        //场上无存活敌人则本轮不触发（与闪电馈赠一致）
        var listEnemy = fightData.dlAttackCreatureEntity.List;
        bool hasAliveEnemy = false;
        if (!listEnemy.IsNull())
        {
            for (int i = 0; i < listEnemy.Count; i++)
            {
                if (listEnemy[i] != null && !listEnemy[i].IsDead())
                {
                    hasAliveEnemy = true;
                    break;
                }
            }
        }
        if (!hasAliveEnemy) return false;

        int roadNum = fightData.sceneRoadNum;
        if (roadNum <= 0) return false;

        //解析参数 "车数,攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"随机道路冲撞BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"车数,攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }
        int cartCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);

        //选路：车数≤路数时无放回随机抽；车数>路数时每路先各1辆，多出的随机重复分配（同路可多辆）
        List<int> listAllRoad = new List<int>(roadNum);
        for (int i = 1; i <= roadNum; i++)
        {
            listAllRoad.Add(i);
        }
        //洗牌（Fisher-Yates）
        for (int i = listAllRoad.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (listAllRoad[i], listAllRoad[j]) = (listAllRoad[j], listAllRoad[i]);
        }
        Dictionary<int, int> dicRoadCartNum = new Dictionary<int, int>();
        int baseRoadNum = Mathf.Min(cartCount, roadNum);
        for (int i = 0; i < baseRoadNum; i++)
        {
            dicRoadCartNum[listAllRoad[i]] = 1;
        }
        for (int i = baseRoadNum; i < cartCount; i++)
        {
            int randomRoad = listAllRoad[UnityEngine.Random.Range(0, listAllRoad.Count)];
            dicRoadCartNum[randomRoad] = dicRoadCartNum.TryGetValue(randomRoad, out int num) ? num + 1 : 1;
        }

        //编排出车批次：第1轮(各路第1辆)立即出车，第2轮起是同一路的下一辆、按间隔错开防重叠
        queuePendingRoadBatch.Clear();
        timeIntervalCurrent = 0;
        int maxCartNum = 0;
        foreach (var kv in dicRoadCartNum)
        {
            maxCartNum = Mathf.Max(maxCartNum, kv.Value);
        }
        for (int round = 0; round < maxCartNum; round++)
        {
            List<int> listRoundRoad = new List<int>();
            foreach (var kv in dicRoadCartNum)
            {
                if (kv.Value > round)
                {
                    listRoundRoad.Add(kv.Key);
                }
            }
            if (round == 0)
            {
                for (int i = 0; i < listRoundRoad.Count; i++)
                {
                    LaunchCart(listRoundRoad[i]);
                }
            }
            else
            {
                queuePendingRoadBatch.Enqueue(listRoundRoad);
            }
        }
        return true;
    }
    #endregion

    #region 矿车发射
    /// <summary>
    /// 向指定道路发射1辆矿车（沿路攻击模块；移动/命中/伤害递减/消失全部由攻击模式框架处理）
    /// </summary>
    protected void LaunchCart(int roadIndex)
    {
        if (attackModeId == 0)
        {
            LogUtil.LogError($"随机道路冲撞BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightData = gameFightLogic.fightData;
        //伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每辆车发车时实时取）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * buffEntityData.GetTriggerValue());
        if (attackDamage <= 0)
            return;

        //纯数据发射路径（照闪电落雷先例）：注入攻击者快照与起终点，矿车沿道路从最左端驶向右端
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        //不暴击（保持闪电不暴击的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = new Vector3(0.5f, 0, roadIndex);
        attackModeData.targetPos = new Vector3(0.5f + fightData.sceneRoadLength, 0, roadIndex);
        attackModeData.attackDirection = Vector3.right;
        attackModeData.attackedId = "";
        //只撞进攻方敌人，穿过自家防守防线不伤人
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
    }
    #endregion
}
