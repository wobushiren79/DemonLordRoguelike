using Unity.Cinemachine;
using Spine.Unity;
using System;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public partial class CameraHandler
{
    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }
    #region 终焉议会摄像头
    public CinemachineCamera SetCameraForDoomCouncilVote(float blendTime = 0.5f)
    {
        manager.HideAllCM();
        var targetBaseScene = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.DoomCouncil);
        if (targetBaseScene == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应场景");
            return null;
        }
        var targetCVListTF = targetBaseScene.transform.Find($"CV_List");
        if (targetCVListTF == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应CV_List Transfrom");
            return null;
        }
        var targetCV = targetCVListTF.GetComponentInChildren<CinemachineCamera>(true);
        //打开切换动画
        manager.SetMainCameraDefaultBlend(blendTime);
        targetCV.gameObject.SetActive(true);
        targetCV.Priority = int.MaxValue;
        return targetCV;
    }
    #endregion

    #region 奖励选择摄像头
    /// <summary>
    /// 设置奖励选择场景的摄像头
    /// </summary>
    public CinemachineCamera SetCameraForRewardSelectScene(float blendTime = 0.5f)
    {
        manager.HideAllCM();
        var targetCV = GetRewardSelectCamera();
        if (targetCV == null)
            return null;
        //打开切换动画
        manager.SetMainCameraDefaultBlend(blendTime);
        targetCV.gameObject.SetActive(true);
        targetCV.Priority = int.MaxValue;
        return targetCV;
    }

    /// <summary>
    /// 获取奖励选择场景的摄像头(仅查找返回,不改激活态/优先级)
    /// </summary>
    protected CinemachineCamera GetRewardSelectCamera()
    {
        var targetBaseScene = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.RewardSelect);
        if (targetBaseScene == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应场景");
            return null;
        }
        var targetCVListTF = targetBaseScene.transform.Find($"CV_List");
        if (targetCVListTF == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应CV_List Transfrom");
            return null;
        }
        return targetCVListTF.GetComponentInChildren<CinemachineCamera>(true);
    }

    /// <summary>
    /// 根据宝箱横向总宽度刷新奖励选择镜头的FOV:宝箱数量多(>=6)时两侧宝箱超出默认视野,按几何关系增大垂直FOV让所有宝箱完整入镜
    /// </summary>
    /// <param name="halfWidthForBox">宝箱排横向半宽(最外侧宝箱中心到排中心距离 + 单箱半宽与边距余量)</param>
    public void RefreshRewardSelectCameraFov(float halfWidthForBox)
    {
        var targetCV = GetRewardSelectCamera();
        if (targetCV == null)
            return;
        //宝箱排在 z=0 一行且相机无偏航角,横向入镜只取决于相机到箱排的 z 向距离与屏幕宽高比
        float distZ = Mathf.Abs(targetCV.transform.position.z);
        if (distZ < 0.01f)
            return;
        //当前配置FOV为基准(预制体配置值),只在不够装时才放大
        float fovDefault = targetCV.Lens.FieldOfView;
        float aspect = manager.mainCamera.aspect;
        //tan(横向半视角)=半宽/距离,再由 tan(hFov/2)=tan(vFov/2)*aspect 反推所需垂直FOV
        float fovNeed = 2f * Mathf.Atan(halfWidthForBox / distZ / aspect) * Mathf.Rad2Deg;
        if (fovNeed > fovDefault)
        {
            targetCV.Lens.FieldOfView = fovNeed;
        }
    }
    #endregion


    #region 战斗场景摄像头

    /// <summary>
    /// 初始化战斗场景视角
    /// </summary>
    public async Task InitFightSceneCamera()
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        var controlTarget = GameControlHandler.Instance.manager.controlTargetForEmpty;
        controlTarget.transform.position = new Vector3(3, 0, 3);

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);

        SetCameraForControl(CinemachineCameraEnum.Fight);

        manager.cm_Fight.Follow = controlTarget.transform;
        manager.cm_Fight.LookAt = controlTarget.transform;
        manager.cm_Fight.PreviousStateIsValid = false;
        await new WaitNextFrame();
    }
    #endregion

    #region  基地场景摄像头相关
    /// <summary>
    /// 初始化基地场景摄像头
    /// </summary>
    public async Task InitBaseSceneControlCamera(CreatureBean creatureData, Vector3 startPosition)
    {
        HideCameraForBaseScene();

        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);
        //设置控制数据
        var controlForGame = GameControlHandler.Instance.manager.controlForGameBase;
        //设置生物显示
        controlForGame.SetCreatureData(creatureData);
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        //初始化位置
        GameControlHandler.Instance.manager.controlTargetForCreature.transform.position = startPosition; 

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);

        SetCameraForControl(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = controlTarget.transform;;
        manager.cm_Base.PreviousStateIsValid = false;
        await new WaitNextFrame();
        //设置偏转
        ChangeAngleForCamera(controlForGame.skeletonAnimation.transform);
    }

    /// <summary>
    /// 设置核心UI
    /// </summary>
    public CinemachineCamera SetBaseCoreCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Core");
    }

    /// <summary>
    /// 设置传送门
    /// </summary>
    public CinemachineCamera SetBasePortalCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Portal", blendTime: 0);
    }

    /// <summary>
    /// 设置成就摄像头
    /// </summary>
    public CinemachineCamera SetAchievementCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Achievement");
    }

    /// <summary>
    /// 设置生物献祭摄像头
    /// </summary>
    public CinemachineCamera SetCreatureSacrificeCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_CreatureSacrifice");
    }

    /// <summary>
    /// 设置生物容器摄像头
    /// </summary>
    public CinemachineCamera SetCreatureVatCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_CreatureVat");
    }

    /// <summary>
    /// 设置扭蛋机摄像头
    /// </summary>
    public CinemachineCamera SetGashaponMachineCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_GashaponMachine");
    }

    /// <summary>
    /// 设置魔汁机摄像头(CV_Juicer,固定机位:打开 UICreatureJuicer 时对准魔汁机建筑)
    /// </summary>
    public CinemachineCamera SetJuicerCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Juicer");
    }

    /// <summary>
    /// 设置扭蛋破碎摄像头
    /// </summary>
    public CinemachineCamera SetGashaponBreakCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_GashaponBreak");
    }

    /// <summary>
    /// 设置游戏开始摄像头
    /// </summary>
    public CinemachineCamera SetGameStartCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_GameStart");
    }

    /// <summary>
    /// 设置创建
    /// </summary>
    public CinemachineCamera SetPreviewCreateCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_PreviewCreate");
    }

    /// <summary>
    /// 设置自定义摄像头
    /// </summary>
    public CinemachineCamera SetCustomCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Custom");
    }

    /// <summary>
    /// 设置基础场景的摄像头
    /// </summary>
    protected CinemachineCamera SetCameraForBaseScene(int priority, bool isEnable, string cvName, float blendTime = 0.5f)
    {
        manager.HideAllCM();
        var targetBaseScene = WorldHandler.Instance.GetCurrentScene();
        if (targetBaseScene == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应场景");
            return null;
        }
        var targetCVListTF = targetBaseScene.transform.Find($"CV_List");
        if (targetCVListTF == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应CV_List Transfrom");
            return null;
        }
        //还原所有摄像头
        var cvList = targetCVListTF.GetComponentsInChildren<CinemachineCamera>(true);
        CinemachineCamera targetCV = null;
        for (int i = 0; i < cvList.Length; i++)
        {
            var targetCVItem = cvList[i];
            if (targetCVItem.name.Equals($"{cvName}"))
            {
                //打开切换动画
                manager.SetMainCameraDefaultBlend(blendTime);
                targetCVItem.gameObject.SetActive(isEnable);
                targetCVItem.Priority = priority;
                targetCV = targetCVItem;
            }
            else
            {
                targetCVItem.gameObject.SetActive(false);
                targetCVItem.Priority = 0;
            }
        }
        return targetCV;
    }

    /// <summary>
    /// 隐藏所有场景摄像头
    /// </summary>
    protected void HideCameraForBaseScene()
    {
        SetCameraForBaseScene(int.MinValue, false, "");
    }
    #endregion

    #region 魔汁机镜头聚焦/震动
    //魔汁机镜头是否已聚焦滴嘴(还原以此为凭,未聚焦时还原是空操作)
    protected bool isJuicerCameraFocused = false;
    //魔汁机镜头聚焦滴嘴前的原始状态缓存(还原用)
    protected Transform juicerCameraOriginalFollow;
    protected Transform juicerCameraOriginalLookAt;
    protected Vector3 juicerCameraOriginalFollowOffset;
    protected Vector3 juicerCameraOriginalComposerOffset;
    //魔汁机镜头 Perlin 原始振幅(首次震动时缓存,-1=未缓存)
    protected float juicerCameraOriginalAmplitude = -1;

    /// <summary>
    /// 获取基地场景指定镜头(仅查找返回,不改激活态/优先级;供魔汁机等流程对镜头做聚焦/震动)
    /// </summary>
    /// <param name="cvName">CV_List 下的镜头节点名</param>
    public CinemachineCamera GetBaseSceneCamera(string cvName)
    {
        var targetBaseScene = WorldHandler.Instance.GetCurrentScene();
        if (targetBaseScene == null)
            return null;
        var targetCVListTF = targetBaseScene.transform.Find("CV_List");
        if (targetCVListTF == null)
            return null;
        var cvList = targetCVListTF.GetComponentsInChildren<CinemachineCamera>(true);
        for (int i = 0; i < cvList.Length; i++)
        {
            if (cvList[i].name.Equals(cvName))
                return cvList[i];
        }
        return null;
    }

    /// <summary>
    /// 魔汁机镜头聚焦滴嘴:跟随/看向目标切到滴嘴并推近特写(缓存原状态,流程结束后用 RestoreJuicerCameraFocus 还原)
    /// </summary>
    /// <param name="targetHole">滴嘴节点</param>
    public void FocusJuicerCameraOnHole(Transform targetHole)
    {
        var targetCV = GetBaseSceneCamera("CV_Juicer");
        if (targetCV == null || targetHole == null)
            return;
        //缓存原始跟随/看向目标与组件偏移(还原用)
        juicerCameraOriginalFollow = targetCV.Follow;
        juicerCameraOriginalLookAt = targetCV.LookAt;
        var follow = targetCV.GetComponent<CinemachineFollow>();
        var composer = targetCV.GetComponent<CinemachineRotationComposer>();
        if (follow != null)
            juicerCameraOriginalFollowOffset = follow.FollowOffset;
        if (composer != null)
            juicerCameraOriginalComposerOffset = composer.TargetOffset;
        isJuicerCameraFocused = true;
        //跟随/看向切到滴嘴,瞄准偏移清零(正对滴嘴)
        targetCV.Follow = targetHole;
        targetCV.LookAt = targetHole;
        if (composer != null)
            composer.TargetOffset = Vector3.zero;
        //推近滴嘴(精华滴落的特写镜头,与滴嘴同高平视)
        if (follow != null)
        {
            DOTween.To(() => follow.FollowOffset, v => follow.FollowOffset = v, new Vector3(0, 0, -1.5f), 0.8f).SetTarget(follow);
        }
    }

    /// <summary>
    /// 还原魔汁机镜头焦点:跟随/看向/偏移恢复聚焦滴嘴前的状态(未聚焦过则空操作)
    /// </summary>
    public void RestoreJuicerCameraFocus()
    {
        if (!isJuicerCameraFocused)
            return;
        isJuicerCameraFocused = false;
        var targetCV = GetBaseSceneCamera("CV_Juicer");
        if (targetCV == null)
            return;
        if (juicerCameraOriginalFollow != null)
            targetCV.Follow = juicerCameraOriginalFollow;
        if (juicerCameraOriginalLookAt != null)
            targetCV.LookAt = juicerCameraOriginalLookAt;
        var follow = targetCV.GetComponent<CinemachineFollow>();
        var composer = targetCV.GetComponent<CinemachineRotationComposer>();
        if (composer != null)
            composer.TargetOffset = juicerCameraOriginalComposerOffset;
        //拉回原来的固定机位
        if (follow != null)
        {
            DOTween.To(() => follow.FollowOffset, v => follow.FollowOffset = v, juicerCameraOriginalFollowOffset, 0.5f).SetTarget(follow);
        }
    }

    /// <summary>
    /// 魔汁机镜头震动(锤子砸落冲击):瞬时抬升 CV_Juicer 自带 Perlin 振幅后回落
    /// </summary>
    /// <param name="amplitude">震动振幅</param>
    /// <param name="timeForShake">振幅回落时长</param>
    public void ShakeJuicerCamera(float amplitude = 0.8f, float timeForShake = 0.35f)
    {
        var targetCV = GetBaseSceneCamera("CV_Juicer");
        if (targetCV == null)
            return;
        var perlin = targetCV.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (perlin == null)
            return;
        //首次震动时缓存原始振幅(回落目标)
        if (juicerCameraOriginalAmplitude < 0)
            juicerCameraOriginalAmplitude = perlin.AmplitudeGain;
        perlin.DOKill();
        perlin.AmplitudeGain = amplitude;
        DOTween.To(() => perlin.AmplitudeGain, v => perlin.AmplitudeGain = v, juicerCameraOriginalAmplitude, timeForShake).SetTarget(perlin);
    }
    #endregion

    #region  控制操作摄像头
    /// <summary>
    /// 设置控制摄像头(故事演出期间直接忽略:镜头由 StoryHandler 专用相机接管,外部切镜会以 blend=0 瞬切抢走 CinemachineBrain,导致演出镜头移动全不可见;演出结束由 StoryHandler.EndStoryCamera 自行归还停靠相机)
    /// </summary>
    public void SetCameraForControl(CinemachineCameraEnum cinemachineCameraEnum)
    {
        //演出期锁镜头:如新手引导末步 UIHandle 打开 UIBaseMain(OpenUI 内调本方法),会把故事相机顶掉造成镜头瞬切跳回
        if (StoryHandler.Instance.manager.isStoryPlaying)
            return;
        manager.HideAllCM();
        switch (cinemachineCameraEnum)
        {
            case CinemachineCameraEnum.Base:
                SetCameraForControlBase();
                break;
            case CinemachineCameraEnum.Fight:
                SetCameraForControlFight();
                break;
        }
    }

    protected void SetCameraForControlBase()
    {
        HideCameraForBaseScene();
        manager.cm_Base.gameObject.SetActive(true);
        manager.cm_Base.Priority = int.MaxValue;
        var currentScene = WorldHandler.Instance.GetCurrentScene();
        if (currentScene == null)
        {
            return;
        }
        var scenePrefabBase = currentScene.GetComponent<ScenePrefabBase>();
        if (scenePrefabBase == null)
        {
            return;
        }
        if (scenePrefabBase is ScenePrefabForBase scenePrefabForBase)
        {
            manager.cm_Base.Lens.FieldOfView = 55;
        }
        else if (scenePrefabBase is ScenePrefabForDoomCouncil scenePrefabForDoomCouncil)
        {
            manager.cm_Base.Lens.FieldOfView = 50;
        }
    }
    
    protected void SetCameraForControlFight()
    {
        manager.cm_Fight.gameObject.SetActive(true);
        manager.cm_Fight.Priority = int.MaxValue;
        //透明排序无需在此设置: 唯一入口 SetCameraForControl 先经 HideAllCM, 其内已按当前场景统一刷新(见 CameraManager.RefreshTransparencySortForCurrentScene)
    }
    #endregion
}
