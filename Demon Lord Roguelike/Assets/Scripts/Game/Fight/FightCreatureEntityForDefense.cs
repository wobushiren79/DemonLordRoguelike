/// <summary>
/// 战斗生物实体-防守生物专属逻辑
/// <para>防守生物：玩家放置在道路上抵御进攻的魔物（CreatureFightTypeEnum.FightDefense）。</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 防守生物-死亡相关
    /// <summary>
    /// 死亡意图切换-防守生物（由 SetCreatureDead 统一分发 非防守生物自动跳过）
    /// </summary>
    public void SetCreatureDeadForDefense()
    {
        if (aiEntity is AIDefenseCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCreatureDead);
        }
    }
    #endregion
}
