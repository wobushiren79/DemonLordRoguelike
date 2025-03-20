using System;
using UnityEngine;

public class WorldHandler : BaseHandler<WorldHandler, WorldManager>
{
    //当前战斗场景
    public GameObject currentFightScene;
    //基地场景
    public GameObject currentBaseScene;

    /// <summary>
    /// 进入游戏进入主界面选项
    /// </summary>
    public void EnterMainForBaseScene()
    {
        ClearWorldData(() =>
        {
            //打开加载UI
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Base);
            //加载基地场景
            LoadBaseScene((targetObj) =>
            {
                //关闭LoadingUI 打开开始UI
                UIHandler.Instance.OpenUIAndCloseOther<UIMainStart>();
            });
        });
    }

    /// <summary>
    /// 进入游戏中 基地场景
    /// </summary>
    public void EnterGameForBaseScene(UserDataBean userData, bool isInitScene)
    {
        Action actionForStart = () =>
        {
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Base);
            //设置基地场景视角
            CameraHandler.Instance.InitBaseSceneControlCamera(() =>
            {
                //关闭LoadingUI
                var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
            }, userData.selfCreature);
        };
        if (isInitScene)
        {
            //清理世界数据
            ClearWorldData(() =>
            {
                //加载基地场景
                LoadBaseScene((targetObj) =>
                {
                    actionForStart?.Invoke();
                });
            });
        }
        else
        {
            actionForStart?.Invoke();
        }
    }



    /// <summary>
    /// 进入战斗场景
    /// </summary>
    public void EnterGameForFightScene(FightBean fightData)
    {
        ClearWorldData(() =>
        {
            //打开加载UI
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData(GameSceneTypeEnum.Fight);
            //开始战斗
            GameHandler.Instance.StartGameFight(fightData);
        });
    }

    /// <summary>
    /// 加载基地场景
    /// </summary>
    /// <param name="actionForComplete"></param>
    public void LoadBaseScene(Action<GameObject> actionForComplete)
    {
        UnLoadBaseScene();
        manager.GetBaseScene((targetScene) =>
        {
            currentBaseScene = Instantiate(targetScene);
            currentBaseScene.SetActive(true);
            currentBaseScene.transform.position = Vector3.zero;
            currentBaseScene.transform.eulerAngles = Vector3.zero;
            actionForComplete?.Invoke(currentBaseScene);
        });
    }

    /// <summary>
    /// 加载战斗场景
    /// </summary>
    public void LoadFightScene(FightBean fightData, Action<GameObject> actionForComplete)
    {
        UnLoadFightScene();

        FightSceneBean fightSceneData = FightSceneCfg.GetItemData(fightData.fightSceneId);
        if (fightSceneData == null)
        {
            LogUtil.LogError($"查询FightScene战斗场景失败  没有找到id为{fightData.fightSceneId}的战斗场景");
            return;
        }
        //获取天空盒
        manager.GetSkybox(fightSceneData.skybox_mat, (skyboxMat) =>
        {

            //设置天空盒
            RenderSettings.skybox = skyboxMat;
            //获取场景
            string dataPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneData.name_res}";
            manager.GetFightScene(dataPath, (targetScene) =>
            {
                currentFightScene = Instantiate(targetScene);
                currentFightScene.SetActive(true);
                currentFightScene.transform.position = Vector3.zero;
                currentFightScene.transform.eulerAngles = Vector3.zero;

                //获取战斗道路
                manager.GetFightSceneRoad((targetSceneRoad) =>
                {
                    var sceneRoad = Instantiate(targetSceneRoad);
                    sceneRoad.transform.SetParent(currentFightScene.transform);
                    //设置道路数据
                    sceneRoad.transform.localScale = new Vector3(fightData.sceneRoadLength, fightData.sceneRoadNum, 1);
                    sceneRoad.transform.eulerAngles = new Vector3(90, 0, 0);
                    sceneRoad.transform.position = new Vector3(fightData.sceneRoadLength / 2f + 0.5f, 0, fightData.sceneRoadNum / 2f + 0.5f);
                    var roadMR = sceneRoad.GetComponent<MeshRenderer>();
                    roadMR.sharedMaterial.SetVector("_GridSize", new Vector2(fightData.sceneRoadLength, fightData.sceneRoadNum));
                    actionForComplete?.Invoke(currentFightScene);
                });
            });
        });
    }

    /// <summary>
    /// 卸载战斗场景
    /// </summary>
    public void UnLoadFightScene()
    {
        //删除已有的战斗场景
        if (currentFightScene != null)
        {
            DestroyImmediate(currentFightScene);
        }
        //移除天空盒
        manager.RemoveSkybox();
    }

    /// <summary>
    /// 卸载基地场景
    /// </summary>
    public void UnLoadBaseScene()
    {
        if (currentBaseScene != null)
        {
            DestroyImmediate(currentBaseScene);
        }
        //移除天空盒
        manager.RemoveSkybox();
    }

    /// <summary>
    /// 清理世界所有数据
    /// </summary>
    public async void ClearWorldData(Action actionForComplete, bool isShowLoading = true)
    {
        //打开加载UI
        if (isShowLoading)
            UIHandler.Instance.OpenUIAndCloseOther<UICommonLoading>();
        //关闭所有控制
        GameControlHandler.Instance.manager.EnableAllControl(false);

        await new WaitNextFrame();
        UnLoadBaseScene();
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
        actionForComplete?.Invoke();
    }
}
