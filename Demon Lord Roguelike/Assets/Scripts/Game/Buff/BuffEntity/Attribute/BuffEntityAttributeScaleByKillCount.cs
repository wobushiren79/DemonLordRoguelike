/// <summary>
/// 动态属性 BUFF：加成率随「本局累计击杀敌人数」缩放——每击杀一只敌人，全体防守魔物该属性永久增长。
/// <para>加成率 = 本局累计防守方击杀敌人数 * 每只加成率(配置 trigger_value_rate)。击杀数取 fightRecordsData.totalKillNumForDef(仅魔物击杀,跨关卡累积、征服run内不重置,累积至BOSS关)。</para>
/// <para>通用功能类(非绑定单一馈赠)：任何"随累计击杀增长的属性加成"效果均可复用。当前用于深渊馈赠「杀红了眼(攻/护/生)」。</para>
/// <para>配置 trigger_creature_type=1(仅作用防守魔物,不含核心)；class_entity_data 决定属性(ATK/DR/HP)。
/// 敌人死亡时由 GameFightLogic 广播全体防守生物 RefreshBaseAttribute 使击杀数增长即时生效。</para>
/// </summary>
public class BuffEntityAttributeScaleByKillCount : BuffEntityAttributeDynamicRate
{
    #region 动态加成率
    /// <summary>
    /// 加成率 = 本局累计击杀敌人数 * 每只加成率；未击杀时为 0
    /// </summary>
    protected override float GetDynamicRate()
    {
        FightBean fightData = GetFightData();
        if (fightData == null || fightData.fightRecordsData == null) return 0f;
        long killNum = fightData.fightRecordsData.totalKillNumForDef;
        if (killNum <= 0) return 0f;
        return killNum * buffEntityData.buffData.trigger_value_rate;
    }
    #endregion
}
