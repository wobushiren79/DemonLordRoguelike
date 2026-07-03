/// <summary>
/// 时间条件属性BUFF：魔物在场存活时间达到阈值后，才把属性 modifier 注入管线。
/// <para>在场时间复用 buffEntityData.timeUpdateTotal（由 UpdateBuffTime 累积）；</para>
/// <para>该累积仅在游戏状态为 Gaming 时推进（GameHandler.Update 只在 Gaming 驱动 UpdateGame），</para>
/// <para>故关卡间暂停/深渊馈赠选择（Settlement 状态）期间不累计，符合"仅游戏中计时"要求。</para>
/// </summary>
public class BuffEntityConditionalAttributeTime : BuffEntityConditionalAttribute
{
    /// <summary>
    /// 累积在场时间；未达标前逐帧检测，跨过阈值当帧置 isPre 并刷新属性（达标后不再重复检测）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (!isPre)
        {
            HandleForEvent();
        }
    }
}
