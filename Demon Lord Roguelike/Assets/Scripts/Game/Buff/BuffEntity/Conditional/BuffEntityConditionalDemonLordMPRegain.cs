/// <summary>
/// 条件触发-魔王回复魔力（SR稀有度BUFF「左膀右臂/舍己为王」：满足前置条件后给魔王(防守核心)回复 trigger_value 点魔力）。
/// <para>前置条件走 pre_info（击杀数2001/累计承伤3001等），事件走 class_entity_events；回复经 FightCreatureBean.ChangeMP 自动钳制到 [0,MP上限]，并刷新魔王魔力显示。</para>
/// </summary>
public class BuffEntityConditionalDemonLordMPRegain : BuffBaseEntity
{
    #region 触发
    /// <summary>
    /// 触发BUFF：给魔王(防守核心)回复魔力
    /// </summary>
    public override bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffConditional(buffEntityData);
        if (isTriggerSuccess == false)
            return false;
        //获取魔王(防守核心)，核心不存在/已死亡不回复
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var coreCreature = gameFightLogic.fightData.fightDefenseCoreCreature;
        if (coreCreature == null || coreCreature.IsDead())
            return false;
        //按创建时随机好的触发值回复魔力（ChangeMP内部钳制[0,MP上限]），并刷新魔力显示
        coreCreature.fightCreatureData.ChangeMP(buffEntityData.GetTriggerValue(), out _, out _);
        coreCreature.RefreshMPShow();
        return true;
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 处理检测：满足前置条件则重置条件计数并触发
    /// </summary>
    public override void HandleForEvent()
    {
        base.HandleForEvent();
        if (CheckIsPre(buffEntityData))
        {
            buffEntityData.conditionalValue = 0;
            //触发BUFF
            TriggerBuffConditional(buffEntityData);
        }
    }
    #endregion
}
