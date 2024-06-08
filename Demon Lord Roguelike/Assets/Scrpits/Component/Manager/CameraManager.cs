using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraManager : BaseManager
{ 
    /// <summary>
    /// ����������ͷ
    /// </summary>
    public void LoadMainCamera()
    {       
        //���û���ҵ�������ͷ �����һ��
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
