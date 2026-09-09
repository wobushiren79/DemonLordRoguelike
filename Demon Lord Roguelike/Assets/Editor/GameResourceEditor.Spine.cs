using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Spine.Unity;

/// <summary>
/// GameResourceEditor Spine 板块（partial）：Spine 皮肤纹理提取（道具图标/皮肤图标，批量与指定 SkeletonData 单独导出）、Spine 资源导入（外部美术目录覆盖导入项目内 Spine 目录）
/// </summary>
public partial class GameResourceEditor
{
    #region 字段

    /// <summary>单独导出时选中的目标 SkeletonDataAsset</summary>
    private SkeletonDataAsset targetSkeletonDataAsset;

    /// <summary>导入项列表的 EditorPrefs 键</summary>
    private const string EditorPrefsKeySpineImportEntries = "GameResourceEditor.SpineImportEntries";

    /// <summary>Spine 资源导入项列表（外部美术目录 → 项目内 Spine 目录）</summary>
    private List<SpineImportEntry> spineImportEntries = new List<SpineImportEntry>();

    /// <summary>单条 Spine 资源导入配置</summary>
    [Serializable]
    private class SpineImportEntry
    {
        /// <summary>目标目录：外部美术资源目录（绝对路径，如 ../资源/生物/人类）</summary>
        public string sourceDir;
        /// <summary>导入目录：项目内 Spine 目录（Assets/ 开头的相对路径，如 Assets/LoadResources/Spine/Creature/Human）</summary>
        public string importDir;
    }

    /// <summary>导入配置列表（JsonUtility 序列化包装）</summary>
    [Serializable]
    private class SpineImportEntryList
    {
        public List<SpineImportEntry> entries = new List<SpineImportEntry>();
    }

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        string json = EditorPrefs.GetString(EditorPrefsKeySpineImportEntries, "");
        if (!string.IsNullOrEmpty(json))
        {
            SpineImportEntryList list = JsonUtility.FromJson<SpineImportEntryList>(json);
            spineImportEntries = list?.entries ?? new List<SpineImportEntry>();
        }
    }

    #endregion

    #region Spine板块UI

    /// <summary>
    /// 绘制 Spine 板块页签
    /// </summary>
    private void DrawSpineTab()
    {
        // ---------- 批量生成图标 ----------
        DrawSectionBox("批量生成图标", () =>
        {
            DrawButton("生成所有 Spine 道具图标", _colorItem, 32, SpineAllItemInit);
            GUILayout.Space(6);
            DrawButton("生成所有 Spine 皮肤图标", _colorSkin, 32, SpineAllSkinInit);
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

        // ---------- Spine 资源导入 ----------
        DrawSpineImportSection();
    }

    #endregion

    #region Spine资源导入UI

    /// <summary>
    /// 绘制 Spine 资源导入区块：导入项列表 + 添加/全部导入按钮
    /// </summary>
    private void DrawSpineImportSection()
    {
        DrawSectionBox("Spine 资源导入", () =>
        {
            GUIStyle hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
                wordWrap  = true
            };
            GUILayout.Label("把目标目录中与导入目录同名的 .atlas.txt / .json / .png 覆盖导入；导入目录没有的文件不复制", hintStyle);
            GUILayout.Space(8);

            if (spineImportEntries.Count == 0)
            {
                GUILayout.Label("暂无导入项，点击下方「添加导入项」", hintStyle);
            }

            // 逐条绘制导入项（删除延后到循环外执行，避免绘制途中改列表）
            int removeIndex = -1;
            for (int i = 0; i < spineImportEntries.Count; i++)
            {
                if (DrawSpineImportEntry(i, spineImportEntries[i]))
                {
                    removeIndex = i;
                }
                if (i < spineImportEntries.Count - 1)
                {
                    GUILayout.Space(6);
                }
            }
            if (removeIndex >= 0)
            {
                spineImportEntries.RemoveAt(removeIndex);
                SaveSpineImportEntries();
                GUIUtility.ExitGUI();
            }

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加导入项", GUILayout.Height(26)))
            {
                spineImportEntries.Add(new SpineImportEntry());
                SaveSpineImportEntries();
            }
            if (GUILayout.Button("保存配置", GUILayout.Height(26), GUILayout.Width(70)))
            {
                SaveSpineImportEntries();
                LogUtil.Log("Spine 资源导入配置已保存");
                ShowNotification(new GUIContent("导入配置已保存"));
            }
            EditorGUI.BeginDisabledGroup(spineImportEntries.Count == 0);
            DrawButton("执行全部导入", _colorAll, 26, ImportAllSpineEntries);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        });
    }

    /// <summary>
    /// 绘制单条导入项（目标目录/导入目录两行 + 导入/删除按钮）
    /// </summary>
    /// <returns>本次点击了「删除」</returns>
    private bool DrawSpineImportEntry(int index, SpineImportEntry entry)
    {
        bool isRemove = false;
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.LabelField($"导入项 {index + 1}", EditorStyles.miniBoldLabel);

            // 目录行只返回新值，统一在赋值写回 entry 后再保存（在 DrawDirRow 内部保存会先于赋值执行，序列化进去的总是旧值）
            string newSourceDir = DrawDirRow("目标目录", "外部美术资源目录（绝对路径），如 项目上级/资源/生物/人类", entry.sourceDir, true);
            if (newSourceDir != entry.sourceDir)
            {
                entry.sourceDir = newSourceDir;
                SaveSpineImportEntries();
            }
            string newImportDir = DrawDirRow("导入目录", "项目内 Spine 目录（Assets/ 开头）", entry.importDir, false);
            if (newImportDir != entry.importDir)
            {
                entry.importDir = newImportDir;
                SaveSpineImportEntries();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(entry.sourceDir) || string.IsNullOrEmpty(entry.importDir));
            if (GUILayout.Button("导入", GUILayout.Width(50)))
            {
                int count = ImportSpineResources(entry.sourceDir, entry.importDir);
                AssetDatabase.Refresh();
                LogUtil.Log($"Spine 资源导入完成（{entry.importDir}）：覆盖 {count} 个文件");
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                isRemove = true;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        return isRemove;
    }

    /// <summary>
    /// 绘制一行目录选择（文本框 + 浏览按钮，支持拖拽文件夹到本行）。只返回最新值，不写回也不保存——保存由调用处在赋值后进行，否则序列化进去的是旧值
    /// </summary>
    /// <param name="isExternal">true=外部目录（存绝对路径）；false=项目内目录（浏览/拖拽结果转为 Assets/ 相对路径并校验）</param>
    private string DrawDirRow(string label, string tooltip, string value, bool isExternal)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(label, tooltip + "；支持拖拽文件夹到本行"));
        value = EditorGUILayout.TextField(value);
        if (GUILayout.Button("浏览", GUILayout.Width(44)))
        {
            string picked = EditorUtility.OpenFolderPanel($"选择{label}", Application.dataPath, "");
            if (!string.IsNullOrEmpty(picked))
            {
                picked = picked.Replace('\\', '/');
                if (isExternal)
                {
                    value = picked;
                }
                else
                {
                    // 项目内目录统一存 Assets/ 相对路径（全限定名：框架层有同名 FileUtil 会遮蔽 UnityEditor.FileUtil）
                    string rel = UnityEditor.FileUtil.GetProjectRelativePath(picked);
                    if (rel.StartsWith("Assets"))
                    {
                        value = rel;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Spine 资源导入", "导入目录必须选择项目 Assets/ 内的目录", "确定");
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 拖拽文件夹（或文件，取所在目录）到本行设置路径
        string droppedDir = GetDroppedDir(GUILayoutUtility.GetLastRect());
        if (droppedDir != null)
        {
            if (isExternal)
            {
                // 目标目录是外部美术目录，统一存绝对路径
                value = Path.GetFullPath(droppedDir).Replace('\\', '/');
            }
            else
            {
                // 导入目录必须落在项目 Assets/ 内，统一存相对路径
                string rel = droppedDir.StartsWith("Assets") ? droppedDir : UnityEditor.FileUtil.GetProjectRelativePath(droppedDir);
                if (rel.StartsWith("Assets"))
                {
                    value = rel;
                }
                else
                {
                    EditorUtility.DisplayDialog("Spine 资源导入", "导入目录必须选择项目 Assets/ 内的目录", "确定");
                }
            }
        }
        return value;
    }

    /// <summary>
    /// 处理目录拖拽：悬停指定区域时显示可复制光标，松开返回拖拽得到的目录路径（拖入文件则取其所在目录），无有效拖拽返回 null
    /// </summary>
    private static string GetDroppedDir(Rect dropRect)
    {
        Event evt = Event.current;
        if (evt == null || !dropRect.Contains(evt.mousePosition)) return null;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return null;
        if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) return null;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        string result = null;
        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            string path = DragAndDrop.paths[0].Replace('\\', '/');
            if (Directory.Exists(path)) result = path;
            else if (File.Exists(path)) result = Path.GetDirectoryName(path).Replace('\\', '/');
        }
        evt.Use();
        return result;
    }

    /// <summary>
    /// 把导入项列表写入 EditorPrefs
    /// </summary>
    private void SaveSpineImportEntries()
    {
        SpineImportEntryList list = new SpineImportEntryList { entries = spineImportEntries };
        EditorPrefs.SetString(EditorPrefsKeySpineImportEntries, JsonUtility.ToJson(list));
    }

    #endregion

    #region Spine资源导入逻辑

    /// <summary>
    /// 执行全部导入项，完成后刷新资源并弹汇总提示
    /// </summary>
    private void ImportAllSpineEntries()
    {
        int totalCount = 0;
        foreach (SpineImportEntry entry in spineImportEntries)
        {
            totalCount += ImportSpineResources(entry.sourceDir, entry.importDir);
        }
        AssetDatabase.Refresh();
        LogUtil.Log($"========== Spine 资源全部导入完成：共覆盖 {totalCount} 个文件 ==========");
        EditorUtility.DisplayDialog("Spine 资源导入", $"全部导入完成，共覆盖 {totalCount} 个文件", "确定");
    }

    /// <summary>
    /// 执行单条导入：把目标目录中与导入目录同名的 .atlas.txt/.json/.png 覆盖复制到导入目录（导入目录没有的文件不复制）
    /// </summary>
    /// <returns>实际覆盖导入的文件数</returns>
    public static int ImportSpineResources(string sourceDir, string importDir)
    {
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            Debug.LogError($"Spine 导入跳过，目标目录不存在: {sourceDir}");
            return 0;
        }
        if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
        {
            Debug.LogError($"Spine 导入跳过，导入目录不存在: {importDir}");
            return 0;
        }

        // 以导入目录为准逐文件回查目标目录，天然实现「导入目录没有的资源不复制」
        int importCount = 0;
        foreach (string importFile in Directory.GetFiles(importDir))
        {
            string fileName = Path.GetFileName(importFile);
            if (!IsSpineImportFile(fileName)) continue;
            string sourceFile = Path.Combine(sourceDir, fileName);
            if (!File.Exists(sourceFile)) continue;
            File.Copy(sourceFile, importFile, true);
            importCount++;
            LogUtil.Log($"Spine 导入: {sourceFile} -> {importFile}");
        }
        return importCount;
    }

    /// <summary>
    /// 判断文件名是否为 Spine 导入目标类型（.atlas.txt 复合后缀 / .json / .png）
    /// </summary>
    private static bool IsSpineImportFile(string fileName)
    {
        return fileName.EndsWith(".atlas.txt", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Spine导出逻辑

    /// <summary>
    /// 从所有生物 Spine 提取道具类皮肤纹理到 Textures/Items，并重新打包 AtlasForItems 图集
    /// </summary>
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

    /// <summary>
    /// 从所有生物 Spine 提取皮肤类皮肤纹理到 Textures/Skins，并重新打包 AtlasForSkins 图集
    /// </summary>
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

    /// <summary>
    /// 从指定 SkeletonDataAsset 提取道具类皮肤纹理到 Textures/Items，并重新打包 AtlasForItems 图集
    /// </summary>
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

    /// <summary>
    /// 从指定 SkeletonDataAsset 提取皮肤类皮肤纹理到 Textures/Skins，并重新打包 AtlasForSkins 图集
    /// </summary>
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

    #endregion
}
