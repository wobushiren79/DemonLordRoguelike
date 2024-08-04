using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class BaseGameLogic
{
    //所有注册事件
    private List<string> listEvents = new List<string>();

    /// <summary>
    /// 准备游戏数据
    /// </summary>
    public virtual void PreGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Pre);
        PreGameForRegisterEvent();
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
        EndGameForUnRegisterEvent();
        System.GC.Collect();
    }

    #region 事件相关
    /// <summary>
    /// 注册事件
    /// </summary>
    public abstract void PreGameForRegisterEvent();

    /// <summary>
    /// 游戏结束注销所有事件
    /// </summary>
    public virtual void EndGameForUnRegisterEvent()
    {
        for (int i = 0; i < listEvents.Count; i++)
        {
            string itemEventName = listEvents[i];
            EventHandler.Instance.UnRegisterEvent(itemEventName);
        }
        listEvents.Clear();
    }

    public void RegisterEvent(string eventName, Action action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }

    public void RegisterEvent<A>(string eventName, Action<A> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B>(string eventName, Action<A, B> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B, C>(string eventName, Action<A, B, C> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    public void RegisterEvent<A, B, C, D>(string eventName, Action<A, B, C, D> action)
    {
        EventHandler.Instance.RegisterEvent(eventName, action);
        listEvents.Add(eventName);
    }
    #endregion
}
