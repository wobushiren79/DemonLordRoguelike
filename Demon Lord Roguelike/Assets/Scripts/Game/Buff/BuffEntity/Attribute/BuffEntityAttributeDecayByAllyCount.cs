using UnityEngine;

/// <summary>
/// 动态属性 BUFF：加成率随「当前场上存活防守魔物数量」反向衰减——友方越少，持有者该属性加成越高(独行者)。
/// <para>加成率 = 基础加成率(配置 trigger_value_rate) - 每友方衰减率(配置 trigger_value, 存百分点: 50=每个友方-50%) * (存活防守魔物数 N - 1)，下限 0。</para>
/// <para>减 1 是扣除自身：只有 1 只时吃满基础加成；与「都是兄弟」(BuffEntityAttributeScaleByDefenseCount) 同口径数友方、方向相反。</para>
/// <para>多持有者各算各的：BUFF 挂在持有者自身互不影响(如 2 只在场，各按 N-1=1 衰减 1 级)。</para>
/// <para>通用功能类(非绑定单一BUFF)：任何"随友军数量衰减的属性加成"效果均可复用。当前用于 R 稀有度BUFF「独行者(攻/速/护/生)」。</para>
/// <para>配置 trigger_creature_type=1(仅作用防守魔物,不含核心)；class_entity_data 决定属性(ATK/ASPD/DR/HP)。
/// 魔物增减(放置/死亡)时由 GameFightLogic 广播全体防守生物 RefreshBaseAttribute 使衰减即时生效(需 BuffManager.hasDynamicRateCreatureBuff 门控放行)。</para>
/// <para>注意：trigger_value 在扭蛋 isRandom 创建时按整数闭区间随机(RoundToInt)，故衰减率按百分点整数配置(50而非0.5)，本类内 /100 还原。</para>
/// </summary>
public class BuffEntityAttributeDecayByAllyCount : BuffEntityAttributeDynamicRate
{
    #region 动态加成率
    /// <summary>
    /// 加成率 = 基础加成率 - 每友方衰减率 * (存活防守魔物数 - 1)；衰减至 0 后不再加成(不会变负)
    /// </summary>
    protected override float GetDynamicRate()
    {
        FightBean fightData = GetFightData();
        var listDefenseEntity = fightData?.dlDefenseCreatureEntity?.List;
        if (listDefenseEntity == null) return 0f;
        //数出当前场上存活的防守魔物数量(排除已死亡)
        int aliveNum = 0;
        for (int i = 0; i < listDefenseEntity.Count; i++)
        {
            var itemEntity = listDefenseEntity[i];
            if (itemEntity == null || itemEntity.fightCreatureData == null || itemEntity.IsDead())
                continue;
            aliveNum++;
        }
        //trigger_value 复用为每友方衰减百分点(50=50%)，/100 还原；扣除自身(aliveNum-1)后每多1友方衰减1级，下限0
        float decayRate = buffEntityData.buffData.trigger_value / 100f;
        float rate = buffEntityData.buffData.trigger_value_rate - decayRate * (aliveNum - 1);
        return Mathf.Max(0f, rate);
    }
    #endregion
}
