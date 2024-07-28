using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraManager
{
    public CinemachineVirtualCamera cm_Fight;
    public CinemachineVirtualCamera cm_Base;

    /// <summary>
    /// 加载主摄像头
    /// </summary>
    public void LoadMainCamera()
    {       
        //如果没有找到主摄像头 则加载一个
        if (mainCamera == null)
        {
            GameObject objCameraDataModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(PathInfo.CameraDataPath);
            GameObject objCameraData = Instantiate(gameObject, objCameraDataModel);
            objCameraData.transform.localPosition = Vector3.zero;
            mainCamera = objCameraData.transform.Find("MainCamera").GetComponent<Camera>();

            cm_Fight = objCameraData.transform.Find("CMFollow").GetComponent<CinemachineVirtualCamera>();
            cm_Base = objCameraData.transform.Find("CMBase").GetComponent<CinemachineVirtualCamera>();
        }
        else
        {
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// 隐藏所有摄像头
    /// </summary>
    public void HideAllCM()
    {
        cm_Fight?.gameObject.SetActive(false);
        cm_Base?.gameObject.SetActive(false);
    }
}
