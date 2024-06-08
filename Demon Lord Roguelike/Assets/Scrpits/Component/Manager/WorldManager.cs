using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : BaseManager
{
    //ս������
    public Dictionary<string, GameObject> dicFightScene = new Dictionary<string, GameObject>();

    public void GetFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"��ѯFightSceneս������ʧ��  û���ҵ�idΪ{fightSceneId}��ս������");
            return;
        }
        string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
        GetModelForAddressables(dicFightScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }
}
