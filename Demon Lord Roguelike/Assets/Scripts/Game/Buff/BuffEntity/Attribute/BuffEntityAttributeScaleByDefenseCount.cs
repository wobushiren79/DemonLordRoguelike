/// <summary>
/// 动态属性 BUFF：加成率随「当前场上存活防守魔物数量」缩放——魔物越多，全体防守魔物该属性加成越高。
/// <para>加成率 = (存活防守魔物数 N - 1) * 每只加成率(配置 trigger_value_rate)。减 1 是扣除自身：只有 1 只时不加成。</para>
/// <para>通用功能类(非绑定单一馈赠)：任何"随友军数量增长的属性加成"效果均可复用。当前用于深渊馈赠「都是兄弟(攻/护/生)」。</para>
/// <para>配置 trigger_creature_type=1(仅作用防守魔物,不含核心)；class_entity_data 决定属性(ATK/DR/HP)。
/// 魔物增减(放置/死亡)时由 GameFightLogic 广播全体防守生物 RefreshBaseAttribute 使 N 变化即时生效。</para>
/// </summary>
public class BuffEntityAttributeScaleByDefenseCount : BuffEntityAttributeDynamicRate
{
    #region 动态加成率
    /// <summary>
    /// 加成率 = (存活防守魔物数 - 1) * 每只加成率；不足 2 只时为 0
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
        if (aliveNum <= 1) return 0f;
        return (aliveNum - 1) * buffEntityData.buffData.trigger_value_rate;
    }
    #endregion
}
