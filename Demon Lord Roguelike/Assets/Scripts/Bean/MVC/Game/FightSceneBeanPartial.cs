using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public partial class FightSceneBean
{
    #region 雾配置

    /// <summary>
    /// 是否配置了雾（fog 字段为空表示不开启雾）
    /// </summary>
    public bool HasFog => !string.IsNullOrEmpty(fog);

    /// <summary>
    /// 解析雾配置字符串（形如 Color:#CEF9FF&Start:8&End:20&Mode:Linear）
    /// </summary>
    /// <param name="fogColor">解析出的雾颜色</param>
    /// <param name="startDistance">线性雾起始距离（此距离内清晰）</param>
    /// <param name="endDistance">线性雾终止距离（超过则全被雾遮住）</param>
    /// <param name="fogMode">雾模式（Linear/Exponential/ExponentialSquared）</param>
    /// <returns>是否解析成功（配置为空返回 false）</returns>
    public bool GetFogParams(out Color fogColor, out float startDistance, out float endDistance, out FogMode fogMode)
    {
        fogColor = Color.white;
        startDistance = 0f;
        endDistance = 100f;
        fogMode = FogMode.Linear;
        if (string.IsNullOrEmpty(fog)) return false;
        //复用框架通用拆解：按 ':' 与 '&' 拆成 Dictionary<string,string>
        var dic = fog.SplitForDictionary();
        if (dic.TryGetValue("Color", out var colorStr)) ColorUtility.TryParseHtmlString(colorStr, out fogColor);
        if (dic.TryGetValue("Start", out var startStr)) float.TryParse(startStr, out startDistance);
        if (dic.TryGetValue("End", out var endStr)) float.TryParse(endStr, out endDistance);
        if (dic.TryGetValue("Mode", out var modeStr) && !Enum.TryParse(modeStr, true, out fogMode)) fogMode = FogMode.Linear;
        return true;
    }

    #endregion

    #region 体积雾配置

    /// <summary>
    /// 是否配置了体积雾（volumetric_fog 字段为空表示不开启体积雾）
    /// </summary>
    public bool HasVolumetricFog => !string.IsNullOrEmpty(volumetric_fog);

    /// <summary>
    /// 解析体积雾配置字符串（形如 Distance:48&Density:0.06&Tint:#B8CCFF&Scattering:0.05&Anisotropy:0.5&Attenuation:96&BaseHeight:0&MaxHeight:12&MainLight:1&AdditionalLight:1，缺省键回退默认值）
    /// </summary>
    /// <param name="fogParams">解析出的体积雾参数</param>
    /// <returns>是否解析成功（配置为空返回 false）</returns>
    public bool GetVolumetricFogParams(out VolumetricFogParamsBean fogParams)
    {
        fogParams = new VolumetricFogParamsBean();
        if (string.IsNullOrEmpty(volumetric_fog)) return false;
        //复用框架通用拆解：按 ':' 与 '&' 拆成 Dictionary<string,string>
        var dic = volumetric_fog.SplitForDictionary();
        if (dic.TryGetValue("Distance", out var strDistance)) float.TryParse(strDistance, out fogParams.distance);
        if (dic.TryGetValue("Density", out var strDensity)) float.TryParse(strDensity, out fogParams.density);
        if (dic.TryGetValue("Tint", out var strTint)) ColorUtility.TryParseHtmlString(strTint, out fogParams.tint);
        if (dic.TryGetValue("Scattering", out var strScattering)) float.TryParse(strScattering, out fogParams.scattering);
        if (dic.TryGetValue("Anisotropy", out var strAnisotropy)) float.TryParse(strAnisotropy, out fogParams.anisotropy);
        if (dic.TryGetValue("Attenuation", out var strAttenuation)) float.TryParse(strAttenuation, out fogParams.attenuationDistance);
        if (dic.TryGetValue("BaseHeight", out var strBaseHeight)) float.TryParse(strBaseHeight, out fogParams.baseHeight);
        if (dic.TryGetValue("MaxHeight", out var strMaxHeight)) float.TryParse(strMaxHeight, out fogParams.maximumHeight);
        //灯光贡献配 1=强制开 0=强制关，缺省=不处理（保持 profile 原值）
        if (dic.TryGetValue("MainLight", out var strMainLight)) fogParams.mainLightContribution = strMainLight == "1";
        if (dic.TryGetValue("AdditionalLight", out var strAdditionalLight)) fogParams.additionalLightContribution = strAdditionalLight == "1";
        return true;
    }

    #endregion

    #region 环境光配置

    /// <summary>
    /// 是否配置了环境光颜色（ambient_light 字段为空表示不修改全局环境光）
    /// </summary>
    public bool HasAmbientLight => !string.IsNullOrEmpty(ambient_light);

    /// <summary>
    /// 解析环境光颜色（形如 #364863）；未配置或解析失败时回退白色
    /// </summary>
    /// <returns>环境光颜色</returns>
    public Color GetAmbientLightColor()
    {
        Color color = Color.white;
        if (string.IsNullOrEmpty(ambient_light)) return color;
        ColorUtility.TryParseHtmlString(ambient_light, out color);
        return color;
    }

    #endregion

    #region 场景细节预制

    /// <summary>
    /// 是否配置了场景细节预制（details 字段为空表示该场景没有细节预制，加载时整个 Details 节点隐藏）
    /// </summary>
    public bool HasDetails => !string.IsNullOrEmpty(details);

    #endregion

    #region 环境音配置

    /// <summary>
    /// 是否配置了环境音（environment_sound 为 AudioInfo 表 id，0=不播放；播放走 AudioHandler.PlayEnvironment 循环通道）
    /// </summary>
    public bool HasEnvironmentSound => environment_sound > 0;

    #endregion

    #region 景深配置

    /// <summary>
    /// 是否配置了景深（depth_of_field 字段为空表示使用默认景深参数）
    /// </summary>
    public bool HasDepthOfField => !string.IsNullOrEmpty(depth_of_field);

    /// <summary>
    /// 解析景深配置字符串（形如 mode:Bokeh&length:130&aperture:12，缺省键回退默认值 Bokeh/180/12）
    /// </summary>
    /// <param name="mode">景深模式（Bokeh/Gaussian）</param>
    /// <param name="focalLength">焦距（毫米），值越大景深越浅</param>
    /// <param name="aperture">光圈（f 值），值越小景深越浅</param>
    /// <returns>是否解析成功（配置为空返回 false）</returns>
    public bool GetDepthOfFieldParams(out DepthOfFieldMode mode, out float focalLength, out float aperture)
    {
        mode = DepthOfFieldMode.Bokeh;
        focalLength = 180f;
        aperture = 12f;
        if (string.IsNullOrEmpty(depth_of_field)) return false;
        //复用框架通用拆解：按 ':' 与 '&' 拆成 Dictionary<string,string>
        var dic = depth_of_field.SplitForDictionary();
        if (dic.TryGetValue("mode", out var strMode) && !Enum.TryParse(strMode, true, out mode)) mode = DepthOfFieldMode.Bokeh;
        if (dic.TryGetValue("length", out var strLength)) float.TryParse(strLength, out focalLength);
        if (dic.TryGetValue("aperture", out var strAperture)) float.TryParse(strAperture, out aperture);
        return true;
    }

    #endregion

    #region 天空盒旋转

    /// <summary>
    /// 解析天空盒旋转角度（形如 "-15,0,0"，逗号分隔的 X,Y,Z 欧拉角）；未配置则回退默认 (0,0,0)
    /// </summary>
    /// <returns>天空盒的三轴旋转欧拉角</returns>
    public Vector3 GetSkyboxRotate()
    {
        Vector3 rotate = Vector3.zero;//未配置时默认零旋转
        if (string.IsNullOrEmpty(skybox_rotate)) return rotate;
        var arr = skybox_rotate.Split(',');
        if (arr.Length > 0) float.TryParse(arr[0], out rotate.x);
        if (arr.Length > 1) float.TryParse(arr[1], out rotate.y);
        if (arr.Length > 2) float.TryParse(arr[2], out rotate.z);
        return rotate;
    }

    #endregion
}
public partial class FightSceneCfg
{
}

/// <summary>
/// 体积雾参数（由 FightSceneBean.volumetric_fog 配置串解析而来；数值默认值与 VolumeHandler.SetVolumetricFog 参数默认值一致，配置缺省键即回退这些值）
/// </summary>
public class VolumetricFogParamsBean
{
    /// <summary>雾渲染的最大距离，值越大远处越浑浊看不清</summary>
    public float distance = 64f;
    /// <summary>雾浓度（0~1），越大越浓越看不清远处</summary>
    public float density = 0.2f;
    /// <summary>雾染色（主光散射部分的颜色，森林可偏冷青绿/灰白）</summary>
    public Color tint = Color.white;
    /// <summary>主光散射强度（0~1），越大朦胧辉光越亮</summary>
    public float scattering = 0.15f;
    /// <summary>散射各向异性（-1~1），正值朝光源方向更亮（穿林光柱/丁达尔感）</summary>
    public float anisotropy = 0.4f;
    /// <summary>光随距离衰减的距离，值越小画面越暗</summary>
    public float attenuationDistance = 128f;
    /// <summary>雾达到设定浓度的世界高度</summary>
    public float baseHeight = 0f;
    /// <summary>雾浓度衰减为 0 的世界高度（此高度以上无雾）</summary>
    public float maximumHeight = 50f;
    /// <summary>主光散射贡献开关（null=不处理保持 profile 原值；体积光柱需显式 true 防 profile 被改）</summary>
    public bool? mainLightContribution = null;
    /// <summary>额外灯散射贡献开关（null=不处理；月光柱等 VolumetricAdditionalLight 聚光灯必须 true 才参与散射）</summary>
    public bool? additionalLightContribution = null;
}
