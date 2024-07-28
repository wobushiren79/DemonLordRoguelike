using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : BaseManager
{
    //战斗场景
    public Dictionary<string, GameObject> dicScene = new Dictionary<string, GameObject>();


    /// <summary>
    /// 获取战斗场景
    /// </summary>
    public void GetFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"查询FightScene战斗场景失败  没有找到id为{fightSceneId}的战斗场景");
            return;
        }
        string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// 获取基地场景
    /// </summary>
    public void GetBaseScene(Action<GameObject> actionForComplete)
    {
        string dataPath = $"{PathInfo.CommonPrefabPath}/BaseScene.prefab";
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }
}
