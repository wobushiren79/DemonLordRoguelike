using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗场景配置编辑窗口：直观编辑 excel_fight_scene[战斗场景].xlsx 的场景参数
/// （场景预制/道路颜色/天空盒/雾/环境光/细节预制），保存写回 Excel 并同步再生 JSON；
/// Play 模式下可把修改实时应用到当前战斗场景看效果（雾/环境光/天空盒/道路色/细节预制切换）。
/// </summary>
public class FightSceneEditorWindow : EditorWindow
{
    #region 菜单入口

    [MenuItem("游戏/战斗场景配置")]
    public static void ShowWindow()
    {
        var window = GetWindow<FightSceneEditorWindow>("战斗场景配置");
        window.minSize = new Vector2(460, 620);
        window.Show();
    }

    #endregion

    #region 数据模型

    /// <summary>
    /// 单行战斗场景配置的编辑模型（与 Excel 列一一对应，颜色/材质等已转为直观类型）
    /// </summary>
    private class FightSceneEditData
    {
        public long id;
        public string nameRes = "";          // name_res 场景预制体文件名
        public Color roadColorA = Color.white;  // road_color_a
        public Color roadColorB = Color.white;  // road_color_b
        public Material skyboxMat;           // skybox_mat（存路径，ObjectField 显示）
        public Vector3 skyboxRotate;         // skybox_rotate
        public bool fogEnabled;              // fog 为空 = 不开雾
        public Color fogColor = Color.white;
        public float fogStart;
        public float fogEnd = 100f;
        public bool ambientEnabled;          // ambient_light 为空 = 不修改环境光
        public Color ambientColor = Color.white;
        public string details = "";          // details 细节预制名
        public string remark = "";           // remark 备注

        /// <summary>原始数据快照（从 Excel 读取/保存成功后刷新），用于脏检查</summary>
        public string originalSnapshot = "";

        /// <summary>
        /// 当前编辑值的快照串（任一字段变化都会改变快照）
        /// </summary>
        public string Snapshot()
        {
            string skyboxPath = skyboxMat == null ? "" : AssetDatabase.GetAssetPath(skyboxMat);
            return string.Join("|", id, nameRes,
                ColorUtility.ToHtmlStringRGB(roadColorA), ColorUtility.ToHtmlStringRGB(roadColorB),
                skyboxPath, skyboxRotate,
                fogEnabled, ColorUtility.ToHtmlStringRGB(fogColor), fogStart, fogEnd,
                ambientEnabled, ColorUtility.ToHtmlStringRGB(ambientColor),
                details, remark);
        }

        /// <summary>是否有未保存修改</summary>
        public bool IsDirty => Snapshot() != originalSnapshot;

        /// <summary>把当前编辑值标记为已保存（刷新快照）</summary>
        public void CommitSnapshot()
        {
            originalSnapshot = Snapshot();
        }
    }

    #endregion

    #region 成员变量

    /// <summary>Excel 文件路径</summary>
    private static string ExcelPath => Application.dataPath + "/Data/Excel/excel_fight_scene[战斗场景].xlsx";
    /// <summary>工作表名</summary>
    private const string SheetName = "FightScene";

    /// <summary>全部场景配置（直读 Excel，不经 JSON 保证最新）</summary>
    private readonly List<FightSceneEditData> listData = new List<FightSceneEditData>();
    /// <summary>当前选中的配置下标</summary>
    private int selectedIndex;
    /// <summary>Play 模式下字段一变即自动应用到当前战斗场景</summary>
    private bool autoApply = true;
    /// <summary>滚动位置</summary>
    private Vector2 scrollPos;

    /// <summary>当前选中行</summary>
    private FightSceneEditData CurrentData => (listData.Count > 0 && selectedIndex < listData.Count) ? listData[selectedIndex] : null;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        LoadFromExcel();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    /// <summary>
    /// Play 状态变化时刷新界面（按钮可用性/提示文案随运行状态变化）
    /// </summary>
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    #endregion

    #region OnGUI 绘制

    private void OnGUI()
    {
        if (listData.Count == 0)
        {
            EditorGUILayout.HelpBox("未读取到战斗场景配置，请确认 Excel 未被占用后点击刷新", MessageType.Warning);
            if (GUILayout.Button("🔄 刷新", GUILayout.Height(30))) LoadFromExcel();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🌲 战斗场景配置", EditorStyles.boldLabel, GUILayout.Width(120));
        string[] options = new string[listData.Count];
        for (int i = 0; i < listData.Count; i++)
            options[i] = $"{listData[i].id} | {listData[i].remark}";
        int newIndex = EditorGUILayout.Popup(selectedIndex, options);
        if (newIndex != selectedIndex)
        {
            if (CurrentData == null || !CurrentData.IsDirty ||
                EditorUtility.DisplayDialog("未保存修改", "当前配置有未保存修改，切换将丢弃，是否继续？", "切换", "取消"))
            {
                selectedIndex = newIndex;
            }
        }
        if (GUILayout.Button("🔄 刷新", GUILayout.Width(60))) { ConfirmLoadIfDirty(); }
        EditorGUILayout.EndHorizontal();

        var data = CurrentData;
        if (data == null) return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 编辑区：任何字段变化时按需自动应用到运行时场景
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("场景资源", EditorStyles.miniBoldLabel);
        // 场景预制体（ObjectField 直观拖拽，存纯文件名）
        GameObject curPrefab = string.IsNullOrEmpty(data.nameRes) ? null
            : AssetDatabase.LoadAssetAtPath<GameObject>($"{PathInfo.FightScenePrefabPath}/{data.nameRes}");
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("场景预制体", curPrefab, typeof(GameObject), false);
        if (newPrefab != curPrefab && newPrefab != null)
            data.nameRes = Path.GetFileName(AssetDatabase.GetAssetPath(newPrefab));
        EditorGUILayout.LabelField("　", data.nameRes, EditorStyles.miniLabel);
        data.remark = EditorGUILayout.TextField("备注", data.remark);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("道路颜色", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        data.roadColorA = EditorGUILayout.ColorField("颜色 A", data.roadColorA);
        data.roadColorB = EditorGUILayout.ColorField("颜色 B", data.roadColorB);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("天空盒", EditorStyles.miniBoldLabel);
        data.skyboxMat = (Material)EditorGUILayout.ObjectField("天空盒材质", data.skyboxMat, typeof(Material), false);
        data.skyboxRotate = EditorGUILayout.Vector3Field("旋转角度", data.skyboxRotate);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("雾（内置距离雾）", EditorStyles.miniBoldLabel);
        data.fogEnabled = EditorGUILayout.Toggle("开启雾", data.fogEnabled);
        if (data.fogEnabled)
        {
            data.fogColor = EditorGUILayout.ColorField("雾颜色", data.fogColor);
            EditorGUILayout.BeginHorizontal();
            data.fogStart = EditorGUILayout.FloatField("起始距离", data.fogStart);
            data.fogEnd = EditorGUILayout.FloatField("终止距离", data.fogEnd);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("模式固定为 Linear（配置格式仅支持 Start/End 线性雾）", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("环境光", EditorStyles.miniBoldLabel);
        data.ambientEnabled = EditorGUILayout.Toggle("修改全局环境光", data.ambientEnabled);
        if (data.ambientEnabled)
        {
            data.ambientColor = EditorGUILayout.ColorField("环境光颜色", data.ambientColor);
            EditorGUILayout.LabelField("进场设置、离场还原；夜晚场景配暗蓝色让受光物体（草等）变暗", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("场景细节预制（Details 下同名子预制）", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        data.details = EditorGUILayout.TextField("预制名", data.details);
        if (GUILayout.Button("Day", GUILayout.Width(40))) data.details = "Day";
        if (GUILayout.Button("Night", GUILayout.Width(50))) data.details = "Night";
        if (GUILayout.Button("清空", GUILayout.Width(45))) data.details = "";
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("空 = 整个 Details 节点隐藏", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        bool changed = EditorGUI.EndChangeCheck();

        // 状态与操作区
        EditorGUILayout.BeginVertical("box");
        if (data.IsDirty)
            EditorGUILayout.LabelField("● 有未保存修改", EditorStyles.boldLabel);

        bool canApply = Application.isPlaying && WorldHandler.Instance != null
            && WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.Fight) != null;
        if (!Application.isPlaying)
            EditorGUILayout.LabelField("未运行游戏：应用按钮不可用（仅预览编辑）", EditorStyles.miniLabel);
        else if (!canApply)
            EditorGUILayout.LabelField("运行中但当前不在战斗场景", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = data.IsDirty ? new Color(0.6f, 0.9f, 0.6f) : Color.white;
        if (GUILayout.Button("💾 保存到 Excel", GUILayout.Height(30))) SaveToExcel(data);
        GUI.backgroundColor = Color.white;
        using (new EditorGUI.DisabledScope(!canApply))
        {
            if (GUILayout.Button("▶ 应用到当前场景", GUILayout.Height(30))) ApplyToRuntimeScene(data);
        }
        EditorGUILayout.EndHorizontal();

        autoApply = EditorGUILayout.Toggle("实时应用（Play 时改动立即生效）", autoApply);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        // 字段变化 + 自动应用开启 → 立即生效
        if (changed && autoApply && canApply) ApplyToRuntimeScene(data);
    }

    /// <summary>
    /// 有未保存修改时先确认，再重新从 Excel 读取
    /// </summary>
    private void ConfirmLoadIfDirty()
    {
        var data = CurrentData;
        if (data == null || !data.IsDirty ||
            EditorUtility.DisplayDialog("未保存修改", "当前配置有未保存修改，刷新将丢弃，是否继续？", "刷新", "取消"))
        {
            LoadFromExcel();
        }
    }

    #endregion

    #region Excel 读取/保存

    /// <summary>
    /// 从 Excel 直读全部场景配置（前 3 行为元数据：字段名/类型/中文注释，第 4 行起为数据）
    /// </summary>
    private void LoadFromExcel()
    {
        listData.Clear();
        ExcelUtil.GetExcelPackage(new FileInfo(ExcelPath), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetName];
            if (sheet == null) { LogUtil.LogError($"工作表不存在：{SheetName}"); return; }
            // 第 1 行字段名 → 列索引
            Dictionary<string, int> dicColumn = new Dictionary<string, int>();
            for (int x = 1; x <= sheet.Dimension.End.Column; x++)
                dicColumn[sheet.Cells[1, x].Text] = x;

            for (int y = 4; y <= sheet.Dimension.End.Row; y++)
            {
                var data = new FightSceneEditData();
                data.id = long.Parse(sheet.Cells[y, dicColumn["id"]].Text);
                data.nameRes = GetCell(sheet, y, dicColumn, "name_res");
                ColorUtility.TryParseHtmlString(GetCell(sheet, y, dicColumn, "road_color_a"), out data.roadColorA);
                ColorUtility.TryParseHtmlString(GetCell(sheet, y, dicColumn, "road_color_b"), out data.roadColorB);
                string skyboxPath = GetCell(sheet, y, dicColumn, "skybox_mat");
                if (!string.IsNullOrEmpty(skyboxPath))
                    data.skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                // skybox_rotate 形如 "-15,0,0"
                string rotateStr = GetCell(sheet, y, dicColumn, "skybox_rotate");
                if (!string.IsNullOrEmpty(rotateStr))
                {
                    var arr = rotateStr.Split(',');
                    if (arr.Length > 0) float.TryParse(arr[0], NumberStyles.Float, CultureInfo.InvariantCulture, out data.skyboxRotate.x);
                    if (arr.Length > 1) float.TryParse(arr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out data.skyboxRotate.y);
                    if (arr.Length > 2) float.TryParse(arr[2], NumberStyles.Float, CultureInfo.InvariantCulture, out data.skyboxRotate.z);
                }
                // fog 解析复用运行时 Bean 的 GetFogParams（单一逻辑源）
                string fogStr = GetCell(sheet, y, dicColumn, "fog");
                data.fogEnabled = !string.IsNullOrEmpty(fogStr);
                if (data.fogEnabled)
                {
                    var bean = new FightSceneBean { fog = fogStr };
                    if (bean.GetFogParams(out var fogColor, out var fogStart, out var fogEnd, out _))
                    {
                        data.fogColor = fogColor;
                        data.fogStart = fogStart;
                        data.fogEnd = fogEnd;
                    }
                }
                string ambientStr = GetCell(sheet, y, dicColumn, "ambient_light");
                data.ambientEnabled = !string.IsNullOrEmpty(ambientStr);
                if (data.ambientEnabled)
                    ColorUtility.TryParseHtmlString(ambientStr, out data.ambientColor);
                data.details = GetCell(sheet, y, dicColumn, "details");
                data.remark = GetCell(sheet, y, dicColumn, "remark");
                data.CommitSnapshot();
                listData.Add(data);
            }
        });
        if (selectedIndex >= listData.Count) selectedIndex = 0;
        Repaint();
    }

    /// <summary>
    /// 按字段名取单元格文本（列不存在或空单元格返回空串）
    /// </summary>
    private string GetCell(ExcelWorksheet sheet, int row, Dictionary<string, int> dicColumn, string fieldName)
    {
        if (!dicColumn.TryGetValue(fieldName, out int x)) return "";
        return sheet.Cells[row, x].Text;
    }

    /// <summary>
    /// 保存当前行到 Excel（SetExcelData 按 id 定位行、字段名定位列），随后再生 JSON 并刷新
    /// </summary>
    private void SaveToExcel(FightSceneEditData data)
    {
        var listChange = new List<ExcelUtil.ExcelChangeData>
        {
            new ExcelUtil.ExcelChangeData(data.id, "name_res", data.nameRes),
            new ExcelUtil.ExcelChangeData(data.id, "road_color_a", "#" + ColorUtility.ToHtmlStringRGB(data.roadColorA)),
            new ExcelUtil.ExcelChangeData(data.id, "road_color_b", "#" + ColorUtility.ToHtmlStringRGB(data.roadColorB)),
            new ExcelUtil.ExcelChangeData(data.id, "skybox_mat", data.skyboxMat == null ? "" : AssetDatabase.GetAssetPath(data.skyboxMat)),
            new ExcelUtil.ExcelChangeData(data.id, "skybox_rotate",
                $"{Fmt(data.skyboxRotate.x)},{Fmt(data.skyboxRotate.y)},{Fmt(data.skyboxRotate.z)}"),
            new ExcelUtil.ExcelChangeData(data.id, "fog", BuildFogString(data)),
            new ExcelUtil.ExcelChangeData(data.id, "ambient_light",
                data.ambientEnabled ? "#" + ColorUtility.ToHtmlStringRGB(data.ambientColor) : ""),
            new ExcelUtil.ExcelChangeData(data.id, "details", data.details),
            new ExcelUtil.ExcelChangeData(data.id, "remark", data.remark),
        };
        ExcelUtil.SetExcelData(ExcelPath, SheetName, listChange);
        // 同步再生运行时 JSON（该 Excel 仅单表，再生安全）
        ExcelUtil.ExcelToJsonItem(ExcelPath);
        AssetDatabase.Refresh();
        data.CommitSnapshot();
        LogUtil.Log($"战斗场景配置已保存：id={data.id}");
    }

    /// <summary>
    /// 拼装雾配置字符串（形如 Color:#CEF9FF&amp;Start:8&amp;End:20&amp;Mode:Linear；未开启返回空）
    /// </summary>
    private string BuildFogString(FightSceneEditData data)
    {
        if (!data.fogEnabled) return "";
        return $"Color:#{ColorUtility.ToHtmlStringRGB(data.fogColor)}&Start:{Fmt(data.fogStart)}&End:{Fmt(data.fogEnd)}&Mode:Linear";
    }

    /// <summary>浮点格式化（固定小数点为 '.'，避免文化差异污染配置串）</summary>
    private string Fmt(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    #endregion

    #region 运行时应用

    /// <summary>
    /// 把当前编辑值应用到正在运行的战斗场景（雾/环境光/天空盒/道路颜色/细节预制切换）。
    /// 仅修改运行时状态不写配置；退出 Play 后 RenderSettings 随场景快照还原，无污染。
    /// </summary>
    private void ApplyToRuntimeScene(FightSceneEditData data)
    {
        GameObject sceneObj = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.Fight);
        if (sceneObj == null) return;

        // 雾：走 VolumeHandler 既有封装
        if (data.fogEnabled)
            VolumeHandler.Instance.SetFog(data.fogColor, FogMode.Linear, data.fogStart, data.fogEnd, isActive: true);
        else
            VolumeHandler.Instance.SetFogActive(false);

        // 环境光：未配置则不动（保持 WorldHandler 设置的当前值）
        if (data.ambientEnabled)
            RenderSettings.ambientLight = data.ambientColor;

        // 天空盒与旋转（与 WorldHandler.LoadFightScene 同参数）
        if (data.skyboxMat != null)
        {
            RenderSettings.skybox = data.skyboxMat;
            RenderSettings.skybox.SetFloat("_RotateX", data.skyboxRotate.x);
            RenderSettings.skybox.SetFloat("_RotateY", data.skyboxRotate.y);
            RenderSettings.skybox.SetFloat("_RotateZ", data.skyboxRotate.z);
        }

        // 道路颜色：找战斗场景下材质带 _ColorA/_ColorB 的 MeshRenderer（即战斗道路）
        var renderers = sceneObj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in renderers)
        {
            var mat = mr.sharedMaterial;
            if (mat != null && mat.HasProperty("_ColorA") && mat.HasProperty("_ColorB"))
            {
                mat.SetColor("_ColorA", data.roadColorA);
                mat.SetColor("_ColorB", data.roadColorB);
            }
        }

        // 细节预制：与 WorldHandler.HandleFightSceneDetails 同逻辑（同名显示、其它隐藏；空=整体隐藏）
        Transform tfDetails = sceneObj.transform.Find("Details");
        if (tfDetails != null)
        {
            string detailsName = data.details.Trim();
            if (string.IsNullOrEmpty(detailsName))
            {
                tfDetails.gameObject.SetActive(false);
            }
            else
            {
                tfDetails.gameObject.SetActive(true);
                for (int i = 0; i < tfDetails.childCount; i++)
                {
                    GameObject objChild = tfDetails.GetChild(i).gameObject;
                    objChild.SetActive(objChild.name == detailsName);
                }
            }
        }
    }

    #endregion
}
