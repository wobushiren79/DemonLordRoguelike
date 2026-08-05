/// <summary>
/// 条件触发-上场无敌（SSR稀有度BUFF「真男人」）：BUFF挂上后自身进入无敌状态（免疫 UnderAttack 伤害），
/// 持续 trigger_value 秒（扭蛋roll 5~10秒，仅Gaming状态计时），期间 color_body 染金色；
/// 到期清除无敌旗标并移除BUFF，颜色随属性刷新还原。
/// </summary>
public class BuffEntityConditionalInvincible : BuffEntityConditional
{
    #region 数据相关
    /// <summary>
    /// 设置数据：开启无敌并刷新颜色上屏（生成时挂BUFF不会自动刷视图颜色，需实体自理）
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        base.SetData(buffEntityData);
        SetInvincible(true);
    }

    /// <summary>
    /// 清理数据：兜底清无敌旗标（外部移除路径，如死亡清BUFF）
    /// </summary>
    public override void ClearData()
    {
        SetInvincible(false);
        base.ClearData();
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加：无敌时长到期后清旗标并移除BUFF
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (buffEntityData == null || buffEntityData.isValid == false) return;
        if (buffEntityData.timeUpdateTotal >= buffEntityData.GetTriggerValue())
        {
            SetInvincible(false);
            buffEntityData.isValid = false;
        }
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 设置自身无敌状态并刷新属性/身体颜色（颜色经 color_body 随BUFF存续自动生效/还原）
    /// </summary>
    /// <param name="isInvincible">是否无敌</param>
    protected virtual void SetInvincible(bool isInvincible)
    {
        if (buffEntityData == null) return;
        var targetCreature = GetFightCreatureEntityForTarget();
        if (targetCreature == null || targetCreature.fightCreatureData == null) return;
        targetCreature.fightCreatureData.isInvincible = isInvincible;
        targetCreature.fightCreatureData.RefreshBaseAttribute();
        targetCreature.RefreshBodyColor();
    }
    #endregion
}
