using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就处理器
/// 监听战斗/统计相关事件 -> 更新统计数据 -> 检查成就达成 -> 处理手动领奖
/// </summary>
public partial class AchievementHandler : BaseHandler<AchievementHandler, AchievementManager>
{
    #region 生命周期

    /// <summary>
    /// 初始化
    /// 注册全局事件监听(幂等)
    /// 说明: 运行期只负责累加统计数据(击杀/通关), 不做任何达成判定;
    /// "是否达成(Reached)"在打开成就界面时由 GetAchievementState 依据统计数据实时计算
    /// </summary>
    public void InitData()
    {
        if (manager.isInited)
            return;
        manager.isInited = true;
        //注册事件(仅用于累加统计数据)
        EventHandler.Instance.RegisterEvent<bool>(EventsInfo.Achievement_CreatureKill, OnEventCreatureKill);
        EventHandler.Instance.RegisterEvent<long, int>(EventsInfo.Achievement_ConquerComplete, OnEventConquerComplete);
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 生物被击杀回调
    /// </summary>
    /// <param name="isAttacker">true:进攻方被击杀(算玩家击杀敌方) false:防御方被击杀(不计入)</param>
    private void OnEventCreatureKill(bool isAttacker)
    {
        //只统计击杀进攻方(敌方); 源头已只为进攻方派发, 此处再做一次保险
        if (!isAttacker) return;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        var achievementData = userData.GetUserAchievementData();
        //运行期只累加统计数据(廉价), 不做达成判定
        achievementData.AddKillCount(1);
    }

    /// <summary>
    /// 征服模式通关回调
    /// </summary>
    /// <param name="worldId">通关的世界id</param>
    /// <param name="difficultyLevel">通关的难度等级</param>
    private void OnEventConquerComplete(long worldId, int difficultyLevel)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        var achievementData = userData.GetUserAchievementData();
        //运行期只累加统计数据, 不做达成判定
        achievementData.AddConquerCompleteCount(worldId, difficultyLevel, 1);
    }

    #endregion

    #region 成就状态/进度

    /// <summary>
    /// 实时计算成就状态(不持久化"达成"标记)
    /// 已领取 -> 读存档返回 Unlocked; 未领取 -> 按统计数据与目标值比对返回 Reached / NotReached
    /// </summary>
    /// <param name="info">成就配置</param>
    public AchievementStateEnum GetAchievementState(AchievementInfoBean info)
    {
        if (info == null) return AchievementStateEnum.NotReached;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return AchievementStateEnum.NotReached;
        var achievementData = userData.GetUserAchievementData();
        //已领取的成就直接返回已解锁(只有该状态持久化在存档)
        if (achievementData.IsAchievementUnlocked(info.id))
            return AchievementStateEnum.Unlocked;
        //未领取的成就按当前统计数据实时判定是否达成
        if (GetAchievementProgress(info) >= info.target_value)
            return AchievementStateEnum.Reached;
        return AchievementStateEnum.NotReached;
    }

    /// <summary>
    /// 获取某成就当前进度
    /// </summary>
    public long GetAchievementProgress(AchievementInfoBean info)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return 0;
        var achievementData = userData.GetUserAchievementData();
        switch (info.GetAchievementType())
        {
            case AchievementTypeEnum.Kill:
                return achievementData.GetTotalKillCount();
            case AchievementTypeEnum.PlayTime:
                return userData.gameTime;
            case AchievementTypeEnum.ConquerComplete:
                return achievementData.GetConquerCompleteCount(info.GetTargetWorldId(), info.target_extra);
            default:
                return 0;
        }
    }

    #endregion

    #region 手动解锁(领奖)

    /// <summary>
    /// 手动解锁成就(领取魔晶奖励)
    /// </summary>
    /// <returns>是否解锁成功</returns>
    public bool TryUnlockAchievement(long achievementId)
    {
        var info = AchievementInfoCfg.GetItemData(achievementId);
        if (info == null) return false;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return false;
        var achievementData = userData.GetUserAchievementData();
        //只有处于"达成未领取"状态才能领奖(实时判定)
        if (GetAchievementState(info) != AchievementStateEnum.Reached)
            return false;
        //发放奖励
        userData.AddCrystal(info.reward_crystal);
        //标记为已解锁(已领取) —— 只有该状态才持久化
        achievementData.SetAchievementUnlocked(achievementId);
        //领奖与发放的魔晶立即落盘
        GameDataHandler.Instance.manager.SaveUserData();
        return true;
    }

    #endregion
}
