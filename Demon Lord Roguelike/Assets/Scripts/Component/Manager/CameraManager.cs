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
        //按当前场景刷新透明排序(所有镜头切换路径都经本方法, 排序设置始终匹配当前场景)
        RefreshTransparencySortForCurrentScene();
    }

    #region 透明排序
    /// <summary>
    /// 设置游戏内场景的透明排序: 固定按世界Z轴而非视距, 让Front层生物Spine Z前移0.1的"显示在前"与镜头角度无关(斜视角下依然生效)
    /// </summary>
    public void SetTransparencySortForGameScene()
    {
        if (mainCamera == null)
            return;
        mainCamera.transparencySortMode = TransparencySortMode.CustomAxis;
        mainCamera.transparencySortAxis = Vector3.forward;
    }

    /// <summary>
    /// 还原默认透明排序(按视距): 非游戏内场景使用
    /// </summary>
    public void ResetTransparencySort()
    {
        if (mainCamera == null)
            return;
        mainCamera.transparencySortMode = TransparencySortMode.Default;
    }

    /// <summary>
    /// 按当前场景刷新透明排序: 战斗/基地/终焉议会场景固定按世界Z轴(见 SetTransparencySortForGameScene), 其余场景(主菜单/奖励选择等)还原默认视距排序
    /// </summary>
    public void RefreshTransparencySortForCurrentScene()
    {
        var sceneType = WorldHandler.Instance.GetCurrentSceneType();
        if (sceneType == GameSceneTypeEnum.Fight || sceneType == GameSceneTypeEnum.BaseGaming || sceneType == GameSceneTypeEnum.DoomCouncil)
            SetTransparencySortForGameScene();
        else
            ResetTransparencySort();
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
