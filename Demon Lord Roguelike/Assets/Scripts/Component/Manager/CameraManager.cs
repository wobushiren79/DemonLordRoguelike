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
        //切走镜头时还原默认透明排序(战斗镜头启用时会重新设置, 保证自定义Z轴排序只在战斗场景生效)
        ResetTransparencySort();
    }

    #region 透明排序
    /// <summary>
    /// 设置战斗场景的透明排序: 固定按世界Z轴而非视距, 让Front层生物Spine Z前移0.1的"显示在前"与镜头角度无关(斜视角下依然生效)
    /// </summary>
    public void SetTransparencySortForFight()
    {
        if (mainCamera == null)
            return;
        mainCamera.transparencySortMode = TransparencySortMode.CustomAxis;
        mainCamera.transparencySortAxis = Vector3.forward;
    }

    /// <summary>
    /// 还原默认透明排序(按视距): 非战斗场景使用
    /// </summary>
    public void ResetTransparencySort()
    {
        if (mainCamera == null)
            return;
        mainCamera.transparencySortMode = TransparencySortMode.Default;
    }
    #endregion

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
