using Cinemachine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CameraHandler : BaseHandler<CameraHandler, CameraManager>
{
    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }

    /// <summary>
    /// ���û��س�������ͷ
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
        //���ù�������
        SpineHandler.Instance.SetSkeletonDataAsset(targetSkeletonAnimation, creatureModel.res_name);
        string[] skinArray = fightCreatureData.creatureData.GetSkinArray();
        //�޸�Ƥ��
        SpineHandler.Instance.ChangeSkeletonSkin(targetSkeletonAnimation.skeleton, skinArray);

        controlTarget.transform.position = new Vector3(0, 0, 3);

        ShowCinemachineCamera(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = targetRenderer;

        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// ����ս�������ӽ�
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
