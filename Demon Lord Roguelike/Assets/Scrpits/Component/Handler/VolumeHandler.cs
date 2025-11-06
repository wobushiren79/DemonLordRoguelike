using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VolumeHandler : BaseHandler<VolumeHandler, VolumeManager>
{
    //当前天空盒
    public AsyncOperationHandle<Material> currentSkyBox;
    
    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(GameSceneTypeEnum gameSceneType)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        switch (gameSceneType)
        {
            case GameSceneTypeEnum.BaseMain:
                SetDepthOfField(DepthOfFieldMode.Bokeh, 5, 150, 10);
                break;
            case GameSceneTypeEnum.BaseGaming:
                float disFollowBase = CameraHandler.Instance.GetDistanceFollow(CameraHandler.Instance.manager.cm_Base);
                SetDepthOfField(DepthOfFieldMode.Bokeh, disFollowBase, 200, 20);
                break;
            case GameSceneTypeEnum.Fight:
                float disFollowFight = CameraHandler.Instance.GetDistanceFollow(CameraHandler.Instance.manager.cm_Fight);
                SetDepthOfField(DepthOfFieldMode.Bokeh, disFollowFight, 260, 12);
                break;
            case GameSceneTypeEnum.RewardSelect:
                SetDepthOfField(DepthOfFieldMode.Bokeh, 15, 150, 10);
                break;
            case GameSceneTypeEnum.DoomCouncil:
                float disFollowDoomCouncil = CameraHandler.Instance.GetDistanceFollow(CameraHandler.Instance.manager.cm_Base);
                SetDepthOfField(DepthOfFieldMode.Bokeh, disFollowDoomCouncil, 200, 20);
                break;
            default:
                SetDepthOfField(DepthOfFieldMode.Bokeh, 4, 140, 10);
                break;
        }
    }

    /// <summary>
    /// 设置远景模糊
    /// </summary>
    /// <param name="mode">Off：选择此选项可禁用景深。Gaussian：选择此选项可使用更快但更有限的景深模式。Bokeh：选择此选项可使用基于散景的景深模式。</param>
    /// <param name="focusDistance">设置从摄像机到焦点的距离</param>
    /// <param name="focalLength">	设置摄像机传感器和摄像机镜头之间的距离（以毫米为单位）。值越大，景深越浅。</param>
    /// <param name="aperture">设置孔径比（也称为 f 值 (f-stop) 或 f 数 (f-number)）。值越小，景深越浅。</param>
    public void SetDepthOfField(DepthOfFieldMode mode, float focusDistance, float focalLength, float aperture, bool isActive = true)
    {
        var depthOfField = manager.depthOfField;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = mode;
        depthOfField.focusDistance.overrideState = true;
        depthOfField.focusDistance.value = focusDistance;
        depthOfField.focalLength.overrideState = true;
        depthOfField.focalLength.value = focalLength;
        depthOfField.aperture.overrideState = true;
        depthOfField.aperture.value = aperture;
        SetDepthOfFieldActive(isActive);
    }

    /// <summary>
    /// 是否开启远景模糊
    /// </summary>
    /// <param name="isActive"></param>
    public void SetDepthOfFieldActive(bool isActive)
    {
        var depthOfField = manager.depthOfField;
        depthOfField.active = isActive;
    }
}
