/// <summary>
/// 战斗生物实体-进攻生物专属逻辑
/// <para>进攻生物：从道路一端进攻魔王的敌方生物（CreatureFightTypeEnum.FightAttack）。</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 进攻生物-状态相关
    /// <summary>
    /// 改变路线（被诱导/嘲讽时切换道路 仅进攻生物响应）
    /// <para>由 AttackModeLure 调用，切换道路后触发诱导意图（AttackCreatureLured）。</para>
    /// </summary>
    /// <param name="targetRoadIndex">目标道路下标</param>
    public void ChangeRoad(int targetRoadIndex)
    {
        fightCreatureData.roadIndex = targetRoadIndex;
        var creatureFightType = fightCreatureData.creatureFightType;
        switch (creatureFightType)
        {
            case CreatureFightTypeEnum.FightAttack:
                aiEntity.ChangeIntent(AIIntentEnum.AttackCreatureLured);
                break;
        }
    }
    #endregion

    #region 进攻生物-死亡相关
    /// <summary>
    /// 死亡意图切换-进攻生物（由 SetCreatureDead 统一分发 非进攻生物自动跳过）
    /// </summary>
    public void SetCreatureDeadForAttack()
    {
        if (aiEntity is AIAttackCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.AttackCreatureDead);
        }
    }
    #endregion
}
