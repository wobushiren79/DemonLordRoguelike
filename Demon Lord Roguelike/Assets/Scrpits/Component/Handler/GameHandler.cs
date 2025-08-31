using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHandler : BaseHandler<GameHandler,GameManager>
{

    // 定义战斗类型与逻辑类型的映射
    static Dictionary<GameFightTypeEnum, Type> dicGamefightLogicType = new Dictionary<GameFightTypeEnum, Type>
    {
        { GameFightTypeEnum.Conquer, typeof(GameFightLogicConquer) },
        { GameFightTypeEnum.Infinite, typeof(GameFightLogicInfinite) },
        { GameFightTypeEnum.Test, typeof(GameFightLogicTest) }
    };

    /// <summary>
    /// 开始游戏战斗
    /// </summary>
    public void StartGameFight(FightBean fightData)
    {
        // 检查是否需要创建新的逻辑实例
        Type targetType = dicGamefightLogicType.GetValueOrDefault(fightData.gameFightType, typeof(GameFightLogic));
        if (manager.gameLogic?.GetType() != targetType)
        {
            manager.gameLogic = (GameFightLogic)Activator.CreateInstance(targetType);
        }
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

    /// <summary>
    /// 创建游戏世界地图数据
    /// </summary>
    public GameWorldMapBean CreateGameWorldMapData(long worldId)
    {
        GameWorldMapBean gameWorldMapData = new GameWorldMapBean(worldId);
        return gameWorldMapData;
    }

    /// <summary>
    /// 结束游戏战斗-强制
    /// </summary>
    public void EndGameFight()
    {
        if (manager.gameLogic == null)
            return;
        manager.gameLogic.EndGame();
    }

    /// <summary>
    /// 结束生物献祭-强制
    /// </summary>
    public void EndCreatureSacrifice()
    {
        if (manager.gameLogic == null)
            return;
        manager.gameLogic.EndGame();
    }

    /// <summary>
    /// Update
    /// </summary>
    public void Update()
    {
        if (manager.gameState == GameStateEnum.Gaming && manager.gameLogic != null)
        {
            manager.gameLogic.UpdateGame();
        }
    }
}
