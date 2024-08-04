using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class BaseGameLogic
{
    //����ע���¼�
    private List<string> listEvents = new List<string>();

    /// <summary>
    /// ׼����Ϸ����
    /// </summary>
    public virtual void PreGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Pre);
        PreGameForRegisterEvent();
    }

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public virtual void StartGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Gaming);
    }

    /// <summary>
    /// ��Ϸ��
    /// </summary>
    public virtual void UpdateGame()
    {

    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public virtual void EndGame()
    {
        ClearGame();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public virtual void ClearGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.End);
        EndGameForUnRegisterEvent();
        System.GC.Collect();
    }

    #region �¼����
    /// <summary>
    /// ע���¼�
    /// </summary>
    public abstract void PreGameForRegisterEvent();

    /// <summary>
    /// ��Ϸ����ע�������¼�
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
