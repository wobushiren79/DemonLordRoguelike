using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager : BaseManager
{
    public GameStateEnum gameState; //游戏状态
    public GameBaseLogic gameLogic;//战斗逻辑

    /// <summary>
    /// 设置游戏状态
    /// </summary>
    /// <param name="gameState"></param>
    public void SetGameState(GameStateEnum gameState)
    {
        this.gameState = gameState;
    }

    /// <summary>
    /// 获取游戏状态
    /// </summary>
    public GameStateEnum GetGameState()
    {
        return gameState;
    }

    /// <summary>
    /// 获取游戏逻辑
    /// </summary>
    public T GetGameLogic<T>() where T : GameBaseLogic
    {
        if (gameLogic is T logic)
        {
            return logic;
        }
        return null;
    }
}
