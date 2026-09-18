using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模式编辑工具 - 挑战100勇士页签
/// 用于可视化编辑 excel_fight_type_challenge_hundred_info[战斗-挑战100勇士] 表：
/// 每行=一个挑战配置(无世界维度)，行内 difficulty_levels 声明该行适配的难度列表；
/// 世界最高已解锁难度在列才可抽中本行，同一难度多行匹配时随机其一，无匹配行的难度不会刷出本模式。
/// 本页签以「配置行列表」为主组织编辑；顶部难度覆盖总览展示各难度可抽中行数(0行=红色警示)，点击格子可按难度筛选行列表。
/// 注意：本表 ID 列表字段(enemy_ids/fight_scene_ids/difficulty_levels)约定用英文逗号 "," 分割（与征服模式的 "&" 不同）。
/// 宿主窗口见 FightModeEditorWindow（菜单：游戏/战斗模式编辑）
/// </summary>
public class FightModeEditorTabChallengeHundred : FightModeEditorTabBase
{
    #region 成员变量

    /// <summary>挑战100勇士 Excel 文件路径</summary>
    private string excelPath;

    /// <summary>NpcInfo Excel 文件路径</summary>
    private string npcInfoExcelPath;

    /// <summary>战斗场景 Excel 文件路径</summary>
    private string fightSceneExcelPath;

    /// <summary>Json 输出目录</summary>
    private string jsonFolderPath;

    /// <summary>工作表名称</summary>
    private const string SheetName = "FightTypeChallengeHundredInfo";

    /// <summary>本表 ID 列表字段的分隔符（与运行时 SplitForArrayLong(',') 一致）</summary>
    private const char ListSeparator = ',';

    /// <summary>难度筛选（0=不过滤，1-10=只看该难度可抽中的行；点击难度覆盖总览格子切换）</summary>
    private int filterDifficulty = 0;

    /// <summary>当前可见的配置行列表（按 id 升序，受难度筛选影响）</summary>
    private List<FightTypeChallengeHundredInfoBean> visibleList = new List<FightTypeChallengeHundredInfoBean>();

    /// <summary>行下拉当前选中索引（对应 visibleList）</summary>
    private int selectedRowIndex = 0;

    /// <summary>当前编辑的数据</summary>
    private FightTypeChallengeHundredInfoBean currentBean;

    /// <summary>原始Bean用于对比变更</summary>
    private FightTypeChallengeHundredInfoBean originalBean;

    /// <summary>字段标签固定宽度</summary>
    private const float FieldLabelWidth = 170f;

    /// <summary>难度格子固定宽度</summary>
    private const float DifficultyCellWidth = 34f;

    /// <summary>难度格子固定高度（两行：难度数字+行数）</summary>
    private const float DifficultyCellHeight = 30f;

    /// <summary>难度页签/覆盖格子按钮样式</summary>
    private GUIStyle difficultyToggleStyle;

    /// <summary>难度覆盖格子带说明文字的样式（小号字两行显示）</summary>
    private GUIStyle overviewCellStyle;

    /// <summary>未保存提示样式(橙色小字)</summary>
    private GUIStyle dirtyHintStyle;

    /// <summary>字段已修改时的编辑框背景色(淡黄)</summary>
    private static readonly Color ModifiedBgColor = new Color(1f, 0.93f, 0.55f);

    /// <summary>难度选中/命中态背景色(蓝)</summary>
    private static readonly Color DifficultyOnBgColor = new Color(0.30f, 0.60f, 0.95f);

    /// <summary>难度无匹配行警示背景色(红)</summary>
    private static readonly Color DifficultyEmptyBgColor = new Color(0.90f, 0.35f, 0.30f);

    /// <summary>滚动位置</summary>
    private Vector2 scrollPos = Vector2.zero;

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    /// <summary>选择区域分组框样式</summary>
    private GUIStyle selectionBoxStyle;

    /// <summary>编辑区域分组框样式</summary>
    private GUIStyle boxStyle;

    /// <summary>数据已加载标记</summary>
    private bool dataLoaded = false;

    /// <summary>所有配置数据（用于查找）</summary>
    private List<FightTypeChallengeHundredInfoBean> allConfigList = new List<FightTypeChallengeHundredInfoBean>();

    /// <summary>列表字段展开状态</summary>
    private Dictionary<string, bool> listFoldoutStates = new Dictionary<string, bool>();

    /// <summary>列表字段编辑模式（true=列表编辑, false=文本编辑）</summary>
    private Dictionary<string, bool> listEditMode = new Dictionary<string, bool>();

    /// <summary>新ID输入缓存</summary>
    private Dictionary<string, long> newIdInputs = new Dictionary<string, long>();

    /// <summary>待删除的索引（延迟删除）</summary>
    private int pendingRemoveIndex = -1;

    /// <summary>待删除的字段Key</summary>
    private string pendingRemoveFieldKey = null;

    /// <summary>NpcInfo ID到名字的映射</summary>
    private Dictionary<long, string> npcNameMap = new Dictionary<long, string>();

    /// <summary>场景ID到名字的映射</summary>
    private Dictionary<long, string> sceneNameMap = new Dictionary<long, string>();

    /// <summary>NPC下拉选项ID列表（升序，与npcOptionNames一一对应）</summary>
    private List<long> npcOptionIds = new List<long>();

    /// <summary>NPC下拉选项显示文本（格式 "[id] 名字"）</summary>
    private List<string> npcOptionNames = new List<string>();

    /// <summary>场景下拉选项ID列表（升序，与sceneOptionNames一一对应）</summary>
    private List<long> sceneOptionIds = new List<long>();

    /// <summary>场景下拉选项显示文本（格式 "[id] 名字"）</summary>
    private List<string> sceneOptionNames = new List<string>();

    #endregion

    #region 初始化与 GUI 入口

    /// <summary>
    /// 宿主窗口启用时初始化路径和加载数据
    /// </summary>
    public override void Init()
    {
        excelPath = Application.dataPath + "/Data/Excel/excel_fight_type_challenge_hundred_info[战斗-挑战100勇士].xlsx";
        npcInfoExcelPath = Application.dataPath + "/Data/Excel/excel_npc_info[NPC信息].xlsx";
        fightSceneExcelPath = Application.dataPath + "/Data/Excel/excel_fight_scene[战斗场景].xlsx";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";

        LoadNpcInfoData();
        LoadFightSceneData();
        LoadAllConfigFromExcel();
        RecomputeVisibleList();
    }

    /// <summary>
    /// GUI 渲染入口：顶部工具栏与选择区固定不滚动，中间编辑区滚动，底部保存栏固定
    /// </summary>
    public override void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        // 顶部工具栏(刷新/导出/打开表格，固定)
        DrawToolbar();

        // 顶部选择区域(难度覆盖总览+配置行选择，固定)
        DrawSelectionArea();

        // 数据编辑区域(滚动)
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (dataLoaded && currentBean != null)
        {
            DrawDataEditArea();
        }
        else if (dataLoaded && currentBean == null)
        {
            if (filterDifficulty > 0)
            {
                EditorGUILayout.HelpBox($"难度 {filterDifficulty} 无可抽中的配置行（该难度不会刷出挑战100勇士模式）。\n可点击「新增行」创建，或点击总览格子取消筛选。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("配置表没有任何数据行，可点击「新增行」创建。", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请选择配置行，点击「加载数据」开始编辑", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();

        // 底部保存栏(固定，始终可见)
        if (dataLoaded && currentBean != null)
        {
            DrawActionButtons();
        }
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化所有自定义 UI 样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        selectionBoxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 2, 2)
        };

        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(4, 4, 4, 4)
        };

        difficultyToggleStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };

        // 覆盖总览格子：小号字+两行(难度数字/可抽中行数)
        overviewCellStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(1, 1, 1, 1)
        };

        dirtyHintStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(1f, 0.70f, 0.30f) : new Color(0.85f, 0.45f, 0.0f) }
        };

        stylesInitialized = true;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载NpcInfo数据（直接从Json文件读取，建立ID到名字的映射）
    /// </summary>
    private void LoadNpcInfoData()
    {
        npcNameMap.Clear();

        string npcJsonPath = jsonFolderPath + "/NpcInfo.txt";
        if (!File.Exists(npcJsonPath))
            return;

        try
        {
            string jsonText = File.ReadAllText(npcJsonPath);
            NpcInfoBean[] npcArray = JsonConvert.DeserializeObject<NpcInfoBean[]>(jsonText);
            if (npcArray == null)
                return;

            foreach (var npc in npcArray)
            {
                // 优先使用 remark 作为显示名，其次使用 name 字段
                string displayName = npc.remark;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = $"name_id:{npc.name}";
                }
                npcNameMap[npc.id] = displayName;
            }

            BuildOptionList(npcNameMap, npcOptionIds, npcOptionNames);
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载NpcInfo数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载FightScene数据（直接从Json文件读取，建立场景ID到名字的映射）
    /// </summary>
    private void LoadFightSceneData()
    {
        sceneNameMap.Clear();

        string sceneJsonPath = jsonFolderPath + "/FightScene.txt";
        if (!File.Exists(sceneJsonPath))
            return;

        try
        {
            string jsonText = File.ReadAllText(sceneJsonPath);
            FightSceneBean[] sceneArray = JsonConvert.DeserializeObject<FightSceneBean[]>(jsonText);
            if (sceneArray == null)
                return;

            foreach (var scene in sceneArray)
            {
                string displayName = scene.remark;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = scene.name_res;
                }
                sceneNameMap[scene.id] = displayName;
            }

            BuildOptionList(sceneNameMap, sceneOptionIds, sceneOptionNames);
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载FightScene数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从Excel加载所有配置数据
    /// </summary>
    private void LoadAllConfigFromExcel()
    {
        allConfigList.Clear();

        if (!File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在:\n{excelPath}", "确定");
            return;
        }

        FileInfo fileInfo = new FileInfo(excelPath);
        ExcelUtil.GetExcelPackage(fileInfo, (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetName];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetName}");
                return;
            }

            int columnCount = sheet.Dimension.End.Column;
            int rowCount = sheet.Dimension.End.Row;

            for (int row = 4; row <= rowCount; row++)
            {
                FightTypeChallengeHundredInfoBean bean = new FightTypeChallengeHundredInfoBean();
                for (int col = 1; col <= columnCount; col++)
                {
                    string fieldName = sheet.Cells[1, col].Text;
                    string cellText = sheet.Cells[row, col].Text;

                    FieldInfo fieldInfo = typeof(FightTypeChallengeHundredInfoBean).GetField(fieldName);
                    if (fieldInfo == null) continue;

                    if (string.IsNullOrEmpty(cellText))
                    {
                        if (fieldInfo.FieldType == typeof(int) || fieldInfo.FieldType == typeof(float) ||
                            fieldInfo.FieldType == typeof(long) || fieldInfo.FieldType == typeof(double))
                        {
                            cellText = "0";
                        }
                        else
                        {
                            continue;
                        }
                    }

                    try
                    {
                        object value = Convert.ChangeType(cellText, fieldInfo.FieldType);
                        fieldInfo.SetValue(bean, value);
                    }
                    catch (Exception e)
                    {
                        LogUtil.LogError($"转换字段 {fieldName} 值 {cellText} 时出错: {e.Message}");
                    }
                }
                allConfigList.Add(bean);
            }
        });
    }

    /// <summary>
    /// 判断配置行是否可被指定难度抽中（difficulty_levels 含该难度；自行解析不走 BeanPartial 缓存，避免编辑后缓存过期）
    /// </summary>
    private bool IsMatchRow(FightTypeChallengeHundredInfoBean bean, int difficulty)
    {
        if (bean == null) return false;
        List<int> levels = ParseIntList(bean.difficulty_levels);
        return levels.Contains(difficulty);
    }

    /// <summary>
    /// 统计指定难度可抽中的配置行数（难度覆盖总览用）
    /// </summary>
    private int CountMatchRows(int difficulty)
    {
        int count = 0;
        foreach (var bean in allConfigList)
        {
            if (IsMatchRow(bean, difficulty)) count++;
        }
        return count;
    }

    /// <summary>
    /// 重算可见行列表（难度筛选/数据刷新后调用；不影响已加载的编辑数据）
    /// </summary>
    private void RecomputeVisibleList()
    {
        visibleList.Clear();
        foreach (var bean in allConfigList)
        {
            if (filterDifficulty > 0 && !IsMatchRow(bean, filterDifficulty)) continue;
            visibleList.Add(bean);
        }
        visibleList.Sort((a, b) => a.id.CompareTo(b.id));
        if (selectedRowIndex >= visibleList.Count)
            selectedRowIndex = 0;
    }

    /// <summary>
    /// 加载当前选中配置行的数据
    /// </summary>
    private void LoadData()
    {
        currentBean = null;
        originalBean = null;

        if (visibleList.Count > 0)
        {
            if (selectedRowIndex < 0 || selectedRowIndex >= visibleList.Count)
                selectedRowIndex = 0;
            currentBean = visibleList[selectedRowIndex];
            // 深拷贝一份原始数据用于对比
            originalBean = JsonConvert.DeserializeObject<FightTypeChallengeHundredInfoBean>(JsonConvert.SerializeObject(currentBean));
        }

        dataLoaded = true;
    }

    /// <summary>
    /// 统计当前数据相对原始数据的变更字段数（用于未保存提示与保存按钮状态）
    /// </summary>
    private int CountChanges()
    {
        if (currentBean == null || originalBean == null) return 0;
        int count = 0;
        FieldInfo[] fields = typeof(FightTypeChallengeHundredInfoBean).GetFields();
        foreach (FieldInfo field in fields)
        {
            if (field.Name == "id") continue; // ID不修改
            if (!Equals(field.GetValue(currentBean), field.GetValue(originalBean))) count++;
        }
        return count;
    }

    /// <summary>
    /// 判断指定字段当前值是否与原始值不同（用于编辑框淡黄高亮）
    /// </summary>
    private bool IsFieldModified(string fieldName)
    {
        if (currentBean == null || originalBean == null) return false;
        return GetFieldValueStr(currentBean, fieldName) != GetFieldValueStr(originalBean, fieldName);
    }

    /// <summary>
    /// 通过反射读取指定Bean字段的字符串值
    /// </summary>
    private string GetFieldValueStr(FightTypeChallengeHundredInfoBean bean, string fieldName)
    {
        if (bean == null) return "-";
        FieldInfo f = typeof(FightTypeChallengeHundredInfoBean).GetField(fieldName);
        if (f == null) return "-";
        object v = f.GetValue(bean);
        return v?.ToString() ?? "";
    }

    #endregion

    #region UI 绘制 - 顶部工具栏

    /// <summary>
    /// 绘制顶部工具栏（刷新/导出/快捷打开各 Excel 表，单行小按钮固定显示）
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("刷新数据", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            LoadAllConfigFromExcel();
            RecomputeVisibleList();
            LoadData();
            EditorUtility.DisplayDialog("完成", "已从Excel重新加载数据", "确定");
        }

        if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            ExportJsonOnly();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField("打开表格:", EditorStyles.miniLabel, GUILayout.Width(60));
        if (GUILayout.Button("挑战100配置", EditorStyles.toolbarButton, GUILayout.Width(76)))
        {
            OpenExcel(excelPath, "挑战100勇士");
        }
        if (GUILayout.Button("NPC配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenExcel(npcInfoExcelPath, "NpcInfo");
        }
        if (GUILayout.Button("场景配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenExcel(fightSceneExcelPath, "战斗场景");
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 选择区域

    /// <summary>
    /// 绘制顶部选择区域（难度覆盖总览 + 配置行下拉 + 加载/新增/删除按钮，附当前编辑状态行）
    /// </summary>
    private void DrawSelectionArea()
    {
        EditorGUILayout.BeginVertical(selectionBoxStyle);

        // 难度覆盖总览(1-10各难度可抽中行数，0行红色警示，点击格子按难度筛选行列表)
        DrawDifficultyOverview();

        EditorGUILayout.BeginHorizontal();

        // 配置行下拉（"[id] 备注 (难度:x,y)"；受难度筛选影响）
        EditorGUILayout.LabelField("配置行", EditorStyles.boldLabel, GUILayout.Width(42));
        if (visibleList.Count > 0)
        {
            string[] rowNames = new string[visibleList.Count];
            for (int i = 0; i < visibleList.Count; i++)
            {
                rowNames[i] = $"[{visibleList[i].id}] {visibleList[i].remark} (难度:{visibleList[i].difficulty_levels})";
            }
            selectedRowIndex = EditorGUILayout.Popup(selectedRowIndex, rowNames, GUILayout.Width(320), GUILayout.Height(22));
        }
        else
        {
            EditorGUILayout.LabelField("(无配置行)", EditorStyles.miniLabel, GUILayout.Width(320), GUILayout.Height(22));
        }

        GUILayout.Space(12);

        // 加载按钮
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
        if (GUILayout.Button("加载数据", GUILayout.Width(80), GUILayout.Height(22)))
        {
            LoadData();
        }
        GUI.backgroundColor = prevColor;

        GUILayout.FlexibleSpace();

        // 新增行按钮（以当前编辑行/表内末行为模板）
        Color addColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
        if (GUILayout.Button("新增行", GUILayout.Width(64), GUILayout.Height(22)))
        {
            CreateNewRow();
        }
        GUI.backgroundColor = addColor;

        // 删除行按钮（仅已加载数据时可用）
        EditorGUI.BeginDisabledGroup(currentBean == null);
        Color delColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("删除本行", GUILayout.Width(64), GUILayout.Height(22)))
        {
            DeleteCurrentRow();
        }
        GUI.backgroundColor = delColor;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        // 状态行：当前编辑信息 + 未加载/未保存提示
        if (dataLoaded && currentBean != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"当前编辑: ID={currentBean.id} | {currentBean.remark}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            // 选择已变更但尚未加载的提示，防止误以为切换即生效
            long selectedRowId = visibleList.Count > 0 && selectedRowIndex < visibleList.Count ? visibleList[selectedRowIndex].id : -1;
            if (currentBean.id != selectedRowId)
            {
                EditorGUILayout.LabelField("⚠ 选择已变更，请点击「加载数据」", dirtyHintStyle);
            }

            int changes = CountChanges();
            if (changes > 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField($"● {changes} 项未保存修改", dirtyHintStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制难度覆盖总览：难度1-10各一格显示可抽中行数（0行红色警示该难度不会刷出本模式），点击格子按难度筛选下方行列表，再点取消筛选
    /// </summary>
    private void DrawDifficultyOverview()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("难度覆盖", EditorStyles.boldLabel, GUILayout.Width(56));

        for (int d = 1; d <= 10; d++)
        {
            int matchCount = CountMatchRows(d);
            bool isFiltered = filterDifficulty == d;
            string cellText = $"{d}\n{matchCount}行";
            string tooltip = matchCount > 0
                ? $"难度 {d}：{matchCount} 行可抽中（点击筛选这些行）"
                : $"难度 {d}：无可抽中行，该难度不会刷出挑战100勇士模式";

            Color prevBg = GUI.backgroundColor;
            if (isFiltered) GUI.backgroundColor = DifficultyOnBgColor;
            else if (matchCount == 0) GUI.backgroundColor = DifficultyEmptyBgColor;
            bool click = GUILayout.Toggle(isFiltered, new GUIContent(cellText, tooltip), overviewCellStyle, GUILayout.Width(DifficultyCellWidth), GUILayout.Height(DifficultyCellHeight));
            GUI.backgroundColor = prevBg;
            if (click && !isFiltered)
            {
                filterDifficulty = d;
                RecomputeVisibleList();
            }
            else if (!click && isFiltered)
            {
                // 再次点击已选中的格子：取消筛选
                filterDifficulty = 0;
                RecomputeVisibleList();
            }
        }

        // 筛选状态提示与清除
        if (filterDifficulty > 0)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField($"仅显示难度 {filterDifficulty} 可抽中的行", dirtyHintStyle, GUILayout.Width(150), GUILayout.Height(DifficultyCellHeight));
            if (GUILayout.Button("清除筛选", EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(DifficultyCellHeight)))
            {
                filterDifficulty = 0;
                RecomputeVisibleList();
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    #endregion

    #region UI 绘制 - 数据编辑区域

    /// <summary>
    /// 绘制数据编辑区域（单栏：字段标签+编辑控件，已修改字段淡黄高亮）
    /// </summary>
    private void DrawDataEditArea()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        // 基础信息
        DrawSectionTitle("基础信息");
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(FieldLabelWidth));
        EditorGUILayout.LongField(currentBean.id, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        // 匹配规则
        DrawSectionTitle("匹配规则");
        currentBean.difficulty_levels = DrawDifficultyLevelsField(new GUIContent("适配难度列表", "difficulty_levels：世界最高已解锁难度在列才可抽中本行，可多选；同一难度多行匹配时随机其一，全不选则任何难度都抽不中本行"), currentBean.difficulty_levels, "difficulty_levels");

        // 敌人配置
        DrawSectionTitle("敌人配置");
        currentBean.enemy_ids = DrawIdListField(new GUIContent("进攻敌人列表", "enemy_ids：npcInfoId 用\",\"分割；每只怪独立随机，总量100"), currentBean.enemy_ids, "enemy_ids");

        // 场景配置
        DrawSectionTitle("场景配置");
        currentBean.fight_scene_ids = DrawIdListField(new GUIContent("战斗场景列表", "fight_scene_ids：用\",\"分割，随机其一"), currentBean.fight_scene_ids, "fight_scene_ids");

        // 进攻配置
        DrawSectionTitle("进攻配置");
        currentBean.attack_intensity_baserate = DrawFloatField(new GUIContent("强度倍率", "attack_intensity_baserate：敌人HP/护甲/攻击力×该值；0或不配按1"), currentBean.attack_intensity_baserate, "attack_intensity_baserate");
        currentBean.attack_show_time = DrawFloatField(new GUIContent("进攻总时间", "attack_show_time：单位秒，100只怪在此时间内出完"), currentBean.attack_show_time, "attack_show_time");

        // 关卡配置(单值"x"或区间"x-y")
        DrawSectionTitle("关卡配置");
        currentBean.road_num = DrawStringField(new GUIContent("道路数量", "road_num：x 固定 或 x-y 区间随机"), currentBean.road_num, "road_num");
        currentBean.road_length = DrawStringField(new GUIContent("道路长度", "road_length：x 固定 或 x-y 区间随机"), currentBean.road_length, "road_length");

        // 奖励配置
        DrawSectionTitle("奖励配置");
        currentBean.drop_crystal = DrawIntField(new GUIContent("击杀掉落魔晶", "drop_crystal"), currentBean.drop_crystal, "drop_crystal");
        currentBean.reward_crystal = DrawStringField(new GUIContent("奖励-每箱魔晶", "reward_crystal：x 固定 或 x-y 区间随机"), currentBean.reward_crystal, "reward_crystal");
        currentBean.reward_equip_rarity = DrawIntField(new GUIContent("奖励-装备稀有度", "reward_equip_rarity"), currentBean.reward_equip_rarity, "reward_equip_rarity");
        currentBean.reward_exp = DrawIntField(new GUIContent("通关经验(阵容每只)", "reward_exp"), currentBean.reward_exp, "reward_exp");

        // 备注
        DrawSectionTitle("备注");
        currentBean.remark = DrawStringField(new GUIContent("备注", "remark"), currentBean.remark, "remark");

        GUILayout.Space(6);
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制分区标题（加粗文字 + 下方细分隔线）
    /// </summary>
    private void DrawSectionTitle(string title)
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(4);
    }

    /// <summary>
    /// 绘制当前值编辑框（值与原始值不同则淡黄背景标记"已修改"）
    /// </summary>
    private string DrawCurrentTextField(string value, bool modified)
    {
        Color prevColor = GUI.backgroundColor;
        if (modified) GUI.backgroundColor = ModifiedBgColor;
        string result = EditorGUILayout.TextField(value);
        GUI.backgroundColor = prevColor;
        return result;
    }

    /// <summary>
    /// 绘制字符串字段
    /// </summary>
    private string DrawStringField(GUIContent labelContent, string value, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        string result = DrawCurrentTextField(value, IsFieldModified(fieldName));
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制适配难度列表字段（1-10 多选页签，命中即亮；附实时覆盖提示，全不选时警示任何难度都抽不中）
    /// </summary>
    private string DrawDifficultyLevelsField(GUIContent labelContent, string value, string fieldName)
    {
        string result = value;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));

        // 当前值：1-10 多选页签（命中即亮），改动后重建为升序逗号串
        List<int> levels = ParseIntList(result);
        bool modified = IsFieldModified(fieldName);
        Color prevColor = GUI.backgroundColor;
        bool changed = false;
        for (int d = 1; d <= 10; d++)
        {
            bool isOn = levels.Contains(d);
            if (isOn) GUI.backgroundColor = DifficultyOnBgColor;
            else if (modified) GUI.backgroundColor = ModifiedBgColor;
            bool newOn = GUILayout.Toggle(isOn, d.ToString(), difficultyToggleStyle, GUILayout.Width(26), GUILayout.Height(20));
            if (newOn != isOn)
            {
                if (newOn) levels.Add(d);
                else levels.Remove(d);
                changed = true;
            }
            GUI.backgroundColor = prevColor;
        }
        if (changed)
        {
            levels.Sort();
            result = BuildIntString(levels);
        }

        // 实时覆盖提示
        if (levels.Count == 0)
        {
            EditorGUILayout.LabelField("⚠ 未选难度，任何难度都抽不中本行", dirtyHintStyle);
        }
        else
        {
            EditorGUILayout.LabelField($"可被难度 {BuildIntString(levels)} 抽中", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制ID列表字段（首行=标签+展开计数+模式切换+快捷开表；展开后为列表/文本编辑区）
    /// </summary>
    private string DrawIdListField(GUIContent labelContent, string value, string fieldKey)
    {
        // 初始化状态(列表默认展开，直接显示逐行编辑明细)
        if (!listFoldoutStates.ContainsKey(fieldKey))
            listFoldoutStates[fieldKey] = true;
        if (!listEditMode.ContainsKey(fieldKey))
            listEditMode[fieldKey] = true;
        if (!newIdInputs.ContainsKey(fieldKey))
            newIdInputs[fieldKey] = 0;

        // 解析当前值为列表
        List<long> idList = ParseIdList(value);

        EditorGUILayout.BeginVertical();

        // 首行：标签 + 展开foldout(共N个) + 编辑模式切换 + 快捷打开配置表 + 已修改标记
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        listFoldoutStates[fieldKey] = EditorGUILayout.Foldout(listFoldoutStates[fieldKey], $"共 {idList.Count} 个", true);

        // 已修改淡黄标记（列表编辑不走文本框，无法套背景色，改用文字标记）
        if (IsFieldModified(fieldKey))
        {
            EditorGUILayout.LabelField("●已修改", dirtyHintStyle, GUILayout.Width(50));
        }

        GUILayout.FlexibleSpace();

        string modeLabel = listEditMode[fieldKey] ? "切换文本编辑" : "切换列表编辑";
        if (GUILayout.Button(modeLabel, EditorStyles.miniButton, GUILayout.Width(90)))
        {
            listEditMode[fieldKey] = !listEditMode[fieldKey];
        }

        // 场景字段：在列表旁提供「打开场景配置表」按钮（打开战斗场景 Excel）
        if (fieldKey.Contains("scene"))
        {
            if (GUILayout.Button("打开场景配置表", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                OpenExcel(fightSceneExcelPath, "战斗场景");
            }
        }
        // 敌人字段：在列表旁提供「打开NpcInfo配置表」按钮（打开NpcInfo Excel）
        else if (fieldKey.Contains("enemy"))
        {
            if (GUILayout.Button("打开NpcInfo配置表", EditorStyles.miniButton, GUILayout.Width(120)))
            {
                OpenExcel(npcInfoExcelPath, "NpcInfo");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (listEditMode[fieldKey])
        {
            // 列表编辑模式（首行 foldout 展开时显示明细）
            if (listFoldoutStates[fieldKey])
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Space(4);

                // 处理延迟删除
                if (pendingRemoveFieldKey == fieldKey && pendingRemoveIndex >= 0 && pendingRemoveIndex < idList.Count)
                {
                    idList.RemoveAt(pendingRemoveIndex);
                    value = BuildIdString(idList);
                    pendingRemoveIndex = -1;
                    pendingRemoveFieldKey = null;
                }

                // 显示每个ID
                for (int i = 0; i < idList.Count; i++)
                {
                    int rowIndex = i; // 闭包捕获副本
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{rowIndex + 1}]", GUILayout.Width(30));

                    // 方式一：直接输入ID
                    long newId = EditorGUILayout.LongField(idList[rowIndex], GUILayout.Width(90));
                    if (newId != idList[rowIndex])
                    {
                        idList[rowIndex] = newId;
                        value = BuildIdString(idList);
                    }

                    // 方式二：下拉按名字选取（与手动输入ID等价，二选一即可）
                    DrawIdDropdown(fieldKey, idList[rowIndex], (selectedId) =>
                    {
                        idList[rowIndex] = selectedId;
                        value = BuildIdString(idList);
                    }, GUILayout.MinWidth(120));

                    Color prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
                    {
                        pendingRemoveIndex = rowIndex;
                        pendingRemoveFieldKey = fieldKey;
                    }
                    GUI.backgroundColor = prevColor;
                    EditorGUILayout.EndHorizontal();
                }

                if (idList.Count == 0)
                {
                    EditorGUILayout.LabelField("（空列表）", EditorStyles.centeredGreyMiniLabel);
                }

                GUILayout.Space(4);

                // 添加新ID行（手动输入ID 或 下拉按名字选取，二者等价，选/填后点 + 添加）
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("新ID:", GUILayout.Width(40));
                newIdInputs[fieldKey] = EditorGUILayout.LongField(newIdInputs[fieldKey], GUILayout.Width(90));
                DrawIdDropdown(fieldKey, newIdInputs[fieldKey], (selectedId) => newIdInputs[fieldKey] = selectedId, GUILayout.MinWidth(120));

                Color addPrevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
                if (GUILayout.Button("+ 添加", GUILayout.Width(60), GUILayout.Height(20)))
                {
                    if (newIdInputs[fieldKey] > 0)
                    {
                        idList.Add(newIdInputs[fieldKey]);
                        value = BuildIdString(idList);
                        newIdInputs[fieldKey] = 0;
                    }
                }
                GUI.backgroundColor = addPrevColor;
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            // 文本编辑模式（直接改逗号分隔串）
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            value = EditorGUILayout.TextArea(value, GUILayout.MinHeight(40));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        return value;
    }

    /// <summary>
    /// 由 ID→名字 映射构建下拉选项列表（按ID升序，显示为 "[id] 名字"）
    /// </summary>
    private void BuildOptionList(Dictionary<long, string> nameMap, List<long> idList, List<string> nameList)
    {
        idList.Clear();
        nameList.Clear();
        List<long> sortedIds = new List<long>(nameMap.Keys);
        sortedIds.Sort();
        foreach (long id in sortedIds)
        {
            idList.Add(id);
            nameList.Add($"[{id}] {nameMap[id]}");
        }
    }

    /// <summary>
    /// 按字段类型取下拉选项（场景字段取场景配置，敌人字段取NpcInfo配置）
    /// </summary>
    private void GetOptionsForField(string fieldKey, out List<long> ids, out List<string> names)
    {
        if (fieldKey.Contains("scene"))
        {
            ids = sceneOptionIds;
            names = sceneOptionNames;
        }
        else
        {
            ids = npcOptionIds;
            names = npcOptionNames;
        }
    }

    /// <summary>
    /// 绘制ID下拉选择框（按名字选取，选中后回调对应ID；当前ID不在选项中时显示空，可配合手动输入ID使用）
    /// </summary>
    private void DrawIdDropdown(string fieldKey, long currentId, Action<long> onSelected, params GUILayoutOption[] options)
    {
        GetOptionsForField(fieldKey, out List<long> ids, out List<string> names);
        if (ids.Count == 0)
        {
            EditorGUILayout.LabelField("(无配置数据)", EditorStyles.miniLabel, options);
            return;
        }
        int index = ids.IndexOf(currentId);
        int newIndex = EditorGUILayout.Popup(index, names.ToArray(), options);
        if (newIndex != index && newIndex >= 0 && newIndex < ids.Count)
        {
            onSelected(ids[newIndex]);
        }
    }

    /// <summary>
    /// 将逗号分隔的字符串解析为 long 列表（本表约定 "," 分割）
    /// </summary>
    private List<long> ParseIdList(string value)
    {
        List<long> result = new List<long>();
        if (string.IsNullOrEmpty(value))
            return result;

        string[] parts = value.Split(ListSeparator);
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;
            if (long.TryParse(trimmed, out long id))
                result.Add(id);
        }
        return result;
    }

    /// <summary>
    /// 将逗号分隔的字符串解析为 int 列表（difficulty_levels 用）
    /// </summary>
    private List<int> ParseIntList(string value)
    {
        List<int> result = new List<int>();
        if (string.IsNullOrEmpty(value))
            return result;

        string[] parts = value.Split(ListSeparator);
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;
            if (int.TryParse(trimmed, out int num))
                result.Add(num);
        }
        return result;
    }

    /// <summary>
    /// 将 long 列表组装为逗号分隔的字符串
    /// </summary>
    private string BuildIdString(List<long> idList)
    {
        if (idList == null || idList.Count == 0)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < idList.Count; i++)
        {
            sb.Append(idList[i]);
            if (i < idList.Count - 1)
                sb.Append(ListSeparator);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 将 int 列表组装为逗号分隔的字符串（调用前需自行排序）
    /// </summary>
    private string BuildIntString(List<int> numList)
    {
        if (numList == null || numList.Count == 0)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < numList.Count; i++)
        {
            sb.Append(numList[i]);
            if (i < numList.Count - 1)
                sb.Append(ListSeparator);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 绘制整数字段
    /// </summary>
    private int DrawIntField(GUIContent labelContent, int value, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        int result = EditorGUILayout.IntField(value);
        GUI.backgroundColor = prevColor;
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制浮点数字段
    /// </summary>
    private float DrawFloatField(GUIContent labelContent, float value, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        float result = EditorGUILayout.FloatField(value);
        GUI.backgroundColor = prevColor;
        EditorGUILayout.EndHorizontal();
        return result;
    }

    #endregion

    #region UI 绘制 - 操作按钮

    /// <summary>
    /// 绘制底部固定保存栏（保存按钮显示变更数、无变更时禁用；附重置按钮）
    /// </summary>
    private void DrawActionButtons()
    {
        int changes = CountChanges();

        Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 保存按钮（无变更时禁用，避免点开"没有检测到数据变更"的空弹窗）
        EditorGUI.BeginDisabledGroup(changes == 0);
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.30f, 0.55f, 0.90f);
        string saveText = changes > 0 ? $"保存到Excel并生成Json ({changes}项变更)" : "保存到Excel并生成Json";
        if (GUILayout.Button(saveText, GUILayout.Width(240), GUILayout.Height(30)))
        {
            SaveData();
        }
        GUI.backgroundColor = prevColor;
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(15);

        // 重置按钮
        if (GUILayout.Button("重置", GUILayout.Width(80), GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要重置当前数据吗？未保存的修改将丢失。", "确定", "取消"))
            {
                LoadData();
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    #endregion

    #region 保存/新增/删除逻辑

    /// <summary>
    /// 保存数据到Excel并重新生成Json（单个 EPPlus 会话按 id 定位行整行覆写，数值列写数值类型）
    /// </summary>
    private void SaveData()
    {
        if (currentBean == null) return;

        int changes = CountChanges();
        if (changes == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到数据变更", "确定");
            return;
        }

        // 确认保存
        if (!EditorUtility.DisplayDialog("确认保存", $"检测到 {changes} 个字段变更，确定保存到Excel并重新生成Json吗？", "保存", "取消"))
        {
            return;
        }

        try
        {
            using (ExcelPackage pack = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = pack.Workbook.Worksheets[SheetName];
                int row = FindRowById(sheet, currentBean.id);
                if (row < 0)
                {
                    EditorUtility.DisplayDialog("错误", $"Excel中未找到 ID={currentBean.id} 的行（可能已被删除），请刷新数据", "确定");
                    return;
                }
                WriteRowCells(sheet, row, currentBean);
                pack.Save();
            }

            // 重新生成Json并刷新资源
            RegenerateJson();

            // 保存后重载：尽量保持选中同一行；若难度筛选把该行滤掉了（difficulty_levels 被改掉），自动取消筛选
            long keepId = currentBean.id;
            LoadAllConfigFromExcel();
            RecomputeVisibleList();
            int keepIndex = visibleList.FindIndex(b => b.id == keepId);
            if (keepIndex < 0 && filterDifficulty > 0)
            {
                filterDifficulty = 0;
                RecomputeVisibleList();
                keepIndex = visibleList.FindIndex(b => b.id == keepId);
            }
            selectedRowIndex = keepIndex >= 0 ? keepIndex : 0;
            LoadData();

            EditorUtility.DisplayDialog("完成", "数据已保存到Excel并重新生成了Json文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}", "确定");
            LogUtil.LogError($"保存失败: {e}");
        }
    }

    /// <summary>
    /// 新增一行配置：id=当前最大id+1，适配难度=当前筛选难度（无筛选则沿用模板行），其余字段复制模板行（优先当前编辑行/表内末行），备注自动生成
    /// </summary>
    private void CreateNewRow()
    {
        // 模板：优先当前编辑行，其次表内末行
        FightTypeChallengeHundredInfoBean template = currentBean;
        if (template == null && allConfigList.Count > 0) template = allConfigList[allConfigList.Count - 1];

        long newId = 0;
        foreach (var bean in allConfigList)
        {
            if (bean.id > newId) newId = bean.id;
        }
        newId++;

        // 适配难度：优先取当前筛选难度，无筛选则沿用模板行（无模板则留空，任何难度都抽不中，需在编辑区勾选）
        string newDifficultyLevels = filterDifficulty > 0 ? filterDifficulty.ToString() : template?.difficulty_levels ?? "";
        string difficultyDesc = string.IsNullOrEmpty(newDifficultyLevels) ? "(空，需在编辑区勾选)" : newDifficultyLevels;

        string templateDesc = template != null ? $"ID {template.id}（{template.remark}）" : "内置默认值";
        if (!EditorUtility.DisplayDialog("确认新增",
            $"将新增一行挑战100勇士配置：\nID = {newId}\n适配难度 = {difficultyDesc}\n模板 = {templateDesc}\n\n确定写入Excel并重新生成Json吗？",
            "新增", "取消"))
        {
            return;
        }

        try
        {
            FightTypeChallengeHundredInfoBean newBean = new FightTypeChallengeHundredInfoBean();
            if (template != null)
            {
                // 复制模板全部字段（id 随后覆盖）
                FieldInfo[] fields = typeof(FightTypeChallengeHundredInfoBean).GetFields();
                foreach (FieldInfo f in fields)
                {
                    if (f.Name == "id") continue;
                    f.SetValue(newBean, f.GetValue(template));
                }
            }
            else
            {
                // 表内无任何行时的兜底默认值
                newBean.enemy_ids = "";
                newBean.attack_intensity_baserate = 1f;
                newBean.attack_show_time = 100f;
                newBean.road_num = "2";
                newBean.road_length = "10";
                newBean.fight_scene_ids = "";
                newBean.drop_crystal = 1;
                newBean.reward_crystal = "100-200";
                newBean.reward_equip_rarity = 1;
                newBean.reward_exp = 50;
            }
            newBean.id = newId;
            newBean.difficulty_levels = newDifficultyLevels;
            newBean.remark = $"挑战100勇士-配置{newId}";

            using (ExcelPackage pack = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = pack.Workbook.Worksheets[SheetName];
                int row = sheet.Dimension.End.Row + 1;
                WriteRowCells(sheet, row, newBean);
                pack.Save();
            }

            // 重新生成Json并刷新资源
            RegenerateJson();

            // 重载并选中新行（新行可能不满足当前筛选，先取消筛选保证可见）
            filterDifficulty = 0;
            LoadAllConfigFromExcel();
            RecomputeVisibleList();
            int newIndex = visibleList.FindIndex(b => b.id == newId);
            if (newIndex >= 0) selectedRowIndex = newIndex;
            LoadData();

            EditorUtility.DisplayDialog("完成", $"已新增 ID={newId} 的配置行并重新生成了Json文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"新增失败: {e.Message}", "确定");
            LogUtil.LogError($"新增失败: {e}");
        }
    }

    /// <summary>
    /// 删除当前编辑的配置行（确认后从 Excel 删行并重导 Json）
    /// </summary>
    private void DeleteCurrentRow()
    {
        if (currentBean == null) return;

        if (!EditorUtility.DisplayDialog("确认删除",
            $"确定删除 ID={currentBean.id}（{currentBean.remark}）的配置行吗？\n删除后立即写回Excel并重新生成Json，不可撤销。",
            "删除", "取消"))
        {
            return;
        }

        try
        {
            long deletedId = currentBean.id;
            using (ExcelPackage pack = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = pack.Workbook.Worksheets[SheetName];
                int row = FindRowById(sheet, deletedId);
                if (row < 0)
                {
                    EditorUtility.DisplayDialog("错误", $"Excel中未找到 ID={deletedId} 的行（可能已被删除），请刷新数据", "确定");
                    return;
                }
                sheet.DeleteRow(row);
                pack.Save();
            }

            // 重新生成Json并刷新资源
            RegenerateJson();

            // 重载并回落到首行
            LoadAllConfigFromExcel();
            selectedRowIndex = 0;
            RecomputeVisibleList();
            LoadData();

            EditorUtility.DisplayDialog("完成", $"已删除 ID={deletedId} 的配置行并重新生成了Json文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"删除失败: {e.Message}", "确定");
            LogUtil.LogError($"删除失败: {e}");
        }
    }

    /// <summary>
    /// 仅导出 Json（直接从当前 Excel 重新生成，不需要数据变更）
    /// </summary>
    private void ExportJsonOnly()
    {
        if (!File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在:\n{excelPath}", "确定");
            return;
        }

        try
        {
            RegenerateJson();
            EditorUtility.DisplayDialog("完成", "已从 Excel 重新导出 Json 文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
            LogUtil.LogError($"导出失败: {e}");
        }
    }

    /// <summary>
    /// 重新生成Json文件（走 ExcelUtil 通用导出：按工作表名匹配 Bean 类型，输出到 JsonText 并刷新 AssetDatabase）
    /// </summary>
    private void RegenerateJson()
    {
        ExcelUtil.ExcelToJsonItem(excelPath);
    }

    /// <summary>
    /// 按 id 定位数据行（第4行起为数据行；未找到返回 -1）
    /// </summary>
    private int FindRowById(ExcelWorksheet sheet, long id)
    {
        int rowCount = sheet.Dimension.End.Row;
        for (int y = 4; y <= rowCount; y++)
        {
            if (long.TryParse(sheet.Cells[y, 1].Text, out long cellId) && cellId == id)
                return y;
        }
        return -1;
    }

    /// <summary>
    /// 把 Bean 全字段写入指定行（按表头名列映射；数值列写数值类型，字符串 null 写空串）
    /// </summary>
    private void WriteRowCells(ExcelWorksheet sheet, int row, FightTypeChallengeHundredInfoBean bean)
    {
        int columnCount = sheet.Dimension.End.Column;
        for (int col = 1; col <= columnCount; col++)
        {
            string fieldName = sheet.Cells[1, col].Text;
            FieldInfo fieldInfo = typeof(FightTypeChallengeHundredInfoBean).GetField(fieldName);
            if (fieldInfo == null) continue;
            object value = fieldInfo.GetValue(bean);
            sheet.Cells[row, col].Value = value ?? "";
        }
    }

    #endregion

    #region 打开 Excel 表格

    /// <summary>
    /// 打开指定 Excel 表格
    /// </summary>
    private void OpenExcel(string path, string desc)
    {
        if (File.Exists(path))
        {
            System.Diagnostics.Process.Start(path);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"{desc} Excel文件不存在:\n{path}", "确定");
        }
    }

    #endregion
}
