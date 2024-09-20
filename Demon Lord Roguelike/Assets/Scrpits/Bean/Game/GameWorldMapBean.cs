using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameWorldMapBean
{
    public long worldId;
    public int currentMapLevel;//当前地图难度
    public int maxMapLevel;//当前进度总难度

    //详情数据
    Dictionary<string, GameWorldMapDetailsBean> dicDetails;

    public GameWorldMapBean(long worldId)
    {
        this.worldId = worldId;
        currentMapLevel = 0;
        maxMapLevel = -1;
    }

    /// <summary>
    /// 获取详情数据
    /// </summary>
    public Dictionary<string, GameWorldMapDetailsBean> GetDetailsData()
    {
        if (dicDetails == null || dicDetails.Count == 0)
        {
            
        }
        return dicDetails;
    }
}

[Serializable]
public class GameWorldMapDetailsBean
{
    public string id;
    public Vector2 mapIndex;//地图上的步骤序号
    public int mapType;//地图类型 0战斗  1商店  2宝藏  3休息点
    public string[] lastId;
    public string[] nextId;
}