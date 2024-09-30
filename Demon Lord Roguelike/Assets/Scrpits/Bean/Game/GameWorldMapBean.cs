using NUnit.Framework.Interfaces;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GameWorldMapBean
{
    public long worldId;
    public int difficutly;//地图难度
    public int currentMapLevel;//当前地图进度
    public int maxMapLevel;//地图总进度

    public Vector2 currentMapPosition;//当前地图位置

    //详情数据
    Dictionary<string, GameWorldMapDetailsBean> dicDetails;

    public GameWorldMapBean(long worldId)
    {
        this.worldId = worldId;
        currentMapLevel = 1;
        maxMapLevel = -1;
        currentMapPosition = Vector2.zero;
    }

    /// <summary>
    /// 获取详情数据
    /// </summary>
    public Dictionary<string, GameWorldMapDetailsBean> GetDetailsData()
    {
        if (dicDetails == null || dicDetails.Count == 0)
        {
            dicDetails = new Dictionary<string, GameWorldMapDetailsBean>();
            int mapLength = 3;//地图总步骤（不包括第一步和最后一步）
            int mapIndexMinSelectNum = 2;//每一步最小选择数量
            int mapIndexMaxSelectNum = 4;//每一步最大选择数量

            //首先生成起始点和终点
            GameWorldMapDetailsBean startPos = new GameWorldMapDetailsBean(1, new Vector2Int(0, 0), mapLength);
            GameWorldMapDetailsBean endPos = new GameWorldMapDetailsBean(2, new Vector2Int(mapLength + 1, 0), mapLength);

            //记录每一步的数据
            Dictionary<int, List<string>> dicMapIndexData = new Dictionary<int, List<string>>() 
            {
                { 0, new List<string>{ startPos.id } },
                { mapLength + 1, new List<string>{ endPos.id } }
            };
            dicDetails.Add(startPos.id, startPos);
            dicDetails.Add(endPos.id, endPos);
            //生成每一步的数据------------------------------------------------------------------------------------------------------------
            for (int x = 0; x < mapLength; x++)
            {
                int randomMapIndexNum = UnityEngine.Random.Range(mapIndexMinSelectNum, mapIndexMaxSelectNum + 1);
                List<string> listDataTemap = new List<string>();
                for (int y = 0; y < randomMapIndexNum; y++)
                {
                    Vector2Int mapIndex = new Vector2Int(x + 1, y);
                    GameWorldMapDetailsBean itemData = new GameWorldMapDetailsBean(1, mapIndex, mapLength);

                    listDataTemap.Add(itemData.id);
                    dicDetails.Add(itemData.id, itemData);
                }
                dicMapIndexData.Add(x + 1, listDataTemap);
            }
            //------------------------------------------------------------------------------------------------------------------------------
            //生成每一步的上下步数据-------------------------------------------------------------------------------------------------------
            foreach (var item in dicDetails)
            {
                var itemData = item.Value;
                //设置地图当前步骤数量
                dicMapIndexData.TryGetValue(itemData.mapPosition.x, out List<string> listIdsCur);
                itemData.mapIndexNum = listIdsCur.Count;

                //如果是起点 添加所有下一步
                if (itemData.mapPosition.x == 0)
                {
                    if (dicMapIndexData.TryGetValue(1, out List<string> listIds))
                    {
                        itemData.nextIds = listIds;
                        //并且给所有下一步的上一步ID赋值
                        for (int i = 0; i < listIds.Count; i++)
                        {
                            dicDetails[listIds[i]].AddLastId(itemData.id);
                        }
                    }
                    else
                    {
                        LogUtil.LogError("数据错误，地图没有下一步数据（起始点）");
                    }
                }
                //如果是终点 没有添加
                else if (itemData.mapPosition.x == mapLength + 1)
                {

                }
                //如果是其他点位
                else
                {
                    //获取下一步
                    if (dicMapIndexData.TryGetValue(itemData.mapPosition.x + 1, out List<string> listIdsNext))
                    {
                        //随机一个点位
                        int randomNextIndex = UnityEngine.Random.Range(0, listIdsNext.Count);
                        string randomNextId = listIdsNext[randomNextIndex];
                        itemData.AddNextId(randomNextId);
                        dicDetails[randomNextId].AddLastId(itemData.id);
                    }
                    else
                    {
                        LogUtil.LogError($"数据错误，地图没有下一步数据 itemData_{itemData.mapPosition}");
                    }

                    //获取上一步
                    if (dicMapIndexData.TryGetValue(itemData.mapPosition.x - 1, out List<string> listIdsLast))
                    {
                        //随机一个点位
                        int randomLastIndex = UnityEngine.Random.Range(0, listIdsLast.Count);
                        string randomLastId = listIdsLast[randomLastIndex];
                        itemData.AddLastId(randomLastId);
                        dicDetails[randomLastId].AddNextId(itemData.id);
                    }
                    else
                    {
                        LogUtil.LogError($"数据错误，地图没有上一步数据 itemData_{itemData.mapPosition}");
                    }

                }
            }
            //------------------------------------------------------------------------------------------------------------------------------
        }
        return dicDetails;
    }
}

[Serializable]
public class GameWorldMapDetailsBean
{
    public string id;
    public Vector2Int mapPosition;//地图上的步骤序号
    public int mapType;//地图类型 1起始点 2终点  3战斗  4商店  5宝藏  6休息点 
    public int mapLength;
    public int mapIndexNum;//地图上该步骤数量
    public List<string> lastIds;
    public List<string> nextIds;

    public GameWorldMapDetailsBean(int mapType, Vector2Int mapPosition, int mapLength)
    {
        id = $"GameWorldMapDetails_{SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N)}";
        this.mapType = mapType;
        this.mapPosition = mapPosition;
        this.mapLength = mapLength;
        lastIds = new List<string>();
        nextIds = new List<string>();
    }


    public void AddLastId(string targteId)
    {
        if (lastIds.Contains(targteId))
        {
            return;
        }
        lastIds.Add(targteId);
    }

    public void AddNextId(string targteId)
    {
        if (nextIds.Contains(targteId))
        {
            return;
        }
        nextIds.Add(targteId);
    }
}