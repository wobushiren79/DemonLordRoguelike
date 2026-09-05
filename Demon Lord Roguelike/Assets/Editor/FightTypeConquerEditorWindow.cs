using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模式难度编辑窗口
/// 用于可视化编辑 excel_fight_type_conquer_info[战斗-征服模式] 表
/// </summary>
public class FightTypeConquerEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/战斗模式难度编辑
    /// </summary>
    [MenuItem("游戏/战斗模式难度编辑")]
    private static void CreateWindow()
    {
        var window = EditorWindow.GetWindow<FightTypeConquerEditorWindow>();
        window.titleContent = new GUIContent("战斗模式难度编辑");
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    #endregion

    #region 成员变量

    /// <summary>Excel 文件路径</summary>
    private string excelPath;

    /// <summary>NpcInfo Excel 文件路径</summary>
    private string npcInfoExcelPath;

    /// <summary>战斗场景 Excel 文件路径</summary>
    private string fightSceneExcelPath;

    /// <summary>世界配置 Excel 文件路径</summary>
    private string worldInfoExcelPath;

    /// <summary>Json 输出目录</summary>
    private string jsonFolderPath;

    /// <summary>工作表名称</summary>
    private const string SheetName = "FightTypeConquerInfo";

    /// <summary>世界ID列表</summary>
    private List<long> worldIdList = new List<long>();

    /// <summary>世界名称列表</summary>
    private List<string> worldNameList = new List<string>();

    /// <summary>当前选中的世界索引</summary>
    private int selectedWorldIndex = 0;

    /// <summary>当前选中的难度等级 (1-10)</summary>
    private int selectedDifficulty = 1;

    /// <summary>当前编辑的数据</summary>
    private FightTypeConquerInfoBean currentBean;

    /// <summary>原始Bean用于对比变更</summary>
    private FightTypeConquerInfoBean originalBean;

    /// <summary>前后对比难度数量(各预览N个)</summary>
    private const int CompareRange = 3;

    /// <summary>前侧难度(level-1~level-3)的数据，只读用于对比(索引0=最近的level-1)</summary>
    private FightTypeConquerInfoBean[] prevBeans = new FightTypeConquerInfoBean[CompareRange];

    /// <summary>后侧难度(level+1~level+3)的数据，只读用于对比(索引0=最近的level+1)</summary>
    private FightTypeConquerInfoBean[] nextBeans = new FightTypeConquerInfoBean[CompareRange];

    /// <summary>对比列(前后难度)固定宽度</summary>
    private const float CompareColumnWidth = 64f;

    /// <summary>字段标签固定宽度</summary>
    private const float FieldLabelWidth = 170f;

    /// <summary>对比单元格样式(只读灰字)</summary>
    private GUIStyle compareCellStyle;

    /// <summary>对比单元格样式(差异高亮)</summary>
    private GUIStyle compareCellDiffStyle;

    /// <summary>ID列表对比单元格样式(名字换行显示)</summary>
    private GUIStyle compareCellWrapStyle;

    /// <summary>ID列表对比单元格样式(名字换行+差异高亮)</summary>
    private GUIStyle compareCellWrapDiffStyle;

    /// <summary>ID列表当前值摘要单元格样式(名字换行显示)</summary>
    private GUIStyle currentCellWrapStyle;

    /// <summary>对比列头样式(前后难度)</summary>
    private GUIStyle compareHeaderStyle;

    /// <summary>对比列头样式(当前列，高亮)</summary>
    private GUIStyle compareHeaderCurrentStyle;

    /// <summary>难度页签按钮样式</summary>
    private GUIStyle difficultyToggleStyle;

    /// <summary>未保存提示样式(橙色小字)</summary>
    private GUIStyle dirtyHintStyle;

    /// <summary>ID列表对比单元格高度(换行显示多个名字)</summary>
    private const float CompareWrapCellHeight = 54f;

    /// <summary>字段已修改时的编辑框背景色(淡黄)</summary>
    private static readonly Color ModifiedBgColor = new Color(1f, 0.93f, 0.55f);

    /// <summary>难度页签选中态背景色(蓝)</summary>
    private static readonly Color DifficultyOnBgColor = new Color(0.30f, 0.60f, 0.95f);

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
    private List<FightTypeConquerInfoBean> allConfigList = new List<FightTypeConquerInfoBean>();

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

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时初始化路径和加载数据
    /// </summary>
    private void OnEnable()
    {
        excelPath = Application.dataPath + "/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx";
        npcInfoExcelPath = Application.dataPath + "/Data/Excel/excel_npc_info[NPC信息].xlsx";
        fightSceneExcelPath = Application.dataPath + "/Data/Excel/excel_fight_scene[战斗场景].xlsx";
        worldInfoExcelPath = Application.dataPath + "/Data/Excel/excel_game_world_info[游戏世界信息].xlsx";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";

        LoadWorldData();
        LoadNpcInfoData();
        LoadFightSceneData();
        LoadAllConfigFromExcel();
    }

    /// <summary>
    /// GUI 渲染入口：顶部工具栏与选择区固定不滚动，中间编辑区滚动，底部保存栏固定
    /// </summary>
    private void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        // 顶部工具栏(刷新/导出/打开表格，固定)
        DrawToolbar();

        // 顶部选择区域(固定)
        DrawSelectionArea();

        // 数据编辑区域(滚动)
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (dataLoaded && currentBean != null)
        {
            DrawDataEditArea();
        }
        else if (dataLoaded && currentBean == null)
        {
            EditorGUILayout.HelpBox($"未找到世界ID {GetSelectedWorldId()} 难度 {selectedDifficulty} 的数据", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("请选择世界与难度，点击「加载数据」开始编辑", MessageType.Info);
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

        compareCellStyle = new GUIStyle(EditorStyles.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };

        compareCellDiffStyle = new GUIStyle(EditorStyles.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(1f, 0.78f, 0.35f) : new Color(0.80f, 0.45f, 0.0f) }
        };

        // ID列表对比单元格：小号字+自动换行，尽量在窄列内放下多个名字
        compareCellWrapStyle = new GUIStyle(EditorStyles.textField)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            fontSize = 10,
            padding = new RectOffset(2, 2, 2, 2),
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };

        compareCellWrapDiffStyle = new GUIStyle(compareCellWrapStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(1f, 0.78f, 0.35f) : new Color(0.80f, 0.45f, 0.0f) }
        };

        // ID列表当前值摘要格：与对比格同高同字号，但用正常字色突出"当前值"
        currentCellWrapStyle = new GUIStyle(compareCellWrapStyle)
        {
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(0.85f, 0.85f, 0.85f) : new Color(0.15f, 0.15f, 0.15f) }
        };

        compareHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        compareHeaderCurrentStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(0.55f, 0.80f, 1f) : new Color(0.10f, 0.35f, 0.75f) }
        };

        difficultyToggleStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
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
    /// 加载世界数据（直接从Json文件读取）
    /// </summary>
    private void LoadWorldData()
    {
        worldIdList.Clear();
        worldNameList.Clear();

        string worldJsonPath = jsonFolderPath + "/GameWorldInfo.txt";
        if (File.Exists(worldJsonPath))
        {
            try
            {
                string jsonText = File.ReadAllText(worldJsonPath);
                GameWorldInfoBean[] worldArray = JsonConvert.DeserializeObject<GameWorldInfoBean[]>(jsonText);
                if (worldArray != null && worldArray.Length > 0)
                {
                    foreach (var bean in worldArray)
                    {
                        worldIdList.Add(bean.id);
                        worldNameList.Add($"[{bean.id}] {bean.remark}");
                    }
                }
            }
            catch (Exception e)
            {
                LogUtil.LogError($"加载世界数据失败: {e.Message}");
            }
        }

        // 如果无法加载，使用默认值
        if (worldIdList.Count == 0)
        {
            worldIdList.Add(1);
            worldNameList.Add("[1] 剑与魔法");
            worldIdList.Add(2);
            worldNameList.Add("[2] 虚空魔界");
            worldIdList.Add(3);
            worldNameList.Add("[3] 刀与剑");
            worldIdList.Add(4);
            worldNameList.Add("[4] 魔法世界");
        }
    }

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
                FightTypeConquerInfoBean bean = new FightTypeConquerInfoBean();
                for (int col = 1; col <= columnCount; col++)
                {
                    string fieldName = sheet.Cells[1, col].Text;
                    string cellText = sheet.Cells[row, col].Text;

                    FieldInfo fieldInfo = typeof(FightTypeConquerInfoBean).GetField(fieldName);
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
    /// 获取当前选中的世界ID
    /// </summary>
    private long GetSelectedWorldId()
    {
        if (selectedWorldIndex >= 0 && selectedWorldIndex < worldIdList.Count)
        {
            return worldIdList[selectedWorldIndex];
        }
        return 1;
    }

    /// <summary>
    /// 加载指定世界和难度的数据
    /// </summary>
    private void LoadData()
    {
        long worldId = GetSelectedWorldId();

        currentBean = null;
        for (int i = 0; i < CompareRange; i++)
        {
            prevBeans[i] = null;
            nextBeans[i] = null;
        }
        foreach (var bean in allConfigList)
        {
            if (bean.world_id != worldId) continue;
            int offset = bean.level - selectedDifficulty;
            if (offset == 0) currentBean = bean;
            else if (offset >= -CompareRange && offset <= -1) prevBeans[-offset - 1] = bean;
            else if (offset >= 1 && offset <= CompareRange) nextBeans[offset - 1] = bean;
        }

        if (currentBean != null)
        {
            // 深拷贝一份原始数据用于对比
            originalBean = JsonConvert.DeserializeObject<FightTypeConquerInfoBean>(JsonConvert.SerializeObject(currentBean));
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
        FieldInfo[] fields = typeof(FightTypeConquerInfoBean).GetFields();
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
            LoadData();
            EditorUtility.DisplayDialog("完成", "已从Excel重新加载数据", "确定");
        }

        if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            ExportJsonOnly();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField("打开表格:", EditorStyles.miniLabel, GUILayout.Width(60));
        if (GUILayout.Button("难度配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenFightTypeConquerExcel();
        }
        if (GUILayout.Button("NPC配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenNpcInfoExcel();
        }
        if (GUILayout.Button("场景配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenFightSceneExcel();
        }
        if (GUILayout.Button("世界配置", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenWorldInfoExcel();
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 选择区域

    /// <summary>
    /// 绘制顶部选择区域（世界下拉 + 难度1-10页签 + 加载按钮，附当前编辑状态行）
    /// </summary>
    private void DrawSelectionArea()
    {
        EditorGUILayout.BeginVertical(selectionBoxStyle);

        EditorGUILayout.BeginHorizontal();

        // 世界选择
        EditorGUILayout.LabelField("世界", EditorStyles.boldLabel, GUILayout.Width(32));
        selectedWorldIndex = EditorGUILayout.Popup(selectedWorldIndex, worldNameList.ToArray(), GUILayout.Width(200), GUILayout.Height(22));

        GUILayout.Space(12);

        // 难度选择(1-10页签，选中态蓝色)
        EditorGUILayout.LabelField("难度", EditorStyles.boldLabel, GUILayout.Width(32));
        for (int d = 1; d <= 10; d++)
        {
            bool isOn = selectedDifficulty == d;
            Color prevBg = GUI.backgroundColor;
            if (isOn) GUI.backgroundColor = DifficultyOnBgColor;
            bool click = GUILayout.Toggle(isOn, d.ToString(), difficultyToggleStyle, GUILayout.Width(26), GUILayout.Height(22));
            GUI.backgroundColor = prevBg;
            if (click && !isOn) selectedDifficulty = d;
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

        EditorGUILayout.EndHorizontal();

        // 状态行：当前编辑信息 + 未加载/未保存提示
        if (dataLoaded && currentBean != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"当前编辑: ID={currentBean.id} | {currentBean.remark}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            // 选择已变更但尚未加载的提示，防止误以为切换即生效
            if (currentBean.world_id != GetSelectedWorldId() || currentBean.level != selectedDifficulty)
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

    #endregion

    #region UI 绘制 - 数据编辑区域

    /// <summary>
    /// 绘制数据编辑区域
    /// </summary>
    private void DrawDataEditArea()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        // 对比列头(前3难度 | 当前 | 后3难度)
        DrawCompareHeader();

        // 整难度一键复制
        DrawCopyAllButtons();

        // 基础信息
        DrawSectionTitle("基础信息");
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(FieldLabelWidth));
        EditorGUILayout.LongField(currentBean.id, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("世界ID:", GUILayout.Width(FieldLabelWidth));
        EditorGUILayout.LongField(currentBean.world_id, GUILayout.Width(200));
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("打开世界配置表", GUILayout.Width(110), GUILayout.Height(18)))
        {
            OpenWorldInfoExcel();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("难度:", GUILayout.Width(FieldLabelWidth));
        EditorGUILayout.IntField(currentBean.level, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        // 场景配置
        DrawSectionTitle("场景配置");
        currentBean.fight_scene_ids = DrawIdListField(new GUIContent("战斗场景列表", "fight_scene_ids：普通关卡的战斗场景池"), currentBean.fight_scene_ids, "fight_scene_ids");
        currentBean.fight_scene_boss_ids = DrawIdListField(new GUIContent("Boss战斗场景列表", "fight_scene_boss_ids：BOSS 关卡的战斗场景池"), currentBean.fight_scene_boss_ids, "fight_scene_boss_ids");

        // 敌人配置
        DrawSectionTitle("敌人配置");
        currentBean.enemy_ids = DrawIdListField(new GUIContent("敌人列表", "enemy_ids：普通敌人刷怪池"), currentBean.enemy_ids, "enemy_ids");
        currentBean.enemy_boss_ids = DrawIdListField(new GUIContent("Boss列表", "enemy_boss_ids：BOSS 关额外刷怪池"), currentBean.enemy_boss_ids, "enemy_boss_ids");
        currentBean.attack_boss_num = DrawStringField(new GUIContent("Boss数量", "attack_boss_num：x 固定 或 x-y 区间随机"), currentBean.attack_boss_num, "attack_boss_num");
        currentBean.attack_start_num = DrawIntField(new GUIContent("第一关敌人数量", "attack_start_num"), currentBean.attack_start_num, "attack_start_num");
        currentBean.attack_show_time = DrawFloatField(new GUIContent("进攻时间", "attack_show_time：单位秒"), currentBean.attack_show_time, "attack_show_time");
        currentBean.attack_num_addrate = DrawFloatField(new GUIContent("每关敌人倍数", "attack_num_addrate"), currentBean.attack_num_addrate, "attack_num_addrate");
        currentBean.attack_num_add = DrawIntField(new GUIContent("每关增加敌人数量", "attack_num_add"), currentBean.attack_num_add, "attack_num_add");
        currentBean.attack_intensity_baserate = DrawFloatField(new GUIContent("基础强度倍率", "attack_intensity_baserate：默认 1，每关都生效"), currentBean.attack_intensity_baserate, "attack_intensity_baserate");
        currentBean.attack_intensity_addrate = DrawFloatField(new GUIContent("每关强度倍率", "attack_intensity_addrate：作用于 HP/护甲/攻击"), currentBean.attack_intensity_addrate, "attack_intensity_addrate");

        // 关卡配置(单值"x"或区间"x-y")
        DrawSectionTitle("关卡配置");
        currentBean.fight_num = DrawStringField(new GUIContent("关卡次数", "fight_num：x 固定 或 x-y 区间随机"), currentBean.fight_num, "fight_num");
        currentBean.road_num = DrawStringField(new GUIContent("道路数量", "road_num：x 固定 或 x-y 区间随机"), currentBean.road_num, "road_num");
        currentBean.road_length = DrawStringField(new GUIContent("道路长度", "road_length：x 固定 或 x-y 区间随机"), currentBean.road_length, "road_length");

        // 难度与奖励
        DrawSectionTitle("难度与奖励");
        currentBean.drop_crystal = DrawIntField(new GUIContent("掉落魔晶", "drop_crystal"), currentBean.drop_crystal, "drop_crystal");
        //奖励魔晶: 由Excel类型行决定(重生成Bean后为string), 统一按字符串编辑, 支持单值"200"或区间"100-200"
        string rewardCrystalStr = DrawRewardCrystalField(new GUIContent("奖励-魔晶", "reward_crystal：x 固定 或 x-y 区间随机"), "reward_crystal");
        SetFieldValueAsString(currentBean, "reward_crystal", rewardCrystalStr);
        currentBean.reward_equip_rarity = DrawIntField(new GUIContent("奖励-装备稀有度", "reward_equip_rarity"), currentBean.reward_equip_rarity, "reward_equip_rarity");

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
    /// 绘制对比列头：前3难度 | 当前难度 | 后3难度（与字段行同网格对齐；缺失难度在列头标注"(无)"）
    /// </summary>
    private void DrawCompareHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);
        // 前3难度(远→近排列，贴近当前列的是level-1)
        for (int i = CompareRange; i >= 1; i--)
        {
            string text = $"难度 {selectedDifficulty - i}";
            if (prevBeans[i - 1] == null) text += "\n(无)";
            EditorGUILayout.LabelField(text, compareHeaderStyle, GUILayout.Width(CompareColumnWidth));
        }
        EditorGUILayout.LabelField($"▶ 当前 难度 {selectedDifficulty} ◀", compareHeaderCurrentStyle);
        // 后3难度(近→远排列)
        for (int i = 1; i <= CompareRange; i++)
        {
            string text = $"难度 {selectedDifficulty + i}";
            if (nextBeans[i - 1] == null) text += "\n(无)";
            EditorGUILayout.LabelField(text, compareHeaderStyle, GUILayout.Width(CompareColumnWidth));
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制整难度一键复制按钮（把前/后3个难度中任一个的全部参数复制到当前，id/world_id/level 保持不变）
    /// </summary>
    private void DrawCopyAllButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);

        // 前3难度(远→近排列，与列头对齐)
        for (int i = CompareRange - 1; i >= 0; i--)
        {
            FightTypeConquerInfoBean bean = prevBeans[i];
            int level = selectedDifficulty - (i + 1);
            EditorGUI.BeginDisabledGroup(bean == null);
            GUIContent content = new GUIContent($"难度{level}→", $"复制难度 {level} 全部数值到当前难度");
            if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Width(CompareColumnWidth), GUILayout.Height(20)))
            {
                CopyAllFrom(bean);
            }
            EditorGUI.EndDisabledGroup();
        }

        GUILayout.FlexibleSpace();

        // 后3难度(近→远排列，与列头对齐)
        for (int i = 0; i < CompareRange; i++)
        {
            FightTypeConquerInfoBean bean = nextBeans[i];
            int level = selectedDifficulty + (i + 1);
            EditorGUI.BeginDisabledGroup(bean == null);
            GUIContent content = new GUIContent($"←难度{level}", $"复制难度 {level} 全部数值到当前难度");
            if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Width(CompareColumnWidth), GUILayout.Height(20)))
            {
                CopyAllFrom(bean);
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 用源难度的全部参数覆盖当前难度（跳过 id/world_id/level，需保存后写回Excel）
    /// </summary>
    private void CopyAllFrom(FightTypeConquerInfoBean src)
    {
        if (src == null || currentBean == null) return;

        if (!EditorUtility.DisplayDialog("确认复制",
            $"确定用难度 {src.level} 的全部参数覆盖当前难度 {currentBean.level} 吗？\n(ID/世界ID/难度 保持不变，复制后仍需点击保存才写回Excel)",
            "复制", "取消"))
        {
            return;
        }

        FieldInfo[] fields = typeof(FightTypeConquerInfoBean).GetFields();
        foreach (FieldInfo f in fields)
        {
            if (f.Name == "id" || f.Name == "world_id" || f.Name == "level") continue;
            f.SetValue(currentBean, f.GetValue(src));
        }
        GUI.FocusControl(null);
    }

    /// <summary>
    /// 通过反射读取指定Bean字段的字符串值（用于对比只读展示）
    /// </summary>
    private string GetFieldValueStr(FightTypeConquerInfoBean bean, string fieldName)
    {
        if (bean == null) return "-";
        FieldInfo f = typeof(FightTypeConquerInfoBean).GetField(fieldName);
        if (f == null) return "-";
        object v = f.GetValue(bean);
        return v?.ToString() ?? "";
    }

    /// <summary>
    /// 通过反射读取指定Bean字段的原始装箱值（用于复制时按原类型赋值）
    /// </summary>
    private object GetFieldValueObject(FightTypeConquerInfoBean bean, string fieldName)
    {
        if (bean == null) return null;
        FieldInfo f = typeof(FightTypeConquerInfoBean).GetField(fieldName);
        return f?.GetValue(bean);
    }

    /// <summary>
    /// 绘制单个对比单元格：只读展示相邻难度值，与当前值不同则高亮，点击可复制到当前
    /// </summary>
    /// <returns>被点击(需复制)返回 true</returns>
    private bool DrawCompareCell(FightTypeConquerInfoBean bean, string fieldName, string currentValueStr)
    {
        if (bean == null)
        {
            EditorGUILayout.LabelField("", compareCellStyle, GUILayout.Width(CompareColumnWidth), GUILayout.Height(18));
            return false;
        }
        string text = GetFieldValueStr(bean, fieldName);
        bool differs = text != currentValueStr;
        GUIStyle style = differs ? compareCellDiffStyle : compareCellStyle;
        // 单元格宽度有限，tooltip放完整值避免长文本被截断后不可读
        GUIContent content = new GUIContent(text, $"{text}\n点击复制到当前难度");
        return GUILayout.Button(content, style, GUILayout.Width(CompareColumnWidth), GUILayout.Height(18));
    }

    /// <summary>
    /// 绘制一侧(前或后)最多3个难度的对比单元格，点击某个单元格即复制其值到当前难度
    /// </summary>
    /// <param name="beans">该侧难度Bean数组(索引0=最近难度)</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="currentValueStr">当前值的字符串形式(用于差异高亮)</param>
    /// <param name="nearOnRight">true=最近难度排最右(前侧)；false=最近难度排最左(后侧)</param>
    /// <returns>被点击的Bean(无点击返回null)</returns>
    private FightTypeConquerInfoBean DrawCompareCells(FightTypeConquerInfoBean[] beans, string fieldName, string currentValueStr, bool nearOnRight)
    {
        FightTypeConquerInfoBean clicked = null;
        if (nearOnRight)
        {
            for (int i = beans.Length - 1; i >= 0; i--)
            {
                if (DrawCompareCell(beans[i], fieldName, currentValueStr)) clicked = beans[i];
            }
        }
        else
        {
            for (int i = 0; i < beans.Length; i++)
            {
                if (DrawCompareCell(beans[i], fieldName, currentValueStr)) clicked = beans[i];
            }
        }
        return clicked;
    }

    /// <summary>
    /// 绘制一侧(前或后)最多3个难度的ID列表对比单元格：单元格内换行显示具体名字，点击复制到当前难度
    /// </summary>
    private FightTypeConquerInfoBean DrawIdListCompareCells(FightTypeConquerInfoBean[] beans, string fieldKey, string currentValueStr, bool nearOnRight)
    {
        FightTypeConquerInfoBean clicked = null;
        if (nearOnRight)
        {
            for (int i = beans.Length - 1; i >= 0; i--)
            {
                if (DrawIdListCompareCell(beans[i], fieldKey, currentValueStr)) clicked = beans[i];
            }
        }
        else
        {
            for (int i = 0; i < beans.Length; i++)
            {
                if (DrawIdListCompareCell(beans[i], fieldKey, currentValueStr)) clicked = beans[i];
            }
        }
        return clicked;
    }

    /// <summary>
    /// 绘制单个ID列表对比单元格：名字以、连接并自动换行直观展示，tooltip 显示完整 "id 名字" 列表
    /// </summary>
    private bool DrawIdListCompareCell(FightTypeConquerInfoBean bean, string fieldKey, string currentValueStr)
    {
        if (bean == null)
        {
            EditorGUILayout.LabelField("", compareCellWrapStyle, GUILayout.Width(CompareColumnWidth), GUILayout.Height(CompareWrapCellHeight));
            return false;
        }
        string rawValue = GetFieldValueStr(bean, fieldKey);
        string cellText = GetIdListCellText(rawValue, fieldKey, out string tooltipList);
        bool differs = rawValue != currentValueStr;
        GUIStyle style = differs ? compareCellWrapDiffStyle : compareCellWrapStyle;
        GUIContent content = new GUIContent(cellText, $"{tooltipList}\n点击复制到当前难度");
        return GUILayout.Button(content, style, GUILayout.Width(CompareColumnWidth), GUILayout.Height(CompareWrapCellHeight));
    }

    /// <summary>
    /// 把 & 分隔的ID串解析成单元格显示文本（名字以、连接）与 tooltip 完整列表（每行 "id 名字"）
    /// </summary>
    private string GetIdListCellText(string rawValue, string fieldKey, out string tooltipList)
    {
        List<long> ids = ParseIdList(rawValue);
        if (ids.Count == 0)
        {
            tooltipList = "(空)";
            return "(空)";
        }
        List<string> names = new List<string>();
        List<string> lines = new List<string>();
        foreach (long id in ids)
        {
            string name = GetDisplayName(id, fieldKey);
            names.Add(name);
            lines.Add($"{id} {name}");
        }
        tooltipList = string.Join("\n", lines);
        return string.Join("、", names);
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
    /// 绘制字符串字段（左=前3难度 / 中=当前可编辑 / 右=后3难度）
    /// </summary>
    private string DrawStringField(GUIContent labelContent, string value, string fieldName)
    {
        string result = value;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, value, true);
        result = DrawCurrentTextField(result, IsFieldModified(fieldName));
        var clickedNext = DrawCompareCells(nextBeans, fieldName, value, false);
        var clicked = clickedPrev != null ? clickedPrev : clickedNext;
        if (clicked != null)
        {
            result = (string)GetFieldValueObject(clicked, fieldName) ?? "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制奖励魔晶字段（统一按字符串编辑：单值"200"固定 或 区间"100-200"随机）
    /// reward_crystal 字段类型由 Excel 类型行决定，Bean 重生成后为 string；旧 int 过渡态经反射读写兼容，任意阶段可编译
    /// </summary>
    private string DrawRewardCrystalField(GUIContent labelContent, string fieldName)
    {
        string result = GetFieldValueStr(currentBean, fieldName);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, result, true);
        result = DrawCurrentTextField(result, IsFieldModified(fieldName));
        var clickedNext = DrawCompareCells(nextBeans, fieldName, result, false);
        var clicked = clickedPrev != null ? clickedPrev : clickedNext;
        if (clicked != null)
        {
            result = GetFieldValueStr(clicked, fieldName);
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 按字段原始类型把字符串写回字段（string 直接赋值；int 过渡态解析数字后赋值，解析失败保持原值）
    /// </summary>
    private void SetFieldValueAsString(FightTypeConquerInfoBean bean, string fieldName, string value)
    {
        if (bean == null) return;
        FieldInfo f = typeof(FightTypeConquerInfoBean).GetField(fieldName);
        if (f == null) return;
        if (f.FieldType == typeof(string))
        {
            f.SetValue(bean, value);
        }
        else if (f.FieldType == typeof(int) && int.TryParse(value, out int intValue))
        {
            //旧int过渡态(Bean未重生成前): 数字字符串解析后写回
            f.SetValue(bean, intValue);
        }
    }

    /// <summary>
    /// 绘制ID列表字段（首行=标签+展开计数+模式切换+快捷开表；次行=前后难度对比+当前值摘要；展开后为列表/文本编辑区）
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

        // 首行：标签 + 展开foldout(共N个) + 编辑模式切换 + 快捷打开配置表
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        listFoldoutStates[fieldKey] = EditorGUILayout.Foldout(listFoldoutStates[fieldKey], $"共 {idList.Count} 个", true);

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
                OpenFightSceneExcel();
            }
        }
        // 敌人字段：在列表旁提供「打开NpcInfo配置表」按钮（打开NpcInfo Excel）
        else if (fieldKey.Contains("enemy"))
        {
            if (GUILayout.Button("打开NpcInfo配置表", EditorStyles.miniButton, GUILayout.Width(120)))
            {
                OpenNpcInfoExcel();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 次行：前后3难度对比 + 中间当前值摘要(与标量字段同一网格对齐；差异高亮，点击对比格复制到当前；完整列表见tooltip)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);
        var clickedPrev = DrawIdListCompareCells(prevBeans, fieldKey, value, true);
        string currentCellText = GetIdListCellText(value, fieldKey, out string currentTooltip);
        Color summaryBg = GUI.backgroundColor;
        if (IsFieldModified(fieldKey)) GUI.backgroundColor = ModifiedBgColor;
        EditorGUILayout.LabelField(new GUIContent(currentCellText, currentTooltip), currentCellWrapStyle, GUILayout.Height(CompareWrapCellHeight));
        GUI.backgroundColor = summaryBg;
        var clickedNext = DrawIdListCompareCells(nextBeans, fieldKey, value, false);
        EditorGUILayout.EndHorizontal();
        var clickedBean = clickedPrev != null ? clickedPrev : clickedNext;
        if (clickedBean != null)
        {
            value = GetFieldValueStr(clickedBean, fieldKey);
            idList = ParseIdList(value);
            GUI.FocusControl(null);
        }

        GUILayout.Space(2);

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
                    long newId = EditorGUILayout.LongField(idList[rowIndex], GUILayout.Width(70));
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
                newIdInputs[fieldKey] = EditorGUILayout.LongField(newIdInputs[fieldKey], GUILayout.Width(70));
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
            // 文本编辑模式（直接改 & 分隔串）
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            value = EditorGUILayout.TextArea(value, GUILayout.MinHeight(40));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        return value;
    }

    /// <summary>
    /// 根据字段类型获取显示名字
    /// </summary>
    private string GetDisplayName(long id, string fieldKey)
    {
        // 场景字段查场景映射
        if (fieldKey.Contains("scene"))
        {
            if (sceneNameMap.TryGetValue(id, out string sceneName))
                return sceneName;
        }
        // 敌人字段查NPC映射
        else if (fieldKey.Contains("enemy"))
        {
            if (npcNameMap.TryGetValue(id, out string npcName))
                return npcName;
        }
        return "(未知)";
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
    /// 将 & 分隔的字符串解析为 long 列表
    /// </summary>
    private List<long> ParseIdList(string value)
    {
        List<long> result = new List<long>();
        if (string.IsNullOrEmpty(value))
            return result;

        string[] parts = value.Split('&');
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
    /// 将 long 列表组装为 & 分隔的字符串
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
                sb.Append("&");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 绘制整数字段（左=前3难度 / 中=当前可编辑 / 右=后3难度）
    /// </summary>
    private int DrawIntField(GUIContent labelContent, int value, string fieldName)
    {
        int result = value;
        string valueStr = value.ToString();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, valueStr, true);
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        result = EditorGUILayout.IntField(result);
        GUI.backgroundColor = prevColor;
        var clickedNext = DrawCompareCells(nextBeans, fieldName, valueStr, false);
        var clicked = clickedPrev != null ? clickedPrev : clickedNext;
        if (clicked != null)
        {
            result = (int)GetFieldValueObject(clicked, fieldName);
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制浮点数字段（左=前3难度 / 中=当前可编辑 / 右=后3难度）
    /// </summary>
    private float DrawFloatField(GUIContent labelContent, float value, string fieldName)
    {
        float result = value;
        string valueStr = value.ToString();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, valueStr, true);
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        result = EditorGUILayout.FloatField(result);
        GUI.backgroundColor = prevColor;
        var clickedNext = DrawCompareCells(nextBeans, fieldName, valueStr, false);
        var clicked = clickedPrev != null ? clickedPrev : clickedNext;
        if (clicked != null)
        {
            result = (float)GetFieldValueObject(clicked, fieldName);
            GUI.FocusControl(null);
        }
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

    #region 保存逻辑

    /// <summary>
    /// 保存数据到Excel并重新生成Json
    /// </summary>
    private void SaveData()
    {
        if (currentBean == null) return;

        // 检查是否有变更
        List<ExcelUtil.ExcelChangeData> changeDataList = new List<ExcelUtil.ExcelChangeData>();

        // 对比所有字段
        FieldInfo[] fields = typeof(FightTypeConquerInfoBean).GetFields();
        foreach (FieldInfo field in fields)
        {
            if (field.Name == "id") continue; // ID不修改

            object currentValue = field.GetValue(currentBean);
            object originalValue = originalBean != null ? field.GetValue(originalBean) : null;

            if (!Equals(currentValue, originalValue))
            {
                string valueStr = currentValue?.ToString() ?? "";
                changeDataList.Add(new ExcelUtil.ExcelChangeData(currentBean.id, field.Name, valueStr));
            }
        }

        if (changeDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到数据变更", "确定");
            return;
        }

        // 确认保存
        if (!EditorUtility.DisplayDialog("确认保存", $"检测到 {changeDataList.Count} 个字段变更，确定保存到Excel并重新生成Json吗？", "保存", "取消"))
        {
            return;
        }

        try
        {
            // 保存到Excel
            ExcelUtil.SetExcelData(excelPath, SheetName, changeDataList);

            // 重新生成Json
            RegenerateJson();

            // 刷新资源
            AssetDatabase.Refresh();

            // 重新加载数据
            LoadAllConfigFromExcel();
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
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "已从 Excel 重新导出 Json 文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
            LogUtil.LogError($"导出失败: {e}");
        }
    }

    /// <summary>
    /// 重新生成Json文件
    /// </summary>
    private void RegenerateJson()
    {
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

            Assembly assembly = Assembly.Load("Assembly-CSharp");
            Type type = assembly.GetType("FightTypeConquerInfoBean");

            if (type == null)
            {
                LogUtil.LogError("未找到 FightTypeConquerInfoBean 类型");
                return;
            }

            List<object> listData = new List<object>();

            for (int row = 4; row <= rowCount; row++)
            {
                object o = assembly.CreateInstance(type.ToString());

                for (int col = 1; col <= columnCount; col++)
                {
                    string fieldName = sheet.Cells[1, col].Text;

                    FieldInfo fieldInfo = type.GetField(fieldName);
                    if (fieldInfo == null) continue;

                    string cellText = sheet.Cells[row, col].Text;

                    if (string.IsNullOrEmpty(cellText))
                    {
                        if (fieldInfo.FieldType == typeof(int) || fieldInfo.FieldType == typeof(float) ||
                            fieldInfo.FieldType == typeof(double) || fieldInfo.FieldType == typeof(long))
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
                        fieldInfo.SetValue(o, value);
                    }
                    catch
                    {
                        LogUtil.LogError($"字段 {fieldName} 值 {cellText} 转换失败");
                    }
                }

                listData.Add(o);
            }

            // 写入Json文件
            string jsonPath = $"{jsonFolderPath}/{SheetName}.txt";
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
            }

            string jsonData = JsonUtil.ToJsonByNet(listData.ToArray());
            File.WriteAllText(jsonPath, jsonData);

            LogUtil.Log($"Json 重新生成完成: {jsonPath}");
        });
    }

    #endregion

    #region 打开 Excel 表格

    /// <summary>
    /// 打开NpcInfo Excel表格
    /// </summary>
    private void OpenNpcInfoExcel()
    {
        if (File.Exists(npcInfoExcelPath))
        {
            System.Diagnostics.Process.Start(npcInfoExcelPath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"NpcInfo Excel文件不存在:\n{npcInfoExcelPath}", "确定");
        }
    }

    /// <summary>
    /// 打开战斗模式难度 Excel表格
    /// </summary>
    private void OpenFightTypeConquerExcel()
    {
        if (File.Exists(excelPath))
        {
            System.Diagnostics.Process.Start(excelPath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"战斗模式难度 Excel文件不存在:\n{excelPath}", "确定");
        }
    }

    /// <summary>
    /// 打开战斗场景 Excel表格
    /// </summary>
    private void OpenFightSceneExcel()
    {
        if (File.Exists(fightSceneExcelPath))
        {
            System.Diagnostics.Process.Start(fightSceneExcelPath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"战斗场景 Excel文件不存在:\n{fightSceneExcelPath}", "确定");
        }
    }

    /// <summary>
    /// 打开世界配置 Excel表格
    /// </summary>
    private void OpenWorldInfoExcel()
    {
        if (File.Exists(worldInfoExcelPath))
        {
            System.Diagnostics.Process.Start(worldInfoExcelPath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"世界配置 Excel文件不存在:\n{worldInfoExcelPath}", "确定");
        }
    }

    #endregion
}
