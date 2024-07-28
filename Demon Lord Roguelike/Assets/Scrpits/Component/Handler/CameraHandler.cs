using Cinemachine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CameraHandler : BaseHandler<CameraHandler, CameraManager>
{
    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }

    /// <summary>
    /// 设置基地场景摄像头
    /// </summary>
    public async void SetBaseSceneCamera(Action actionForComplete,FightCreatureBean fightCreatureData)
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        var controlTarget = GameControlHandler.Instance.manager.controlTargetForCreature;
        var targetRenderer = controlTarget.transform.Find("Renderer");
        var targetSkeletonAnimation = targetRenderer.GetComponent<SkeletonAnimation>();

        var creatureInfo = fightCreatureData.GetCreatureInfo();
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        //设置骨骼数据
        SpineHandler.Instance.SetSkeletonDataAsset(targetSkeletonAnimation, creatureModel.res_name);
        string[] skinArray = fightCreatureData.creatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(targetSkeletonAnimation.skeleton, skinArray);

        controlTarget.transform.position = new Vector3(0, 0, 3);

        ShowCinemachineCamera(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = targetRenderer;

        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 设置战斗场景视角
    /// </summary>
    public async void SetFightSceneCamera(Action actionForComplete)
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        var controlTarget = GameControlHandler.Instance.manager.controlTargetForEmpty;
        controlTarget.transform.position = new Vector3(3, 0, 3);

        ShowCinemachineCamera(CinemachineCameraEnum.Fight);

        manager.cm_Fight.Follow = controlTarget.transform;
        manager.cm_Fight.LookAt = controlTarget.transform;

        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }

    public void ShowCinemachineCamera(CinemachineCameraEnum cinemachineCameraEnum)
    {
        manager.HideAllCM();
        switch (cinemachineCameraEnum)
        {
            case CinemachineCameraEnum.Base:
                manager.cm_Base.gameObject.SetActive(true);
                manager.cm_Base.Priority = int.MaxValue;
                break;
            case CinemachineCameraEnum.Fight:
                manager.cm_Fight.gameObject.SetActive(true);
                manager.cm_Fight.Priority = int.MaxValue;
                break;
        }
    }
}
