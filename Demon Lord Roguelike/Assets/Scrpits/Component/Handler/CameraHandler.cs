using Cinemachine;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

public partial class CameraHandler
{
    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }

    /// <summary>
    /// ���ú���UI
    /// </summary>
    public bool SetBaseCoreCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_Core");
    }

    /// <summary>
    /// ����Ť����UI
    /// </summary>
    public bool SetGashaponMachineCamera(int priority, bool isEnable)
    {
        return SetCameraForBaseScene(priority, isEnable, "CV_GashaponMachine");
    }

    protected bool SetCameraForBaseScene(int priority, bool isEnable, string cvName)
    {
        var targetBaseScene = WorldHandler.Instance.currentBaseScene;
        if (targetBaseScene == null)
        {
            LogUtil.LogError("��������ͷʧ�� û���ҵ���Ӧ����");
            return false;
        }
        var targetCVTF = targetBaseScene.transform.Find($"CV_List/{cvName}");
        if (targetCVTF == null)
        {
            LogUtil.LogError("��������ͷʧ�� û���ҵ���ӦCV Transfrom");
            return false;
        }
        var targetCV = targetCVTF.GetComponent<CinemachineVirtualCamera>();
        if (targetCV == null)
        {
            LogUtil.LogError("��������ͷʧ�� û���ҵ���ӦCV CinemachineVirtualCamera");
            return false;
        }
        //���л�����
        manager.SetMainCameraDefaultBlend(0.5f);
        targetCV.gameObject.SetActive(isEnable);
        targetCV.Priority = priority;
        return true;
    }


    /// <summary>
    /// ���û��س�������ͷ
    /// </summary>
    public async void SetBaseSceneCamera(Action actionForComplete, FightCreatureBean fightCreatureData)
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

        controlTarget.transform.position = new Vector3(0, 0, -3);

        //�ر��л�����
        manager.SetMainCameraDefaultBlend(0);

        ShowCinemachineCamera(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = targetRenderer;
        manager.cm_Base.PreviousStateIsValid = false;
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

        //�ر��л�����
        manager.SetMainCameraDefaultBlend(0);

        ShowCinemachineCamera(CinemachineCameraEnum.Fight);

        manager.cm_Fight.Follow = controlTarget.transform;
        manager.cm_Fight.LookAt = controlTarget.transform;
        manager.cm_Fight.PreviousStateIsValid = false;
        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// ���ÿ�Ƭ���Ծ�ͷ
    /// </summary>
    public void SetCardTestCamera()
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        //�ر��л�����
        manager.SetMainCameraDefaultBlend(0);
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
