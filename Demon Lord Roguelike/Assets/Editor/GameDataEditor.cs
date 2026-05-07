using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Spine.Unity;

public class GameDataEditor : EditorWindow
{
    private SkeletonDataAsset targetSkeletonDataAsset;

    private readonly Color _colorBatch   = new Color(0.30f, 0.55f, 0.90f); // 批量操作蓝
    private readonly Color _colorRefresh = new Color(0.55f, 0.55f, 0.55f); // 刷新灰
    private readonly Color _colorAll     = new Color(0.20f, 0.75f, 0.35f); // 一键生成绿
    private readonly Color _colorItem    = new Color(0.90f, 0.55f, 0.25f); // 道具橙
    private readonly Color _colorSkin    = new Color(0.75f, 0.40f, 0.80f); // 皮肤紫

    [MenuItem("游戏/Spine")]
    public static void ShowWindow()
    {
        GetWindow<GameDataEditor>("Spine 资源生成", typeof(SceneView)).minSize = new Vector2(340, 420);
    }

    private void OnGUI()
    {
        // 标题
        GUILayout.Space(12);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize  = 16
        };
        GUILayout.Label("Spine 资源生成工具", titleStyle);
        GUILayout.Space(4);

        GUIStyle subStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 11
        };
        GUILayout.Label("批量导出 & 指定资源导出", subStyle);
        GUILayout.Space(16);

        // ---------- 批量操作 ----------
        DrawSectionBox("批量操作", () =>
        {
            DrawButton("生成所有 Spine 道具图标", _colorItem, 32, SpineAllItemInit);
            GUILayout.Space(6);
            DrawButton("生成所有 Spine 皮肤图标", _colorSkin, 32, SpineAllSkinInit);
            GUILayout.Space(6);
            DrawButton("刷新所有图集", _colorRefresh, 32, RefreshAllAtlases);
            GUILayout.Space(10);
            DrawButton("一键生成所有资源", _colorAll, 38, GenerateAllResources);
        });

        GUILayout.Space(16);

        // ---------- 指定 SkeletonData 单独导出 ----------
        DrawSectionBox("指定 SkeletonData 单独导出", () =>
        {
            EditorGUILayout.LabelField("选择目标资源", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            targetSkeletonDataAsset = EditorGUILayout.ObjectField(
                targetSkeletonDataAsset,
                typeof(SkeletonDataAsset),
                false
            ) as SkeletonDataAsset;
            EditorGUILayout.EndHorizontal();

            if (targetSkeletonDataAsset != null)
            {
                GUIStyle nameStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 12
                };
                GUILayout.Space(4);
                GUILayout.Label($"已选中: {targetSkeletonDataAsset.name}", nameStyle);
            }
            else
            {
                GUIStyle hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 11
                };
                GUILayout.Space(4);
                GUILayout.Label("请在上方拖拽或选择一个 SkeletonDataAsset", hintStyle);
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(targetSkeletonDataAsset == null);
            DrawButton("导出选中 — 道具图标", _colorItem, 32, () => SpineSelectedItemInit(targetSkeletonDataAsset));
            GUILayout.Space(6);
            DrawButton("导出选中 — 皮肤图标", _colorSkin, 32, () => SpineSelectedSkinInit(targetSkeletonDataAsset));
            EditorGUI.EndDisabledGroup();
        });

        GUILayout.Space(16);
    }

    #region UI Helpers

    private void DrawSectionBox(string header, System.Action content)
    {
        Rect rect = EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            GUILayout.Space(8);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(8, 8, 0, 4)
            };
            GUILayout.Label(header, headerStyle);

            GUILayout.Space(4);
            Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.4f));
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();
            content?.Invoke();
            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawButton(string label, Color color, float height, System.Action onClick)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        if (GUILayout.Button(label, GUILayout.Height(height)))
        {
            onClick?.Invoke();
        }
        GUI.backgroundColor = prev;
    }

    #endregion

    #region Export Logic

    public static void SpineAllItemInit()
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Items";
        string filterSkinName = "Clothes,Pants,Weapon,Shoes,Hat,Mask,NoseRing,Arrow";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, null, true, null, filterSkinName);

        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForItems.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            LogUtil.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void SpineAllSkinInit()
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Skins";
        string filterSkinName = "Eye,Head,Mouth,Body,Hair,Horn,Wing";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, null, true, null, filterSkinName);

        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForSkins.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            LogUtil.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void SpineSelectedItemInit(SkeletonDataAsset skeletonDataAsset)
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Items";
        string filterSkinName = "Clothes,Pants,Weapon,Shoes,Hat,Mask,NoseRing,Arrow";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, skeletonDataAsset, true, null, filterSkinName);

        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForItems.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            LogUtil.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void SpineSelectedSkinInit(SkeletonDataAsset skeletonDataAsset)
    {
        string inputPath = "Assets/LoadResources/Spine/Creature";
        string outputPath = "Assets/LoadResources/Textures/Skins";
        string filterSkinName = "Eye,Head,Mouth,Body,Hair,Horn,Wing";//筛选名字
        SpineWindow.ExtractSkinTextures(inputPath, outputPath, skeletonDataAsset, true, null, filterSkinName);

        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForSkins.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
        if (atlas != null)
        {
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            LogUtil.Log($"已重新生成图集: {atlas.name}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RefreshAllAtlases()
    {
        string targetPath = "Assets/LoadResources/Textures/SpriteAtlas";
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { targetPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            
            if (atlas != null)
            {
                SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
                LogUtil.Log($"已重新生成图集: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void GenerateAllResources()
    {
        LogUtil.Log("========== 开始生成所有资源 ==========");
        
        SpineAllItemInit();
        SpineAllSkinInit();
        RefreshAllAtlases();
        
        LogUtil.Log("========== 所有资源生成完成 ==========");
    }

    #endregion
}
