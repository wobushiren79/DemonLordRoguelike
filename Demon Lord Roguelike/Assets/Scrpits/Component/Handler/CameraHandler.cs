using Unity.Cinemachine;
using Spine.Unity;
using System;
using UnityEngine;
using System.Threading.Tasks;

public partial class CameraHandler
{
    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }

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

        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        var targetRenderer = controlTarget.transform.Find("Renderer");

        var targetSkeletonAnimation = targetRenderer.GetComponent<SkeletonAnimation>();

        //展示生物数据
        CreatureHandler.Instance.SetCreatureData(targetSkeletonAnimation, creatureData, isNeedWeapon : false);
        //初始化位置
        controlTarget.transform.position = startPosition; 

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);

        SetCameraForControl(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = targetRenderer;
        manager.cm_Base.PreviousStateIsValid = false;
        await new WaitNextFrame();
        //设置偏转
        ChangeAngleForCamera(targetRenderer.transform);
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
                HideCameraForBaseScene();
                manager.cm_Base.gameObject.SetActive(true);
                manager.cm_Base.Priority = int.MaxValue;
                break;
            case CinemachineCameraEnum.Fight:
                manager.cm_Fight.gameObject.SetActive(true);
                manager.cm_Fight.Priority = int.MaxValue;
                break;
        }
    }
    #endregion
}
