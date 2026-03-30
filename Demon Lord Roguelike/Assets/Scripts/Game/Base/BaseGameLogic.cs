using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class BaseGameLogic : BaseEvent
{
    public GameStateEnum gameState; //游戏状态
        
    /// <summary>
    /// 准备游戏数据
    /// </summary>
    public virtual void PreGame()
    {
        ChangeGameState(GameStateEnum.Pre);
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public virtual void StartGame()
    {
        ChangeGameState(GameStateEnum.Gaming);
    }

    /// <summary>
    /// 游戏中
    /// </summary>
    public virtual void UpdateGame()
    {

    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    public virtual async void EndGame()
    {
        await ClearGame();
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public virtual async Task ClearGame()
    {
        ChangeGameState(GameStateEnum.End);
        UnRegisterAllEvent();
        System.GC.Collect();
    }

    /// <summary>
    /// 改变游戏状态
    /// </summary>
    /// <param name="gameState"></param>
    public virtual void ChangeGameState(GameStateEnum gameState)
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
}
