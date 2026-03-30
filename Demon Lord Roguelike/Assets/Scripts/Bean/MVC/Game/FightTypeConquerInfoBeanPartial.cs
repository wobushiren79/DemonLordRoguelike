using System;
using System.Collections.Generic;
public partial class FightTypeConquerInfoBean
{
    protected long[] fightSceneIds;
    protected long[] fightSceneBossIds;
    protected long[] emenyIds;
    protected long[] emenyBossIds;

    /// <summary>
    /// 获取随机战斗场景
    /// </summary>
    public long GetRandomFightScene(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = fightSceneBossIds;
        }
        else
        {
            targetIds = fightSceneIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = fight_scene_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = fight_scene_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
    }

    /// <summary>
    /// 获取战斗敌人数据
    /// </summary>
    /// <param name="isBoss"></param>
    public long GetRandomEmenyId(bool isBoss)
    {
        long[] targetIds;
        if (isBoss)
        {
            targetIds = emenyBossIds;
        }
        else
        {
            targetIds = emenyIds;
        }
        if (targetIds == null)
        {

            if (isBoss)
            {
                targetIds = enemy_boss_ids.SplitForArrayLong('&');
            }
            else
            {
                targetIds = enemy_ids.SplitForArrayLong('&');
            }
        }
        return targetIds.GetRandomData();
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
