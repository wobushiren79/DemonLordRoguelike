/// <summary>
/// 前置条件：魔物在场存活时间达到阈值（单位秒）
/// <para>复用 buffEntityData.timeUpdateTotal（由 BUFF 逐帧 UpdateBuffTime 累积）；</para>
/// <para>该累积仅在游戏状态 Gaming 时推进，故暂停/深渊馈赠选择期间不计时。</para>
/// <para>EventRole 默认 None：不参与被攻击/攻击事件的归属过滤（纯时间驱动）。</para>
/// </summary>
public class BuffPreEntityForOnFieldTime : BuffBasePreEntity
{
    /// <summary>
    /// 检测是否满足前置条件：累计在场时间（秒）是否达到阈值
    /// </summary>
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        FightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creatureEntity == null)
        {
            return false;
        }
        //在场存活时间（仅游戏中累积）是否达到阈值
        if (buffEntityData.timeUpdateTotal >= preValue)
        {
            return true;
        }
        return false;
    }
}
