using System.Collections.Generic;
using Spine.Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

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
    //public void Awake()
    //{
    //    objBuildingCore = transform.Find("Core/Building").gameObject;
    //    effectEggBreak = transform.Find("Effect/EggBreak").GetComponent<VisualEffect>();
    //}
    public override void InitSceneData()
    {
        base.InitSceneData();
        BuildingVatRefresh();
        EventHandler.Instance.RegisterEvent<int>(EventsInfo.CreatureAscend_UpdateVat, EventForCreatureAscendUpdateVat);
    }

    public void OnDestroy()
    {
        EventHandler.Instance.UnRegisterEvent<int>(EventsInfo.CreatureAscend_UpdateVat, EventForCreatureAscendUpdateVat);
    }

    public void Update()
    {
        HandleUpdateForBuildingCore();
    }

    #region 核心建筑眼球处理
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

    #region Vat相关
    /// <summary>
    /// 事件-刷新数据
    /// </summary>
    /// <param name="targetIndex"></param>
    public void EventForCreatureAscendUpdateVat(int targetIndex)
    {

    }

    /// <summary>
    /// 刷新建筑容器
    /// </summary>
    public void BuildingVatRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        int showVatNum = 5;
        for (int i = 0; i < objBuildingVat.transform.childCount; i++)
        {
            var itemVat = objBuildingVat.transform.GetChild(i);
            if (i < showVatNum)
            {
                itemVat.gameObject.SetActive(true);
                var itemAscendDetails = userAscend.GetAscendData(i);
                if (itemAscendDetails != null)
                {
                    var creatureData = userData.GetBackpackCreature(itemAscendDetails.creatureId);
                    BuildingVatSetState(itemVat, 3, creatureData);
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
                if(targetIndex == -1)
                {
                    //如果VAT是空的。则直接关闭舱门
                    if(itemAscendDetails == null)
                    {
                        BuildingVatSetState(itemVat, 0, null);
                    }
                    continue;
                }
                //是展示的VAT
                if(i == targetIndex)
                {
                    //如果VAT是空的，则打开舱门
                    if(itemAscendDetails == null)
                    {
                        BuildingVatSetState(itemVat, 1, null);
                    }
                }
                //不是展示的VAT
                else
                {
                    //如果VAT是空的。则直接关闭舱门
                    if(itemAscendDetails == null)
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
    /// <param name="state">0关闭未设置生物 1打开未设置生物 2打开设置了生物 </param>
    public void BuildingVatSetState(Transform targetVat, int state, CreatureBean creatureData)
    {
        Animator vatAnim = targetVat.GetComponent<Animator>();
        vatAnim.SetInteger("State", 0);

        Transform tfWater = targetVat.Find("Water");
        tfWater.gameObject.SetActive(false);

        Transform tfCreature = targetVat.Find("Creature");
        tfCreature.gameObject.SetActive(false);

        SkeletonAnimation skeletonAnimation = tfCreature.GetComponent<SkeletonAnimation>();
        switch (state)
        {
            case 0:
                break;
            case 1:
                vatAnim.SetInteger("State", 1);
                break;
            case 2:
                vatAnim.SetInteger("State", 1);
                tfCreature.gameObject.SetActive(true);
                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData);
                break;
            case 3:
                tfWater.gameObject.SetActive(true);
                tfCreature.gameObject.SetActive(true);
                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData);
                break;
        }
    }
    #endregion
}
