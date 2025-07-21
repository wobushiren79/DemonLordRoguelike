using System;
using System.Collections.Generic;
public partial class FightTypeConquerInfoBean
{
    /// <summary>
    /// 获取随机战斗场景
    /// </summary>
    public long GetRandomFightScene(bool isBoss)
    {
        string sceneStr;
        if (isBoss)
        {
            sceneStr = fight_scene_ids;
        }
        else
        {
            sceneStr = fight_scene_boss_ids;
        }
        return sceneStr.SplitAndRandomForLong(',');
    }
    
}
public partial class FightTypeConquerInfoCfg
{

    public static FightTypeConquerInfoBean GetItemData(long worldId, int difficultyLevel)
    {
        var allData = GetAllData();
        foreach (var itemData in allData)
        {
            FightTypeConquerInfoBean fightTypeConquerInfo = itemData.Value;
            if (fightTypeConquerInfo.world_id == worldId && fightTypeConquerInfo.level == difficultyLevel)
            {
                return fightTypeConquerInfo;
            }
        }
        return null;
    }
}
