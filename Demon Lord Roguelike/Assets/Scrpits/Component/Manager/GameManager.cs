using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager : BaseManager
{
    public GameStateEnum gameState; //游戏状态
    public BaseGameLogic gameLogic;//战斗逻辑

    public Dictionary<string, GameObject> dicObjModel = new Dictionary<string, GameObject>();

    /// <summary>
    /// 同步获取obj
    /// </summary>
    public GameObject GetGameObjectSync(string objPath)
    {
        GameObject objModel = GetModelForAddressablesSync(dicObjModel, objPath);
        if (objModel != null)
        {
            GameObject targetObj= Instantiate(objModel);
            return targetObj; 
        }
        return null;
    }

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
    public T GetGameLogic<T>() where T : BaseGameLogic
    {
        if (gameLogic == null)
        {
            return null;
        }
        if (gameLogic is T logic)
        {
            return logic;
        }
        return null;
    }
}
