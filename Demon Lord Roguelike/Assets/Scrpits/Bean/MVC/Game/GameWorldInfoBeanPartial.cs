
using System.Collections.Generic;
using UnityEngine;

public partial class GameWorldInfoBean
{
    public Dictionary<int, GameWorldInfoDifficultyBean> dicDifficultyData = new Dictionary<int, GameWorldInfoDifficultyBean>();

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

    /// <summary>
    /// 获取难度数据
    /// </summary>
    public GameWorldInfoDifficultyBean GetDifficultyData(int difficulty)
    {
        if (dicDifficultyData.TryGetValue(difficulty, out GameWorldInfoDifficultyBean targetDifficultyData))
        {
            return targetDifficultyData;
        }
        string difficultyData = null;
        targetDifficultyData = new GameWorldInfoDifficultyBean();
        targetDifficultyData.difficulty = difficulty;

        switch (difficulty)
        {
            case 1:
                difficultyData = difficulty_1;
                break;
            case 2:
                difficultyData = difficulty_2;
                break;
            case 3:
                difficultyData = difficulty_3;
                break;
            case 4:
                difficultyData = difficulty_4;
                break;
            case 5:
                difficultyData = difficulty_5;
                break;
            case 6:
                difficultyData = difficulty_6;
                break;
            case 7:
                difficultyData = difficulty_7;
                break;
            case 8:
                difficultyData = difficulty_8;
                break;
            case 9:
                difficultyData = difficulty_9;
                break;
            case 10:
                difficultyData = difficulty_10;
                break;
        }
        if (!difficultyData.IsNull())
        {
            var difficultyDataArray = difficultyData.Split('|');
            for (int i = 0; i < difficultyDataArray.Length; i++)
            {
                var itemData = difficultyDataArray[i];
                if (itemData.Contains("maxLevel:"))
                {
                    var maxLevelData = itemData.Split(":");
                    var maxLevelDetailsData = maxLevelData[1].Split("-");
                    targetDifficultyData.minLevelNum = int.Parse(maxLevelDetailsData[0]);
                    targetDifficultyData.maxLevelNum = int.Parse(maxLevelDetailsData[1]);
                }
            }
        }
        dicDifficultyData[difficulty] = targetDifficultyData;
        return targetDifficultyData;
    }
}

public class GameWorldInfoDifficultyBean
{
    public int difficulty;
    public int minLevelNum;
    public int maxLevelNum;
}


public partial class GameWorldInfoCfg
{
}
