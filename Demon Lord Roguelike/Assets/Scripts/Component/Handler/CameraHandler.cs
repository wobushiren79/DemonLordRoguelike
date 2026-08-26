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
    /// 设置基础场景的摄像头
    /// </summary>
    public CinemachineCamera SetCameraForRewardSelectScene(float blendTime = 0.5f)
    {
        manager.HideAllCM();
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
        var targetCV = targetCVListTF.GetComponentInChildren<CinemachineCamera>(true);
        //打开切换动画
        manager.SetMainCameraDefaultBlend(blendTime);
        targetCV.gameObject.SetActive(true);
        targetCV.Priority = int.MaxValue;
        return targetCV;
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

    #region 故事演出镜头
    //故事演出接管前 cm_Base 的跟随/看向目标缓存(基地=魔王本体;战斗的 cm_Fight 本就跟随自由目标,无需缓存)
    protected Transform storyCameraOriginalFollow;
    protected Transform storyCameraOriginalLookAt;

    /// <summary>
    /// 故事演出开始接管镜头:返回演出期可自由补间的移动目标(战斗=controlTargetForEmpty;基地=cm_Base 跟随从魔王临时切到 controlTargetForEmpty,位置先同步到魔王位保证镜头连续)
    /// </summary>
    /// <param name="isFightScene">当前是否战斗场景</param>
    public Transform BeginStoryCameraControl(bool isFightScene)
    {
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForEmpty;
        if (!isFightScene)
        {
            //基地:cm_Base 跟随魔王本体(controlTargetForCreature),演出改跟随自由目标,避免移动魔王
            storyCameraOriginalFollow = manager.cm_Base.Follow;
            storyCameraOriginalLookAt = manager.cm_Base.LookAt;
            var creatureTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
            if (creatureTarget != null)
                controlTarget.transform.position = creatureTarget.transform.position;
            manager.cm_Base.Follow = controlTarget.transform;
            manager.cm_Base.LookAt = controlTarget.transform;
        }
        return controlTarget.transform;
    }

    /// <summary>
    /// 故事演出镜头移动:补间演出跟随目标到指定位置(unscaled,战斗演出 timeScale=0 下照常;先打断在途移动防并发步骤补间叠加)
    /// </summary>
    /// <param name="targetPos">目标世界坐标</param>
    /// <param name="duration">时长秒</param>
    /// <param name="easeIndex">缓动序号(0=走DOTween默认缓动,其余按 DG.Tweening.Ease 强转)</param>
    public async UniTask MoveStoryCameraTarget(Vector3 targetPos, float duration, int easeIndex)
    {
        var controlTarget = GameControlHandler.Instance.manager.controlTargetForEmpty;
        controlTarget.transform.DOKill();
        var tween = controlTarget.transform.DOMove(targetPos, duration).SetUpdate(true);
        if (easeIndex > 0 && System.Enum.IsDefined(typeof(Ease), easeIndex))
            tween.SetEase((Ease)easeIndex);
        await GTask.WaitTween(tween, null);
    }

    /// <summary>
    /// 故事演出结束归还镜头:跟随目标补间回演出起始位后,基地恢复 cm_Base 跟随魔王本体
    /// </summary>
    /// <param name="originPos">演出起始位(BeginStoryCameraControl 接管时记录)</param>
    /// <param name="duration">回位时长秒</param>
    public async UniTask EndStoryCameraControl(Vector3 originPos, float duration)
    {
        await MoveStoryCameraTarget(originPos, duration, 0);
        if (storyCameraOriginalFollow != null)
        {
            manager.cm_Base.Follow = storyCameraOriginalFollow;
            manager.cm_Base.LookAt = storyCameraOriginalLookAt != null ? storyCameraOriginalLookAt : storyCameraOriginalFollow;
            storyCameraOriginalFollow = null;
            storyCameraOriginalLookAt = null;
        }
    }
    #endregion


    #region  卡片测试摄像头
    /// <summary>
    /// 设置卡片测试镜头
    /// </summary>
    public void SetCardTestCamera()
    {
        manager.HideAllCM();

        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);
    }
    #endregion

    #region  控制操作摄像头
    /// <summary>
    /// 设置控制摄像头
    /// </summary>
    public void SetCameraForControl(CinemachineCameraEnum cinemachineCameraEnum)
    {
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
        //战斗镜头启用时设置自定义Z轴透明排序(仅在战斗场景生效, HideAllCM会还原)
        manager.SetTransparencySortForFight();
    }
    #endregion
}
