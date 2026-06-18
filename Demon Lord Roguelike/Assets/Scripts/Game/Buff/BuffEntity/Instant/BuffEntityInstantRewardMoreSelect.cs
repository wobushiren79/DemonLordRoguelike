/// <summary>
/// 深渊馈赠「再来一瓶」即时BUFF - 战斗通关领奖时可选择的奖励次数+1
/// 选取馈赠时即触发，将加成累加到征服战斗数据(rewardAddSelectNum)，BOSS通关领奖初始化时读取生效；
/// 与「增殖」一致采用即时触发模式，因此可重复选取叠加(不依赖BUFF在容器中常驻)。
/// 注意：可选次数最终会被裁剪到不超过实际奖励数量，超出的次数无对应宝箱可开，不会生效
/// </summary>
public class BuffEntityInstantRewardMoreSelect : BuffEntityInstant
{
    #region 触发逻辑
    /// <summary>
    /// 触发BUFF：为征服战斗数据累加1次领奖可选择次数
    /// </summary>
    /// <param name="buffEntityData">BUFF实体数据</param>
    public override bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffInstant(buffEntityData);
        if (isTriggerSuccess == false) return false;

        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (gameFightLogic != null && gameFightLogic.fightData is FightBeanForConquer fightDataForConquer)
        {
            fightDataForConquer.rewardAddSelectNum += 1;
        }
        return true;
    }
    #endregion
}
