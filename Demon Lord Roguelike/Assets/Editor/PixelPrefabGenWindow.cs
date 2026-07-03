using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成像素预制 编辑器窗口
/// 固化"复制模板材质/预制 → 换贴图/材质"的固定流程：
/// 为每张选中的图片生成一个材质(_BaseMap = 该图)和一个预制(材质 = 生成的材质)。
/// 模板材质/预制默认取 Mat_Building_Tree_1 / Building_Tree_2D_1，保证 shader、参数与预制结构一致。
/// </summary>
public class PixelPrefabGenWindow : EditorWindow
{
    #region 常量与默认值

    /// <summary>默认模板材质路径</summary>
    private const string DefaultTemplateMatPath = "Assets/LoadResources/Materials/Building/Mat_Building_Tree_1.mat";

    /// <summary>默认模板预制路径</summary>
    private const string DefaultTemplatePrefabPath = "Assets/LoadResources/Prefabs/Tree/Building_Tree_2D_1.prefab";

    /// <summary>默认材质输出目录</summary>
    private const string DefaultMatDir = "Assets/LoadResources/Materials/Building";

    /// <summary>预制输出根目录（实际目录 = 该根目录/图片类别）</summary>
    private const string PrefabRootDir = "Assets/LoadResources/Prefabs";

    #endregion

    #region 内部数据

    /// <summary>单张图片对应的生成项</summary>
    private class GenItem
    {
        public Texture2D texture;
        public string matName;
        public string prefabName;
    }

    #endregion

    #region 成员变量

    /// <summary>模板材质（shader 与参数来源）</summary>
    private Material templateMat;

    /// <summary>模板预制（预制结构来源）</summary>
    private GameObject templatePrefab;

    /// <summary>材质输出目录</summary>
    private string matOutputDir = DefaultMatDir;

    /// <summary>预制输出目录（默认按图片类别自动派生）</summary>
    private string prefabOutputDir = PrefabRootDir;

    /// <summary>预制目录是否仍由图片类别自动派生（用户手动改过后置 false）</summary>
    private bool prefabDirAuto = true;

    /// <summary>待生成的图片项列表</summary>
    private readonly List<GenItem> items = new List<GenItem>();

    /// <summary>列表滚动位置</summary>
    private Vector2 scrollPos;

    #endregion

    #region 菜单项与窗口创建

    /// <summary>菜单项：游戏/生成像素预制</summary>
    [MenuItem("游戏/生成像素预制")]
    private static void OpenFromMenu()
    {
        ShowWindow();
    }

    /// <summary>右键 Project 资源菜单：Assets/生成像素预制（选中图片时可用）</summary>
    [MenuItem("Assets/生成像素预制", false, 20)]
    private static void OpenFromAssets()
    {
        ShowWindow();
    }

    /// <summary>右键菜单可用性校验：至少选中一张贴图</summary>
    [MenuItem("Assets/生成像素预制", true)]
    private static bool OpenFromAssetsValidate()
    {
        foreach (var obj in Selection.objects)
            if (obj is Texture2D) return true;
        return false;
    }

    /// <summary>创建/显示窗口并带入当前选中的贴图</summary>
    private static void ShowWindow()
    {
        var window = GetWindow<PixelPrefabGenWindow>();
        window.titleContent = new GUIContent("生成像素预制");
        window.minSize = new Vector2(600, 440);
        window.AddSelectedTextures();
        window.Show();
    }

    #endregion

    #region 生命周期

    /// <summary>启用时补齐默认模板</summary>
    private void OnEnable()
    {
        if (templateMat == null)
            templateMat = AssetDatabase.LoadAssetAtPath<Material>(DefaultTemplateMatPath);
        if (templatePrefab == null)
            templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplatePrefabPath);
    }

    #endregion

    #region GUI 绘制

    /// <summary>窗口主绘制</summary>
    private void OnGUI()
    {
        EditorGUILayout.Space();
        // 模板区：shader/参数与预制结构来源
        EditorGUILayout.LabelField("模板（shader/参数与预制结构来源）", EditorStyles.boldLabel);
        templateMat = (Material)EditorGUILayout.ObjectField("模板材质", templateMat, typeof(Material), false);
        templatePrefab = (GameObject)EditorGUILayout.ObjectField("模板预制", templatePrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        // 输出目录区
        EditorGUILayout.LabelField("输出路径", EditorStyles.boldLabel);
        DrawDirField("材质输出目录", ref matOutputDir);
        // 预制目录：用户一旦手动/拖拽/浏览改动即停止自动派生
        if (DrawDirField("预制输出目录", ref prefabOutputDir)) prefabDirAuto = false;

        EditorGUILayout.Space();
        // 图片列表工具条
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"图片列表（{items.Count}）", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加空行", GUILayout.Width(90))) items.Add(new GenItem());
            if (GUILayout.Button("从选中添加", GUILayout.Width(90))) AddSelectedTextures();
            if (GUILayout.Button("清空", GUILayout.Width(60))) items.Clear();
        }

        DrawTextureDropArea();
        DrawListHeader();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < items.Count; i++)
            DrawItemRow(i);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        // 生成按钮
        GUI.enabled = CanGenerate();
        if (GUILayout.Button("开始生成", GUILayout.Height(32)))
            Generate();
        GUI.enabled = true;
    }

    /// <summary>绘制目录字段（文本框 + 浏览/定位按钮 + 支持拖入文件夹）；返回本帧是否被用户改动</summary>
    private bool DrawDirField(string label, ref string dir)
    {
        bool changed = false;
        using (new EditorGUILayout.HorizontalScope())
        {
            var content = new GUIContent(label, "可将 Project 里的文件夹直接拖到此行设置路径");
            EditorGUI.BeginChangeCheck();
            dir = EditorGUILayout.TextField(content, dir);
            if (EditorGUI.EndChangeCheck()) changed = true;
            if (GUILayout.Button(new GUIContent("浏览", "打开系统文件夹选择框"), GUILayout.Width(50)))
            {
                string abs = EditorUtility.OpenFolderPanel(label, dir, "");
                string rel = ToAssetsRelative(abs);
                if (!string.IsNullOrEmpty(rel)) { dir = rel; changed = true; }
                else if (!string.IsNullOrEmpty(abs)) EditorUtility.DisplayDialog("路径无效", "请选择工程 Assets 目录内的文件夹", "确定");
            }
            if (GUILayout.Button(new GUIContent("定位", "在 Project 窗口高亮该目录"), GUILayout.Width(50)))
                PingFolder(dir);
        }
        // 整行作为拖放区：拖入文件夹即设置路径
        if (HandleFolderDragAndDrop(GUILayoutUtility.GetLastRect(), ref dir)) changed = true;
        return changed;
    }

    /// <summary>绘制图片拖放区：拖入贴图或含贴图的文件夹即批量加入列表</summary>
    private void DrawTextureDropArea()
    {
        var rect = GUILayoutUtility.GetRect(0, 38, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "把图片（或含图片的文件夹）拖到此处批量添加", EditorStyles.helpBox);
        var e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

        var texs = ExtractDroppedTextures();
        if (texs.Count == 0) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; return; }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var t in texs) AddTexture(t);
            UpdateAutoPrefabDir();
            GUI.changed = true;
        }
        e.Use();
    }

    /// <summary>列表表头</summary>
    private void DrawListHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("源图片", GUILayout.Width(150));
            EditorGUILayout.LabelField("材质名", GUILayout.Width(190));
            EditorGUILayout.LabelField("预制名", GUILayout.Width(190));
            EditorGUILayout.LabelField("", GUILayout.Width(24));
        }
    }

    /// <summary>绘制单行生成项</summary>
    private void DrawItemRow(int i)
    {
        var it = items[i];
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            var newTex = (Texture2D)EditorGUILayout.ObjectField(it.texture, typeof(Texture2D), false, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                it.texture = newTex;
                // 换图后若名字为空则按图片名智能派生
                if (newTex != null)
                {
                    if (string.IsNullOrEmpty(it.matName)) it.matName = DeriveMatName(newTex.name);
                    if (string.IsNullOrEmpty(it.prefabName)) it.prefabName = DerivePrefabName(newTex.name);
                }
                UpdateAutoPrefabDir();
            }
            it.matName = EditorGUILayout.TextField(it.matName, GUILayout.Width(190));
            it.prefabName = EditorGUILayout.TextField(it.prefabName, GUILayout.Width(190));
            if (GUILayout.Button("X", GUILayout.Width(24))) { items.RemoveAt(i); GUIUtility.ExitGUI(); }
        }
    }

    #endregion

    #region 列表操作

    /// <summary>把当前选中的贴图加入列表（去重并智能派生命名）</summary>
    private void AddSelectedTextures()
    {
        foreach (var obj in Selection.objects)
            AddTexture(obj as Texture2D);
        UpdateAutoPrefabDir();
    }

    /// <summary>把单张贴图加入列表（去重并智能派生命名）</summary>
    private void AddTexture(Texture2D tex)
    {
        if (tex == null) return;
        if (items.Exists(x => x.texture == tex)) return;
        items.Add(new GenItem
        {
            texture = tex,
            matName = DeriveMatName(tex.name),
            prefabName = DerivePrefabName(tex.name),
        });
    }

    /// <summary>当预制目录处于自动模式时，按首张图片类别设为 PrefabRootDir/&lt;类别&gt;</summary>
    private void UpdateAutoPrefabDir()
    {
        if (!prefabDirAuto) return;
        foreach (var it in items)
        {
            if (it.texture == null) continue;
            ParseName(it.texture.name, out var cat, out _);
            if (!string.IsNullOrEmpty(cat)) prefabOutputDir = $"{PrefabRootDir}/{cat}";
            break;
        }
    }

    /// <summary>从当前拖拽数据解析出所有贴图（拖入文件夹则取其内部全部贴图）</summary>
    private static List<Texture2D> ExtractDroppedTextures()
    {
        var result = new List<Texture2D>();
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (obj is Texture2D t) { result.Add(t); continue; }
            // 文件夹：收集内部所有贴图
            string p = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { p }))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                    if (tex != null && !result.Contains(tex)) result.Add(tex);
                }
            }
        }
        return result;
    }

    #endregion

    #region 命名派生

    /// <summary>从图片名解析 (类别, 序号)，序号可能为空</summary>
    private static void ParseName(string texName, out string category, out string number)
    {
        number = "";
        string baseName = texName;
        var m = Regex.Match(texName, @"^(.*?)_(\d+)$");
        if (m.Success) { baseName = m.Groups[1].Value; number = m.Groups[2].Value; }
        category = ToPascalCase(baseName);
    }

    /// <summary>派生材质名：Mat_Building_&lt;类别&gt;[_&lt;序号&gt;]</summary>
    private static string DeriveMatName(string texName)
    {
        ParseName(texName, out var cat, out var num);
        return string.IsNullOrEmpty(num) ? $"Mat_Building_{cat}" : $"Mat_Building_{cat}_{num}";
    }

    /// <summary>派生预制名：Building_&lt;类别&gt;_2D[_&lt;序号&gt;]</summary>
    private static string DerivePrefabName(string texName)
    {
        ParseName(texName, out var cat, out var num);
        return string.IsNullOrEmpty(num) ? $"Building_{cat}_2D" : $"Building_{cat}_2D_{num}";
    }

    /// <summary>下划线/空格/连字符分段转 PascalCase</summary>
    private static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in parts)
            sb.Append(char.ToUpper(p[0])).Append(p.Substring(1));
        return sb.ToString();
    }

    #endregion

    #region 生成逻辑

    /// <summary>是否满足生成条件</summary>
    private bool CanGenerate()
    {
        if (templateMat == null || templatePrefab == null) return false;
        if (string.IsNullOrEmpty(matOutputDir) || string.IsNullOrEmpty(prefabOutputDir)) return false;
        return items.Exists(x => x.texture != null && !string.IsNullOrEmpty(x.matName) && !string.IsNullOrEmpty(x.prefabName));
    }

    /// <summary>执行批量生成：复制模板材质/预制并换贴图/材质</summary>
    private void Generate()
    {
        string srcMatPath = AssetDatabase.GetAssetPath(templateMat);
        string srcPrefabPath = AssetDatabase.GetAssetPath(templatePrefab);
        EnsureFolder(matOutputDir);
        EnsureFolder(prefabOutputDir);

        var log = new StringBuilder();
        int ok = 0;
        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.texture == null || string.IsNullOrEmpty(it.matName) || string.IsNullOrEmpty(it.prefabName))
                { log.AppendLine($"[{i}] 跳过(缺图/缺名)"); continue; }

                // 材质：不存在则复制模板，随后换 _BaseMap（幂等）
                string matPath = $"{matOutputDir}/{it.matName}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
                    AssetDatabase.CopyAsset(srcMatPath, matPath);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) { log.AppendLine($"[{i}] 材质创建失败 {matPath}"); continue; }
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", it.texture);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", it.texture);
                EditorUtility.SetDirty(mat);

                // 预制：不存在则复制模板，随后换 MeshRenderer 材质
                string prefabPath = $"{prefabOutputDir}/{it.prefabName}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                    AssetDatabase.CopyAsset(srcPrefabPath, prefabPath);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                var mr = root.GetComponentInChildren<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);

                ok++;
                log.AppendLine($"[{i}] {it.texture.name} -> {it.matName} / {it.prefabName}");
            }
        }
        finally
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        Debug.Log($"[生成像素预制] 完成 {ok}/{items.Count}\n{log}");
        EditorUtility.DisplayDialog("生成像素预制", $"完成：成功 {ok} / 共 {items.Count}", "确定");
    }

    #endregion

    #region 工具方法

    /// <summary>在 Project 窗口高亮定位目录（不存在则提示）</summary>
    private static void PingFolder(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return;
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dir);
        if (obj == null)
        {
            EditorUtility.DisplayDialog("定位失败", $"目录不存在：{dir}\n（可先点\"浏览\"选择，或生成后再定位）", "确定");
            return;
        }
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }

    /// <summary>处理文件夹拖放：拖入区域内即把 dir 设为该文件夹（仅接受工程内文件夹）；返回是否已设置</summary>
    private static bool HandleFolderDragAndDrop(Rect area, ref string dir)
    {
        var e = Event.current;
        if (!area.Contains(e.mousePosition)) return false;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return false;

        string folder = ExtractDroppedFolder();
        if (folder == null) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; return false; }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        bool set = false;
        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            dir = folder;
            set = true;
            GUI.changed = true;
        }
        e.Use();
        return set;
    }

    /// <summary>从当前拖拽数据中解析出工程内文件夹路径（拖入文件则取其所在目录），无则返回 null</summary>
    private static string ExtractDroppedFolder()
    {
        // 从 Project 拖对象：DefaultAsset 文件夹
        foreach (var obj in DragAndDrop.objectReferences)
        {
            string p = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p)) return p;
        }
        // 从路径解析：工程内相对路径或系统绝对路径
        foreach (var raw in DragAndDrop.paths)
        {
            if (string.IsNullOrEmpty(raw)) continue;
            if (AssetDatabase.IsValidFolder(raw)) return raw;
            string rel = ToAssetsRelative(raw);
            if (!string.IsNullOrEmpty(rel) && AssetDatabase.IsValidFolder(rel)) return rel;
            // 拖入的是文件则取其所在目录
            if (System.IO.File.Exists(raw))
            {
                string parent = System.IO.Path.GetDirectoryName(raw)?.Replace('\\', '/');
                string relParent = ToAssetsRelative(parent);
                if (!string.IsNullOrEmpty(relParent) && AssetDatabase.IsValidFolder(relParent)) return relParent;
            }
        }
        return null;
    }

    /// <summary>确保 Assets 下多级目录存在（逐级创建）</summary>
    private static void EnsureFolder(string assetDir)
    {
        if (string.IsNullOrEmpty(assetDir) || AssetDatabase.IsValidFolder(assetDir)) return;
        var parts = assetDir.Split('/');
        string cur = parts[0]; // Assets
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    /// <summary>绝对路径转 Assets 相对路径（不在工程内返回 null）</summary>
    private static string ToAssetsRelative(string abs)
    {
        if (string.IsNullOrEmpty(abs)) return null;
        abs = abs.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (abs == dataPath) return "Assets";
        if (abs.StartsWith(dataPath + "/")) return "Assets" + abs.Substring(dataPath.Length);
        return null;
    }

    #endregion
}
