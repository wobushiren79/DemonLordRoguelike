using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHandler : BaseHandler<GameHandler, GameManager>
{
    public override void Awake()
    {
        base.Awake();
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, EventForWorldEnterGameForBaseScene);
        EventHandler.Instance.RegisterEvent(EventsInfo.GameFightLogic_EndGame, EventForGameFightLogicEndGame);
        EventHandler.Instance.RegisterEvent<int>(EventsInfo.GameFightLogic_DropAddCrystal, EventForGameFightLogicDropAddCrystal);
        EventHandler.Instance.RegisterEvent<int>(EventsInfo.GameFightLogic_AddExp, EventForGameFightLogicAddExp);
    }

    /// <summary>
    /// Update
    /// </summary>
    public void Update()
    {
        if (manager.gameLogic != null && manager.gameLogic.gameState == GameStateEnum.Gaming)
        {
            manager.gameLogic.UpdateGame();
        }
    }

    #region  开始一种游戏模式
    /// <summary>
    /// 开始终焉议会
    /// </summary>
    /// <param name="doomCouncilInfo"></param>
    public void StartDoomCouncil(DoomCouncilBean doomCouncilData)
    {
        if (manager.gameLogic == null || manager.gameLogic is not DoomCouncilLogic)
            manager.gameLogic = new DoomCouncilLogic();
        var gameLogic = manager.gameLogic as DoomCouncilLogic;
        gameLogic.doomCouncilData = doomCouncilData;
        manager.gameLogic.PreGame();
    }

    /// <summary>
    /// 开始游戏战斗
    /// </summary>
    public void StartGameFight(FightBean fightData)
    {
        // 检查是否需要创建新的逻辑实例
        string fightTypeName = $"GameFightLogic{fightData.gameFightType.GetEnumName()}";
        manager.gameLogic = ReflexUtil.CreateInstance<GameFightLogic>(fightTypeName);
        GameFightLogic gameFightLogic = (GameFightLogic)manager.gameLogic;
        gameFightLogic.fightData = fightData;
        gameFightLogic.PreGame();
    }

    /// <summary>
    /// 开始扭蛋
    /// </summary>
    public void StartGashaponMachine(GashaponMachineBean gashaponMachineData)
    {
        if (manager.gameLogic == null || manager.gameLogic is not GashaponMachineLogic)
            manager.gameLogic = new GashaponMachineLogic();
        var gameLogic = manager.gameLogic as GashaponMachineLogic;
        gameLogic.gashaponMachineData = gashaponMachineData;
        manager.gameLogic.PreGame();
    }

    /// <summary>
    /// 开始献祭
    /// </summary>
    public void StartCreatureSacrifice(CreatureSacrificeBean creatureSacrificeData)
    {
        if (manager.gameLogic == null)
            manager.gameLogic = new CreatureSacrificeLogic();
        var gameLogic = manager.gameLogic as CreatureSacrificeLogic;
        gameLogic.creatureSacrificeData = creatureSacrificeData;
        manager.gameLogic.PreGame();
    }
    #endregion

    #region 回调事件通知
    /// <summary>
    /// 场景切换-基地
    /// </summary>
    public void EventForWorldEnterGameForBaseScene()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userTempData = userData.GetUserTempData();
        userTempData.TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.WorldEnterGameForBaseScene);
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void EventForGameFightLogicEndGame()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userTempData = userData.GetUserTempData();
        userTempData.TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.GameFightLogicEndGame);
    }

    /// <summary>
    /// 掉落宝石拾取
    /// </summary>
    public void EventForGameFightLogicDropAddCrystal(int addCrystal)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userTempData = userData.GetUserTempData();
        userTempData.TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.GameFightLogicDropAddCrystal, dataInt: addCrystal);
    }

    /// <summary>
    /// 增加经验
    /// </summary>
    public void EventForGameFightLogicAddExp(int addExp)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userTempData = userData.GetUserTempData();
        userTempData.TriggerDoomCouncil(TriggerTypeDoomCouncilEntityEnum.GameFightLogicAddExp, dataInt: addExp);
    }
    #endregion
}
