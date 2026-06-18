/// <summary>
/// 深渊馈赠「奖励多多」即时BUFF - 战斗通关领奖时获得的奖励物品数量+1
/// 选取馈赠时即触发，将加成累加到征服战斗数据(rewardAddItemNum)，BOSS通关领奖初始化时读取生效；
/// 与「增殖」一致采用即时触发模式，因此可重复选取叠加(不依赖BUFF在容器中常驻)
/// </summary>
public class BuffEntityInstantRewardMoreItem : BuffEntityInstant
{
    #region 触发逻辑
    /// <summary>
    /// 触发BUFF：为征服战斗数据累加1个领奖奖励物品数量
    /// </summary>
    /// <param name="buffEntityData">BUFF实体数据</param>
    public override bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffInstant(buffEntityData);
        if (isTriggerSuccess == false) return false;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic != null && gameFightLogic.fightData is FightBeanForConquer fightDataForConquer)
        {
            fightDataForConquer.rewardAddItemNum += 1;
        }
        return true;
    }
    #endregion
}
