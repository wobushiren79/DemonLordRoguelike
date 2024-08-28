using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class GashaponMachineLogic : BaseGameLogic
{
    public GashaponMachineBean gashaponMachineData;

    //展示吐蛋的间隔
    public float timeForAnimShowEggInterval = 0.3f;

    //核心建筑
    public GameObject objBuildingCore;
    public VisualEffect effectEggBreak;

    //蛋预制
    public List<GameObject> listEggObjPool = new List<GameObject>();


    //所有生成数据
    public List<GashaponItemBean> listGashaponData = new List<GashaponItemBean>();
    //所有生成数据
    public List<GameObject> listEggObj = new List<GameObject>();
    //当前破碎的index
    public int currentBreakIndex = 0;

    public bool isRegisterEvent = false;

    public string eggChildFbxName = "Egg_1";
    public string eggChildRendererName = "Renderer";
    public override void PreGameForRegisterEvent()
    {
        if (!isRegisterEvent)
        {
            isRegisterEvent = true;
            this.RegisterEvent<GameObject, GashaponItemBean>(EventsInfo.GashaponMachine_ClickBreak, EventForEggBreak);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickNext, EventForNextEgg);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickReset, EventForReset);
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickEnd, EventForEnd);
        }
    }

    public override void PreGame()
    {
        base.PreGame();
        //初始化场景
        InitSceneData();
        //初始化数据
        InitGashaponMachineData(() =>
        {
            //开始
            StartGame();
        });
        //设置摄像头
        CameraHandler.Instance.SetGashaponMachineCamera(int.MaxValue, true);
        //先暂时关闭所有UI
        UIHandler.Instance.CloseAllUI();
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
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        objBuildingCore = baseSceneObj.transform.Find("Core/Building").gameObject;
        effectEggBreak = baseSceneObj.transform.Find("Effect/EggBreak").GetComponent<VisualEffect>();

        listEggObj.Clear();
        for (int i = 0; i < listEggObjPool.Count; i++)
        {
            var targetEgg = listEggObjPool[i];
            var eggTF = targetEgg.transform.Find(eggChildFbxName);
            var eggSpine = targetEgg.transform.Find(eggChildRendererName).GetComponent<SkeletonAnimation>();
            eggTF.ShowObj(false);
            eggSpine.ShowObj(false);
        }
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
            GashaponItemBean itemGashapon = new GashaponItemBean(999990+i);
            listGashaponData.Add(itemGashapon);

            var creatureInfo = itemGashapon.creatureData.GetCreatureInfo();
            var caretureModelInfo = CreatureModelCfg.GetItemData(creatureInfo.model_id);
            listPreLoadSpineData.Add(caretureModelInfo.res_name);
            if (!caretureModelInfo.ui_show_spine.IsNull())
            {
                listPreLoadSpineData.Add(caretureModelInfo.ui_show_spine);
            }
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

    public override void ClearGame()
    {
        base.ClearGame();

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
    public void AnimForEggBreak(GameObject targetEgg, GashaponItemBean gashaponItemData,Action actionForComplete)
    {
        var eggTF = targetEgg.transform.Find(eggChildFbxName);
        var eggSpine = targetEgg.transform.Find(eggChildRendererName).GetComponent<SkeletonAnimation>();

        targetEgg.transform.DOKill();
        eggSpine.transform.DOKill();

        eggTF.ShowObj(false);
        eggSpine.ShowObj(true);

        var creatureData = gashaponItemData.creatureData;
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);

        //设置大小
        eggSpine.transform.localScale = Vector3.zero;
        SpineHandler.Instance.SetSkeletonDataAsset(eggSpine, creatureModel.res_name);
        string[] skinArray = creatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(eggSpine.skeleton, skinArray);
        //播放缩放动画
        eggSpine.transform.DOScale(Vector3.one * creatureModel.size_spine, 0.2f).OnComplete(() =>
        {
            actionForComplete?.Invoke();
        });
        //播放spine动画
        eggSpine.AnimationState.SetAnimation(0, SpineAnimationStateEnum.Idle.ToString(), true);


        MeshRenderer eggRenderer = eggTF.GetComponentInChildren<MeshRenderer>();
        Color eggColor1 = eggRenderer.material.GetColor("_Color_1");
        Color eggColor2 = eggRenderer.material.GetColor("_Color_2");

        //播放蛋壳破碎粒子
        effectEggBreak.SetVector3("MeshSize", eggTF.transform.localScale);
        effectEggBreak.SetVector3("StartPosition", eggTF.transform.position);
        effectEggBreak.SetVector4("Color1", eggColor1);
        effectEggBreak.SetVector4("Color2", eggColor2);
        effectEggBreak.SendEvent("OnPlay");
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
            objEgg = GameHandler.Instance.manager.GetGameObjectSync("Assets/LoadResources/Common/Gashapon_1.prefab");
            listEggObjPool.Add(objEgg);
        }
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        objEgg.transform.SetParent(baseSceneObj.transform);
        objEgg.gameObject.SetActive(true);
        objEgg.transform.position = objBuildingCore.transform.position + new Vector3(0, 0.5f, -0.2f);
        objEgg.transform.localScale = Vector3.zero;
        objEgg.transform.eulerAngles = Vector3.zero;
        objEgg.transform.DOKill();

        var eggTF = objEgg.transform.Find(eggChildFbxName);
        eggTF.ShowObj(true);
        eggTF.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);

        MeshRenderer eggRenderer = eggTF.GetComponentInChildren<MeshRenderer>();
        eggRenderer.material.SetColor("_Color_1", Color.white);
        eggRenderer.material.SetColor("_Color_2", new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f),1));
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
            .DOJump(objBuildingCore.transform.position + new Vector3(-startPos + index, 0, -2), 1, 5, animTimeForEggJump)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                actionForComplete?.Invoke(objEgg);
            });

        objBuildingCore.transform.localScale = Vector3.one;
        objBuildingCore.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), timeForAnimShowEggInterval, 2, 0.5f);
    }
    #endregion
}