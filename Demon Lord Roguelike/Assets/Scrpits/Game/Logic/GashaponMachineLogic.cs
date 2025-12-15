using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class GashaponMachineLogic : BaseGameLogic
{
    public GashaponMachineBean gashaponMachineData;

    //展示吐蛋的间隔
    public float timeForAnimShowEggInterval = 0.3f;

    //场景预制
    public ScenePrefabForBase scenePrefab;

    //蛋预制
    public List<GameObject> listEggObjPool = new List<GameObject>();


    //所有生成数据
    public List<GashaponItemBean> listGashaponData = new List<GashaponItemBean>();
    //所有生成数据
    public List<GameObject> listEggObj = new List<GameObject>();
    //当前破碎的index
    public int currentBreakIndex = 0;

    public bool isRegisterEvent = false;

    //蛋的模型子层级名字
    public string eggChildFbxName = "Egg_1";
    //蛋的渲染子层级名字
    public string eggChildRendererName = "Renderer";
    //蛋预制的资源路径
    public string pathForEggPrefab="Assets/LoadResources/Common/Gashapon_1.prefab";
    public override void PreGame()
    {
        base.PreGame();
        //注册事件
        if (!isRegisterEvent)
        {
            isRegisterEvent = true;
            this.RegisterEvent<GameObject, GashaponItemBean>(EventsInfo.GashaponMachine_ClickBreak, EventForEggBreak);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickNext, EventForNextEgg);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickReset, EventForReset);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickEnd, EventForEnd);
        }
        //初始化场景
        InitSceneData();
        //初始化数据
        InitGashaponMachineData(() =>
        {
            //开始
            StartGame();
        });
    }

    public override void StartGame()
    {
        base.StartGame();
        ProcessForShowEgg((listEgg) =>
        {
            ProcessForEggBreak(currentBreakIndex);
        });
    }

    /// <summary>
    /// 处理场景数据
    /// </summary>
    public void InitSceneData()
    {
        //场景实例
        var scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);

        listEggObj.Clear();
        for (int i = 0; i < listEggObjPool.Count; i++)
        {
            var targetEgg = listEggObjPool[i];
            var eggTF = targetEgg.transform.Find(eggChildFbxName);
            var eggSpine = targetEgg.transform.Find(eggChildRendererName).GetComponent<SkeletonAnimation>();
            eggTF.ShowObj(false);
            eggSpine.ShowObj(false);
        }
        //设置摄像头
        CameraHandler.Instance.SetGashaponMachineCamera(int.MaxValue, true);
        //先暂时关闭所有UI
        UIHandler.Instance.CloseAllUI();
    }

    /// <summary>
    /// 处理所有蛋的数据
    /// </summary>
    public void InitGashaponMachineData(Action actionForComplete)
    {
        currentBreakIndex = 0;
        listGashaponData = new List<GashaponItemBean>();
        int eggNum = gashaponMachineData.gashaponNum;

        List<string> listPreLoadSpineData = new List<string>();
        for (int i = 0; i < eggNum; i++)
        {
            //随机一个生物
            int randomCreatureIndex = UnityEngine.Random.Range(0, gashaponMachineData.listCreatureRandomData.Count);
            var randomCreatureData = gashaponMachineData.listCreatureRandomData[randomCreatureIndex];

            GashaponItemBean itemGashapon = new GashaponItemBean(randomCreatureData.creatureId,randomCreatureData);
            listGashaponData.Add(itemGashapon);

            var creatureInfo = itemGashapon.creatureData.creatureInfo;
            var caretureModelInfo = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            listPreLoadSpineData.Add(caretureModelInfo.res_name);
            if (!caretureModelInfo.ui_show_spine.IsNull())
            {
                listPreLoadSpineData.Add(caretureModelInfo.ui_show_spine);
            }
            //添加生成的数据
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            userData.AddBackpackCreature(itemGashapon.creatureData);

        }
        SpineHandler.Instance.PreLoadSkeletonDataAsset(listPreLoadSpineData, (dicData) =>
        {
            actionForComplete?.Invoke();
        });
    }

    /// <summary>
    /// 流程-展示egg
    /// </summary>
    public async void ProcessForShowEgg(Action<List<GameObject>> actionForComplete)
    {
        int gashaponNum = gashaponMachineData.gashaponNum;
        int showNum = 0;
        listEggObj = new List<GameObject>();
        Action<GameObject> actionForShowEnd = (targetEgg) =>
        {
            listEggObj.Add(targetEgg);
            showNum++;
            //展示完成
            if (showNum == gashaponNum)
            {
                actionForComplete?.Invoke(listEggObj);
            }
        };
        for (int i = 0; i < gashaponNum; i++)
        {
            AnimForShowEgg(i, actionForShowEnd);
            await new WaitForSeconds(timeForAnimShowEggInterval);
        }
    }

    /// <summary>
    /// 流程-蛋破碎
    /// </summary>
    public void ProcessForEggBreak(int targetIndex)
    {
        this.currentBreakIndex = targetIndex;
        //获取列表中第一个蛋
        var targetEggObj = listEggObj[currentBreakIndex];
        var targetEggData = listGashaponData[currentBreakIndex];
        //设置摄像头
        var targetCamera = CameraHandler.Instance.SetGashaponBreakCamera(int.MaxValue, true);
        targetCamera.LookAt = targetEggObj.transform;
        targetCamera.Follow = targetEggObj.transform;
        //打开UI
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponBreak>();
        targetUI.InitForClick(targetEggObj, targetEggData);
        //晃动蛋
        AnimForEggPunch(targetEggObj);
    }

    public override void UpdateGame()
    {
        base.UpdateGame();
    }

    public override void EndGame()
    {
        base.EndGame();

        UIHandler.Instance.OpenUIAndCloseOther<UIGashaponMachine>();
    }

    public override async Task ClearGame()
    {
        CameraHandler.Instance.SetGashaponMachineCamera(0, false);
        if (!listEggObjPool.IsNull())
        {
            for (int i = 0; i < listEggObjPool.Count; i++)
            {
                var itemEgg = listEggObjPool[i];
                GameObject.DestroyImmediate(itemEgg);
            }
            listEggObjPool.Clear();
        }
        isRegisterEvent = false;
        await base.ClearGame();
    }

    #region 事件
    /// <summary>
    /// 事件-展示蛋破碎
    /// </summary>
    public void EventForEggBreak(GameObject targetEgg, GashaponItemBean gashaponItemData)
    {
        Action aciontForComplete = () =>
        {
            var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponBreak>();
            if (currentBreakIndex == gashaponMachineData.gashaponNum - 1)
            {
                targetUI.InitFoEnd();
            }
            else
            {
                targetUI.InitForBreak(targetEgg, gashaponItemData);
            }
        };
        AnimForEggBreak(targetEgg, gashaponItemData, aciontForComplete);
    }

    /// <summary>
    /// 事件-下一个蛋
    /// </summary>
    public void EventForNextEgg()
    {
        ProcessForEggBreak(currentBreakIndex + 1);
    }

    /// <summary>
    /// 事件-重置
    /// </summary>
    public void EventForReset()
    {
        PreGame();
    }

    /// <summary>
    /// 事件-结束
    /// </summary>
    public void EventForEnd()
    {
        EndGame();
    }
    #endregion

    #region 动画
    /// <summary>
    /// 动画-蛋破壳
    /// </summary>
    /// <param name="targetEgg"></param>
    public void AnimForEggBreak(GameObject targetEgg, GashaponItemBean gashaponItemData, Action actionForComplete)
    {
        var eggTF = targetEgg.transform.Find(eggChildFbxName);
        var eggSpine = targetEgg.transform.Find(eggChildRendererName).GetComponent<SkeletonAnimation>();

        targetEgg.transform.DOKill();
        eggSpine.transform.DOKill();

        eggTF.ShowObj(false);
        eggSpine.ShowObj(true);

        var creatureData = gashaponItemData.creatureData;
        //设置大小
        eggSpine.transform.localScale = Vector3.zero;
        //设置spine
        CreatureHandler.Instance.SetCreatureData(eggSpine, creatureData);
        //播放缩放动画
        eggSpine.transform.DOScale(Vector3.one * creatureData.creatureModel.size_spine, 0.2f).OnComplete(() =>
        {
            actionForComplete?.Invoke();
        });
        //播放spine动画
        SpineHandler.Instance.PlayAnim(eggSpine, SpineAnimationStateEnum.Idle, creatureData, true);

        MeshRenderer eggRenderer = eggTF.GetComponentInChildren<MeshRenderer>();
        Color eggColor1 = eggRenderer.material.GetColor("_Color_1");
        Color eggColor2 = eggRenderer.material.GetColor("_Color_2");

        //播放蛋壳破碎粒子
        scenePrefab.effectEggBreak.SetVector3("MeshSize", eggTF.transform.localScale);
        scenePrefab.effectEggBreak.SetVector3("StartPosition", eggTF.transform.position);
        scenePrefab.effectEggBreak.SetVector4("Color1", eggColor1);
        scenePrefab.effectEggBreak.SetVector4("Color2", eggColor2);
        scenePrefab.effectEggBreak.SendEvent("OnPlay");
    }

    /// <summary>
    /// 动画-蛋摇动
    /// </summary>
    public void AnimForEggPunch(GameObject targetEgg)
    {
        targetEgg.transform.DOKill();
        float randomX = UnityEngine.Random.Range(-10f, 10f);
        float randomZ = UnityEngine.Random.Range(-10f, 10f);
        targetEgg.transform.DOPunchRotation(new Vector3(randomX, 0, randomZ), 1, 4, 1).OnComplete(() =>
        {
            AnimForEggPunch(targetEgg);
        });
    }

    /// <summary>
    /// 吐蛋动画
    /// </summary>
    public void AnimForShowEgg(int index, Action<GameObject> actionForComplete)
    {
        GameObject objEgg;
        if (index < listEggObjPool.Count)
        {
            //使用老蛋
            objEgg = listEggObjPool[index];
        }
        else
        {
            //创建一个新蛋
            objEgg = GameHandler.Instance.manager.GetGameObjectSync(pathForEggPrefab);
            listEggObjPool.Add(objEgg);
        }
        var baseSceneObj = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.BaseGaming);
        objEgg.transform.SetParent(baseSceneObj.transform);
        objEgg.gameObject.SetActive(true);
        objEgg.transform.position = scenePrefab.objBuildingCore.transform.position + new Vector3(0, 0.5f, -0.2f);
        objEgg.transform.localScale = Vector3.zero;
        objEgg.transform.eulerAngles = Vector3.zero;
        objEgg.transform.DOKill();

        var eggTF = objEgg.transform.Find(eggChildFbxName);
        eggTF.ShowObj(true);
        eggTF.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);

        MeshRenderer eggRenderer = eggTF.GetComponentInChildren<MeshRenderer>();
        eggRenderer.material.SetColor("_Color_1", Color.white);
        eggRenderer.material.SetColor("_Color_2", new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1));
        float startPos;
        if (gashaponMachineData.gashaponNum == 1)
        {
            startPos = 0;
        }
        else if (gashaponMachineData.gashaponNum == 10)
        {
            startPos = 4.5f;
        }
        else
        {
            startPos = gashaponMachineData.gashaponNum / 2f;
        }
        float animTimeForEggJump = 1;
        objEgg.transform.DOScale(Vector3.one, animTimeForEggJump / 2f);
        objEgg.transform
            .DOJump(scenePrefab.objBuildingCore.transform.position + new Vector3(-startPos + index, 0, -2), 1, 5, animTimeForEggJump)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                actionForComplete?.Invoke(objEgg);
            });
        //监视之塔做一个吐出的动作
        scenePrefab.AnimForBuildingCoreSpit(timeForAnimShowEggInterval);
    }
    #endregion
}