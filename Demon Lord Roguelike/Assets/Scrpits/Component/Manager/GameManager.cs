using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager : BaseManager
{
    public GameStateEnum gameState; //��Ϸ״̬
    public GameBaseLogic gameLogic;//ս���߼�

    /// <summary>
    /// ������Ϸ״̬
    /// </summary>
    /// <param name="gameState"></param>
    public void SetGameState(GameStateEnum gameState)
    {
        this.gameState = gameState;
    }

    /// <summary>
    /// ��ȡ��Ϸ״̬
    /// </summary>
    public GameStateEnum GetGameState()
    {
        return gameState;
    }

    /// <summary>
    /// ��ȡ��Ϸ�߼�
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
