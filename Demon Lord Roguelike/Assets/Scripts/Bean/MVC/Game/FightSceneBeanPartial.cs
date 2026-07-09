using System;
using System.Collections.Generic;
using UnityEngine;

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
