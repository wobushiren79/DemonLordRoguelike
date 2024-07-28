using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : BaseManager
{
    //ս������
    public Dictionary<string, GameObject> dicScene = new Dictionary<string, GameObject>();


    /// <summary>
    /// ��ȡս������
    /// </summary>
    public void GetFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"��ѯFightSceneս������ʧ��  û���ҵ�idΪ{fightSceneId}��ս������");
            return;
        }
        string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// ��ȡ���س���
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
