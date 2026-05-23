using UnityEngine;

/// <summary>
/// BUFF前置条件在"被攻击/攻击"事件中关注的角色
/// 用于让 BuffBaseEntity.EventForUnderAttack 判断本次事件是否要响应
/// </summary>
public enum BuffPreEventRole
{
    /// <summary>
    /// 不关注被攻击/攻击事件（如KillNum），事件归属过滤直接放行
    /// </summary>
    None = 0,
    /// <summary>
    /// 关注"BUFF目标作为被攻击者"的事件（如HPRateLess、UnderAttackDamage）
    /// </summary>
    Attacked = 1,
    /// <summary>
    /// 关注"BUFF目标作为攻击者"的事件（如AttackDamage）
    /// </summary>
    Attacker = 2,
}

public class BuffBasePreEntity
{
    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public virtual bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        return false;
    }

    /// <summary>
    /// 该前置条件在 UnderAttack 事件中关注的角色
    /// 默认 None：不参与归属过滤
    /// </summary>
    public virtual BuffPreEventRole GetEventRole()
    {
        return BuffPreEventRole.None;
    }

    /// <summary>
    /// 获取触发生物
    /// </summary>
    public FightCreatureEntity GetTargetCreatureEntity(string creatureId)
    {
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic == null)
        {
            return null;
        }
        var creatureEntity = gameFightLogic.fightData.GetCreatureById(creatureId);
        if (creatureEntity == null)
        {
            return null;
        }
        return creatureEntity;
    }
}
