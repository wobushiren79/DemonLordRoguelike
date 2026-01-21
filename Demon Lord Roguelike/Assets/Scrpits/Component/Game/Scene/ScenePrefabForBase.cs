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
    //终焉议会
    public GameObject obBuildingjDoomCouncil;
    //光线
    public Light lightSun;
    public GameObject lightRay;

    public Color vatColorStart = new Color(0, 0.4f, 1f, 0.4f);
    public Color vatColorEnd = new Color(0.11f, 0.06f, 0.5f, 0.7f);

    /// <summary>
    /// 初始化场景
    /// </summary>
    public override async Task InitSceneData()
    {
        await base.InitSceneData();
        EventHandler.Instance.RegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForCreatureAscendAddProgress);
        EventHandler.Instance.RegisterEvent<long>(EventsInfo.User_AddUnlock, EventForUserAddUnlock);

        objBuildingVat.gameObject.SetActive(false);
        objBuildingAltar.gameObject.SetActive(false);
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public override async Task RefreshScene()
    {
        await base.RefreshScene();
        BuildingVatRefresh();
        BuildingAltarRefresh();
        BuildingDoomCouncilRefresh();
    }

    /// <summary>
    /// 动画-建筑出现
    /// </summary>
    public async Task AnimForBuildingShow(float timeForShow)
    {
        var taskAltar = AnimForBuildingAltarShow(timeForShow);
        var taskVat = AnimForBuildingVatShow(timeForShow);
        var taskDoomCouncil = AnimForBuildingDoomCouncilShow(timeForShow);
        await new WaitForSeconds(timeForShow);
    }

    public void OnDestroy()
    {
        EventHandler.Instance.UnRegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForCreatureAscendAddProgress);
        EventHandler.Instance.UnRegisterEvent<long>(EventsInfo.User_AddUnlock, EventForUserAddUnlock);
    }

    public void Update()
    {
        HandleUpdateForBuildingCore();
    }

    #region 核心建筑
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

    /// <summary>
    /// 动画-核心建筑吐出
    /// </summary>
    public void AnimForBuildingCoreSpit(float timeAnim = 0.3f)
    {
        objBuildingCore.transform.localScale = Vector3.one;
        objBuildingCore.transform.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), timeAnim, 2, 0.5f);
    }
    #endregion

    #region 终焉议会
    public void BuildingDoomCouncilRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        bool isUnlockDoomCouncil = userUnlock.CheckIsUnlock(UnlockEnum.DoomCouncil);
        //是否解锁祭坛
        if (isUnlockDoomCouncil)
        {
            obBuildingjDoomCouncil.gameObject.SetActive(true);
        }
        else
        {
            obBuildingjDoomCouncil.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 动画-祭坛出现
    /// </summary>
    public async Task AnimForBuildingDoomCouncilShow(float timeForShow)
    {        
        if (!obBuildingjDoomCouncil.activeSelf)
        {
            return;
        }
        AnimForBuildingShowItem(obBuildingjDoomCouncil.transform, -1f, timeForShow);
        await new WaitForSeconds(timeForShow);
    }
    #endregion

    #region 献祭相关
    public void BuildingAltarRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        bool isUnlockAltar = userUnlock.CheckIsUnlock(UnlockEnum.Altar);
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

    /// <summary>
    /// 动画-祭坛出现
    /// </summary>
    public async Task AnimForBuildingAltarShow(float timeForShow)
    {
        if (!objBuildingAltar.activeSelf)
        {
            return;
        }
        for (int i = 1; i <= 4; i++)
        {
            //4个柱子从地下出现
            var targetItemTF = objBuildingAltar.transform.Find($"Altar_{i}");
            AnimForBuildingShowItem(targetItemTF, -1f, timeForShow);
        }
        await new WaitForSeconds(timeForShow);
    }
    #endregion

    #region Vat相关

    /// <summary>
    /// 刷新建筑容器
    /// </summary>
    /// <param name="refeshState">刷新状态 0：刷新所有 1只刷新有进度的</param>
    public void BuildingVatRefresh(int refeshState = 0)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        //需要展示的容器数量
        int showVatNum = userUnlock.GetUnlockCreatureVatNum();
        if (showVatNum > 0)
        {
            objBuildingVat.gameObject.SetActive(true);
        }
        else
        {
            objBuildingVat.gameObject.SetActive(false);
        }

        for (int i = 0; i < objBuildingVat.transform.childCount; i++)
        {
            var itemVat = objBuildingVat.transform.GetChild(i);
            if (i < showVatNum)
            {
                itemVat.gameObject.SetActive(true);
                var itemAscendDetails = userAscend.GetAscendData(i);
                //所有刷新
                if (refeshState == 0)
                {
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
                //只刷新有进度的
                else if (refeshState == 1)
                {
                    if (itemAscendDetails != null)
                    {
                        var creatureData = userData.GetBackpackCreature(itemAscendDetails.creatureUUId);
                        BuildingVatSetState(itemVat, 3, creatureData, itemAscendDetails.progress);
                    }
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
        Transform tfCreature = targetVat.Find("CreatureObj");
        SkeletonAnimation skeletonAnimation = tfCreature.GetComponentInChildren<SkeletonAnimation>();
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

                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData, isNeedEquip: false, isNeedWeapon: false);
                break;
            case 3:
                tfWater.gameObject.SetActive(true);
                tfCreature.gameObject.SetActive(true);
                vatAnim.SetInteger("State", 0);

                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData, isNeedEquip: false, isNeedWeapon: false);
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

        Transform tfCreature = targetVat.Find("CreatureObj");
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
                float delayShowTime = UnityEngine.Random.Range(0f, animTimeAddCreature * 0.1f);
                DOVirtual.DelayedCall(delayShowTime, () =>
                {
                    //设置生物样子      
                    SkeletonAnimation skeletonAnimation = targetCreatureObj.GetComponentInChildren<SkeletonAnimation>();
                    CreatureHandler.Instance.SetCreatureData(skeletonAnimation, itemCreatureData, isNeedEquip: false, isNeedWeapon: false);
                    targetCreatureObj.SetActive(true);
                    //设置生物位置
                    float randomX = UnityEngine.Random.Range(0, 2) == 0 ? -1f : 1f;
                    float randomY = UnityEngine.Random.Range(1f, 1.5f);
                    Vector3 startPosition = tfCreature.position + new Vector3(randomX, randomY, 0);
                    targetCreatureObj.transform.position = startPosition;
                    targetCreatureObj.transform.localScale = Vector3.one * 0.8f;
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
                    itemJumAnim.Append(targetCreatureObj.transform.DOJump(tfCreature.position + new Vector3(0, 0.5f, 0), 1.75f, 1, timeAnimJump));
                    itemJumAnim.Join(targetCreatureObj.transform.DOScale(Vector3.one * 0.2f, timeAnimJump));
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
    
    /// <summary>
    /// 动画-祭坛出现
    /// </summary>
    public async Task AnimForBuildingVatShow(float timeForShow,int targetVatIndex = -1)
    {
        if (!objBuildingVat.activeSelf)
        {
            return;
        }
        for (int i = 0; i < objBuildingVat.transform.childCount; i++)
        {
            var itemVat = objBuildingVat.transform.GetChild(i);
            if (itemVat.gameObject.activeSelf)
            {
                //如果没有指定的vat
                if (targetVatIndex > 0)
                {
                    if (targetVatIndex == i)
                    {
                        AnimForBuildingShowItem(itemVat, 0, timeForShow);
                    }
                }
                //如果有指定vat
                else
                {
                    AnimForBuildingShowItem(itemVat, -1.8f, timeForShow);
                }
            }
        }
        await new WaitForSeconds(timeForShow);
    }
    #endregion

    #region 回调
    public void EventForUserAddUnlock(long unlockId)
    {
        float timeForShow = 1;
        UnlockEnum unlockEnum = (UnlockEnum)unlockId;
        Task taskRefresh = null;
        Task taskAnimShow = null;
        switch (unlockEnum)
        {
            case UnlockEnum.Altar:
                taskRefresh = RefreshScene();
                taskAnimShow = AnimForBuildingAltarShow(timeForShow);
                break;
            case UnlockEnum.DoomCouncil:
                taskRefresh = RefreshScene();
                taskAnimShow = AnimForBuildingDoomCouncilShow(timeForShow);
                break;
            case UnlockEnum.CreatureVat:
            case UnlockEnum.CreatureVatAdd:
                taskRefresh = RefreshScene();
                UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                UserUnlockBean userUnlock = userData.GetUserUnlockData();
                int showVatNum = userUnlock.GetUnlockCreatureVatNum();
                int targetIndexVat = showVatNum - 1;
                taskAnimShow = AnimForBuildingVatShow(timeForShow, targetIndexVat);
                break;
        }
    }

    /// <summary>
    /// 事件-生物进阶进度变化
    /// </summary>
    public void EventForCreatureAscendAddProgress()
    {
        BuildingVatRefresh(1);
    }

    #endregion
    #region 其他
    public async void AnimForBuildingShowItem(Transform targetTF, float originY, float timeForShow)
    {       
        //播放粒子
        EffectBean effectData = new EffectBean();
        effectData.timeForShow = 3f;
        effectData.effectName = "EffectSmoke_1";
        effectData.effectPosition = targetTF.position + new Vector3(0, 0.2f, 0);
        effectData.isDestoryPlayEnd = true;
        //播放移动动画
        targetTF.position = targetTF.position.SetY(originY);
        float timeForShowReal = UnityEngine.Random.Range(timeForShow / 2f, timeForShow);
        float timeForDelayShow = timeForShow - timeForShowReal;
        targetTF.DOMoveY(0, timeForShowReal).SetDelay(timeForDelayShow);
        await new WaitForSeconds(timeForDelayShow);

        EffectHandler.Instance.ShowEffect(effectData);
    }
    #endregion
}
