using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Test3DBuild 独立 HD-2D 宫殿生成器，所有场景资源均由本工具原创生成并集中保存。
/// </summary>
public partial class PalaceThroneHallGeneratorWindow : EditorWindow
{
    #region 常量与状态

    private const string RootFolder = "Assets/Test3DBuild";
    private const string ScenePath = RootFolder + "/Scenes/PalaceThroneHall_HD2D.unity";
    private const string TextureFolder = RootFolder + "/Textures";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string MeshFolder = RootFolder + "/Meshes";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string ProfileFolder = RootFolder + "/Profiles";
    private const string GeneratedRootName = "PalaceThroneHall_Generated";

    private static readonly Dictionary<string, Texture2D> TextureAssets = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Material> MaterialAssets = new Dictionary<string, Material>();
    private static readonly Dictionary<string, Mesh> MeshAssets = new Dictionary<string, Mesh>();

    #endregion

    #region 菜单与窗口

    /// <summary>打开宫殿生成器窗口。</summary>
    [MenuItem("游戏/场景工具/HD2D 宫殿生成器")]
    public static void OpenWindow()
    {
        PalaceThroneHallGeneratorWindow window = GetWindow<PalaceThroneHallGeneratorWindow>();
        window.titleContent = new GUIContent("HD2D 宫殿生成器");
        window.minSize = new Vector2(430, 210);
        window.Show();
    }

    /// <summary>绘制生成器说明与执行按钮。</summary>
    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("全原创 HD-2D 宫殿王座厅", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("所有模型、纹理、材质、Prefab 和后处理均生成到 Assets/Test3DBuild，不引用或修改项目现有美术资源。", MessageType.Info);
        EditorGUILayout.LabelField("输出场景", ScenePath);
        EditorGUILayout.Space();
        if (GUILayout.Button("生成 / 重建宫殿", GUILayout.Height(34)))
        {
            try
            {
                string result = GeneratePalaceThroneHall();
                EditorUtility.DisplayDialog("生成完成", result, "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("生成失败", exception.Message, "确定");
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("重建只替换 PalaceThroneHall_Generated 根节点。其他场景若有未保存修改，工具会中止以保护工作。", MessageType.None);
    }

    #endregion

    #region 生成主流程

    /// <summary>生成完整宫殿场景，供窗口按钮和 Unity MCP 非 Play 调用。</summary>
    public static string GeneratePalaceThroneHall()
    {
        ValidateGenerationState();
        TextureAssets.Clear();
        MaterialAssets.Clear();
        MeshAssets.Clear();
        EnsureOutputFolders();
        CreateAllTextures();
        CreateAllMaterials();
        CreateAllMeshes();
        VolumeProfile profile = CreateVolumeProfile();
        Scene scene = PrepareScene();

        try
        {
            BuildPalaceScene(scene, profile);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Unity 未能保存宫殿场景：" + ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string result = "宫殿已生成：" + ScenePath + "\n所有资源均位于 " + RootFolder + "。";
            Debug.Log("[Test3DBuild] " + result);
            return result;
        }
        catch (Exception exception)
        {
            Debug.LogError("[Test3DBuild] 宫殿生成失败，未保存半成品场景：" + exception);
            throw;
        }
    }

    /// <summary>验证当前编辑器状态不会导致 Play 冲突或未保存工作丢失。</summary>
    private static void ValidateGenerationState()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("请先退出 Play 模式，再生成宫殿。");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("场景存在未保存修改，请先手动保存或放弃修改：" + scene.path);
        }
    }

    /// <summary>打开目标场景并移除旧的专属生成根节点，保留其他人工根节点。</summary>
    private static Scene PrepareScene()
    {
        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        List<GameObject> existingRoots = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == GeneratedRootName) existingRoots.Add(root);
        if (existingRoots.Count > 1)
            throw new InvalidOperationException("检测到多个 " + GeneratedRootName + "，请先手动整理场景。");
        if (existingRoots.Count == 1)
            UnityEngine.Object.DestroyImmediate(existingRoots[0]);

        SceneManager.SetActiveScene(scene);
        return scene;
    }

    #endregion

    #region 原创纹理与材质

    /// <summary>生成场景全部原创像素纹理。</summary>
    private static void CreateAllTextures()
    {
        TextureAssets["PurpleStone"] = PalaceTextureFactory.CreatePurpleStone(TextureFolder + "/Palace_PurpleStone.png");
        TextureAssets["WallStone"] = PalaceTextureFactory.CreateWallStone(TextureFolder + "/Palace_WallStone.png");
        TextureAssets["DaisStone"] = PalaceTextureFactory.CreateDaisStone(TextureFolder + "/Palace_DaisStone.png");
        TextureAssets["Carpet"] = PalaceTextureFactory.CreateCarpet(TextureFolder + "/Palace_Carpet.png");
        TextureAssets["Velvet"] = PalaceTextureFactory.CreateVelvet(TextureFolder + "/Palace_Velvet.png");
        TextureAssets["Banner"] = PalaceTextureFactory.CreateBanner(TextureFolder + "/Palace_Banner.png");
        TextureAssets["Window"] = PalaceTextureFactory.CreateWindow(TextureFolder + "/Palace_Window.png");
        TextureAssets["Flame"] = PalaceTextureFactory.CreateFlame(TextureFolder + "/Palace_Flame.png");
    }

    /// <summary>生成场景全部独立 URP 材质。</summary>
    private static void CreateAllMaterials()
    {
        MaterialAssets["PurpleStone"] = CreateLitMaterial("PurpleStone", TextureAssets["PurpleStone"], Color.white, 0f, 0.18f, new Vector2(8, 10), false, false, null);
        MaterialAssets["WallStone"] = CreateLitMaterial("WallStone", TextureAssets["WallStone"], Color.white, 0f, 0.15f, new Vector2(6, 3), false, false, null);
        MaterialAssets["ColumnStone"] = CreateLitMaterial("ColumnStone", TextureAssets["WallStone"], new Color(0.82f, 0.80f, 0.84f), 0f, 0.16f, new Vector2(2, 4), false, false, null);
        MaterialAssets["DaisStone"] = CreateLitMaterial("DaisStone", TextureAssets["DaisStone"], Color.white, 0f, 0.22f, new Vector2(3, 2), false, false, null);
        MaterialAssets["CarpetMain"] = CreateLitMaterial("CarpetMain", TextureAssets["Carpet"], Color.white, 0f, 0.05f, new Vector2(1, 3), false, false, null);
        MaterialAssets["CarpetStep"] = CreateLitMaterial("CarpetStep", TextureAssets["Carpet"], Color.white, 0f, 0.05f, Vector2.one, false, false, null);
        MaterialAssets["Velvet"] = CreateLitMaterial("Velvet", TextureAssets["Velvet"], Color.white, 0f, 0.28f, new Vector2(2, 2), false, false, null);
        MaterialAssets["Banner"] = CreateLitMaterial("Banner", TextureAssets["Banner"], Color.white, 0f, 0.08f, Vector2.one, false, true, null);
        MaterialAssets["Window"] = CreateLitMaterial("Window", TextureAssets["Window"], Color.white, 0f, 0.25f, Vector2.one, true, true, new Color(0.08f, 0.12f, 0.18f) * 0.8f);
        MaterialAssets["Flame"] = CreateLitMaterial("Flame", TextureAssets["Flame"], Color.white, 0f, 0f, Vector2.one, true, true, new Color(1f, 0.42f, 0.04f) * 3f);

        MaterialAssets["Gold"] = CreateLitMaterial("Gold", null, new Color(0.82f, 0.50f, 0.11f), 0.72f, 0.48f, Vector2.one, false, false, null);
        MaterialAssets["LightGold"] = CreateLitMaterial("LightGold", null, new Color(0.96f, 0.68f, 0.22f), 0.68f, 0.5f, Vector2.one, false, false, null);
        MaterialAssets["StoneStatue"] = CreateLitMaterial("StoneStatue", null, new Color(0.43f, 0.44f, 0.46f), 0f, 0.12f, Vector2.one, false, true, null);
        MaterialAssets["StoneDark"] = CreateLitMaterial("StoneDark", null, new Color(0.16f, 0.15f, 0.18f), 0f, 0.15f, Vector2.one, false, false, null);
        MaterialAssets["StoneTrim"] = CreateLitMaterial("StoneTrim", null, new Color(0.28f, 0.25f, 0.30f), 0f, 0.18f, Vector2.one, false, false, null);
        MaterialAssets["Pot"] = CreateLitMaterial("Pot", null, new Color(0.68f, 0.62f, 0.55f), 0f, 0.22f, Vector2.one, false, true, null);
        MaterialAssets["Dirt"] = CreateLitMaterial("Dirt", null, new Color(0.12f, 0.07f, 0.04f), 0f, 0.05f, Vector2.one, false, false, null);
        MaterialAssets["Leaf"] = CreateLitMaterial("Leaf", null, new Color(0.12f, 0.35f, 0.12f), 0f, 0.15f, Vector2.one, false, true, null);
        MaterialAssets["LeafLight"] = CreateLitMaterial("LeafLight", null, new Color(0.28f, 0.52f, 0.16f), 0f, 0.15f, Vector2.one, false, true, null);
        MaterialAssets["FlowerPink"] = CreateLitMaterial("FlowerPink", null, new Color(0.82f, 0.16f, 0.30f), 0f, 0.2f, Vector2.one, false, false, null);
        MaterialAssets["FlowerWhite"] = CreateLitMaterial("FlowerWhite", null, new Color(0.92f, 0.85f, 0.72f), 0f, 0.2f, Vector2.one, false, false, null);
        MaterialAssets["Candle"] = CreateLitMaterial("Candle", null, new Color(0.88f, 0.81f, 0.62f), 0f, 0.18f, Vector2.one, false, false, null);
        MaterialAssets["Wick"] = CreateLitMaterial("Wick", null, new Color(0.05f, 0.035f, 0.025f), 0f, 0f, Vector2.one, false, false, null);
        MaterialAssets["Gem"] = CreateLitMaterial("Gem", null, new Color(0.65f, 0.025f, 0.08f), 0.35f, 0.65f, Vector2.one, false, false, new Color(0.35f, 0.01f, 0.02f));
    }

    /// <summary>创建或更新一个独立 URP/Lit 材质。</summary>
    private static Material CreateLitMaterial(string name, Texture2D texture, Color color, float metallic, float smoothness, Vector2 tiling, bool alphaClip, bool doubleSided, Color? emission)
    {
        string path = MaterialFolder + "/Palace_" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("找不到 Unity URP/Lit Shader。");
            material = new Material(shader);
            material.name = "Palace_" + name;
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
        material.SetFloat("_Cutoff", 0.4f);
        material.SetFloat("_Cull", doubleSided ? 0f : 2f);
        if (alphaClip) material.EnableKeyword("_ALPHATEST_ON");
        else material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", alphaClip ? "TransparentCutout" : "Opaque");
        material.renderQueue = alphaClip ? (int)RenderQueue.AlphaTest : (int)RenderQueue.Geometry;

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    #endregion

    #region 原创网格与后处理资源

    /// <summary>生成场景全部原创网格资源。</summary>
    private static void CreateAllMeshes()
    {
        MeshAssets["Box"] = SaveMesh(PalaceMeshFactory.CreateBox(), MeshFolder + "/Palace_Box.asset");
        MeshAssets["Cylinder"] = SaveMesh(PalaceMeshFactory.CreateCylinder(24), MeshFolder + "/Palace_Cylinder.asset");
        MeshAssets["Cone"] = SaveMesh(PalaceMeshFactory.CreateCone(16), MeshFolder + "/Palace_Cone.asset");
        MeshAssets["Sphere"] = SaveMesh(PalaceMeshFactory.CreateSphere(12, 6), MeshFolder + "/Palace_Sphere.asset");
        MeshAssets["Quad"] = SaveMesh(PalaceMeshFactory.CreateQuad(), MeshFolder + "/Palace_Quad.asset");
        MeshAssets["Banner"] = SaveMesh(PalaceMeshFactory.CreateBanner(8, 12), MeshFolder + "/Palace_Banner.asset");
        MeshAssets["Wing"] = SaveMesh(PalaceMeshFactory.CreateWing(), MeshFolder + "/Palace_DragonWing.asset");
        Vector2[] potProfile =
        {
            new Vector2(0.28f, 0f), new Vector2(0.34f, 0.08f), new Vector2(0.40f, 0.43f),
            new Vector2(0.47f, 0.52f), new Vector2(0.47f, 0.61f), new Vector2(0.37f, 0.61f),
            new Vector2(0.34f, 0.52f)
        };
        MeshAssets["FlowerPot"] = SaveMesh(PalaceMeshFactory.CreateLathe(potProfile, 24), MeshFolder + "/Palace_FlowerPot.asset");
    }

    /// <summary>保存网格并保留已有资源 GUID，避免重复生成破坏引用。</summary>
    private static Mesh SaveMesh(Mesh source, string path)
    {
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(source, path);
            return source;
        }

        existing.Clear();
        existing.vertices = source.vertices;
        existing.normals = source.normals;
        existing.tangents = source.tangents;
        existing.uv = source.uv;
        existing.subMeshCount = source.subMeshCount;
        for (int i = 0; i < source.subMeshCount; i++)
            existing.SetTriangles(source.GetTriangles(i), i);
        existing.RecalculateBounds();
        EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(source);
        return existing;
    }

    /// <summary>创建全新的宫殿 Volume Profile，不复制或引用共享配置。</summary>
    private static VolumeProfile CreateVolumeProfile()
    {
        string path = ProfileFolder + "/PalaceThroneHallVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PalaceThroneHallVolumeProfile";
            AssetDatabase.CreateAsset(profile, path);
        }

        Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.05f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.7f;
        bloom.scatter.overrideState = true; bloom.scatter.value = 0.65f;

        Vignette vignette = GetOrAddVolumeComponent<Vignette>(profile);
        vignette.intensity.overrideState = true; vignette.intensity.value = 0.22f;
        vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.35f;

        Tonemapping tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
        tonemapping.mode.overrideState = true; tonemapping.mode.value = TonemappingMode.Neutral;

        DepthOfField depthOfField = GetOrAddVolumeComponent<DepthOfField>(profile);
        depthOfField.mode.overrideState = true; depthOfField.mode.value = DepthOfFieldMode.Bokeh;
        depthOfField.focusDistance.overrideState = true; depthOfField.focusDistance.value = 21f;
        depthOfField.aperture.overrideState = true; depthOfField.aperture.value = 18f;
        depthOfField.focalLength.overrideState = true; depthOfField.focalLength.value = 35f;

        ColorAdjustments colorAdjustments = GetOrAddVolumeComponent<ColorAdjustments>(profile);
        colorAdjustments.postExposure.overrideState = true; colorAdjustments.postExposure.value = 0.1f;
        colorAdjustments.contrast.overrideState = true; colorAdjustments.contrast.value = 8f;
        colorAdjustments.saturation.overrideState = true; colorAdjustments.saturation.value = 5f;

        EditorUtility.SetDirty(profile);
        return profile;
    }

    /// <summary>获取或新增 Profile 子组件，并确保其作为专属资源子资产保存。</summary>
    private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component;
        if (!profile.TryGet(out component))
        {
            component = profile.Add<T>();
            if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
        }
        component.active = true;
        EditorUtility.SetDirty(component);
        return component;
    }

    #endregion

    #region 路径与依赖验证

    /// <summary>创建全部专属输出目录。</summary>
    private static void EnsureOutputFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(RootFolder + "/Scenes");
        EnsureFolder(TextureFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(ProfileFolder);
        EnsureFolder(RootFolder + "/Screenshots");
    }

    /// <summary>逐级创建 Assets 目录。</summary>
    private static void EnsureFolder(string assetDirectory)
    {
        if (AssetDatabase.IsValidFolder(assetDirectory)) return;
        string[] parts = assetDirectory.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    /// <summary>检查场景是否意外引用了 Test3DBuild 之外的项目 Assets 资源。</summary>
    public static string ValidateGeneratedSceneDependencies()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            return "场景尚未生成：" + ScenePath;
        string[] dependencies = AssetDatabase.GetDependencies(ScenePath, false);
        List<string> externalAssets = new List<string>();
        foreach (string dependency in dependencies)
            if (dependency.StartsWith("Assets/") && !dependency.StartsWith(RootFolder + "/")) externalAssets.Add(dependency);
        return externalAssets.Count == 0 ? "依赖检查通过：未引用 Test3DBuild 之外的项目 Assets 资源。" : "发现外部项目资源：\n" + string.Join("\n", externalAssets);
    }

    #endregion
}
