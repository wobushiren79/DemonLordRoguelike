using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WorldManager : BaseManager
{
    //场景相关
    public Dictionary<string, GameObject> dicScene = new Dictionary<string, GameObject>();
    //当前天空盒
    public AsyncOperationHandle<Material> currentSkyBox;

    #region 场景相关
    public async Task<GameObject> GetScene(string dataPath)
    {
        var target = await GetModelForAddressables(dicScene, dataPath);
        return Instantiate(target);
    }

    /// <summary>
    /// 获取战斗场景
    /// </summary>
    public async Task<GameObject> GetFightScene(string dataPath)
    {
        var targetScene = await GetScene(dataPath);
        return targetScene;
    }
    
    /// <summary>
    /// 获取游戏场景
    /// </summary>
    public async Task<GameObject> GetGameScene(string sceneName)
    {
        string dataPath = $"{PathInfo.CommonPrefabScenesPath}/{sceneName}.prefab";
        var targetScene = await GetScene(dataPath);
        targetScene.gameObject.SetActive(true);
        //初始化数据
        ScenePrefabBase scenePrefabBase = targetScene.GetComponent<ScenePrefabBase>();
        await scenePrefabBase.InitSceneData();
        return targetScene;
    } 

    /// <summary>
    /// 获取终焉议会
    /// </summary>
    public async Task<GameObject> GetDoomCouncilScene()
    {
        return await GetGameScene("RewardSelectScene");
    }

    /// <summary>
    /// 获取奖励场景
    /// </summary>
    public async Task<GameObject> GetRewardSelectScene()
    {
         return await GetGameScene("RewardSelectScene");
    }

    /// <summary>
    /// 获取基地场景
    /// </summary>
    public async Task<GameObject> GetBaseScene()
    {
        return await GetGameScene("BaseScene");
    }

    /// <summary>
    /// 获取战斗场景的道路
    /// </summary>
    /// <param name="actionForComplete"></param>
    public async Task<GameObject> GetFightSceneRoad()
    {
        string dataPath = $"{PathInfo.CommonPrefabPath}/FightSceneRoad.prefab";
        var targetScene = await GetScene(dataPath);
        return targetScene;
    }
    #endregion

    #region 天空盒

    /// <summary>
    /// 获取天空盒材质
    /// </summary>
    /// <param name="skyboxPath"></param>
    /// <param name="actionForComplete"></param>
    public async Task<Material> GetSkybox(string skyboxPath)
    {
        //加载天空盒子
        AsyncOperationHandle<Material> data = await LoadAddressablesUtil.LoadAssetAsync<Material>(skyboxPath);
        currentSkyBox = data;
        return data.Result;
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

    /// <summary>
    /// 设置天空盒颜色
    /// </summary>
    /// <param name="colorSky"></param>
    public void SetSkyboxColor(CameraClearFlags cameraClearFlags, Color colorSky)
    {
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        mainCamera.clearFlags = cameraClearFlags;
        mainCamera.backgroundColor = colorSky;
    }
    #endregion
}
