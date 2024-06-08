using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : BaseManager
{
    //战斗场景
    public Dictionary<string, GameObject> dicFightScene = new Dictionary<string, GameObject>();

    public void GetFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"查询FightScene战斗场景失败  没有找到id为{fightSceneId}的战斗场景");
            return;
        }
        string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
        GetModelForAddressables(dicFightScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }
}
