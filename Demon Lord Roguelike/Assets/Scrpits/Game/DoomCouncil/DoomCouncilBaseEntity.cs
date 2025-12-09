public class DoomCouncilBaseEntity
{
    public long doomCouncilBillId;

    /// <summary>
    /// 首次添加时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerFirst()
    {
        return false;
    }

    /// <summary>
    /// 战斗时掉落魔晶拾取
    /// </summary>
    /// <returns></returns>
    public virtual bool TriggerGameFightLogicDropAddCrystal(int addCrystal)
    {
        return false;
    }

    /// <summary>
    /// 战斗时添加经验
    /// </summary>
    /// <returns></returns>
    public virtual bool TriggerGameFightLogicAddExp(int addExp)
    {
        return false;
    }

    /// <summary>
    /// 战斗结束时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerGameFightLogicEndGame()
    {
        return false;
    }

    /// <summary>
    /// 进入基地时触发
    /// </summary>
    /// <returns>是否结束</returns>
    public virtual bool TriggerWorldEnterGameForBaseScene()
    {
        return false;
    }
}