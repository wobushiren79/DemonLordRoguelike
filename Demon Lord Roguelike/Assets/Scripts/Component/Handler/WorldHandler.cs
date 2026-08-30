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
    //进入战斗场景前的全局环境光缓存（供卸载时还原）
    protected Color cacheAmbientLight;
    //是否已缓存环境光（仅配置过 ambient_light 的战斗场景会缓存）
    protected bool hasCacheAmbientLight;

    public T GetCurrentScenePrefab<T>(GameSceneTypeEnum gameSceneType) where T : ScenePrefabBase
    {
        var baseSceneObj = GetCurrentScene(gameSceneType);
        return baseSceneObj?.GetComponent<T>();
    }

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

    public T GetCurrentScenePrefab<T>() where T : ScenePrefabForBase
    {
        return currentScene?.GetComponent<T>();
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
    /// <param name="isClearLastGame">
    /// 是否清理上一场战斗：true 时卸载战斗场景并清理战斗生物/AI/BUFF/魔王核心等运行时实体。
    /// 征服模式通关BOSS后进入领奖场景必须传 true，否则上一关BOSS的战斗场景不会被卸载，会与领奖场景叠加残留显示。
    /// 独立测试(LauncherTest)直接打开领奖界面时无上一场战斗，保持 false。
    /// </param>
    /// <returns></returns>
    public async Task EnterRewardSelectScene(bool isClearLastGame = false)
    {
        //清理上一场战斗(卸载战斗场景 + 清理战斗实体)，避免战斗场景残留在领奖场景中
        if (isClearLastGame)
        {
            BaseGameLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<BaseGameLogic>();
            if (gameLogic != null)
            {
                await gameLogic.ClearGame();
            }
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
        //清理所有BUFF缓存
        BuffHandler.Instance.manager.ClearAll();
        //清理掉用户数据
        GameDataHandler.Instance.ClearUserData();
        //回到主菜单=真实开始/读档的统一收口点:复位测试模拟标记,防止上一个模拟测试(献祭/进阶)残留导致正式游戏静默不落盘(测试走EnterGameForBaseScene不经此处,不会误清)
        GameDataHandler.Instance.manager.isTestSimulation = false;
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
    public async void EnterGameForBaseScene(
        UserDataBean userData, 
        bool isClearWorld = true,//是否要清理世界并重新加载
        bool isAnimForBuildingShow = false//是否要播放建筑出现动画
    )
    {
        //镜头初始化
        CameraHandler.Instance.InitData();
        if (isClearWorld)
        {
            //清理世界数据
            await ClearWorldData();
            //加载基地场景
            await LoadBaseScene();
        }
        //设置基地场景视角
        Vector3 startControlPos = Vector3.zero;
        await CameraHandler.Instance.InitBaseSceneControlCamera(userData.selfCreature, startControlPos);

        var baseSceneObj = GetCurrentScene(GameSceneTypeEnum.BaseGaming);
        var scenePrefab = baseSceneObj.GetComponent<ScenePrefabForBase>();
        await scenePrefab.RefreshScene();
        //如果有遮罩 需要隐藏遮罩
        UIHandler.Instance.HideMask(0, null, null);
        //是否要播放建筑出现动画
        float timeAnimBuildingShow = 1;
        if (isAnimForBuildingShow)
        {
            //播放建筑出现动画
            var taskAnimBuildingShow = scenePrefab.AnimForBuildingShow(timeAnimBuildingShow);
            //播放基地控制器出现动画
            GameControlHandler.Instance.AnimForBaseControlShow(startControlPos, animTime: 1);
            //等待1秒动画(仅在播放建筑出现动画时需要等待)
            await new WaitForSeconds(timeAnimBuildingShow);
        }
        //环境参数初始化
        VolumeHandler.Instance.InitData(GameSceneTypeEnum.BaseGaming);
        //关闭LoadingUI
        var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
        //播放音乐
        AudioHandler.Instance.PlayMusicForGaming();
        //事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.World_EnterGameForBaseScene);
        //清理所有游戏主界面UI
        UIHandler.Instance.DestoryAllMainUI();
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
        float roadAlpha = 0.5f;//道路透明度（场景未配置时默认0.5）
        //如果议会 特殊加载议会场景
        if (fightData.gameFightType == GameFightTypeEnum.DoomCouncil)
        {
            targetScene = await LoadDoomCouncilScene();

            targetScene.SetActive(true);
            targetScene.transform.position = new Vector3(-0.5f, WorldManager.FightSceneHeightY, 1f);
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
            //按场景配置的旋转角度设置天空盒欧拉角（skybox_rotate 形如 "-15,0,0"）
            Vector3 skyboxRotate = fightSceneData.GetSkyboxRotate();
            RenderSettings.skybox.SetFloat("_RotateX", skyboxRotate.x);
            RenderSettings.skybox.SetFloat("_RotateY", skyboxRotate.y);
            RenderSettings.skybox.SetFloat("_RotateZ", skyboxRotate.z);

            //获取场景-----------------------------------------------------------
            string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
            targetScene = await manager.GetFightScene(dataPath);

            targetScene.SetActive(true);
            targetScene.transform.position = new Vector3(0, WorldManager.FightSceneHeightY, -(fightData.sceneRoadNumMax - fightData.sceneRoadNum) / 2f);
            targetScene.transform.eulerAngles = Vector3.zero;

            //设置天空盒颜色
            ColorUtility.TryParseHtmlString("#00000000", out var targetColorSky);
            manager.SetSkyboxColor(CameraClearFlags.Skybox, targetColorSky);

            roadColorA = fightSceneData.road_color_a;
            roadColorB = fightSceneData.road_color_b;
            roadAlpha = fightSceneData.GetRoadAlpha();

            //按场景配置开启内置雾（未配置 fog 则不开启，其它场景在卸载时已关闭）
            if (fightSceneData.HasFog && fightSceneData.GetFogParams(out var fogColor, out var fogStart, out var fogEnd, out var fogMode))
            {
                VolumeHandler.Instance.SetFog(fogColor, fogMode, fogStart, fogEnd, isActive: true);
            }

            //按场景配置设置全局环境光（未配置 ambient_light 则不修改；进场缓存原值，卸载时还原）
            if (fightSceneData.HasAmbientLight)
            {
                if (!hasCacheAmbientLight)
                {
                    cacheAmbientLight = RenderSettings.ambientLight;
                    hasCacheAmbientLight = true;
                }
                RenderSettings.ambientLight = fightSceneData.GetAmbientLightColor();
            }

            //按场景配置处理细节预制（Details 下同名子预制显示、其它隐藏；未配置则整个 Details 隐藏）
            HandleFightSceneDetails(targetScene, fightSceneData);

            //按场景配置开启体积雾（未配置 volumetric_fog 则不开启；离场由下次 InitData 统一关闭兜底）
            if (fightSceneData.HasVolumetricFog && fightSceneData.GetVolumetricFogParams(out var volumetricFogParams))
            {
                VolumeHandler.Instance.SetVolumetricFog(volumetricFogParams.distance, volumetricFogParams.density, volumetricFogParams.tint, volumetricFogParams.scattering, volumetricFogParams.anisotropy, volumetricFogParams.attenuationDistance, volumetricFogParams.baseHeight, volumetricFogParams.maximumHeight, true, volumetricFogParams.mainLightContribution, volumetricFogParams.additionalLightContribution);
            }

            //按场景配置播放环境音（AudioInfo 表 id，空/0=不播；离场在 UnLoadScene(Fight) 停止）
            if (fightSceneData.HasEnvironmentSound)
            {
                AudioHandler.Instance.PlayEnvironment(fightSceneData.environment_sound);
            }
        }

        dicCurrentScene.Add(GameSceneTypeEnum.Fight, targetScene);
        currentScene = targetScene;

        //获取战斗道路-----------------------------------------------------------
        var sceneRoad = await manager.GetFightSceneRoad();
        sceneRoad.transform.SetParent(targetScene.transform);
        //设置道路数据
        sceneRoad.transform.localScale = new Vector3(fightData.sceneRoadLength, fightData.sceneRoadNum, 1);
        sceneRoad.transform.eulerAngles = new Vector3(90, 0, 0);
        sceneRoad.transform.position = new Vector3(fightData.sceneRoadLength / 2f + 0.5f, WorldManager.FightSceneRoadHeightY, fightData.sceneRoadNum / 2f + 0.5f);
        var roadMR = sceneRoad.GetComponent<MeshRenderer>();
        //道路参数走 MaterialPropertyBlock 写入，避免 sharedMaterial 直接改共享材质资源（编辑器下会持久化污染 .mat，曾导致道路透明度串场景）
        var roadMPB = new MaterialPropertyBlock();
        roadMR.GetPropertyBlock(roadMPB);
        roadMPB.SetVector("_GridSize", new Vector2(fightData.sceneRoadLength, fightData.sceneRoadNum));

        ColorUtility.TryParseHtmlString($"{roadColorA}", out var colorA);
        ColorUtility.TryParseHtmlString($"{roadColorB}", out var colorB);
        roadMPB.SetColor("_ColorA", colorA);
        roadMPB.SetColor("_ColorB", colorB);
        //道路透明度：shader 的 Alpha 仅取 _Alpha 浮点（与颜色 alpha 无关），按场景配置设置
        roadMPB.SetFloat("_Alpha", roadAlpha);
        roadMR.SetPropertyBlock(roadMPB);
    }

    /// <summary>
    /// 还原进入战斗场景前的全局环境光（仅当某次加载配置过 ambient_light 才有缓存，还原后清除标记）
    /// </summary>
    protected void RestoreFightSceneAmbientLight()
    {
        if (!hasCacheAmbientLight)
            return;
        RenderSettings.ambientLight = cacheAmbientLight;
        hasCacheAmbientLight = false;
    }

    /// <summary>
    /// 处理战斗场景的细节预制（场景根下名为 Details 的直接子物体）：
    /// 配置了 details 时只显示 Details 下同名的子预制、隐藏其它子预制；
    /// 未配置 details 时整个 Details 节点隐藏；场景里没有 Details 节点则不处理
    /// </summary>
    /// <param name="targetScene">战斗场景根物体</param>
    /// <param name="fightSceneData">战斗场景配置数据</param>
    protected void HandleFightSceneDetails(GameObject targetScene, FightSceneBean fightSceneData)
    {
        Transform tfDetails = targetScene.transform.Find("Details");
        if (tfDetails == null)
            return;
        //未配置 details：该场景没有细节预制，整个 Details 节点隐藏
        if (!fightSceneData.HasDetails)
        {
            tfDetails.gameObject.SetActive(false);
            return;
        }
        //配置了 details：只显示同名子预制，隐藏其它子预制
        tfDetails.gameObject.SetActive(true);
        string detailsName = fightSceneData.details.Trim();
        bool isFind = false;
        for (int i = 0; i < tfDetails.childCount; i++)
        {
            GameObject objChild = tfDetails.GetChild(i).gameObject;
            bool isTarget = objChild.name == detailsName;
            if (isTarget) isFind = true;
            objChild.SetActive(isTarget);
        }
        if (!isFind)
        {
            LogUtil.LogWarning($"战斗场景 {fightSceneData.name_res} 配置了细节预制 {detailsName}，但 Details 节点下没有找到同名子物体");
        }
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
                DestroyImmediate(targetScene);
            }
            dicCurrentScene.Remove(gameSceneType);
        }
        //卸载领奖场景时关闭体积雾
        if (gameSceneType == GameSceneTypeEnum.RewardSelect)
        {
            VolumeHandler.Instance.SetVolumetricFogActive(false);
        }
        //卸载战斗场景时关闭内置雾，防止森林雾残留
        if (gameSceneType == GameSceneTypeEnum.Fight)
        {
            VolumeHandler.Instance.SetFogActive(false);
            //还原进入战斗前的全局环境光（仅配置过 ambient_light 的场景有缓存）
            RestoreFightSceneAmbientLight();
            //停止战斗场景配置的环境音（仅配置了 environment_sound 的场景播放过，停止本身幂等）
            AudioHandler.Instance.StopEnvironment();
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
            if (targetScene == null)
                continue;
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
        //关闭内置雾，防止森林雾残留到其它场景
        VolumeHandler.Instance.SetFogActive(false);
        //还原进入战斗前的全局环境光
        RestoreFightSceneAmbientLight();
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
        //关闭所有控制(内部经 EnabledControl(false) 会顺带清空冲刺残影池)
        GameControlHandler.Instance.manager.EnableAllControl(false);
        //停止所有连续音效，防走路/环境循环音跨场景残留
        AudioHandler.Instance.StopAllLoopSound();
        await new WaitNextFrame();
        //卸载场景
        await UnLoadAllScene();
        await new WaitNextFrame();
        //logic清理
        BaseGameLogic gameLogic = GameHandler.Instance.manager.GetGameLogic<BaseGameLogic>();
        if (gameLogic != null)
        {
           await gameLogic.ClearGame();
        }
        await new WaitNextFrame();
        //清理粒子(统一入口：实例+飘字+拖尾VFX,与 ClearGame 同一收口)
        EffectHandler.Instance.ClearAllEffect();
        await new WaitNextFrame();
        //清理缓存
        System.GC.Collect();
        await new WaitNextFrame();
    }
    #endregion
}
