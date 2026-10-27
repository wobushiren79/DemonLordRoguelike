using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Test3DBuild 宫殿生成器的场景布局与装饰构建部分。
/// </summary>
public partial class PalaceThroneHallGeneratorWindow
{
    #region 场景总装

    /// <summary>按建筑、台阶、地毯、装饰、灯光的顺序组装完整场景。</summary>
    private static void BuildPalaceScene(Scene scene, VolumeProfile profile)
    {
        Transform root = CreateGroup(GeneratedRootName, null);
        Transform architecture = CreateGroup("Architecture", root);
        Transform throneAndStairs = CreateGroup("ThroneAndStairs", root);
        Transform carpet = CreateGroup("Carpet", root);
        Transform decorations = CreateGroup("Decorations", root);
        Transform lighting = CreateGroup("Lighting", root);
        Transform cameraAndVolume = CreateGroup("CameraAndVolume", root);

        BuildArchitecture(architecture);
        BuildDais(throneAndStairs);
        BuildCarpet(carpet);
        BuildThrone(throneAndStairs);
        BuildDecorations(decorations);
        BuildLighting(lighting);
        BuildCameraAndVolume(cameraAndVolume, profile);
        ConfigureRenderSettings();
    }

    #endregion

    #region 建筑与台阶

    /// <summary>生成紫色地面、石墙、窗、圆柱和建筑装饰线。</summary>
    private static void BuildArchitecture(Transform parent)
    {
        CreateBox("PurpleStoneFloor", parent, new Vector3(0, -0.125f, 1), new Vector3(18, 0.25f, 20), MaterialAssets["PurpleStone"], true);
        CreateBox("BackWall", parent, new Vector3(0, 3.6f, 10.5f), new Vector3(18, 7.2f, 0.5f), MaterialAssets["WallStone"], true);
        CreateBox("LeftWall", parent, new Vector3(-9, 3.6f, 3), new Vector3(0.5f, 7.2f, 15), MaterialAssets["WallStone"], true);
        CreateBox("RightWall", parent, new Vector3(9, 3.6f, 3), new Vector3(0.5f, 7.2f, 15), MaterialAssets["WallStone"], true);

        CreateBox("BackWallBase", parent, new Vector3(0, 0.35f, 10.15f), new Vector3(18, 0.7f, 0.35f), MaterialAssets["StoneTrim"], false);
        CreateBox("BackWallCrown", parent, new Vector3(0, 6.85f, 10.15f), new Vector3(18, 0.28f, 0.45f), MaterialAssets["Gold"], false);
        CreateBox("LeftWallBase", parent, new Vector3(-8.72f, 0.35f, 3), new Vector3(0.35f, 0.7f, 15), MaterialAssets["StoneTrim"], false);
        CreateBox("RightWallBase", parent, new Vector3(8.72f, 0.35f, 3), new Vector3(0.35f, 0.7f, 15), MaterialAssets["StoneTrim"], false);

        CreateColumn(-6.7f, 7.3f, parent);
        CreateColumn(6.7f, 7.3f, parent);
        CreateColumn(-7.55f, 0.8f, parent);
        CreateColumn(7.55f, 0.8f, parent);
        CreateWindow(-4.0f, 4.1f, 10.21f, parent);
        CreateWindow(4.0f, 4.1f, 10.21f, parent);
    }

    /// <summary>生成一根带基座、柱身和柱头的粗圆柱。</summary>
    private static void CreateColumn(float x, float z, Transform parent)
    {
        Transform column = CreateGroup("StoneColumn", parent);
        column.localPosition = new Vector3(x, 0, z);
        CreateBox("ColumnPlinth", column, new Vector3(0, 0.175f, 0), new Vector3(2.1f, 0.35f, 2.1f), MaterialAssets["DaisStone"], true);
        CreateCylinder("ColumnBaseRing", column, new Vector3(0, 0.475f, 0), new Vector3(1.95f, 0.25f, 1.95f), MaterialAssets["ColumnStone"], false);
        CreateCylinder("ColumnShaft", column, new Vector3(0, 3f, 0), new Vector3(1.55f, 4.8f, 1.55f), MaterialAssets["ColumnStone"], true);
        CreateCylinder("ColumnCapitalRing", column, new Vector3(0, 5.48f, 0), new Vector3(1.9f, 0.28f, 1.9f), MaterialAssets["ColumnStone"], false);
        CreateBox("ColumnCapital", column, new Vector3(0, 5.78f, 0), new Vector3(2.1f, 0.35f, 2.1f), MaterialAssets["DaisStone"], false);
    }

    /// <summary>在后墙生成原创像素拱窗。</summary>
    private static void CreateWindow(float x, float y, float z, Transform parent)
    {
        CreateQuad("ArchedWindow", parent, new Vector3(x, y, z), Vector3.zero, new Vector3(1.8f, 3.1f, 1), MaterialAssets["Window"], ShadowCastingMode.TwoSided);
        CreateBox("WindowSill", parent, new Vector3(x, y - 1.6f, z - 0.08f), new Vector3(2.05f, 0.18f, 0.35f), MaterialAssets["DaisStone"], false);
    }

    /// <summary>生成中央三级王座台和金色台阶边线。</summary>
    private static void BuildDais(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            float width = 7f - i * 0.4f;
            float depth = 4.4f - i * 0.6f;
            float centerZ = 6.4f + i * 0.3f;
            float bottom = i * 0.25f;
            float frontZ = centerZ - depth * 0.5f;
            CreateBox("DaisStep_" + (i + 1), parent, new Vector3(0, bottom + 0.125f, centerZ), new Vector3(width, 0.25f, depth), MaterialAssets["DaisStone"], true);
            CreateBox("StepGoldEdge_" + (i + 1), parent, new Vector3(0, bottom + 0.21f, frontZ - 0.025f), new Vector3(width, 0.07f, 0.06f), MaterialAssets["Gold"], false);
        }
    }

    #endregion

    #region 王座与地毯

    /// <summary>生成红金地毯主通道、台阶踏面与立面铺毯。</summary>
    private static void BuildCarpet(Transform parent)
    {
        CreateBox("MainCarpet", parent, new Vector3(0, 0.025f, -1.95f), new Vector3(7.4f, 0.035f, 12.3f), MaterialAssets["CarpetMain"], false);
        CreateBox("MainCarpetGoldLeft", parent, new Vector3(-3.72f, 0.048f, -1.95f), new Vector3(0.07f, 0.025f, 12.3f), MaterialAssets["Gold"], false);
        CreateBox("MainCarpetGoldRight", parent, new Vector3(3.72f, 0.048f, -1.95f), new Vector3(0.07f, 0.025f, 12.3f), MaterialAssets["Gold"], false);

        for (int i = 0; i < 3; i++)
        {
            float depth = 4.4f - i * 0.6f;
            float centerZ = 6.4f + i * 0.3f;
            float bottom = i * 0.25f;
            float frontZ = centerZ - depth * 0.5f;
            CreateBox("StepCarpetTop_" + (i + 1), parent, new Vector3(0, bottom + 0.268f, centerZ), new Vector3(4f, 0.025f, depth), MaterialAssets["CarpetStep"], false);
            CreateBox("StepCarpetRiser_" + (i + 1), parent, new Vector3(0, bottom + 0.125f, frontZ - 0.04f), new Vector3(4f, 0.25f, 0.025f), MaterialAssets["CarpetStep"], false);
        }
    }

    /// <summary>生成带红色坐垫、金色框架和放射状背饰的原创三维王座。</summary>
    private static void BuildThrone(Transform parent)
    {
        Transform throne = CreateGroup("GoldenThrone", parent);
        throne.localPosition = new Vector3(0, 0.79f, 7.1f);

        CreateBox("ThroneBase", throne, new Vector3(0, 0.43f, 0), new Vector3(1.95f, 0.22f, 1.6f), MaterialAssets["Gold"], true);
        CreateBox("ThroneSeatCushion", throne, new Vector3(0, 0.65f, -0.03f), new Vector3(1.7f, 0.28f, 1.4f), MaterialAssets["Velvet"], true);
        CreateBox("ThroneBackCushion", throne, new Vector3(0, 1.65f, 0.55f), new Vector3(1.55f, 1.5f, 0.25f), MaterialAssets["Velvet"], true);
        CreateSphere("ThroneArchCushion", throne, new Vector3(0, 2.25f, 0.55f), new Vector3(1.55f, 1.1f, 0.25f), MaterialAssets["Velvet"]);
        for (int i = 0; i < 10; i++)
        {
            float angleA = i / 10f * Mathf.PI;
            float angleB = (i + 1) / 10f * Mathf.PI;
            Vector3 pointA = new Vector3(Mathf.Cos(angleA) * 0.9f, 2.25f + Mathf.Sin(angleA) * 0.85f, 0.55f);
            Vector3 pointB = new Vector3(Mathf.Cos(angleB) * 0.9f, 2.25f + Mathf.Sin(angleB) * 0.85f, 0.55f);
            CreateRodBetween("ThroneArchFrame", throne, pointA, pointB, 0.12f, MaterialAssets["LightGold"]);
        }

        foreach (float side in new[] { -1f, 1f })
        {
            CreateCylinder("ThroneBackPost", throne, new Vector3(side * 0.9f, 1.7f, 0.55f), new Vector3(0.18f, 2.75f, 0.18f), MaterialAssets["Gold"], false);
            CreateSphere("ThronePostFinial", throne, new Vector3(side * 0.9f, 3.15f, 0.55f), Vector3.one * 0.3f, MaterialAssets["LightGold"]);
            CreateBox("ThroneArmrest", throne, new Vector3(side * 1f, 1.05f, -0.02f), new Vector3(0.17f, 0.17f, 1.5f), MaterialAssets["Gold"], false);
            CreateSphere("ThroneArmFinial", throne, new Vector3(side * 1f, 1.08f, -0.78f), Vector3.one * 0.25f, MaterialAssets["LightGold"]);
            foreach (float z in new[] { -0.5f, 0.5f })
                CreateCylinder("ThroneLeg", throne, new Vector3(side * 0.75f, 0.23f, z), new Vector3(0.18f, 0.46f, 0.18f), MaterialAssets["Gold"], false);
        }

        Vector3 rayOrigin = new Vector3(0, 2.05f, 0.78f);
        for (int i = 0; i < 11; i++)
        {
            float angle = Mathf.Lerp(28f, 152f, i / 10f) * Mathf.Deg2Rad;
            float length = i % 2 == 0 ? 1.65f : 1.35f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            Vector3 end = rayOrigin + direction * length;
            CreateRodBetween("ThroneSunRay", throne, rayOrigin, end, 0.065f, MaterialAssets["Gold"]);
            CreateConeBetween("ThroneSunRayTip", throne, end, end + direction * 0.25f, 0.16f, MaterialAssets["LightGold"]);
        }
        CreateSphere("ThroneCrest", throne, new Vector3(0, 3.45f, 0.72f), new Vector3(0.36f, 0.36f, 0.2f), MaterialAssets["LightGold"]);

        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                CreateSphere("ThroneBackStud", throne, new Vector3((column - 1) * 0.43f, 1.35f + row * 0.48f, 0.39f), Vector3.one * 0.075f, MaterialAssets["Gem"]);
    }

    #endregion

    #region 装饰生成与摆放

    /// <summary>生成并成对摆放烛台、龙雕像、花盆与旗帜。</summary>
    private static void BuildDecorations(Transform parent)
    {
        GameObject candelabraPrefab = CreateCandelabraPrefab();
        SpawnPrefab(candelabraPrefab, "Candelabra_FL", parent, new Vector3(-5.5f, 0, 1.1f), Vector3.zero, Vector3.one * 1.2f);
        SpawnPrefab(candelabraPrefab, "Candelabra_FR", parent, new Vector3(5.5f, 0, 1.1f), Vector3.zero, Vector3.one * 1.2f);
        SpawnPrefab(candelabraPrefab, "Candelabra_BL", parent, new Vector3(-2.55f, 0.79f, 7.65f), Vector3.zero, Vector3.one * 0.9f);
        SpawnPrefab(candelabraPrefab, "Candelabra_BR", parent, new Vector3(2.55f, 0.79f, 7.65f), Vector3.zero, Vector3.one * 0.9f);

        GameObject statuePrefab = CreateDragonStatuePrefab();
        SpawnPrefab(statuePrefab, "DragonStatue_L", parent, new Vector3(-4.25f, 0, 7.6f), Vector3.zero, Vector3.one * 0.9f);
        SpawnPrefab(statuePrefab, "DragonStatue_R", parent, new Vector3(4.25f, 0, 7.6f), Vector3.zero, Vector3.one * 0.9f);

        GameObject flowerPotPrefab = CreateFlowerPotPrefab();
        SpawnPrefab(flowerPotPrefab, "FlowerPot_RearL", parent, new Vector3(-4f, 0, 5.5f), Vector3.zero, Vector3.one);
        SpawnPrefab(flowerPotPrefab, "FlowerPot_RearR", parent, new Vector3(4f, 0, 5.5f), Vector3.zero, Vector3.one);
        SpawnPrefab(flowerPotPrefab, "FlowerPot_SideL", parent, new Vector3(-7.25f, 0, 3.8f), Vector3.zero, Vector3.one * 1.25f);
        SpawnPrefab(flowerPotPrefab, "FlowerPot_SideR", parent, new Vector3(7.25f, 0, 3.8f), Vector3.zero, Vector3.one * 1.25f);

        CreateBanner("BannerCenter", parent, new Vector3(0, 6.05f, 10.18f), new Vector2(2.3f, 2.8f));
        CreateBanner("BannerLeft", parent, new Vector3(-8.05f, 5.75f, 10.18f), new Vector2(1.5f, 2.2f));
        CreateBanner("BannerRight", parent, new Vector3(8.05f, 5.75f, 10.18f), new Vector2(1.5f, 2.2f));
    }

    /// <summary>创建原创五臂金烛台 Prefab。</summary>
    private static GameObject CreateCandelabraPrefab()
    {
        Transform root = CreateGroup("Palace_Candelabra_Source", null);
        CreateCylinder("CandelabraBaseWide", root, new Vector3(0, 0.06f, 0), new Vector3(0.7f, 0.12f, 0.7f), MaterialAssets["Gold"], false);
        CreateCylinder("CandelabraBaseInset", root, new Vector3(0, 0.16f, 0), new Vector3(0.45f, 0.12f, 0.45f), MaterialAssets["LightGold"], false);
        CreateCylinder("CandelabraStem", root, new Vector3(0, 0.76f, 0), new Vector3(0.13f, 1.15f, 0.13f), MaterialAssets["Gold"], false);
        CreateSphere("CandelabraStemOrb", root, new Vector3(0, 1.18f, 0), Vector3.one * 0.26f, MaterialAssets["LightGold"]);

        float[] xs = { 0f, -0.36f, 0.36f, -0.64f, 0.64f };
        float[] baseHeights = { 1.55f, 1.34f, 1.34f, 1.13f, 1.13f };
        for (int i = 0; i < xs.Length; i++)
        {
            float x = xs[i];
            float baseY = baseHeights[i];
            if (i == 0)
            {
                CreateRodBetween("CenterCandleArm", root, new Vector3(0, 1.2f, 0), new Vector3(0, baseY, 0), 0.065f, MaterialAssets["Gold"]);
            }
            else
            {
                Vector3 middle = new Vector3(x * 0.68f, baseY - 0.22f, 0);
                CreateRodBetween("CandleArmLower", root, new Vector3(0, 1.02f, 0), middle, 0.06f, MaterialAssets["Gold"]);
                CreateRodBetween("CandleArmUpper", root, middle, new Vector3(x, baseY, 0), 0.06f, MaterialAssets["Gold"]);
            }

            CreateCylinder("CandleCup", root, new Vector3(x, baseY, 0), new Vector3(0.2f, 0.08f, 0.2f), MaterialAssets["LightGold"], false);
            CreateCylinder("CandleBody", root, new Vector3(x, baseY + 0.24f, 0), new Vector3(0.12f, 0.44f, 0.12f), MaterialAssets["Candle"], false);
            CreateCylinder("CandleWick", root, new Vector3(x, baseY + 0.48f, 0), new Vector3(0.025f, 0.08f, 0.025f), MaterialAssets["Wick"], false);
            Vector3 flamePosition = new Vector3(x, baseY + 0.64f, 0);
            CreateQuad("CandleFlameFront", root, flamePosition, Vector3.zero, new Vector3(0.18f, 0.32f, 1), MaterialAssets["Flame"], ShadowCastingMode.Off);
            CreateQuad("CandleFlameSide", root, flamePosition, new Vector3(0, 90, 0), new Vector3(0.18f, 0.32f, 1), MaterialAssets["Flame"], ShadowCastingMode.Off);
        }

        CreatePointLight(root, "CandleWarmLight", new Vector3(0, 1.75f, -0.1f), new Color(1f, 0.48f, 0.15f), 1.35f, 4.2f);
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 1f, 0);
        collider.size = new Vector3(1.5f, 2.1f, 0.75f);
        return SavePrefabAndDestroy(root.gameObject, PrefabFolder + "/Palace_Candelabra.prefab");
    }

    /// <summary>创建带基座和双翼的原创风格化石像龙 Prefab。</summary>
    private static GameObject CreateDragonStatuePrefab()
    {
        Transform root = CreateGroup("Palace_DragonStatue_Source", null);
        CreateBox("StatuePlinth", root, new Vector3(0, 0.125f, 0), new Vector3(1.5f, 0.25f, 1.5f), MaterialAssets["StoneStatue"], false);
        CreateBox("StatuePedestal", root, new Vector3(0, 0.65f, 0), new Vector3(1.05f, 0.8f, 1.05f), MaterialAssets["StoneStatue"], false);
        CreateBox("StatuePedestalCap", root, new Vector3(0, 1.13f, 0), new Vector3(1.35f, 0.2f, 1.35f), MaterialAssets["StoneStatue"], false);
        CreateSphere("DragonBody", root, new Vector3(0, 1.85f, 0), new Vector3(0.8f, 1.1f, 0.65f), MaterialAssets["StoneStatue"]);
        CreateRodBetween("DragonNeck", root, new Vector3(0, 2.15f, -0.05f), new Vector3(0, 2.72f, -0.18f), 0.3f, MaterialAssets["StoneStatue"]);
        CreateSphere("DragonHead", root, new Vector3(0, 2.88f, -0.25f), new Vector3(0.55f, 0.45f, 0.6f), MaterialAssets["StoneStatue"]);
        CreateBox("DragonSnout", root, new Vector3(0, 2.78f, -0.55f), new Vector3(0.38f, 0.22f, 0.42f), MaterialAssets["StoneStatue"], false);
        foreach (float side in new[] { -1f, 1f })
        {
            CreateConeBetween("DragonHorn", root, new Vector3(side * 0.16f, 3.05f, -0.08f), new Vector3(side * 0.42f, 3.26f, 0.08f), 0.14f, MaterialAssets["StoneStatue"]);
            CreateRodBetween("DragonLeg", root, new Vector3(side * 0.24f, 1.65f, 0), new Vector3(side * 0.34f, 1.22f, -0.2f), 0.22f, MaterialAssets["StoneStatue"]);
            CreateConeBetween("DragonClaw", root, new Vector3(side * 0.34f, 1.24f, -0.2f), new Vector3(side * 0.38f, 1.2f, -0.48f), 0.13f, MaterialAssets["StoneStatue"]);
            CreateSphere("DragonEye", root, new Vector3(side * 0.14f, 2.94f, -0.49f), Vector3.one * 0.055f, MaterialAssets["StoneDark"]);
        }
        CreateRodBetween("DragonTailBase", root, new Vector3(0, 1.65f, 0.24f), new Vector3(0.3f, 1.42f, 0.65f), 0.25f, MaterialAssets["StoneStatue"]);
        CreateConeBetween("DragonTailTip", root, new Vector3(0.3f, 1.42f, 0.65f), new Vector3(0.58f, 1.58f, 1f), 0.22f, MaterialAssets["StoneStatue"]);
        CreateMeshObject("DragonWingRight", MeshAssets["Wing"], MaterialAssets["StoneStatue"], root, new Vector3(0.2f, 2f, 0.15f), Vector3.zero, Vector3.one, false, ShadowCastingMode.TwoSided);
        CreateMeshObject("DragonWingLeft", MeshAssets["Wing"], MaterialAssets["StoneStatue"], root, new Vector3(-0.2f, 2f, 0.15f), Vector3.zero, new Vector3(-1, 1, 1), false, ShadowCastingMode.TwoSided);
        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 wingRoot = new Vector3(side * 0.2f, 2f, 0.06f);
            CreateRodBetween("DragonWingRibTop", root, wingRoot, new Vector3(side * 1.45f, 3.45f, 0.06f), 0.045f, MaterialAssets["StoneStatue"]);
            CreateRodBetween("DragonWingRibLower", root, wingRoot, new Vector3(side * 1.35f, 2.05f, 0.06f), 0.045f, MaterialAssets["StoneStatue"]);
        }
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 1.75f, 0);
        collider.size = new Vector3(2.8f, 3.5f, 1.5f);
        return SavePrefabAndDestroy(root.gameObject, PrefabFolder + "/Palace_DragonStatue.prefab");
    }

    /// <summary>创建原创旋转体花盆、低多边形绿植与花卉 Prefab。</summary>
    private static GameObject CreateFlowerPotPrefab()
    {
        Transform root = CreateGroup("Palace_FlowerPot_Source", null);
        CreateMeshObject("FlowerPot", MeshAssets["FlowerPot"], MaterialAssets["Pot"], root, Vector3.zero, Vector3.zero, Vector3.one * 1.2f, false, ShadowCastingMode.On);
        CreateCylinder("PotDirt", root, new Vector3(0, 0.7f, 0), new Vector3(0.72f, 0.05f, 0.72f), MaterialAssets["Dirt"], false);
        for (int i = 0; i < 9; i++)
        {
            float angle = i / 9f * 360f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(radians) * 0.2f, 0.86f + (i % 3) * 0.06f, Mathf.Sin(radians) * 0.2f);
            Vector3 scale = new Vector3(0.18f, 0.48f, 0.13f);
            Vector3 rotation = new Vector3(Mathf.Sin(radians) * 25f, angle, Mathf.Cos(radians) * 25f);
            CreateMeshObject("PlantLeaf", MeshAssets["Sphere"], MaterialAssets[i % 2 == 0 ? "Leaf" : "LeafLight"], root, position, rotation, scale, false, ShadowCastingMode.On);
        }
        for (int i = 0; i < 5; i++)
        {
            float angle = (i / 5f * 360f + 30f) * Mathf.Deg2Rad;
            Vector3 flowerPosition = new Vector3(Mathf.Cos(angle) * 0.27f, 1.12f + (i % 2) * 0.1f, Mathf.Sin(angle) * 0.27f);
            CreateRodBetween("FlowerStem", root, new Vector3(0, 0.75f, 0), flowerPosition, 0.035f, MaterialAssets["Leaf"]);
            CreateSphere("FlowerBloom", root, flowerPosition, new Vector3(0.16f, 0.12f, 0.16f), MaterialAssets[i % 2 == 0 ? "FlowerPink" : "FlowerWhite"]);
        }
        BoxCollider collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.55f, 0);
        collider.size = new Vector3(1.1f, 1.25f, 1.1f);
        return SavePrefabAndDestroy(root.gameObject, PrefabFolder + "/Palace_FlowerPot.prefab");
    }

    /// <summary>创建一面带金色横杆的纹章旗帜。</summary>
    private static void CreateBanner(string name, Transform parent, Vector3 position, Vector2 size)
    {
        Transform banner = CreateGroup(name, parent);
        banner.localPosition = position;
        CreateMeshObject("BannerCloth", MeshAssets["Banner"], MaterialAssets["Banner"], banner, Vector3.zero, Vector3.zero, new Vector3(size.x, size.y, 1), false, ShadowCastingMode.TwoSided);
        CreateRodBetween("BannerGoldRod", banner, new Vector3(-size.x * 0.62f, 0.04f, 0), new Vector3(size.x * 0.62f, 0.04f, 0), 0.09f, MaterialAssets["Gold"]);
        CreateSphere("BannerRodFinialL", banner, new Vector3(-size.x * 0.66f, 0.04f, 0), Vector3.one * 0.14f, MaterialAssets["LightGold"]);
        CreateSphere("BannerRodFinialR", banner, new Vector3(size.x * 0.66f, 0.04f, 0), Vector3.one * 0.14f, MaterialAssets["LightGold"]);
    }

    #endregion

    #region 灯光、相机与环境

    /// <summary>生成暖色主光与低强度冷色补光。</summary>
    private static void BuildLighting(Transform parent)
    {
        GameObject mainObject = new GameObject("MainLight");
        mainObject.transform.SetParent(parent, false);
        mainObject.transform.localEulerAngles = new Vector3(52, -32, 0);
        Light mainLight = mainObject.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.color = new Color(1f, 0.79f, 0.58f);
        mainLight.intensity = 1.2f;
        mainLight.shadows = LightShadows.Soft;
        mainLight.shadowStrength = 0.78f;
        RenderSettings.sun = mainLight;

        GameObject fillObject = new GameObject("CoolFillLight");
        fillObject.transform.SetParent(parent, false);
        fillObject.transform.localEulerAngles = new Vector3(28, 145, 0);
        Light fillLight = fillObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.43f, 0.49f, 0.72f);
        fillLight.intensity = 0.25f;
        fillLight.shadows = LightShadows.None;
    }

    /// <summary>生成固定斜俯视透视相机和独立全局 Volume。</summary>
    private static void BuildCameraAndVolume(Transform parent, VolumeProfile profile)
    {
        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0, 10, -12);
        cameraObject.transform.LookAt(new Vector3(0, 2.2f, 4));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 36f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.025f, 0.05f);
        camera.allowHDR = true;
        camera.allowMSAA = true;
        UniversalAdditionalCameraData cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null) cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask = 1;
        cameraObject.AddComponent<AudioListener>();

        GameObject volumeObject = new GameObject("PalaceVolume");
        volumeObject.transform.SetParent(parent, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.weight = 1;
        volume.profile = profile;
    }

    /// <summary>配置只保存在当前场景中的环境光与线性雾。</summary>
    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.27f, 0.42f);
        RenderSettings.ambientIntensity = 0.7f;
        RenderSettings.reflectionIntensity = 0.25f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.12f, 0.085f, 0.16f);
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 50f;
    }

    #endregion

    #region 场景物体与 Prefab 工具

    /// <summary>创建空分组并重置本地变换。</summary>
    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        if (parent != null) gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        return gameObject.transform;
    }

    /// <summary>创建带盒体网格的物体。</summary>
    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool addCollider)
    {
        return CreateMeshObject(name, MeshAssets["Box"], material, parent, position, Vector3.zero, scale, addCollider, ShadowCastingMode.On);
    }

    /// <summary>创建圆柱物体，可附带胶囊碰撞。</summary>
    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool addCapsuleCollider)
    {
        GameObject gameObject = CreateMeshObject(name, MeshAssets["Cylinder"], material, parent, position, Vector3.zero, scale, false, ShadowCastingMode.On);
        if (addCapsuleCollider)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.5f;
            collider.height = 1f;
        }
        return gameObject;
    }

    /// <summary>创建低多边形球体物体。</summary>
    private static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return CreateMeshObject(name, MeshAssets["Sphere"], material, parent, position, Vector3.zero, scale, false, ShadowCastingMode.On);
    }

    /// <summary>创建朝向负 Z 的纹理面片。</summary>
    private static GameObject CreateQuad(string name, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale, Material material, ShadowCastingMode shadowMode)
    {
        return CreateMeshObject(name, MeshAssets["Quad"], material, parent, position, rotation, scale, false, shadowMode);
    }

    /// <summary>创建使用专属网格和材质的场景物体。</summary>
    private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale, bool addBoxCollider, ShadowCastingMode shadowMode)
    {
        GameObject gameObject = new GameObject(name);
        if (parent != null) gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localEulerAngles = rotation;
        gameObject.transform.localScale = scale;
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = shadowMode;
        renderer.receiveShadows = shadowMode != ShadowCastingMode.Off;
        if (addBoxCollider)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = mesh.bounds.size;
        }
        return gameObject;
    }

    /// <summary>在两点之间创建圆柱杆件。</summary>
    private static GameObject CreateRodBetween(string name, Transform parent, Vector3 start, Vector3 end, float diameter, Material material)
    {
        Vector3 delta = end - start;
        Vector3 rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized).eulerAngles;
        return CreateMeshObject(name, MeshAssets["Cylinder"], material, parent, (start + end) * 0.5f, rotation, new Vector3(diameter, delta.magnitude, diameter), false, ShadowCastingMode.On);
    }

    /// <summary>在两点之间创建底面朝起点、尖端朝终点的圆锥。</summary>
    private static GameObject CreateConeBetween(string name, Transform parent, Vector3 start, Vector3 end, float diameter, Material material)
    {
        Vector3 delta = end - start;
        Vector3 rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized).eulerAngles;
        return CreateMeshObject(name, MeshAssets["Cone"], material, parent, (start + end) * 0.5f, rotation, new Vector3(diameter, delta.magnitude, diameter), false, ShadowCastingMode.On);
    }

    /// <summary>在指定父物体下创建暖色点光。</summary>
    private static Light CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        return light;
    }

    /// <summary>把临时场景物体保存为专属 Prefab，随后销毁临时物体。</summary>
    private static GameObject SavePrefabAndDestroy(GameObject source, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
        UnityEngine.Object.DestroyImmediate(source);
        if (prefab == null) throw new System.InvalidOperationException("无法保存 Prefab：" + path);
        return prefab;
    }

    /// <summary>实例化专属 Prefab 并设置本地变换。</summary>
    private static GameObject SpawnPrefab(GameObject prefab, string name, Transform parent, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (gameObject == null) throw new System.InvalidOperationException("无法实例化 Prefab：" + prefab.name);
        gameObject.name = name;
        gameObject.transform.localPosition = position;
        gameObject.transform.localEulerAngles = rotation;
        gameObject.transform.localScale = scale;
        return gameObject;
    }

    #endregion
}
