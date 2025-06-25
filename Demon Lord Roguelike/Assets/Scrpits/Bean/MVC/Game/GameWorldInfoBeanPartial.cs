
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

/// <summary>
/// 游戏世界随机数据
/// </summary>
public partial class GameWorldInfoRandomBean
{
    //游戏类型
    public GameFightTypeEnum gameFightType;
    //道路数量
    public int roadNum;
    //UI显示位置
    public Vector2 uiPosition;

    /// <summary>
    /// 随机设置游戏类型
    /// </summary>
    public void SetGameFightTypeRandom(UserUnlockWorldBean userUnlockWorldData)
    {
        //随机世界模式
        List<GameFightTypeEnum> listRandomGameFightType = new List<GameFightTypeEnum>() { GameFightTypeEnum.Conquer };
        //如果无尽模式解锁了
        if (userUnlockWorldData.difficultyInfinite == 1)
        {
            listRandomGameFightType.Add(GameFightTypeEnum.Infinite);
        }
        var randomIndex = Random.Range(0, listRandomGameFightType.Count);
        gameFightType = listRandomGameFightType[randomIndex];
    }

    
}