using DG.Tweening;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class GashaponMachineLogic : BaseGameLogic
{
    public GashaponMachineBean gashaponMachineData;

    //展示吐蛋的间隔
    public float timeForAnimShowEggInterval = 0.2f;

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
    private bool isPreparingBatch;

    //蛋的模型子层级名字
    public string eggChildFbxName = "Egg_1";
    //蛋的渲染子层级名字
    public string eggChildRendererName = "Renderer";
    //蛋预制的资源路径
    public string pathForEggPrefab = "Assets/LoadResources/Common/Gashapon_1.prefab";

    #region 孕育流程
    /// <summary>
    /// 整批生成及提交成功后进入破壳演出，失败时保留当前商店或上一批的结果界面
    /// </summary>
    public override void PreGame()
    {
        if (isPreparingBatch || gameState == GameStateEnum.Pre)
            return;

        isPreparingBatch = true;
        try
        {
            scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForBase>(GameSceneTypeEnum.BaseGaming);
            if (!TryCreateGashaponBatch(out var rewards, out var spineResources))
                return;

            listGashaponData = rewards;
            currentBreakIndex = 0;
            GameHandler.Instance.manager.gameLogic = this;
            BeginGashaponPresentation(spineResources);
        }
        finally
        {
            isPreparingBatch = false;
        }
    }

    /// <summary>
    /// 初始化当批场景和事件，并在预加载模型后开始展示
    /// </summary>
    private void BeginGashaponPresentation(List<string> spineResources)
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
            this.RegisterEvent(EventsInfo.GashaponMachine_ClickShowAll, EventForShowAll);
        }
        InitSceneData();
        VolumeHandler.Instance.SetDepthOfFieldActive(false);
        SpineHandler.Instance.PreLoadSkeletonDataAsset(spineResources, _ => StartGame());
    }

    public override void StartGame()
    {
        base.StartGame();
        ProcessForShowEgg((listEgg) =>
        {
            ProcessForFocusEgg(currentBreakIndex);
        });
    }

    public override void UpdateGame()
    {
        base.UpdateGame();
    }

    public override void EndGame()
    {
        base.EndGame();
        VolumeHandler.Instance.SetDepthOfFieldActive(true);
        UIHandler.Instance.OpenUIAndCloseOther<UIGashaponMachine>();
    }


    /// <summary>
    /// 处理场景数据
    /// </summary>
    public void InitSceneData()
    {
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
    /// 在不扣款的情况下生成整批魔物，再一次提交费用和完整结果
    /// </summary>
    private bool TryCreateGashaponBatch(out List<GashaponItemBean> rewards, out List<string> spineResources)
    {
        rewards = null;
        spineResources = null;
        if (gashaponMachineData == null || gashaponMachineData.gashaponNum <= 0
            || gashaponMachineData.listCreatureRandomData.IsNull())
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(2000017));
            return false;
        }

        var dataManager = GameDataHandler.Instance.manager;
        if (!dataManager.GetUserData().CheckHasCrystal(gashaponMachineData.payCrystal, isHint: true))
            return false;

        var candidates = gashaponMachineData.listCreatureRandomData;
        int eggNum = gashaponMachineData.gashaponNum;
        rewards = new List<GashaponItemBean>(eggNum);
        spineResources = new List<string>();
        try
        {
            if (scenePrefab == null)
                throw new InvalidOperationException("未找到孕育场景");

            for (int i = 0; i < eggNum; i++)
            {
                var candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                var reward = new GashaponItemBean(candidate.creatureId, candidate);
                rewards.Add(reward);
                var modelInfo = CreatureModelCfg.GetItemData(reward.creatureData.creatureInfo.model_id);
                spineResources.Add(modelInfo.res_name);
                if (!modelInfo.ui_show_spine.IsNull())
                    spineResources.Add(modelInfo.ui_show_spine);
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"生成孕育结果失败:{e.Message}");
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(2000034));
            return false;
        }

        if (dataManager.TryCommitGashaponBatch(gashaponMachineData.payCrystal, rewards))
            return true;

        UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(2000034));
        return false;
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
    /// 流程-展示所有蛋
    /// </summary>
    public void ProcessForShowAllEgg()
    {
        //设置摄像头
        var targetCamera = CameraHandler.Instance.SetGashaponMachineCamera(int.MaxValue, true);
        //打开UI
        var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponBreak>();
        targetUI.InitForEnd(listGashaponData);
    }

    /// <summary>
    /// 流程-焦点某一个蛋
    /// </summary>
    public void ProcessForFocusEgg(int targetIndex)
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
        targetUI.InitForShow(targetEggObj, targetEggData);
        //晃动蛋
        AnimForEggPunch(targetEggObj);
    }

    /// <summary>
    /// 清理游戏
    /// </summary>
    public override async Task ClearGame()
    {
        //ClearWorldData 先卸载场景再清逻辑，此时摄像头已随场景销毁，仅场景仍在时才需还原（否则报"没有找到对应场景"）
        if (WorldHandler.Instance.GetCurrentScene() != null)
        {
            CameraHandler.Instance.SetGashaponMachineCamera(0, false);
        }
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
    #endregion

    #region 事件
    /// <summary>
    /// 事件-展示蛋破碎
    /// </summary>
    public void EventForEggBreak(GameObject targetEgg, GashaponItemBean gashaponItemData)
    {
        Action aciontForComplete = () =>
        {
            var targetUI = UIHandler.Instance.OpenUIAndCloseOther<UIGashaponBreak>();
            targetUI.InitForBreak(targetEgg, gashaponItemData);
        };
        AnimForEggBreak(targetEgg, gashaponItemData, aciontForComplete);
    }

    /// <summary>
    /// 事件-下一个蛋
    /// </summary>
    public void EventForNextEgg()
    {
        //如果已经没有蛋了
        int nextIndex = currentBreakIndex + 1;
        if (nextIndex >= listEggObj.Count)
        {
            ProcessForShowAllEgg();
        }
        else
        {
            ProcessForFocusEgg(nextIndex);
        }
    }

    /// <summary>
    /// 事件-重置
    /// </summary>
    public void EventForReset()
    {
        if (gameState == GameStateEnum.Gaming)
            PreGame();
    }

    /// <summary>
    /// 事件-结束
    /// </summary>
    public void EventForEnd()
    {
        EndGame();
    }

    /// <summary>
    /// 事件-展示所有
    /// </summary>
    public async void EventForShowAll()
    {
        for (int i = 0; i < listGashaponData.Count; i++)
        {
            var targetEggData = listGashaponData[i];
            if (targetEggData.isBreak)
            {
                continue;
            }
            var targetEggObj = listEggObj[i];
            AnimForEggBreak(targetEggObj, targetEggData, null);
            //每个蛋壳破碎依次间隔0.05秒，避免同一时间全部打开
            await new WaitForSeconds(0.05f);
        }
        //等最后一个蛋的破壳动画播完，全部破碎后再打开展示全部UI
        await new WaitForSeconds(0.2f);
        ProcessForShowAllEgg();
    }
    #endregion

    #region 动画
    /// <summary>
    /// 动画-蛋破壳
    /// </summary>
    /// <param name="targetEgg"></param>
    public void AnimForEggBreak(GameObject targetEgg, GashaponItemBean targetGashaponData, Action actionForComplete)
    {
        targetGashaponData.isBreak = true;

        var eggTF = targetEgg.transform.Find(eggChildFbxName);
        var eggSpine = targetEgg.transform.Find(eggChildRendererName).GetComponent<SkeletonAnimation>();

        targetEgg.transform.DOKill();
        eggSpine.transform.DOKill();
        targetEgg.transform.eulerAngles = Vector3.zero;

        Vector3 originEggSize = eggTF.localScale;
        eggTF.ShowObj(true);
        //蛋壳放大
        eggTF
            .DOScale(originEggSize * 2f, 0.1f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                eggTF.localScale = originEggSize;
                eggTF.ShowObj(false);
            });

        eggSpine.ShowObj(true);
        var creatureData = targetGashaponData.creatureData;
        //设置大小
        eggSpine.transform.localScale = Vector3.zero;
        //设置spine
        CreatureHandler.Instance.SetCreatureData(eggSpine, creatureData);
        //播放缩放动画
        eggSpine.transform
            .DOScale(Vector3.one * creatureData.creatureModel.size_spine, 0.2f)
            .OnComplete(() =>
            {
                actionForComplete?.Invoke();
            });
        //播放spine动画
        SpineHandler.Instance.PlayAnim(eggSpine, SpineAnimationStateEnum.Idle, creatureData, true);

        MeshRenderer eggRenderer = eggTF.GetComponentInChildren<MeshRenderer>();
        Color eggColor1 = eggRenderer.material.GetColor("_Color_1");
        Color eggColor2 = eggRenderer.material.GetColor("_Color_2");

        //播放蛋壳破碎音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_break_1);
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

        float startPos = VectorUtil.GetCenterToTwoSide(0, 1, gashaponMachineData.gashaponNum, index);
        float animTimeForEggJump = 0.5f;
        //播放蛋出现音效
        AudioHandler.Instance.PlaySound(AudioEnum.sound_btn_31);
        objEgg.transform.DOScale(Vector3.one, animTimeForEggJump / 2f);
        objEgg.transform
            .DOJump(scenePrefab.objBuildingCore.transform.position + new Vector3(startPos, 0, -2), 0.5f, 3, animTimeForEggJump)
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
