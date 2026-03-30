using Unity.Cinemachine;
using UnityEngine;

public partial class CameraManager
{
    public CinemachineCamera cm_Fight;
    public CinemachineCamera cm_Base;

    public CinemachineBrain cinemachineBrain;

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

            cm_Fight = objCameraData.transform.Find("CMFollow").GetComponent<CinemachineCamera>();
            cm_Base = objCameraData.transform.Find("CMBase").GetComponent<CinemachineCamera>();

            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
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

    /// <summary>
    /// 设置主摄像头的默认切换动画
    /// </summary>
    public void SetMainCameraDefaultBlend(float time, CinemachineBlendDefinition.Styles style = CinemachineBlendDefinition.Styles.EaseInOut)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.DefaultBlend.Style = style;
            cinemachineBrain.DefaultBlend.Time = time;
        }
    }
}
