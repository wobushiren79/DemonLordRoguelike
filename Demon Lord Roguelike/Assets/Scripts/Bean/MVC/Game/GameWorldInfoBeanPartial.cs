
using System;
using System.Collections.Generic;
using UnityEngine;
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
    //道路长度
    public int roadLength;
    //UI显示位置
    public Vector2 uiPosition;
    //关卡数量
    public int fightNum;
    //图标种子
    public int iconSeed;
    //难度等级
    public int difficultyLevel;

    public GameWorldInfoRandomBean()
    {
        difficultyLevel = 1;
    }

    /// <summary>
    /// 随机设置游戏类型
    /// </summary>
    public void SetGameFightTypeRandom(long worldId)
    {
        this.worldId = worldId;
        var gameWorldInfo = GameWorldInfoCfg.GetItemData(worldId);
        //随机世界模式
        List<GameFightTypeEnum> listRandomGameFightType = new List<GameFightTypeEnum>() 
        { 
            GameFightTypeEnum.Conquer //默认有征服模式
        };
        //如果无尽模式解锁了
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var UserUnlock = userData.GetUserUnlockData();
        if (UserUnlock.CheckIsUnlock(gameWorldInfo.unlock_id_infinite))
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


    /// <summary>
    /// 设置征服模式数据
    /// </summary>
    public void SetRandomDataForConquer()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlock = userData.GetUserUnlockData();
        //设置默认难度等级（默认最高）
        difficultyLevel = userUnlock.GetUnlockGameWorldConquerDifficultyLevel(worldId);
        //获取征服模式游戏数据
        FightTypeConquerInfoBean fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, difficultyLevel);
        if (fightTypeConquerInfo == null)
        {
            LogUtil.LogError($"初始化征服游戏模式失败 worldId:{worldId} difficultyLevel:{difficultyLevel}");
            return;
        }
        //随机道路数量
        int roadNumRandom = UnityEngine.Random.Range(fightTypeConquerInfo.road_num_min, fightTypeConquerInfo.road_num_max + 1);
        roadNum = roadNumRandom;
        //随机道路长度
        int roadLengthRandom = UnityEngine.Random.Range(fightTypeConquerInfo.road_length_min, fightTypeConquerInfo.road_length_max + 1);
        roadLength = roadLengthRandom;
        //随机关卡数量
        int fightNumRandom = UnityEngine.Random.Range(fightTypeConquerInfo.fight_num_min, fightTypeConquerInfo.fight_num_max + 1);
        fightNum = fightNumRandom;
    }

    /// <summary>
    /// 设置无限模式数据
    /// </summary>
    public void SetRandomDataForInfinite()
    {
        int roadNumRandom = UnityEngine.Random.Range(1, 7);
        roadNum = roadNumRandom;
    }
}