using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WorldHandler : BaseHandler<WorldHandler, WorldManager>
{
    //当前场景
    public Dictionary<GameSceneTypeEnum, GameObject> dicCurrentScene = new Dictionary<GameSceneTypeEnum, GameObject>();
    
    public GameObject GetCurrentScene(GameSceneTypeEnum gameSceneType)
    {
        if(gameSceneType == GameSceneTypeEnum.BaseMain)
        {
            gameSceneType = GameSceneTypeEnum.BaseGaming;
        }
        if (dicCurrentScene.TryGetValue(gameSceneType,out var targetScene))
        {
            return targetScene;
        }
        return null;
    }

    #region 进入场景
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
        //加载奖励选择
        var targetObj = await LoadRewardSelectScene();
        //镜头初始化
        CameraHandler.Instance.InitData();
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.RewardSelect);

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
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature);
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

    public async Task<GameObject> LoadRewardSelectScene()
    {
        UnLoadScene(GameSceneTypeEnum.RewardSelect);
        var targetScene = await manager.GetRewardSelectScene();
        targetScene.SetActive(true);
        targetScene.transform.position = Vector3.zero;
        targetScene.transform.eulerAngles = Vector3.zero;
        dicCurrentScene.Add(GameSceneTypeEnum.RewardSelect, targetScene);

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
        UnLoadScene(GameSceneTypeEnum.BaseGaming);

        var targetScene = await manager.GetBaseScene();
        targetScene.SetActive(true);
        targetScene.transform.position = Vector3.zero;
        targetScene.transform.eulerAngles = Vector3.zero;
        ScenePrefabBase scenePrefabBase = targetScene.GetComponent<ScenePrefabBase>();
        scenePrefabBase.InitSceneData();

        dicCurrentScene.Add(GameSceneTypeEnum.BaseGaming, targetScene);

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
        UnLoadScene(GameSceneTypeEnum.Fight);

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
        var targetScene = await manager.GetFightScene(dataPath);
        targetScene.SetActive(true);
        targetScene.transform.position = new Vector3(0, 0, -(fightData.sceneRoadNumMax - fightData.sceneRoadNum) / 2f);
        targetScene.transform.eulerAngles = Vector3.zero;
        dicCurrentScene.Add(GameSceneTypeEnum.Fight, targetScene);

        //设置天空盒颜色
        ColorUtility.TryParseHtmlString("#00000000", out var targetColorSky);
        manager.SetSkyboxColor(CameraClearFlags.Skybox, targetColorSky);

        //获取战斗道路-----------------------------------------------------------
        var sceneRoad = await manager.GetFightSceneRoad();
        sceneRoad.transform.SetParent(targetScene.transform);
        //设置道路数据
        sceneRoad.transform.localScale = new Vector3(fightData.sceneRoadLength, fightData.sceneRoadNum, 1);
        sceneRoad.transform.eulerAngles = new Vector3(90, 0, 0);
        sceneRoad.transform.position = new Vector3(fightData.sceneRoadLength / 2f + 0.5f, 0, fightData.sceneRoadNum / 2f + 0.5f);
        var roadMR = sceneRoad.GetComponent<MeshRenderer>();
        roadMR.sharedMaterial.SetVector("_GridSize", new Vector2(fightData.sceneRoadLength, fightData.sceneRoadNum));

        ColorUtility.TryParseHtmlString($"{fightSceneData.road_color_a}", out var colorA);
        ColorUtility.TryParseHtmlString($"{fightSceneData.road_color_b}", out var colorB);
        roadMR.sharedMaterial.SetColor("_ColorA", colorA);
        roadMR.sharedMaterial.SetColor("_ColorB", colorB);
    }
    #endregion

    #region 卸载场景
    /// <summary>
    /// 卸载战斗场景
    /// </summary>
    public void UnLoadScene(GameSceneTypeEnum gameSceneType, bool isRemoveSkybox = true)
    {
        if (dicCurrentScene.TryGetValue(gameSceneType, out var targetScene))
        {
            DestroyImmediate(targetScene);
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
    public void UnLoadAllScene(bool isRemoveSkybox = true)
    {
        foreach (var itemData in dicCurrentScene)
        {
            DestroyImmediate(itemData.Value);
        }
        dicCurrentScene.Clear();
        //移除天空盒
        if (isRemoveSkybox)
        {
            manager.RemoveSkybox();
        }
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
        UnLoadAllScene();
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
