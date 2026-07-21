using System;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject objBuildingjDoomCouncil;
    //成就石碑
    public GameObject objBuildingAchievement;
    //魔汁机
    public GameObject objBuildingJuicer;
    //光线
    public Light lightSun;
    public GameObject lightRay;
    //移动边界(空物体,用 localScale 的 x/z 表达 BOX 长/宽,角色移动不可超出;取代旧的写死圆形半径)
    public GameObject objBoard;

    public Color vatColorStart = new Color(0, 0.4f, 1f, 0.4f);
    public Color vatColorEnd = new Color(0.11f, 0.06f, 0.5f, 0.7f);

    /// <summary>
    /// 所有走"出现动画"的建筑对象登记表(不含常驻的核心建筑);新增建筑只需在此登记一处,音效/整场景出现判断据此,无需再改散落条件
    /// </summary>
    private GameObject[] AllBuildingShowObjs => new[] { objBuildingAltar, objBuildingVat, objBuildingjDoomCouncil, objBuildingAchievement, objBuildingJuicer };

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
        BuildingAchievementRefresh();
        BuildingJuicerRefresh();
    }

    /// <summary>
    /// 动画-建筑出现
    /// </summary>
    public async Task AnimForBuildingShow(float timeForShow)
    {
        //有任意建筑会出现时才播放建造音效(全新无建筑存档则不播),与 EventForUserAddUnlock 同口径:总时长跟随出现动画,末段0.5秒渐弱收尾
        bool hasBuildingShow = AllBuildingShowObjs.Any(obj => obj != null && obj.activeSelf);
        if (hasBuildingShow)
        {
            AudioHandler.Instance.PlaySoundTimedFade(AudioEnum.sound_building_2, timeForShow, Mathf.Max(0f, timeForShow - 0.5f), -1f);
        }
        var taskAltar = AnimForBuildingAltarShow(timeForShow);
        var taskVat = AnimForBuildingVatShow(timeForShow);
        var taskDoomCouncil = AnimForBuildingDoomCouncilShow(timeForShow);
        var taskAchievement = AnimForBuildingAchievementShow(timeForShow);
        var taskJuicer = AnimForBuildingJuicerShow(timeForShow);
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
            objBuildingjDoomCouncil.gameObject.SetActive(true);
        }
        else
        {
            objBuildingjDoomCouncil.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// 动画-祭坛出现
    /// </summary>
    public async Task AnimForBuildingDoomCouncilShow(float timeForShow)
    {
        if (!objBuildingjDoomCouncil.activeSelf)
        {
            return;
        }
        AnimForBuildingShowItem(objBuildingjDoomCouncil.transform, -1f, timeForShow);
        await new WaitForSeconds(timeForShow);
    }
    #endregion

    #region 成就石碑
    /// <summary>
    /// 刷新成就石碑
    /// </summary>
    public void BuildingAchievementRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        bool isUnlockAchievement = userUnlock.CheckIsUnlock(UnlockEnum.Achievement);
        //是否解锁成就石碑
        if (isUnlockAchievement)
        {
            objBuildingAchievement.gameObject.SetActive(true);
        }
        else
        {
            objBuildingAchievement.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 动画-成就石碑出现
    /// </summary>
    public async Task AnimForBuildingAchievementShow(float timeForShow)
    {
        if (!objBuildingAchievement.activeSelf)
        {
            return;
        }
        AnimForBuildingShowItem(objBuildingAchievement.transform, -1f, timeForShow);
        await new WaitForSeconds(timeForShow);
    }
    #endregion

    #region 魔汁机
    /// <summary>
    /// 刷新魔汁机:按魔汁机研究解锁(UnlockEnum.Juicer)显隐建筑;建筑上的交互碰撞体(命名 JuicerInteraction)随之启用/关闭
    /// </summary>
    public void BuildingJuicerRefresh()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        bool isUnlockJuicer = userUnlock.CheckIsUnlock(UnlockEnum.Juicer);
        //是否解锁魔汁机
        if (isUnlockJuicer)
        {
            objBuildingJuicer.gameObject.SetActive(true);
        }
        else
        {
            objBuildingJuicer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 动画-魔汁机出现
    /// </summary>
    public async Task AnimForBuildingJuicerShow(float timeForShow)
    {
        if (!objBuildingJuicer.activeSelf)
        {
            return;
        }
        AnimForBuildingShowItem(objBuildingJuicer.transform, -1f, timeForShow);
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
                        BuildingVatRefreshItemWithProgress(itemVat, itemAscendDetails);
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
                        BuildingVatRefreshItemWithProgress(itemVat, itemAscendDetails);
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
    /// 刷新单个有进阶数据的容器:取托管在进阶数据里的目标生物本体,按归一化进度(水色)设为进阶中状态(state=3)。
    /// </summary>
    /// <param name="itemVat">容器Transform</param>
    /// <param name="itemAscendDetails">该容器的进阶详情数据</param>
    private void BuildingVatRefreshItemWithProgress(Transform itemVat, UserAscendDetailsBean itemAscendDetails)
    {
        //目标生物本体托管在进阶数据里(无托管数据=托管前的旧存档,按空态展示兜底防NRE)
        var creatureData = itemAscendDetails.creatureData;
        if (creatureData == null)
        {
            BuildingVatSetState(itemVat, 0, null);
            return;
        }
        //进度已是秒数,水色按归一化(0~1)Lerp
        BuildingVatSetState(itemVat, 3, creatureData, itemAscendDetails.GetProgressNormalized());
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
                //未进阶:隐藏进度条
                BuildingVatSetProgress(targetVat, false);
                break;
            case 1:
                tfWater.gameObject.SetActive(false);
                tfCreature.gameObject.SetActive(false);
                vatAnim.SetInteger("State", 1);
                //未进阶:隐藏进度条
                BuildingVatSetProgress(targetVat, false);
                break;
            case 2:
                tfWater.gameObject.SetActive(false);
                tfCreature.gameObject.SetActive(true);
                vatAnim.SetInteger("State", 1);

                CreatureHandler.Instance.SetCreatureData(skeletonAnimation, creatureData, isNeedEquip: false, isNeedWeapon: false);
                //已放入生物但未开始进阶:隐藏进度条
                BuildingVatSetProgress(targetVat, false);
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
                //进阶中:显示进度条并按归一化进度刷新填充量与分段配色
                BuildingVatSetProgress(targetVat, true, progress);
                break;
        }
    }

    /// <summary>
    /// 设置容器进阶进度条(Progress)的显隐与数值:仅进阶中(state=3)显示,
    /// 填充量(_Progress)=归一化进度,进度颜色(_FillColor)复用 ColorUtil.GetProgressColor
    /// 分段配色(与孵化缸UI进度条 UICreatureVat.RefreshVatProgress 同口径)。
    /// </summary>
    /// <param name="targetVat">容器Transform</param>
    /// <param name="isShow">是否显示进度条(仅进阶中显示)</param>
    /// <param name="progress">归一化进度(0~1),-1 或越界会被 Clamp01 兜底</param>
    private void BuildingVatSetProgress(Transform targetVat, bool isShow, float progress = 0)
    {
        Transform tfProgress = targetVat.Find("Progress");
        if (tfProgress == null)
        {
            return;
        }
        tfProgress.gameObject.SetActive(isShow);
        if (!isShow)
        {
            return;
        }
        MeshRenderer renderProgress = tfProgress.GetComponent<MeshRenderer>();
        float progressClamp = Mathf.Clamp01(progress);
        renderProgress.material.SetFloat("_Progress", progressClamp);
        renderProgress.material.SetColor("_FillColor", ColorUtil.GetProgressColor(progressClamp));
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
        //随水位一起显示进阶进度条,起始进度0(后续由被动tick刷新)
        BuildingVatSetProgress(targetVat, true, 0);
        //水位上升动画
        renderWater.material.DOFloat(1, "_WaterLevel", animTimeWater).OnComplete(() =>
        {
            actionForComplete?.Invoke();
        });
        //水位上升音效:长音效按水位动画时长截断播放,末段0.5s线性淡出收尾
        AudioHandler.Instance.PlaySoundTimedFade(AudioEnum.sound_water_1, animTimeWater, animTimeWater - 0.5f, -1f);
        //生物添加完毕之后关闭盖子
        DOVirtual.DelayedCall(animTimeAddCreature, () =>
        {
            vatAnim.SetInteger("State", 0);
            AudioHandler.Instance.PlaySound(AudioEnum.sound_door_2);
        });
        //材料生物生成
        if (!listMaterialCreatureData.IsNull())
        {
            for (int i = 0; i < listMaterialCreatureData.Count; i++)
            {
                var itemCreatureData = listMaterialCreatureData[i];
                var targetCreatureObj = Instantiate(gameObject, objVatMaterialCreature);
                targetCreatureObj.SetActive(false);
                //按序号0.13s级联错开+微抖动:保证各素材落水间隔>0.1s(避开同音效防抖,每只落水声都能播),且最多10只时最后一只也在盖盖(2s)前落水
                float delayShowTime = i * 0.13f + UnityEngine.Random.Range(0f, 0.02f);
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
                        //落水瞬间播水声(每只素材各播一次)
                        AudioHandler.Instance.PlaySound(AudioEnum.sound_water_3);
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
                //指定了具体vat(targetVatIndex>=0，含第0个)：只让该vat从地下(-1.8)升起
                if (targetVatIndex >= 0)
                {
                    if (targetVatIndex == i)
                    {
                        AnimForBuildingShowItem(itemVat, -1.8f, timeForShow);
                    }
                }
                //未指定(targetVatIndex<0，整场景出现)：所有已激活vat一起从地下升起
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
    /// <summary>
    /// 判断该解锁是否会触发建筑(设施)出现动画
    /// 与 EventForUserAddUnlock 的 switch 分支保持一致，是「设施解锁」的唯一判定来源
    /// (研究界面据此决定解锁后是否延迟切设施镜头，让粒子先展示)
    /// </summary>
    public static bool IsBuildingShowUnlock(long unlockId)
    {
        UnlockEnum unlockEnum = (UnlockEnum)unlockId;
        switch (unlockEnum)
        {
            case UnlockEnum.Altar:
            case UnlockEnum.DoomCouncil:
            case UnlockEnum.Achievement:
            case UnlockEnum.Juicer:
            case UnlockEnum.CreatureVat:
            case UnlockEnum.CreatureVatAdd:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 事件-新增解锁
    /// 触发对应建筑的出现动画；若当前停留在研究界面，则先锁屏并切到自定义镜头、隐藏研究UI，
    /// 待出现动画播完(含停留1秒)后再还原核心镜头、恢复研究UI显示并解除锁屏，
    /// 全程锁屏防止出现动画期间操作
    /// </summary>
    public async void EventForUserAddUnlock(long unlockId)
    {
        float timeForShow = 2;
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
            case UnlockEnum.Achievement:
                taskRefresh = RefreshScene();
                taskAnimShow = AnimForBuildingAchievementShow(timeForShow);
                break;
            case UnlockEnum.Juicer:
                taskRefresh = RefreshScene();
                taskAnimShow = AnimForBuildingJuicerShow(timeForShow);
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
        //没有对应的出现动画 直接结束
        if (taskAnimShow == null)
        {
            return;
        }

        //建筑出现动画期间播放建造音效：总时长跟随出现动画(timeForShow)，末段0.5秒渐弱收尾
        AudioHandler.Instance.PlaySoundTimedFade(AudioEnum.sound_building_2, timeForShow, Mathf.Max(0f, timeForShow - 0.5f), -1f);

        //仅当解锁发生在研究界面时，才切到自定义镜头并隐藏研究UI观看出现动画
        bool isResearchShow = UIHandler.Instance.GetOpenUIName() == nameof(UIBaseResearch);
        UIBaseResearch researchUI = isResearchShow ? UIHandler.Instance.GetUI<UIBaseResearch>() : null;
        if (researchUI != null)
        {
            //设施出现动画期间全程锁屏，防止动画播放中操作
            UIHandler.Instance.ShowScreenLock();
            //启用自定义镜头
            CameraHandler.Instance.SetCustomCamera(int.MaxValue, true);
            //关闭(隐藏)研究UI
            researchUI.gameObject.SetActive(false);
        }

        //等待场景刷新与出现动画播放完成
        if (taskRefresh != null)
        {
            await taskRefresh;
        }
        await taskAnimShow;

        //动画播完后多停留1秒再还原
        await new WaitForSeconds(1);

        if (researchUI != null)
        {
            //还原核心镜头
            CameraHandler.Instance.SetBaseCoreCamera(int.MaxValue, true);
            //重新打开(显示)研究UI
            researchUI.gameObject.SetActive(true);
            //出现动画结束，解除锁屏
            UIHandler.Instance.HideScreenLock();
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
