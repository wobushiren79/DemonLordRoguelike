
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameWorldInfoBean
{
    /// <summary>
    /// 获取地图坐标
    /// </summary>
    /// <returns></returns>
    public Vector2 GetMapPosition()
    {
        if (map_pos.IsNull())
        {
            return Vector2.zero;
        }
        return map_pos.SplitForVector2(',');
    }
}

[Serializable]
/// <summary>
/// 游戏世界随机数据
/// </summary>
public partial class GameWorldInfoRandomBean
{
    public long worldId;
    //游戏类型
    public GameFightTypeEnum gameFightType;
    //道路数量
    public int roadNum;
    //UI显示位置
    public Vector2 uiPosition;
    //关卡数量
    public int fightNum;
    //图标种子
    public int iconSeed;

    /// <summary>
    /// 随机设置游戏类型
    /// </summary>
    public void SetGameFightTypeRandom(UserUnlockWorldBean userUnlockWorldData)
    {
        worldId = userUnlockWorldData.worldId;
        //随机世界模式
        List<GameFightTypeEnum> listRandomGameFightType = new List<GameFightTypeEnum>() { GameFightTypeEnum.Conquer };
        //如果无尽模式解锁了
        if (userUnlockWorldData.difficultyInfinite == 1)
        {
            listRandomGameFightType.Add(GameFightTypeEnum.Infinite);
        }
        var randomIndex = UnityEngine.Random.Range(0, listRandomGameFightType.Count);
        gameFightType = listRandomGameFightType[randomIndex];

        //设置随机数据
        SetRandomData(gameFightType);
    }

    /// <summary>
    /// 设置随机数据
    /// </summary>
    /// <param name="gameFightTypeEnum"></param>
    public void SetRandomData(GameFightTypeEnum gameFightTypeEnum)
    {
        switch (gameFightTypeEnum)
        {
            case GameFightTypeEnum.Conquer:
                SetRandomDataForConquer();
                break;
            case GameFightTypeEnum.Infinite:
                SetRandomDataForInfinite();
                break;
        }
    }

    public void SetRandomDataForConquer()
    {
        int roadNumRandom = UnityEngine.Random.Range(1, 7);
        roadNum = roadNumRandom;

        int fightNumRandom = UnityEngine.Random.Range(5, 10);
        fightNum = fightNumRandom;
    }

    public void SetRandomDataForInfinite()
    {
        int roadNumRandom = UnityEngine.Random.Range(1, 7);
        roadNum = roadNumRandom;
    }
}