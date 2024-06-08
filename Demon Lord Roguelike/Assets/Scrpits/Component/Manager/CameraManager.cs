using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraManager : BaseManager
{ 
    /// <summary>
    /// 加载主摄像头
    /// </summary>
    public void LoadMainCamera()
    {       
        //如果没有找到主摄像头 则加载一个
        if (mainCamera == null)
        {
            GameObject objCameraModel = LoadAddressablesUtil.LoadAssetSync<GameObject>(PathInfo.CameraMainPath);
            GameObject objCamera = Instantiate(gameObject, objCameraModel);
            objCamera.transform.localPosition = Vector3.zero;
            mainCamera = objCamera.GetComponent<Camera>();
        }
        else
        {
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.localPosition = Vector3.zero;
        }
    }
}
