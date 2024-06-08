using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldHandler : BaseHandler<WorldHandler, WorldManager>
{
    //当前战斗场景
    public GameObject currentFightScene;

    /// <summary>
    /// 加载战斗场景
    /// </summary>
    public void LoadFightScene(int fightSceneId, Action<GameObject> actionForComplete)
    {
        UnLoadFightScene();
        manager.GetFightScene(fightSceneId, (targetScene) =>
        {
            currentFightScene = Instantiate(targetScene);
            currentFightScene.SetActive(true);
            currentFightScene.transform.position = Vector3.zero;
            currentFightScene.transform.eulerAngles = Vector3.zero;
            actionForComplete?.Invoke(currentFightScene);
        });
    }

    /// <summary>
    /// 卸载战斗场景
    /// </summary>
    public void UnLoadFightScene()
    {
        if (currentFightScene != null)
        {
            Destroy(currentFightScene);
        }
    }
}
