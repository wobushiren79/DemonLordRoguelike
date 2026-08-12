using UnityEngine;

/// <summary>
/// 条件触发-荆棘反伤（SSR稀有度BUFF「荆棘之躯」）：自身受到攻击时，把名义承伤×rate反弹给攻击者，
/// 反弹伤害上限为自身当前生命值；反伤走正常 UnderAttack 管线（伤害数字/伤害统计/击杀事件）。
/// </summary>
public class BuffEntityConditionalThorns : BuffEntityConditional
{
    #region 事件回调
    /// <summary>
    /// 事件触发-被攻击（整体重写做归属过滤：仅自己挨打才反弹；不调base避免空pre_info时全场攻击都累积条件值）
    /// </summary>
    public override void EventForUnderAttack(FightUnderAttackBean fightUnderAttack)
    {
        if (buffEntityData == null || buffEntityData.isValid == false) return;
        //必须是自己挨打；攻击者是自己时不处理（防自残循环）
        if (!fightUnderAttack.attackedId.Equals(buffEntityData.targetCreatureUUId)) return;
        if (fightUnderAttack.attackerId.Equals(buffEntityData.targetCreatureUUId)) return;
        //获取自身与攻击者
        var selfCreature = GetFightCreatureEntityForTarget();
        if (selfCreature == null || selfCreature.fightCreatureData == null || selfCreature.IsDead()) return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var attackerCreature = gameFightLogic.fightData.GetCreatureById(fightUnderAttack.attackerId, CreatureFightTypeEnum.None);
        if (attackerCreature == null || attackerCreature.fightCreatureData == null || attackerCreature.IsDead()) return;
        //反弹伤害=名义承伤×rate（与「痛楚馈赠」同口径），上限为自身当前生命值
        float rate = buffEntityData.GetTriggerValueRate();
        int reflectDamage = Mathf.Min((int)(fightUnderAttack.attackerDamage * rate), selfCreature.fightCreatureData.HPCurrent);
        if (reflectDamage <= 0) return;
        //触发判定（几率+粒子）
        if (!TriggerBuffConditional(buffEntityData)) return;
        //造反伤数据：稀有度自BUFF applier==target==自己，bean默认attackerId=自己，被攻击者改写为原攻击者
        FightUnderAttackBean reflectData = FightHandler.Instance.GetFightUnderAttackData(buffEntityData, reflectDamage);
        reflectData.attackedId = fightUnderAttack.attackerId;
        attackerCreature.UnderAttack(reflectData);
    }
    #endregion
}
