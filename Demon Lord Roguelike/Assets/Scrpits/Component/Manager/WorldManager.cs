using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WorldManager : BaseManager
{
    //战斗场景
    public Dictionary<string, GameObject> dicScene = new Dictionary<string, GameObject>();
    //当前天空盒
    public AsyncOperationHandle<Material> currentSkyBox;

    /// <summary>
    /// 获取战斗场景
    /// </summary>
    public void GetFightScene(string dataPath, Action<GameObject> actionForComplete)
    {
        //加载战斗场景
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// 获取天空并摄制
    /// </summary>
    /// <param name="skyboxPath"></param>
    /// <param name="actionForComplete"></param>
    public void GetSkybox(string skyboxPath, Action<Material> actionForComplete)
    {
        //加载天空盒子
        LoadAddressablesUtil.LoadAssetAsync<Material>(skyboxPath, data =>
        {
            currentSkyBox = data; 
            actionForComplete?.Invoke(data.Result);
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

    /// <summary>
    /// 获取战斗场景的道路
    /// </summary>
    /// <param name="actionForComplete"></param>
    public void GetFightSceneRoad(Action<GameObject> actionForComplete)
    {
        string dataPath = $"{PathInfo.CommonPrefabPath}/FightSceneRoad.prefab";
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// 移除天空盒
    /// </summary>
    public void RemoveSkybox()
    {
        if (currentSkyBox.IsValid() && currentSkyBox.Status == AsyncOperationStatus.Succeeded)
        {
            currentSkyBox.Release();
        }
        RenderSettings.skybox = null;
    }
}
