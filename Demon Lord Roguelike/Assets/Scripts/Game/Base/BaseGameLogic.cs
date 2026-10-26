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
    /// 更新-结算暂停态（gameState=Settlement）
    /// <para>结算/选择界面等暂停态下游戏整体冻结（GameHandler 不再驱动 UpdateGame），但仍需每帧继续生效的轻量逻辑在此推进。</para>
    /// <para>默认空实现；GameFightLogic 重写为继续推进魔王「自动拾取魔晶」研究。</para>
    /// </summary>
    public virtual void UpdateGameForSettlement()
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
    public virtual Task ClearGame()
    {
        ChangeGameState(GameStateEnum.End);
        UnRegisterAllEvent();
        System.GC.Collect();
        return Task.CompletedTask;
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
