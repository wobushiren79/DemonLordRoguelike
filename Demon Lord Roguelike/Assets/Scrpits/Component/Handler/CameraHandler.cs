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
    /// 设置战斗场景视角
    /// </summary>
    public void SetFightSceneCamera()
    {
        var mainCamera = manager.mainCamera;
        mainCamera.gameObject.SetActive(true);
        mainCamera.transform.eulerAngles = new Vector3(30, 0, 0);
        //设置初始视角位置
        mainCamera.transform.position = new Vector3(3, 3, -1);

        mainCamera.fieldOfView = 60;

    }
}
