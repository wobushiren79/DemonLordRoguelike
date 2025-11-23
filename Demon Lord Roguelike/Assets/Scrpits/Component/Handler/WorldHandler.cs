using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WorldHandler : BaseHandler<WorldHandler, WorldManager>
{
    //当前场景 用于处理同时存在多个场景
    protected Dictionary<GameSceneTypeEnum, GameObject> dicCurrentScene = new Dictionary<GameSceneTypeEnum, GameObject>();
    //当前场景
    protected GameObject currentScene;

    public GameObject GetCurrentScene(GameSceneTypeEnum gameSceneType)
    {
        if (gameSceneType == GameSceneTypeEnum.BaseMain)
        {
            gameSceneType = GameSceneTypeEnum.BaseGaming;
        }
        if (dicCurrentScene.TryGetValue(gameSceneType, out var targetScene))
        {
            return targetScene;
        }
        return null;
    }
    
    public GameObject GetCurrentScene()
    {
        return currentScene;
    }

    #region 进入场景
    /// <summary>
    /// 进入终焉议会场景
    /// </summary>
    /// <returns></returns>
    public async Task EnterDoomCouncilScene()
    {
        await ClearWorldData();
        //镜头初始化
        CameraHandler.Instance.InitData();
        //加载奖励选择
        var baseSceneObj = await LoadDoomCouncilScene();
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.DoomCouncil);
    }

    /// <summary>
    /// 进入奖励选择场景
    /// </summary>
    /// <returns></returns>
    public async Task EnterRewardSelectScene(bool isClearWorldData = false)
    {
        if (isClearWorldData)
        {
            await ClearWorldData();
        }
        //镜头初始化
        CameraHandler.Instance.InitData();
        //加载奖励选择
        var targetObj = await LoadRewardSelectScene();
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.RewardSelect);
        //镜头切换
        CameraHandler.Instance.SetCameraForRewardSelectScene(0);
    }

    /// <summary>
    /// 进入游戏进入主界面选项
    /// </summary>
    public async void EnterMainForBaseScene()
    {
        await ClearWorldData();
        //清理掉用户数据
        GameDataHandler.Instance.ClearUserData();
        //打开加载UI
        UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
        //镜头初始化
        CameraHandler.Instance.InitData();
        //加载基地场景
        var targetObj = await LoadBaseScene();
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.BaseMain);
        //关闭LoadingUI 打开开始UI
        UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
        //播放音乐
        AudioHandler.Instance.PlayMusicForMain();
    }

    /// <summary>
    /// 进入游戏中 基地场景
    /// </summary>
    public async void EnterGameForBaseScene(UserDataBean userData, bool isInitScene)
    {
        //镜头初始化
        CameraHandler.Instance.InitData();
        if (isInitScene)
        {
            //清理世界数据
            await ClearWorldData();
            //加载基地场景
            await LoadBaseScene();
        }
        //设置基地场景视角
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature, Vector3.zero);
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.BaseGaming);
        //关闭LoadingUI
        var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
        //播放音乐
        AudioHandler.Instance.PlayMusicForGaming();
    }

    /// <summary>
    /// 进入战斗场景
    /// </summary>
    public async void EnterGameForFightScene(FightBean fightData)
    {
        //清理世界数据
        await ClearWorldData();
        //打开加载UI
        UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
        //镜头初始化
        CameraHandler.Instance.InitData();
        //开始战斗
        GameHandler.Instance.StartGameFight(fightData);
        //播放音乐
        AudioHandler.Instance.PlayMusicForFight();
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.Fight);
    }
    #endregion

    #region 加载场景
    /// <summary>
    /// 加载终焉议会场景
    /// </summary>
    /// <returns></returns>
    public async Task<GameObject> LoadDoomCouncilScene()
    {
        await UnLoadScene(GameSceneTypeEnum.DoomCouncil);
        var targetScene = await manager.GetDoomCouncilScene();
        targetScene.SetActive(true);
        targetScene.transform.position = Vector3.zero;
        targetScene.transform.eulerAngles = Vector3.zero;

        dicCurrentScene.Add(GameSceneTypeEnum.DoomCouncil, targetScene);
        currentScene = targetScene;

        //设置天空颜色
        ColorUtility.TryParseHtmlString("#080613", out var targetColorSky);
        manager.SetSkyboxColor(CameraClearFlags.SolidColor, targetColorSky);
        //移除天空盒 设置纯粹的颜色
        manager.RemoveSkybox();
        return targetScene;
    }
    
    /// <summary>
    /// 加载奖励场景
    /// </summary>
    /// <returns></returns>
    public async Task<GameObject> LoadRewardSelectScene()
    {
        await UnLoadScene(GameSceneTypeEnum.RewardSelect);
        var targetScene = await manager.GetRewardSelectScene();
        targetScene.SetActive(true);
        targetScene.transform.position = Vector3.zero;
        targetScene.transform.eulerAngles = Vector3.zero;

        dicCurrentScene.Add(GameSceneTypeEnum.RewardSelect, targetScene);
        currentScene = targetScene;

        //设置天空颜色
        ColorUtility.TryParseHtmlString("#080613", out var targetColorSky);
        manager.SetSkyboxColor(CameraClearFlags.SolidColor, targetColorSky);
        //移除天空盒 设置纯粹的颜色
        manager.RemoveSkybox();
        return targetScene;
    }

    /// <summary>
    /// 加载基地场景
    /// </summary>
    /// <param name="actionForComplete"></param>
    public async Task<GameObject> LoadBaseScene()
    {
        await UnLoadScene(GameSceneTypeEnum.BaseGaming);

        var targetScene = await manager.GetBaseScene();
        targetScene.SetActive(true);
        targetScene.transform.position = Vector3.zero;
        targetScene.transform.eulerAngles = Vector3.zero;

        dicCurrentScene.Add(GameSceneTypeEnum.BaseGaming, targetScene);
        currentScene = targetScene;

        //设置天空颜色
        ColorUtility.TryParseHtmlString("#080613", out var targetColorSky);
        manager.SetSkyboxColor(CameraClearFlags.SolidColor, targetColorSky);
        //移除天空盒 设置纯粹的颜色
        manager.RemoveSkybox();
        return targetScene;
    }

    /// <summary>
    /// 加载战斗场景
    /// </summary>
    public async Task LoadFightScene(FightBean fightData)
    {
        await UnLoadScene(GameSceneTypeEnum.Fight);
        GameObject targetScene;//目标场景
        string roadColorA = "#ffffff00";//道路颜色A
        string roadColorB= "#ffffff00";//道路颜色B
        //如果议会 特殊加载议会场景
        if (fightData.gameFightType == GameFightTypeEnum.DoomCouncil)
        {
            targetScene = await LoadDoomCouncilScene();

            targetScene.SetActive(true);
            targetScene.transform.position = new Vector3(-0.5f, -0.00001f, 1f);
            targetScene.transform.eulerAngles = new Vector3(0, 90, 0);
        }
        else
        {
            FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightData.fightSceneId);
            if (fightSceneData == null)
            {
                LogUtil.LogError($"查询FightScene战斗场景失败  没有找到id为{fightData.fightSceneId}的战斗场景");
                return;
            }
            //加载天空盒-----------------------------------------------------------
            var skyboxMat = await manager.GetSkybox(fightSceneData.skybox_mat);
            //设置天空盒
            RenderSettings.skybox = skyboxMat;
            RenderSettings.skybox.SetFloat("_RotateX", -15);
            RenderSettings.skybox.SetFloat("_RotateY", 0);
            RenderSettings.skybox.SetFloat("_RotateZ", 0);

            //获取场景-----------------------------------------------------------
            string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
            targetScene = await manager.GetFightScene(dataPath);

            targetScene.SetActive(true);
            targetScene.transform.position = new Vector3(0, -0.00001f, -(fightData.sceneRoadNumMax - fightData.sceneRoadNum) / 2f);
            targetScene.transform.eulerAngles = Vector3.zero;

            //设置天空盒颜色
            ColorUtility.TryParseHtmlString("#00000000", out var targetColorSky);
            manager.SetSkyboxColor(CameraClearFlags.Skybox, targetColorSky);

            roadColorA = fightSceneData.road_color_a;
            roadColorB = fightSceneData.road_color_b;
        }

        dicCurrentScene.Add(GameSceneTypeEnum.Fight, targetScene);
        currentScene = targetScene;

        //获取战斗道路-----------------------------------------------------------
        var sceneRoad = await manager.GetFightSceneRoad();
        sceneRoad.transform.SetParent(targetScene.transform);
        //设置道路数据
        sceneRoad.transform.localScale = new Vector3(fightData.sceneRoadLength, fightData.sceneRoadNum, 1);
        sceneRoad.transform.eulerAngles = new Vector3(90, 0, 0);
        sceneRoad.transform.position = new Vector3(fightData.sceneRoadLength / 2f + 0.5f, 0, fightData.sceneRoadNum / 2f + 0.5f);
        var roadMR = sceneRoad.GetComponent<MeshRenderer>();
        roadMR.sharedMaterial.SetVector("_GridSize", new Vector2(fightData.sceneRoadLength, fightData.sceneRoadNum));

        ColorUtility.TryParseHtmlString($"{roadColorA}", out var colorA);
        ColorUtility.TryParseHtmlString($"{roadColorB}", out var colorB);
        roadMR.sharedMaterial.SetColor("_ColorA", colorA);
        roadMR.sharedMaterial.SetColor("_ColorB", colorB);
    }
    #endregion

    #region 卸载场景
    /// <summary>
    /// 卸载战斗场景
    /// </summary>
    public async Task UnLoadScene(GameSceneTypeEnum gameSceneType, bool isRemoveSkybox = true)
    {
        if (dicCurrentScene.TryGetValue(gameSceneType, out var targetScene))
        {
            var scenePrefabBase = targetScene.GetComponent<ScenePrefabBase>();
            if (scenePrefabBase != null)
            {
                await scenePrefabBase.DestoryScene();
            }
            else
            {
                //战斗场景没有ScenePrefabBase
                DestroyImmediate(scenePrefabBase.gameObject);
            }
            dicCurrentScene.Remove(gameSceneType);
        }
        //移除天空盒
        if (isRemoveSkybox)
        {
            manager.RemoveSkybox();
        }
    }

    /// <summary>
    /// 卸载所有场景
    /// </summary>
    public async Task UnLoadAllScene(bool isRemoveSkybox = true)
    {
        foreach (var itemData in dicCurrentScene)
        {
            var targetScene = itemData.Value;
            var scenePrefabBase = targetScene.GetComponent<ScenePrefabBase>();
            if (scenePrefabBase != null)
            {
                await scenePrefabBase.DestoryScene();
            }
            else
            {
                //战斗场景没有ScenePrefabBase
                DestroyImmediate(targetScene);
            }
        }
        dicCurrentScene.Clear();
        //移除天空盒
        if (isRemoveSkybox)
        {
            manager.RemoveSkybox();
        }
        currentScene = null;
    }
    #endregion

    #region 清理
    /// <summary>
    /// 清理世界所有数据
    /// </summary>
    public async Task ClearWorldData(bool isShowLoading = true)
    {
        //打开加载UI
        if (isShowLoading)
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
        //关闭所有控制
        GameControlHandler.Instance.manager.EnableAllControl(false);
        await new WaitNextFrame();
        //卸载场景
        await UnLoadAllScene();
        await new WaitNextFrame();
        //logic清理
        BaseGameLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<BaseGameLogic>();
        if (gameLogic != null)
        {
            gameLogic.ClearGame();
        }
        await new WaitNextFrame();
        //清理粒子
        EffectHandler.Instance.manager.Clear();
        await new WaitNextFrame();
        //清理缓存
        System.GC.Collect();
        await new WaitNextFrame();
    }
    #endregion
}
