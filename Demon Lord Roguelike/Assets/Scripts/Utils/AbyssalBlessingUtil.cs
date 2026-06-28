using System.Collections.Generic;

/// <summary>
/// 深渊馈赠通用工具：提供与「属性管线 / 攻速管线」一致的查询口径，供 UI/逻辑判断馈赠对生物的作用。
/// </summary>
public static class AbyssalBlessingUtil
{
    /// <summary>
    /// 判断「某深渊馈赠 BUFF 是否实际作用于指定战斗类型的生物」。口径与属性管线(FightCreatureBean.CollectFromBuffList)
    /// 及攻速管线(BuffHandler.ChangeAttackTimeDataForBuff)完全一致，供战斗卡片展示「作用在本魔物身上的馈赠」复用，确保展示与实际效果同步。
    /// <para>判定三连：① trigger_creature_type 过滤(None 或 == 本生物战斗类型)；② 单体定向过滤(单体定向馈赠仅作用于锁定的那一只生物)；
    /// ③ 仅「会改变生物属性/攻速」的 BUFF(IAttributeModifierSource / BuffEntityAttributeAttackTime) 才算作用于生物，
    /// 借此排除掉落(钱多多)/奖励(奖励多多·再来一瓶)/复制(增殖)等不修改生物数值的馈赠。</para>
    /// <para>注意：单体定向馈赠按 UUID 精确匹配，复制魔物(增殖)产生的克隆体是新 UUID，故不会显示/继承针对原魔物的单体定向馈赠——这是预期行为(克隆体只继承全体性馈赠)。</para>
    /// </summary>
    /// <param name="buff">馈赠池中的某个 BUFF 实例</param>
    /// <param name="creatureData">目标生物数据</param>
    /// <param name="creatureFightType">目标生物的战斗类型(战斗卡片上的魔物为 FightDefense)</param>
    public static bool IsAbyssalBlessingTargetCreature(BuffBaseEntity buff, CreatureBean creatureData, CreatureFightTypeEnum creatureFightType)
    {
        if (buff == null || creatureData == null)
            return false;
        var buffEntityData = buff.buffEntityData;
        if (buffEntityData == null || !buffEntityData.isValid)
            return false;
        var buffInfo = buffEntityData.GetBuffInfo();
        if (buffInfo == null)
            return false;
        //① 生物类型过滤
        CreatureFightTypeEnum triggerCreatureType = buffInfo.GetTriggerCreatureType();
        if (triggerCreatureType != CreatureFightTypeEnum.None && triggerCreatureType != creatureFightType)
            return false;
        //② 单体定向过滤：仅作用于被随机锁定的那一只生物
        if (buff is IBuffSingleTarget singleTargetBuff && singleTargetBuff.SingleTargetCreatureUUId != creatureData.creatureUUId)
            return false;
        //③ 仅会改变生物属性/攻速的 BUFF 才算作用于生物
        return buff is IAttributeModifierSource || buff is BuffEntityAttributeAttackTime;
    }

    /// <summary>
    /// 收集「当前作用于指定生物」的所有深渊馈赠实体(去重到馈赠粒度：某馈赠的任一 BUFF 命中即收该馈赠一次)。
    /// 判定口径复用 <see cref="IsAbyssalBlessingTargetCreature"/>(与属性/攻速管线一致)：含全体防守加成(强身健体/伤害性极强等)
    /// 与定向到本魔物的馈赠(大力出奇迹等)，排除作用敌方/防守核心/掉落奖励/复制等不修改本魔物数值的馈赠。
    /// <para>供战斗卡片(UIViewCreatureCardItemForFight)展示「作用在本魔物身上的馈赠」图标等场景复用；result 复用传入(零分配)，内部先 Clear。</para>
    /// <para>注：克隆体(增殖)是新 UUID，单体定向馈赠按 UUID 精确匹配故不会被收集——预期行为(克隆体只显示全体性馈赠)。</para>
    /// </summary>
    /// <param name="creatureData">目标生物(为 null 时清空结果直接返回)</param>
    /// <param name="creatureFightType">目标生物战斗类型(战斗卡片上的魔物固定为 FightDefense)</param>
    /// <param name="result">收集结果(复用，内部先 Clear)</param>
    public static void CollectAbyssalBlessingEntityBean(CreatureBean creatureData, CreatureFightTypeEnum creatureFightType, List<AbyssalBlessingEntityBean> result)
    {
        result.Clear();
        if (creatureData == null)
            return;
        var dicAbyssalBlessing = BuffHandler.Instance.manager.dicAbyssalBlessingBuffsActivie;
        //键(馈赠实体)列表与值(BUFF 列表)严格同序，直接按下标配对取值，省一次字典查找
        for (int i = 0; i < dicAbyssalBlessing.ListKey.Count; i++)
        {
            var listBuff = dicAbyssalBlessing.List[i];
            if (listBuff == null)
                continue;
            //该馈赠的任一 BUFF 实际作用于本魔物即视为作用于本魔物
            for (int j = 0; j < listBuff.Count; j++)
            {
                if (IsAbyssalBlessingTargetCreature(listBuff[j], creatureData, creatureFightType))
                {
                    result.Add(dicAbyssalBlessing.ListKey[i]);
                    break;
                }
            }
        }
    }
}
