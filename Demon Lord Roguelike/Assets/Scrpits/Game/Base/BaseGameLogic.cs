using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public abstract class BaseGameLogic : BaseEvent
{
    /// <summary>
    /// ׼����Ϸ����
    /// </summary>
    public virtual void PreGame()
    {
        GameHandler.Instance.manager.SetGameState(GameStateEnum.Pre);
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
        UnRegisterAllEvent();
        System.GC.Collect();
    }
}
