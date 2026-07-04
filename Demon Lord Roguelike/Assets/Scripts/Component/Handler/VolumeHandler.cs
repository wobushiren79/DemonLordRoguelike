using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 后处理/环境渲染 Handler（游戏层 partial）：按游戏场景初始化环境参数 + 第三方体积雾(URP Volumetric Fog)。
/// 引擎原生能力(景深/内置雾)见框架层同名 partial。
/// </summary>
public partial class VolumeHandler
{
    #region 场景初始化

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData(GameSceneTypeEnum gameSceneType)
    {
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        //体积雾默认关闭，只有需要的场景（如领奖）才显式开启
        SetVolumetricFogActive(false);
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
                SetDepthOfField(DepthOfFieldMode.Bokeh, disFollowFight, 180, 12);
                break;
            case GameSceneTypeEnum.RewardSelect:
                SetDepthOfField(DepthOfFieldMode.Bokeh, 15, 150, 10);
                SetVolumetricFogForRewardSelect();
                break;
            case GameSceneTypeEnum.DoomCouncil:
                float disFollowDoomCouncil = CameraHandler.Instance.GetDistanceFollow(CameraHandler.Instance.manager.cm_Base);
                SetDepthOfField(DepthOfFieldMode.Bokeh, disFollowDoomCouncil * 1.5f, 200, 20);
                break;
            default:
                SetDepthOfField(DepthOfFieldMode.Bokeh, 4, 140, 10);
                break;
        }
    }

    #endregion

    #region 体积雾 Volumetric Fog（第三方 URP Volumetric Fog）

    /// <summary>
    /// 设置体积雾参数（第三方 URP Volumetric Fog）
    /// </summary>
    /// <param name="distance">雾渲染的最大距离，值越大远处越浑浊看不清</param>
    /// <param name="density">雾浓度（0~1），越大越浓越看不清远处</param>
    /// <param name="tint">雾染色（主光散射部分的颜色，森林可偏冷青绿/灰白）</param>
    /// <param name="scattering">主光散射强度（0~1），越大朦胧辉光越亮</param>
    /// <param name="anisotropy">散射各向异性（-1~1），正值朝光源方向更亮（穿林光柱/丁达尔感）</param>
    /// <param name="attenuationDistance">光随距离衰减的距离，值越小画面越暗</param>
    /// <param name="baseHeight">雾达到设定浓度的世界高度</param>
    /// <param name="maximumHeight">雾浓度衰减为 0 的世界高度（此高度以上无雾）</param>
    /// <param name="isActive">是否开启体积雾</param>
    public void SetVolumetricFog(float distance, float density, Color tint, float scattering = 0.15f, float anisotropy = 0.4f, float attenuationDistance = 128f, float baseHeight = 0f, float maximumHeight = 50f, bool isActive = true)
    {
        var volumetricFog = manager.volumetricFog;
        if (volumetricFog == null) return;
        volumetricFog.distance.overrideState = true;
        volumetricFog.distance.value = distance;
        volumetricFog.density.overrideState = true;
        volumetricFog.density.value = density;
        volumetricFog.tint.overrideState = true;
        volumetricFog.tint.value = tint;
        volumetricFog.scattering.overrideState = true;
        volumetricFog.scattering.value = scattering;
        volumetricFog.anisotropy.overrideState = true;
        volumetricFog.anisotropy.value = anisotropy;
        volumetricFog.attenuationDistance.overrideState = true;
        volumetricFog.attenuationDistance.value = attenuationDistance;
        volumetricFog.baseHeight.overrideState = true;
        volumetricFog.baseHeight.value = baseHeight;
        volumetricFog.maximumHeight.overrideState = true;
        volumetricFog.maximumHeight.value = maximumHeight;
        SetVolumetricFogActive(isActive);
    }

    /// <summary>
    /// 设置领奖场景专用的体积雾参数并开启（沿用领奖场景原有数值）
    /// </summary>
    public void SetVolumetricFogForRewardSelect()
    {
        //领奖场景雾数值：距离64/浓度0.2/白色染色/散射0.15/各向异性0.4/衰减128/高度0~50
        SetVolumetricFog(64f, 0.2f, Color.white, 0.15f, 0.4f, 128f, 0f, 50f, true);
    }

    /// <summary>
    /// 是否开启体积雾
    /// </summary>
    /// <param name="isActive">true 开启体积雾，false 关闭</param>
    public void SetVolumetricFogActive(bool isActive)
    {
        var volumetricFog = manager.volumetricFog;
        if (volumetricFog == null) return;
        volumetricFog.active = isActive;
        volumetricFog.enabled.overrideState = true;
        volumetricFog.enabled.value = isActive;
    }

    #endregion
}
