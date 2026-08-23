using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天降范围-随机落点：发射时从全场存活防守生物中随机选取落点目标（替换 attacked 实参），
/// 其余下落/范围结算/叠BUFF 逻辑全部复用父类 AttackModeFalluponArea。
/// 用于火/水大魔法师BOSS技能（攻击模块 700001 陨石 / 700002 水滴，经 AttackModeExtInfo 100002/100003 以 5 秒 BossSkill 挂载）。
/// </summary>
public class AttackModeFalluponAreaRandom : AttackModeFalluponArea
{
    /// <summary>
    /// 随机落点最大重试次数（死亡实体偶发残留时换抽，全落空则沿用原目标）
    /// </summary>
    private const int RandomRetryMax = 3;

    #region 攻击入口
    /// <summary>
    /// 攻击-生物：先把 attacked 换成随机防守生物再调父类（targetPos 随之变为随机落点）
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        FightCreatureEntity randomTarget = GetRandomDefenseCreature();
        if (randomTarget != null)
        {
            attacked = randomTarget;
        }
        base.StartAttack(attacker, attacked, actionForAttackEnd);
    }
    #endregion

    #region 随机目标
    /// <summary>
    /// 从场上存活防守生物中随机取一只作为落点（不含魔王核心；最多重试 RandomRetryMax 次，全落空返回null沿用原目标）
    /// </summary>
    private FightCreatureEntity GetRandomDefenseCreature()
    {
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        List<FightCreatureEntity> listDefense = fightLogic?.fightData?.dlDefenseCreatureEntity?.List;
        if (listDefense.IsNull())
        {
            return null;
        }
        int count = listDefense.Count;
        for (int i = 0; i < RandomRetryMax; i++)
        {
            var candidate = listDefense[UnityEngine.Random.Range(0, count)];
            if (candidate != null && !candidate.IsDead() && candidate.creatureObj != null)
            {
                return candidate;
            }
        }
        return null;
    }
    #endregion
}
