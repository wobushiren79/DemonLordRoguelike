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
    public void SetBaseCoreCamera(int priority, bool isEnable)
    {
        SetCameraForBaseScene(priority, isEnable, "CV_Core");
    }

    /// <summary>
    /// ����Ť����UI
    /// </summary>
    public void SetGashaponMachineCamera(int priority, bool isEnable)
    {
        SetCameraForBaseScene(priority, isEnable, "CV_GashaponMachine");
    }

    protected void SetCameraForBaseScene(int priority, bool isEnable, string cvName)
    {
        manager.HideAllCM();
        var targetBaseScene = WorldHandler.Instance.currentBaseScene;
        if (targetBaseScene == null)
        {
            LogUtil.LogError("��������ͷʧ�� û���ҵ���Ӧ����");
            return;
        }
        var targetCVListTF = targetBaseScene.transform.Find($"CV_List");
        if (targetCVListTF == null)
        {
            LogUtil.LogError("��������ͷʧ�� û���ҵ���ӦCV_List Transfrom");
            return;
        }
        //��ԭ��������ͷ
        var cvList = targetCVListTF.GetComponentsInChildren<CinemachineVirtualCamera>(true);
        for (int i = 0; i < cvList.Length; i++)
        {
            var targetCV = cvList[i];
            if (targetCV.name.Equals($"{cvName}"))
            {
                //���л�����
                manager.SetMainCameraDefaultBlend(0.5f);
                targetCV.gameObject.SetActive(isEnable);
                targetCV.Priority = priority;
            }
            else
            {
                targetCV.gameObject.SetActive(false);
                targetCV.Priority = 0;
            }
        }
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
        //���ô�С
        targetRenderer.transform.localScale = Vector3.one * creatureModel.size_spine;
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
        //����ƫת
        targetRenderer.transform.eulerAngles = mainCamera.transform.eulerAngles;

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
