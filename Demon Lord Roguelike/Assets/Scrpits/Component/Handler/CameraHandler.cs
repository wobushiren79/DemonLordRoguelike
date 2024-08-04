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
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        manager.LoadMainCamera();
    }

    /// <summary>
    /// 设置核心UI
    /// </summary>
    public void SetBaseCoreCamera(int priority, bool isEnable)
    {
        SetCameraForBaseScene(priority, isEnable, "CV_Core");
    }

    /// <summary>
    /// 设置扭蛋机UI
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
            LogUtil.LogError("设置摄像头失败 没有找到对应场景");
            return;
        }
        var targetCVListTF = targetBaseScene.transform.Find($"CV_List");
        if (targetCVListTF == null)
        {
            LogUtil.LogError("设置摄像头失败 没有找到对应CV_List Transfrom");
            return;
        }
        //还原所有摄像头
        var cvList = targetCVListTF.GetComponentsInChildren<CinemachineVirtualCamera>(true);
        for (int i = 0; i < cvList.Length; i++)
        {
            var targetCV = cvList[i];
            if (targetCV.name.Equals($"{cvName}"))
            {
                //打开切换动画
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
    /// 设置基地场景摄像头
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
        //设置大小
        targetRenderer.transform.localScale = Vector3.one * creatureModel.size_spine;
        //设置骨骼数据
        SpineHandler.Instance.SetSkeletonDataAsset(targetSkeletonAnimation, creatureModel.res_name);
        string[] skinArray = fightCreatureData.creatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(targetSkeletonAnimation.skeleton, skinArray);

        controlTarget.transform.position = new Vector3(0, 0, -3);

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);

        ShowCinemachineCamera(CinemachineCameraEnum.Base);

        manager.cm_Base.Follow = controlTarget.transform;
        manager.cm_Base.LookAt = targetRenderer;
        manager.cm_Base.PreviousStateIsValid = false;
        await new WaitNextFrame();
        //设置偏转
        targetRenderer.transform.eulerAngles = mainCamera.transform.eulerAngles;

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

        //关闭切换动画
        manager.SetMainCameraDefaultBlend(0);

        ShowCinemachineCamera(CinemachineCameraEnum.Fight);

        manager.cm_Fight.Follow = controlTarget.transform;
        manager.cm_Fight.LookAt = controlTarget.transform;
        manager.cm_Fight.PreviousStateIsValid = false;
        await new WaitNextFrame();
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 设置卡片测试镜头
    /// </summary>
    public void SetCardTestCamera()
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);

        //关闭切换动画
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
