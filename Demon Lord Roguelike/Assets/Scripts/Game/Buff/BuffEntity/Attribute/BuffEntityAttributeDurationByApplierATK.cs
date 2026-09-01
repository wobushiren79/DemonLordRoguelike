/// <summary>
/// 属性BUFF-持续时间按施加者攻击力加成（如烂泥史莱姆(id=3003)的「粘液减速」1000200001）：
/// 实际持续时间 = 配置 trigger_time + 施加者当前ATK × <see cref="DurationAddPerATK"/>。
/// 属性修正部分（如 MSPD -40%）仍走基类 BuffEntityAttribute 的 modifier 管线不变，本类只接管时长门槛。
/// </summary>
public class BuffEntityAttributeDurationByApplierATK : BuffEntityAttribute
{
    /// <summary>每点施加者ATK增加的持续秒数</summary>
    public const float DurationAddPerATK = 0.1f;

    /// <summary>ATK折算的额外持续秒数快照（施加者读不到时沿用最后一次有效值）</summary>
    protected float durationAddCache = 0;

    #region 数据相关
    /// <summary>
    /// 清理数据
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        durationAddCache = 0;
    }
    #endregion

    #region 时长门槛
    /// <summary>
    /// 动态持续时间：配置 trigger_time + 施加者ATK×系数；施加者在场时实时刷新快照（Refresh堆叠刷新施加者后自然跟随），离场/死亡则用最后快照兜底
    /// </summary>
    protected override float GetTriggerTimeForUpdate()
    {
        var applierCreature = GetFightCreatureEntityForApplier();
        if (applierCreature != null && applierCreature.fightCreatureData != null && !applierCreature.IsDead())
        {
            durationAddCache = applierCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK) * DurationAddPerATK;
        }
        return buffEntityData.GetTriggerTime() + durationAddCache;
    }
    #endregion
}
