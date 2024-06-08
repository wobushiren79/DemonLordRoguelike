using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHandler : BaseHandler<GameHandler,GameManager>
{   
    /// <summary>
    /// 开始游戏战斗
    /// </summary>
    public void StartGameFight(FightBean fightData)
    {
        if (manager.gameLogic == null)
            manager.gameLogic = new GameFightLogic();
        var gameLogic = manager.gameLogic as GameFightLogic;
        gameLogic.fightData = fightData;
        manager.gameLogic.PreGame();
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
