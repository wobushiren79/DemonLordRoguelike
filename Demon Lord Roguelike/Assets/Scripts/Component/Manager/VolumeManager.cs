using UnityEngine.Rendering;

/// <summary>
/// 后处理/环境渲染 Manager（游戏层 partial）：持有第三方 VolumeComponent(URP Volumetric Fog 体积雾)。
/// 全局 Volume、Profile 及引擎原生组件(景深)见框架层同名 partial。
/// </summary>
public partial class VolumeManager
{
    //体积雾（第三方 URP Volumetric Fog）
    protected VolumetricFogVolumeComponent _volumetricFog;
    public VolumetricFogVolumeComponent volumetricFog
    {
        get
        {
            if (_volumetricFog == null)
            {
                volumeProfile.TryGet(out _volumetricFog);
            }
            return _volumetricFog;
        }
    }
}
