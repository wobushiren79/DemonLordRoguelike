using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class BaseGameLogic : BaseEvent
{
    /// <summary>
    /// 准备游戏数据
    /// </summary>
    public virtual void PreGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Pre);
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public virtual void StartGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Gaming);
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
    public virtual void EndGame()
    {
        ClearGame();
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public virtual void ClearGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.End);
        UnRegisterAllEvent();
        System.GC.Collect();
    }

    /// <summary>
    /// 改变游戏状态
    /// </summary>
    /// <param name="gameState"></param>
    public virtual void ChangeGameState(GameStateEnum gameState)
    {

    }
}
