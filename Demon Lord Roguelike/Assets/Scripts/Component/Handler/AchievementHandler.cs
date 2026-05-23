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
    /// </summary>
    public void InitData()
    {
        if (manager.isInited)
            return;
        manager.isInited = true;
        //注册事件
        EventHandler.Instance.RegisterEvent<bool>(EventsInfo.Achievement_CreatureKill, OnEventCreatureKill);
        EventHandler.Instance.RegisterEvent<int>(EventsInfo.Achievement_ConquerComplete, OnEventConquerComplete);
        EventHandler.Instance.RegisterEvent(EventsInfo.Achievement_GameTimeChange, OnEventGameTimeChange);
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 生物被击杀回调
    /// </summary>
    /// <param name="isAttacker">true:进攻方被击杀(算玩家击杀敌方) false:防御方被击杀(不计入)</param>
    private void OnEventCreatureKill(bool isAttacker)
    {
        //只统计击杀进攻方(敌方)
        if (!isAttacker) return;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        var achievementData = userData.GetUserAchievementData();
        achievementData.AddKillCount(1);
        CheckAchievementsByType(AchievementTypeEnum.Kill);
    }

    /// <summary>
    /// 征服模式通关回调
    /// </summary>
    /// <param name="difficultyLevel">通关的难度等级</param>
    private void OnEventConquerComplete(int difficultyLevel)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        var achievementData = userData.GetUserAchievementData();
        achievementData.AddConquerCompleteCount(difficultyLevel, 1);
        CheckAchievementsByType(AchievementTypeEnum.ConquerComplete);
    }

    /// <summary>
    /// 游戏时间变化回调(每秒触发一次)
    /// </summary>
    private void OnEventGameTimeChange()
    {
        CheckAchievementsByType(AchievementTypeEnum.PlayTime);
    }

    #endregion

    #region 成就达成判定

    /// <summary>
    /// 按类型批量检查成就达成情况
    /// </summary>
    public void CheckAchievementsByType(AchievementTypeEnum achievementType)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return;
        var achievementData = userData.GetUserAchievementData();

        var allList = manager.GetAllAchievementsSorted();
        for (int i = 0; i < allList.Count; i++)
        {
            var info = allList[i];
            if (info.GetAchievementType() != achievementType)
                continue;
            //只检查"未达成"的成就
            var state = achievementData.GetAchievementState(info.id);
            if (state != AchievementStateEnum.NotReached)
                continue;

            long curProgress = GetAchievementProgress(info);
            if (curProgress >= info.target_value)
            {
                achievementData.SetAchievementState(info.id, AchievementStateEnum.Reached);
            }
            EventHandler.Instance.TriggerEvent(EventsInfo.Achievement_ProgressChange, info.id);
        }
    }

    /// <summary>
    /// 全量检查所有成就(UI 打开时调用)
    /// </summary>
    public void CheckAllAchievements()
    {
        CheckAchievementsByType(AchievementTypeEnum.Kill);
        CheckAchievementsByType(AchievementTypeEnum.PlayTime);
        CheckAchievementsByType(AchievementTypeEnum.ConquerComplete);
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
                return achievementData.GetConquerCompleteCount(info.target_extra);
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
        var state = achievementData.GetAchievementState(achievementId);
        //只有处于"达成未解锁"状态才能领奖
        if (state != AchievementStateEnum.Reached)
            return false;
        //发放奖励
        userData.AddCrystal(info.reward_crystal);
        //更新状态
        achievementData.SetAchievementState(achievementId, AchievementStateEnum.Unlocked);
        return true;
    }

    #endregion
}
