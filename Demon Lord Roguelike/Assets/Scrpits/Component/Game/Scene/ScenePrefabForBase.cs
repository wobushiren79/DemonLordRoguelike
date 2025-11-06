using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;
using Unity.Burst.Intrinsics;
using System.Threading.Tasks;

public class ScenePrefabForBase : ScenePrefabBase
{
    //核心建筑
    public GameObject objBuildingCore;
    //核心眼睛
    public GameObject objBuildingCoreEye;

    public VisualEffect effectEggBreak;
    //祭坛
    public GameObject objBuildingAltar;
    //容器
    public GameObject objBuildingVat;
    public GameObject objVatMaterialCreature;
    //光线
    public Light lightSun;
    public GameObject lightRay;

    public Color vatColorStart = new Color(0, 0.4f, 1f, 0.4f);
    public Color vatColorEnd = new Color(0.11f, 0.06f, 0.5f, 0.7f);

    //public void Awake()
    //{
    //    objBuildingCore = transform.Find("Core/Building").gameObject;
    //    effectEggBreak = transform.Find("Effect/EggBreak").GetComponent<VisualEffect>();
    //}
    /// <summary>
    /// 初始化场景
    /// </summary>
    public override async Task InitSceneData()
    {
        await base.InitSceneData();
        EventHandler.Instance.RegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForCreatureAscendAddProgress);
        await RefreshScene();
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public override async Task RefreshScene()
    {
        await base.RefreshScene();
        BuildingVatRefresh();
        BuildingAltarRefresh();
    }

    public void OnDestroy()
    {
        EventHandler.Instance.UnRegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForCreatureAscendAddProgress);
    }

    public void Update()
    {
        HandleUpdateForBuildingCore();
    }

    #region 核心建筑眼球
    //核心建筑眼球看向目标
    protected Vector3 targetLookAtForBuildingCoreEye;
    //核心建筑眼球转动速度
    protected float speedRotationForBuildingCoreEye = 0.1f;
    //核心建筑眼球默认看向位置
    protected Vector3 positionDefLookAtForBuildingCoreEye = new Vector3(0, 0, -1000f);
    /// <summary>
    /// 处理核心建筑的眼睛
    /// </summary>
    public void HandleUpdateForBuildingCore()
    {
        var controlForBaseCreature = GameControlHandler.Instance.manager.controlTargetForCreature;
        Vector3 targetLookAtPosition = positionDefLookAtForBuildingCoreEye;
        if (controlForBaseCreature != null && controlForBaseCreature.gameObject.activeSelf)
        {
            targetLookAtPosition = controlForBaseCreature.transform.position;
        }
        else
        {
            Camera mainCamera = CameraHandler.Instance.manager.mainCamera;
            //看向摄像头
            if (mainCamera != null)
            {
                targetLookAtPosition = mainCamera.transform.position;
            }
        }
        targetLookAtForBuildingCoreEye = Vector3.Lerp(targetLookAtPosition, targetLookAtForBuildingCoreEye, Time.deltaTime * speedRotationForBuildingCoreEye);
        objBuildingCoreEye.transform.LookAt(targetLookAtForBuildingCoreEye);
        return;
    }
    #endregion

    #region 献祭相关
    public void BuildingAltarRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        bool isUnlockAltar = userUnlock.CheckIsUnlock(21001001);
        //是否解锁祭坛
        if (isUnlockAltar)
        {
            objBuildingAltar.gameObject.SetActive(true);
        }
        else
        {
            objBuildingAltar.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Vat相关
    /// <summary>
    /// 事件-刷新数据
    /// </summary>
    public void EventForCreatureAscendAddProgress()
    {
        BuildingVatRefresh();
    }

    /// <summary>
    /// 刷新建筑容器
    /// </summary>
    public void BuildingVatRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();     
        UserAscendBean userAscend = userData.GetUserAscendData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        int showVatNum = 0;
        //解锁id 210000001-210000006 一共6个
        for (int i = 1; i <= 6; i++)
        {
            bool isUnlockVat = userUnlock.CheckIsUnlock(21000000 + i);
            if (isUnlockVat)
            {
                showVatNum++;
            }
        }

        for (int i = 0; i < objBuildingVat.transform.childCount; i++)
        {
            var itemVat = objBuildingVat.transform.GetChild(i);
            if (i < showVatNum)
            {
                itemVat.gameObject.SetActive(true);
                var itemAscendDetails = userAscend.GetAscendData(i);
                if (itemAscendDetails != null)
                {
                    var creatureData = userData.GetBackpackCreature(itemAscendDetails.creatureUUId);
                    BuildingVatSetState(itemVat, 3, creatureData, itemAscendDetails.progress);
                }
                else
                {
                    BuildingVatSetState(itemVat, 0, null);
                }
            }
            else
            {
                itemVat.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 展示VAT
    /// </summary>
    /// <param name="targetIndex"></param>
    public void BuildingVatShow(int targetIndex)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();

        for (int i = 0; i < objBuildingVat.transform.childCount; i++)
        {
            var itemVat = objBuildingVat.transform.GetChild(i);
            if (itemVat.gameObject.activeSelf && itemVat.gameObject.activeInHierarchy)
            {
                var itemAscendDetails = userAscend.GetAscendData(i);
                //如果是结束展示
                if (targetIndex == -1)
                {
                    //如果VAT是空的。则直接关闭舱门
                    if (itemAscendDetails == null)
                    {
                        BuildingVatSetState(itemVat, 0, null);
                    }
                    continue;
                }
                //是展示的VAT
                if (i == targetIndex)
                {
                    //如果VAT是空的，则打开舱门
                    if (itemAscendDetails == null)
                    {
                        BuildingVatSetState(itemVat, 1, null);
                    }
                }
                //不是展示的VAT
                else
                {
                    //如果VAT是空的。则直接关闭舱门
                    if (itemAscendDetails == null)
                    {
                        BuildingVatSetState(itemVat, 0, null);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 设置容器的状态
    /// </summary>
    /// <param name="state">0关闭未设置生物 1打开未设置生物 2打开设置了生物 3关闭开始加强</param>
    public void BuildingVatSetState(Transform targetVat, int state, CreatureBean creatureData, float progress = 0)
    {
        Animator vatAnim = targetVat.GetComponent<Animator>();
        Transform tfWater = targetVat.Find("Water");
        MeshRenderer renderWater = targetVat.Find("Water/Water").GetComponent<MeshRenderer>();
        Transform tfCreature = targetVat.Find("Creature");
        SkeletonAnimation skeletonAnimation = tfCreature.GetComponent<SkeletonAnimation>();
        switch (state)
        {
            case 0:
                tfWater.gameObject.SetActive(false);
                tfCreature.gameObject.SetActive(false);
                vatAnim.SetInteger("State", 0);
                break;
            case 1:
                tfWater.gameObject.SetActive(false);
                tfCreature.gameObject.SetActive(false);
                vatAnim.SetInteger("State", 1);
                break;
            case 2:
                tfWater.gameObject.SetActive(false);
                tfCreature.gameObject.SetActive(true);
                vatAnim.SetInteger("State", 1);

                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData);
                break;
            case 3:
                tfWater.gameObject.SetActive(true);
                tfCreature.gameObject.SetActive(true);
                vatAnim.SetInteger("State", 0);

                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData);
                renderWater.material.SetFloat("_WaterLevel", 1);
                if (progress != -1)
                {
                    Color water = Color.Lerp(vatColorStart, vatColorEnd, progress);
                    renderWater.material.SetColor("_Color", water);
                }
                break;
        }
    }



    /// <summary>
    /// 开始添加
    /// </summary>
    public void BuildingVatAnimForStart(Transform targetVat, CreatureBean creatureData, List<CreatureBean> listMaterialCreatureData, Action actionForComplete)
    {
        //加水 加生物
        float animTimeWater = 3f;
        float animTimeAddCreature = 2;
        float timeAnimJump = 0.75f;
        //打开舱门
        Animator vatAnim = targetVat.GetComponent<Animator>();
        vatAnim.SetInteger("State", 1);

        Transform tfCreature = targetVat.Find("Creature");
        tfCreature.gameObject.SetActive(true);

        Transform tfWater = targetVat.Find("Water");
        tfWater.gameObject.SetActive(true);
        MeshRenderer renderWater = targetVat.Find("Water/Water").GetComponent<MeshRenderer>();
        renderWater.material.SetFloat("_WaterLevel", -1);
        renderWater.material.SetColor("_Color", vatColorStart);
        //水位上升动画
        renderWater.material.DOFloat(1, "_WaterLevel", animTimeWater).OnComplete(() =>
        {
            actionForComplete?.Invoke();
        });
        //生物添加完毕之后关闭盖子
        DOVirtual.DelayedCall(animTimeAddCreature, () =>
        {
            vatAnim.SetInteger("State", 0);
        });
        //材料生物生成
        if (!listMaterialCreatureData.IsNull())
        {
            for (int i = 0; i < listMaterialCreatureData.Count; i++)
            {
                var itemCreatureData = listMaterialCreatureData[i];
                var targetCreatureObj = Instantiate(gameObject, objVatMaterialCreature);
                targetCreatureObj.SetActive(false);
                float delayShowTime = UnityEngine.Random.Range(0f, animTimeAddCreature * 0.9f);
                DOVirtual.DelayedCall(delayShowTime, () =>
                {
                    //设置生物样子      
                    SkeletonAnimation skeletonAnimation = targetCreatureObj.GetComponentInChildren<SkeletonAnimation>();
                    CreatureHandler.Instance.SetCreatureData(skeletonAnimation, itemCreatureData);
                    targetCreatureObj.SetActive(true);
                    //设置生物位置
                    Vector3 startPosition = tfCreature.position + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 1, UnityEngine.Random.Range(0.5f, 1.5f));
                    targetCreatureObj.transform.position = startPosition;
                    targetCreatureObj.transform.localScale = Vector3.one;
                    targetCreatureObj.transform.eulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(0, 360));
                    Sequence itemJumAnim = DOTween.Sequence();

                    Color startColor = Color.white;
                    Color endColor = new Color(1, 1, 1, 0);
                    var colorAnim = DOTween
                    .To(() => startColor, x => startColor = x, endColor, timeAnimJump / 2f)
                    .SetDelay(timeAnimJump / 2f)
                    .OnUpdate(() =>
                    {
                        // 在每帧更新时执行的操作
                        skeletonAnimation.skeleton.SetColor(startColor);
                    });
                    itemJumAnim.Append(targetCreatureObj.transform.DOJump(tfCreature.position + new Vector3(0, -0.15f, 0), 1.5f, 1, timeAnimJump));
                    itemJumAnim.Join(targetCreatureObj.transform.DOScale(Vector3.one * 0.75f, timeAnimJump));
                    itemJumAnim.Join(targetCreatureObj.transform.DORotate(new Vector3(0, 0, 360), timeAnimJump, RotateMode.WorldAxisAdd));
                    itemJumAnim.OnComplete(() =>
                    {
                        targetCreatureObj.gameObject.SetActive(false);
                        Destroy(targetCreatureObj);
                    });
                });
            }
        }
    }

    #endregion
}
