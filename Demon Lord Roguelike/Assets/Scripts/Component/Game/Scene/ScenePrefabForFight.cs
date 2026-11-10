using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 普通战斗场景根组件，统一承接场景实例表现与生命周期。
/// </summary>
[DisallowMultipleComponent]
public sealed class ScenePrefabForFight : ScenePrefabBase
{
    #region 场景初始化

    /// <summary>
    /// 初始化场景实例，应用杂物阴影并在 Details 激活前随机花海种子与图集排，随后由花海 OnEnable 完成一次生成。
    /// </summary>
    public override Task InitSceneData()
    {
        System.Random random = new System.Random();
        bool sceneClutterShadow = GameDataHandler.Instance.manager.GetGameConfig().sceneClutterShadow;
        FlowerSeaInstanceRenderer[] flowerSeas = GetComponentsInChildren<FlowerSeaInstanceRenderer>(true);
        foreach (FlowerSeaInstanceRenderer flowerSea in flowerSeas)
        {
            flowerSea.castShadows = sceneClutterShadow;
            flowerSea.randomSeed = random.Next();
            if (flowerSea.textureMode != FlowerSeaInstanceRenderer.FlowerSeaTextureMode.Atlas)
                continue;

            int columns = Mathf.Max(1, flowerSea.atlasGrid.x);
            int rows = Mathf.Max(1, flowerSea.atlasGrid.y);
            int selectedRow = random.Next(rows);
            flowerSea.atlasUseAllCells = false;
            flowerSea.atlasSelectedCells = new List<Vector2Int>(columns);
            for (int column = 0; column < columns; column++)
                flowerSea.atlasSelectedCells.Add(new Vector2Int(column, selectedRow));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 设置当前战斗场景所有花海是否投射阴影，仅改渲染状态，不重新生成花海。
    /// </summary>
    /// <param name="isEnabled">是否开启花海阴影</param>
    public void SetFlowerSeaShadows(bool isEnabled)
    {
        FlowerSeaInstanceRenderer[] flowerSeas = GetComponentsInChildren<FlowerSeaInstanceRenderer>(true);
        foreach (FlowerSeaInstanceRenderer flowerSea in flowerSeas)
            flowerSea.castShadows = isEnabled;
    }

    #endregion

    #region 天空盒设置

    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    private static readonly int YawId = Shader.PropertyToID("_RotateY");

    /// <summary>是否启用当前场景的动态天空盒</summary>
    [SerializeField] private bool enableSkyboxMotion;
    /// <summary>相对源天空盒的曝光倍率</summary>
    [SerializeField, Min(0f)] private float exposureMultiplier = 1f;
    /// <summary>天空盒每秒绕垂直轴旋转的角度</summary>
    [SerializeField] private float yawDegreesPerSecond;

    private Material sourceSkybox;
    private Material sceneSkybox;

    #endregion

    #region 生命周期

    /// <summary>
    /// 按需复制进入战斗时已加载的天空盒，保留场景配置的初始朝向。
    /// </summary>
    private void OnEnable()
    {
        if (!enableSkyboxMotion)
            return;

        var skybox = RenderSettings.skybox;
        if (skybox == null || !skybox.HasProperty(ExposureId) || !skybox.HasProperty(YawId))
            return;

        sourceSkybox = skybox;
        sceneSkybox = new Material(skybox) { hideFlags = HideFlags.HideAndDontSave };
        sceneSkybox.SetFloat(ExposureId, skybox.GetFloat(ExposureId) * exposureMultiplier);
        RenderSettings.skybox = sceneSkybox;
    }

    /// <summary>
    /// 绕垂直轴移动云层，保持地平线高度；外部替换天空盒后停止更新。
    /// </summary>
    private void Update()
    {
        if (sceneSkybox == null || RenderSettings.skybox != sceneSkybox)
            return;

        float yaw = sceneSkybox.GetFloat(YawId);
        sceneSkybox.SetFloat(YawId, Mathf.Repeat(yaw + yawDegreesPerSecond * Time.deltaTime, 360f));
    }

    /// <summary>
    /// 离开场景时释放实例，仅在仍持有当前天空盒时恢复源材质。
    /// </summary>
    private void OnDisable()
    {
        if (sceneSkybox == null)
            return;

        if (RenderSettings.skybox == sceneSkybox)
            RenderSettings.skybox = sourceSkybox;

        Destroy(sceneSkybox);
        sceneSkybox = null;
        sourceSkybox = null;
    }

    #endregion
}
