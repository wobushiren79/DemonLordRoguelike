using System.Collections.Generic;

/// <summary>
/// 动态属性 BUFF：选取时随机锁定一只防守生物，加成率随「该只生物自身累计击杀敌人数」缩放——只有被锁定那只越杀越强。
/// <para>加成率 = 锁定生物自身累计击杀数 * 每只加成率(配置 trigger_value_rate)。击杀数取 fightRecordsData.GetRecordsForCreatureData(锁定UUID).killNum
/// (按 creatureUUId 持久累积：该生物阵亡后下一关重新上场 UUID 不变，之前击杀加成保留；征服 run 内跨关卡不重置)。</para>
/// <para>通用功能类(非绑定单一馈赠)：任何"随单只自身累计击杀增长的属性加成"效果均可复用。当前用于深渊馈赠「杀红了眼(攻/护/生)」。</para>
/// <para>同时实现 <see cref="IBuffSingleTarget"/>：由 FightCreatureBean.CollectFromBuffList 把 modifier 限定到锁定那只生物；
/// 配置 trigger_creature_type=1 作防守类兜底过滤，class_entity_data 决定属性(ATK/DR/HP)。
/// 敌人死亡时由 GameFightLogic 广播全体防守生物 RefreshBaseAttribute 使击杀数增长即时生效(重算面偏大但结果正确)。</para>
/// </summary>
public class BuffEntityAttributeScaleByKillCount : BuffEntityAttributeDynamicRate, IBuffSingleTarget
{
    #region 单体定向
    private string singleTargetCreatureUUId;

    /// <summary>
    /// 选取馈赠时被随机锁定的单只防守生物 UUID（为空表示未锁定，加成不生效）
    /// </summary>
    public string SingleTargetCreatureUUId => singleTargetCreatureUUId;

    /// <summary>
    /// 设置数据：先按 class_entity_data 解析属性类型(父类逻辑)，再随机锁定一只防守生物
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        singleTargetCreatureUUId = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()?.fightData?.GetRandomDefenseCreatureUUId();
    }

    /// <summary>
    /// 清理数据：归还对象池前重置随机目标，避免复用串味
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        singleTargetCreatureUUId = null;
    }
    #endregion

    #region 动态加成率
    /// <summary>
    /// 加成率 = 锁定生物自身累计击杀数 * 每只加成率；未锁定或未击杀时为 0
    /// </summary>
    protected override float GetDynamicRate()
    {
        if (singleTargetCreatureUUId.IsNull()) return 0f;
        FightBean fightData = GetFightData();
        if (fightData == null || fightData.fightRecordsData == null) return 0f;
        var recordsData = fightData.fightRecordsData.GetRecordsForCreatureData(singleTargetCreatureUUId, false);
        if (recordsData == null) return 0f;
        long killNum = recordsData.killNum;
        if (killNum <= 0) return 0f;
        return killNum * buffEntityData.buffData.trigger_value_rate;
    }
    #endregion
}
