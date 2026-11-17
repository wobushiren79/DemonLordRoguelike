using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 策反死者：持有者击杀进攻生物后召唤骷髅战士，继承死者的生命上限、攻击力和移速。
/// </summary>
public class BuffEntityConditionalSummonSkeletons : BuffEntityConditional
{
    #region 配置与生命周期
    private NpcInfoBean summonNpcInfo;
    private long defenseCreatureId;

    /// <summary>
    /// 从“NPC ID:防守职业 ID”配置取得骷髅战士模板。
    /// </summary>
    public override void SetData(BuffEntityBean buffEntityData)
    {
        summonNpcInfo = null;
        defenseCreatureId = 0;
        var pair = buffEntityData.GetBuffInfo()?.class_entity_data?.Split(':');
        if (pair?.Length == 2
            && long.TryParse(pair[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long npcId)
            && long.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long creatureId)
            && CreatureInfoCfg.GetItemData(creatureId)?.GetCreatureType() == CreatureTypeEnum.GashaponMachine)
        {
            summonNpcInfo = NpcInfoCfg.GetItemData(npcId);
            defenseCreatureId = creatureId;
        }

        base.SetData(buffEntityData);
    }

    /// <summary>
    /// 清除缓存的召唤模板和基类事件订阅。
    /// </summary>
    public override void ClearData()
    {
        summonNpcInfo = null;
        defenseCreatureId = 0;
        base.ClearData();
    }
    #endregion

    #region 击杀事件
    /// <summary>
    /// 只接受持有者归属明确的有效致死伤害，且召唤物自身不能触发。
    /// </summary>
    public override void EventForUnderAttackDead(FightUnderAttackBean fightUnderAttack)
    {
        if (buffEntityData == null || !buffEntityData.isValid || fightUnderAttack == null
            || fightUnderAttack.attackerId != buffEntityData.targetCreatureUUId || fightUnderAttack.damageApplied <= 0
            || summonNpcInfo == null)
        {
            return;
        }

        var fightData = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>()?.fightData;
        var owner = fightData?.GetCreatureById(fightUnderAttack.attackerId, CreatureFightTypeEnum.FightDefense);
        if (owner == null || owner.fightCreatureData.isSummoned)
            return;

        var killed = fightData.GetCreatureById(fightUnderAttack.attackedId, CreatureFightTypeEnum.FightAttack);
        if (killed == null || !killed.IsDead())
            return;

        var killedData = killed.fightCreatureData;
        int roadIndex = killedData.roadIndex;
        int summonCount = Mathf.RoundToInt(buffEntityData.GetTriggerValue());
        if (summonCount <= 0 || roadIndex < 1 || roadIndex > fightData.sceneRoadNum || !TriggerBuffConditional(buffEntityData))
            return;

        Vector3 deathPosition = killedData.positionDead;
        //继承死者已结算的最终属性作为本批骷髅的基础值，保留小数。
        var inheritedAttributes = new Dictionary<CreatureAttributeTypeEnum, float>
        {
            [CreatureAttributeTypeEnum.HP] = killedData.GetAttribute(CreatureAttributeTypeEnum.HP),
            [CreatureAttributeTypeEnum.ATK] = killedData.GetAttribute(CreatureAttributeTypeEnum.ATK),
            [CreatureAttributeTypeEnum.MSPD] = killedData.GetAttribute(CreatureAttributeTypeEnum.MSPD),
        };
        for (int i = 0; i < summonCount; i++)
        {
            CreatureHandler.Instance.CreateSummonedDefenseCreature(summonNpcInfo, defenseCreatureId, deathPosition, roadIndex, inheritedAttributes);
        }
    }
    #endregion
}
