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
    /// "是否达成(Reached)"在打开成就界面时由 GetCurrentLevelState 依据统计数据实时计算
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

    #region 成就进度/统计

    /// <summary>
    /// 获取某成就当前统计进度(原始累计值, 与各级目标值比对用)
    /// </summary>
    public long GetAchievementProgress(AchievementInfoBean info)
    {
        if (info == null) return 0;
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
            case AchievementTypeEnum.ConquerWorldClear:
                //进度 = 该世界【已通关的不同难度个数】(通关次数≥1的难度种类数); 各级目标值为 1..N 种
                return achievementData.GetClearedDifficultyCountByWorld(info.GetTargetWorldId());
            default:
                return 0;
        }
    }

    #endregion

    #region 等级(单行多级)

    /// <summary>
    /// 获取该成就已领取的等级数(0=尚未领取任何等级)
    /// </summary>
    public int GetClaimedLevelCount(AchievementInfoBean info)
    {
        if (info == null) return 0;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return 0;
        return userData.GetUserAchievementData().GetClaimedLevelCount(info.id);
    }

    /// <summary>
    /// 获取该成就"当前激活等级"的0基索引(= 已领取等级数)。
    /// 即下一个待领取的等级; 当 &gt;= 等级总数 时表示整族已完成。
    /// </summary>
    public int GetCurrentLevelIndex(AchievementInfoBean info)
    {
        return GetClaimedLevelCount(info);
    }

    /// <summary>
    /// 该成就是否已整族完成(已领取等级数 &gt;= 等级总数)
    /// </summary>
    public bool IsCompleted(AchievementInfoBean info)
    {
        if (info == null) return false;
        return GetClaimedLevelCount(info) >= info.GetLevelCount();
    }

    /// <summary>
    /// 实时计算"当前激活等级"的状态(不持久化"达成"标记)。
    /// 整族完成 -> Unlocked; 否则按 当前激活等级目标值 vs 统计数据 返回 Reached / NotReached。
    /// 等级门控天然成立: 当前激活等级 = 已领取数, 必须逐级领取, 无法跳级。
    /// </summary>
    public AchievementStateEnum GetCurrentLevelState(AchievementInfoBean info)
    {
        if (info == null) return AchievementStateEnum.NotReached;
        int levelCount = info.GetLevelCount();
        int currentIndex = GetClaimedLevelCount(info);
        //已领取数达到/超过总级数 -> 整族完成
        if (currentIndex >= levelCount) return AchievementStateEnum.Unlocked;
        //当前激活等级: 统计数据达到该级目标值则可领取
        if (GetAchievementProgress(info) >= info.GetLevelTargetValue(currentIndex))
            return AchievementStateEnum.Reached;
        return AchievementStateEnum.NotReached;
    }

    #endregion

    #region 手动领取(下一等级)

    /// <summary>
    /// 领取该成就的"当前激活等级"(发放该级魔晶奖励, 已领取等级数+1)。
    /// 仅当前激活等级处于 Reached(达成未领取) 时成功; 整族完成或未达成时返回 false。
    /// </summary>
    /// <param name="achievementId">成就ID</param>
    /// <returns>是否领取成功</returns>
    public bool TryUnlockNextLevel(long achievementId)
    {
        var info = AchievementInfoCfg.GetItemData(achievementId);
        if (info == null) return false;
        var userData = GameDataHandler.Instance.manager.GetUserData();
        if (userData == null) return false;
        var achievementData = userData.GetUserAchievementData();
        int levelCount = info.GetLevelCount();
        int currentIndex = achievementData.GetClaimedLevelCount(achievementId);
        //整族已完成
        if (currentIndex >= levelCount) return false;
        //当前激活等级是否达成
        if (GetAchievementProgress(info) < info.GetLevelTargetValue(currentIndex))
            return false;
        //发放当前激活等级奖励
        userData.AddCrystal(info.GetLevelReward(currentIndex));
        //已领取等级数 +1(持久化)
        achievementData.SetClaimedLevelCount(achievementId, currentIndex + 1);
        //领奖与发放的魔晶立即落盘
        GameDataHandler.Instance.manager.SaveUserData();
        return true;
    }

    #endregion
}
