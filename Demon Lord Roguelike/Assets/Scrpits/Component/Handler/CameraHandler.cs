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
    /// ����ս�������ӽ�
    /// </summary>
    public void SetFightSceneCamera()
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);
        mainCamera.transform.eulerAngles = new Vector3(30, 0, 0);
        mainCamera.transform.position = new Vector3(0, 3, -4);
        mainCamera.fieldOfView = 60;
    }
}