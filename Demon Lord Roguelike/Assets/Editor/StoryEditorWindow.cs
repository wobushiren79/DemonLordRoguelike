using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 故事演出编辑器
/// 编辑 StoryInfo(故事)/StoryDetailsInfo(步骤)/StoryTalkInfo(对话) 三张配置表 + excel_language 对应 sheet，
/// 保存时按「删除(行号降序)->修改->新增」单会话写回 xlsx，并重新导出运行时 JSON。
/// 约定：故事名/对话内容的 textId == 业务行 id（与 ConversationCouncilorInfo 同例）；步骤执行顺序只由 step_order 决定。
/// </summary>
public class StoryEditorWindow : EditorWindow
{
    #region 路径与常量
    private const string SheetStory = "StoryInfo";
    private const string SheetDetails = "StoryDetailsInfo";
    private const string SheetTalk = "StoryTalkInfo";
    private static string ExcelDir => Application.dataPath + "/Data/Excel";
    private static string ExcelPathStory => ExcelDir + "/excel_story_info[故事信息].xlsx";
    private static string ExcelPathDetails => ExcelDir + "/excel_story_details_info[故事详情信息].xlsx";
    private static string ExcelPathTalk => ExcelDir + "/excel_story_talk_info[故事对话信息].xlsx";
    private static string ExcelPathLanguage => ExcelDir + "/excel_language[多语言_FrameWork].xlsx";
    /// <summary>镜头目标标记(基地建筑/通用),与 StoryHandler.GetStoryMarkerPosition 保持一致</summary>
    private static readonly string[] CameraMarkers = { "back", "self", "core", "portal", "gashapon", "juicer", "altar", "vat", "achievement", "council" };
    #endregion

    #region 行数据模型
    /// <summary>故事行(StoryInfo + 语言表名字)</summary>
    private class StoryRow
    {
        public long id;
        public string nameCn = "";
        public int triggerType = 1;
        public int sceneType = 1;
        public int triggerCondition = 1;
        public int priority;
        public bool isOnce = true;
        public bool valid = true;
        public string remark = "";
        public bool isNew;
        private string snapshot = "";
        /// <summary>提交快照(保存/加载后调用)</summary>
        public void CommitSnapshot() { snapshot = $"{triggerType}|{sceneType}|{triggerCondition}|{priority}|{isOnce}|{valid}|{remark}|{nameCn}"; isNew = false; }
        /// <summary>相对快照是否有改动</summary>
        public bool IsDirty => isNew || snapshot != $"{triggerType}|{sceneType}|{triggerCondition}|{priority}|{isOnce}|{valid}|{remark}|{nameCn}";
    }

    /// <summary>步骤行(StoryDetailsInfo)</summary>
    private class StepRow
    {
        public long id;
        public long storyId;
        public int stepOrder;
        public int stepType = 1;
        public bool isAsync;
        public string param1 = "";
        public string param2 = "";
        public string param3 = "";
        public string param4 = "";
        public string remark = "";
        public bool isNew;
        public bool foldout = true;
        private string snapshot = "";
        public void CommitSnapshot() { snapshot = $"{storyId}|{stepOrder}|{stepType}|{isAsync}|{param1}|{param2}|{param3}|{param4}|{remark}"; isNew = false; }
        public bool IsDirty => isNew || snapshot != $"{storyId}|{stepOrder}|{stepType}|{isAsync}|{param1}|{param2}|{param3}|{param4}|{remark}";
    }

    /// <summary>对话行(StoryTalkInfo + 语言表内容)</summary>
    private class TalkRow
    {
        public long id;
        public long storyId;
        public long npcId;
        public string contentCn = "";
        public string remark = "";
        public bool isNew;
        private string snapshot = "";
        public void CommitSnapshot() { snapshot = $"{storyId}|{npcId}|{contentCn}|{remark}"; isNew = false; }
        public bool IsDirty => isNew || snapshot != $"{storyId}|{npcId}|{contentCn}|{remark}";
    }
    #endregion

    #region 字段
    private List<StoryRow> allStories = new List<StoryRow>();
    private List<StepRow> allSteps = new List<StepRow>();
    private List<TalkRow> allTalks = new List<TalkRow>();
    private readonly HashSet<long> deletedStoryIds = new HashSet<long>();
    private readonly HashSet<long> deletedStepIds = new HashSet<long>();
    private readonly HashSet<long> deletedTalkIds = new HashSet<long>();

    private StoryRow selectedStory;
    private string searchText = "";
    private Vector2 scrollStoryList;
    private Vector2 scrollStepList;
    private Vector2 scrollStoryField;
    private Vector2 scrollTalkList;

    //NPC下拉缓存(0=旁白 + NpcInfo 全表)
    private long[] npcIds;
    private string[] npcLabels;
    //对话选择下拉缓存(按故事过滤:仅当前故事的对话 + story_id=0 的通用对话)
    private string[] talkLabels;
    private List<TalkRow> filteredTalks;
    private long talkLabelsForStoryId = -1;
    #endregion

    #region 窗口生命周期
    /// <summary>
    /// 打开故事演出编辑器
    /// </summary>
    [MenuItem("游戏/故事演出编辑")]
    private static void CreateWindow()
    {
        var window = GetWindow<StoryEditorWindow>();
        window.titleContent = new GUIContent("故事演出编辑");
        window.minSize = new Vector2(1500, 750);
        window.Show();
    }

    /// <summary>
    /// 打开窗口时加载三表与语言表数据
    /// </summary>
    private void OnEnable()
    {
        LoadAll();
    }
    #endregion

    #region 数据加载
    /// <summary>
    /// 从 xlsx 加载全部数据(三表业务行 + 语言表中文文本)
    /// </summary>
    private void LoadAll()
    {
        allStories.Clear();
        allSteps.Clear();
        allTalks.Clear();
        deletedStoryIds.Clear();
        deletedStepIds.Clear();
        deletedTalkIds.Clear();
        selectedStory = null;

        LoadStoryRows();
        LoadStepRows();
        LoadTalkRows();
        LoadLanguageText(SheetStory, (id, cn) =>
        {
            var story = allStories.Find(s => s.id == id);
            if (story != null) { story.nameCn = cn; }
        });
        LoadLanguageText(SheetTalk, (id, cn) =>
        {
            var talk = allTalks.Find(t => t.id == id);
            if (talk != null) { talk.contentCn = cn; }
        });

        foreach (var s in allStories) s.CommitSnapshot();
        foreach (var s in allSteps) s.CommitSnapshot();
        foreach (var t in allTalks) t.CommitSnapshot();
        npcIds = null;
        talkLabels = null;
        if (allStories.Count > 0)
            selectedStory = allStories[0];
    }

    /// <summary>
    /// 读取故事表
    /// </summary>
    private void LoadStoryRows()
    {
        ReadSheet(ExcelPathStory, SheetStory, (sheet, colMap, row) =>
        {
            var story = new StoryRow();
            story.id = GetCellLong(sheet, row, colMap, "id");
            story.triggerType = GetCellInt(sheet, row, colMap, "trigger_type", 1);
            story.sceneType = GetCellInt(sheet, row, colMap, "scene_type", 1);
            story.triggerCondition = GetCellInt(sheet, row, colMap, "trigger_condition", 1);
            story.priority = GetCellInt(sheet, row, colMap, "priority");
            story.isOnce = GetCellInt(sheet, row, colMap, "is_once", 1) == 1;
            story.valid = GetCellInt(sheet, row, colMap, "valid", 1) != 0;
            story.remark = GetCellText(sheet, row, colMap, "remark");
            allStories.Add(story);
        });
        allStories.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 读取步骤表
    /// </summary>
    private void LoadStepRows()
    {
        ReadSheet(ExcelPathDetails, SheetDetails, (sheet, colMap, row) =>
        {
            var step = new StepRow();
            step.id = GetCellLong(sheet, row, colMap, "id");
            step.storyId = GetCellLong(sheet, row, colMap, "story_id");
            step.stepOrder = GetCellInt(sheet, row, colMap, "step_order", 1);
            step.stepType = GetCellInt(sheet, row, colMap, "step_type", 1);
            step.isAsync = GetCellInt(sheet, row, colMap, "is_async") == 1;
            step.param1 = GetCellText(sheet, row, colMap, "param_1");
            step.param2 = GetCellText(sheet, row, colMap, "param_2");
            step.param3 = GetCellText(sheet, row, colMap, "param_3");
            step.param4 = GetCellText(sheet, row, colMap, "param_4");
            step.remark = GetCellText(sheet, row, colMap, "remark");
            allSteps.Add(step);
        });
    }

    /// <summary>
    /// 读取对话表
    /// </summary>
    private void LoadTalkRows()
    {
        ReadSheet(ExcelPathTalk, SheetTalk, (sheet, colMap, row) =>
        {
            var talk = new TalkRow();
            talk.id = GetCellLong(sheet, row, colMap, "id");
            talk.storyId = GetCellLong(sheet, row, colMap, "story_id");
            talk.npcId = GetCellLong(sheet, row, colMap, "npc_id");
            talk.remark = GetCellText(sheet, row, colMap, "remark");
            allTalks.Add(talk);
        });
        allTalks.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 读取语言表指定 sheet 的中文文本并按 id 回填(英文及其他语种不在编辑器展示/编辑,由语言表人工补录)
    /// </summary>
    private void LoadLanguageText(string sheetName, Action<long, string> actionFill)
    {
        ReadSheet(ExcelPathLanguage, sheetName, (sheet, colMap, row) =>
        {
            long id = GetCellLong(sheet, row, colMap, "id");
            string cn = GetCellText(sheet, row, colMap, "content_cn");
            actionFill(id, cn);
        });
    }

    /// <summary>
    /// 通用读表:打开 xlsx 指定 sheet,逐数据行(第4行起)回调
    /// </summary>
    private void ReadSheet(string excelPath, string sheetName, Action<ExcelWorksheet, Dictionary<string, int>, int> actionRow)
    {
        var fileInfo = new FileInfo(excelPath);
        if (!fileInfo.Exists)
        {
            LogUtil.LogError($"故事演出编辑器:找不到配置表 {excelPath}");
            return;
        }
        ExcelUtil.GetExcelPackage(fileInfo, ep =>
        {
            var sheet = ep.Workbook.Worksheets[sheetName];
            if (sheet == null)
            {
                LogUtil.LogError($"故事演出编辑器:表 {fileInfo.Name} 中找不到工作表 {sheetName}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                if (!long.TryParse(sheet.Cells[row, 1].Text, out _))
                    continue;
                actionRow(sheet, colMap, row);
            }
        });
    }
    #endregion

    #region Excel 单元格辅助
    /// <summary>
    /// 构建 表头名->列号 映射
    /// </summary>
    private Dictionary<string, int> BuildColMap(ExcelWorksheet sheet)
    {
        var colMap = new Dictionary<string, int>();
        for (int col = 1; col <= sheet.Dimension.End.Column; col++)
        {
            string header = sheet.Cells[1, col].Text;
            if (!string.IsNullOrEmpty(header) && !colMap.ContainsKey(header))
                colMap.Add(header, col);
        }
        return colMap;
    }

    /// <summary>
    /// 按表头取单元格文本(列不存在/空返回"")
    /// </summary>
    private string GetCellText(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header)
    {
        if (!colMap.TryGetValue(header, out int col))
            return "";
        return sheet.Cells[row, col].Text ?? "";
    }

    private long GetCellLong(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, long defaultValue = 0)
    {
        string text = GetCellText(sheet, row, colMap, header);
        return long.TryParse(text, out long value) ? value : defaultValue;
    }

    private int GetCellInt(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, int defaultValue = 0)
    {
        string text = GetCellText(sheet, row, colMap, header);
        return int.TryParse(text, out int value) ? value : defaultValue;
    }

    /// <summary>
    /// 按 id 定位数据行(未找到返回-1)
    /// </summary>
    private int FindRowById(ExcelWorksheet sheet, long id)
    {
        for (int row = 4; row <= sheet.Dimension.End.Row; row++)
        {
            if (long.TryParse(sheet.Cells[row, 1].Text, out long cellId) && cellId == id)
                return row;
        }
        return -1;
    }

    /// <summary>
    /// 按表头写单元格(long 写数值,保持列类型)
    /// </summary>
    private void SetCellLong(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, long value)
    {
        if (colMap.TryGetValue(header, out int col))
            sheet.Cells[row, col].Value = value;
    }

    /// <summary>
    /// 按表头写单元格文本
    /// </summary>
    private void SetCellText(ExcelWorksheet sheet, int row, Dictionary<string, int> colMap, string header, string value)
    {
        if (colMap.TryGetValue(header, out int col))
            sheet.Cells[row, col].Value = value ?? "";
    }
    #endregion

    #region 界面绘制
    /// <summary>
    /// 绘制窗口(工具栏 + 左故事列表/故事字段/步骤编排/对话列表 四栏)
    /// </summary>
    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        DrawStoryListColumn();
        DrawStoryFieldColumn();
        DrawStepListColumn();
        DrawTalkListColumn();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 顶部工具栏:保存/重载/打开配置表/提示
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("💾 保存全部变更", EditorStyles.toolbarButton, GUILayout.Width(110)))
            SaveAll();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("🔄 重新加载", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (!HasAnyChange() || EditorUtility.DisplayDialog("放弃变更", "存在未保存的变更，重新加载将丢失它们。继续？", "放弃变更", "取消"))
                LoadAll();
        }
        if (GUILayout.Button("📂 故事表", EditorStyles.toolbarButton, GUILayout.Width(70))) OpenExcel(ExcelPathStory);
        if (GUILayout.Button("📂 步骤表", EditorStyles.toolbarButton, GUILayout.Width(70))) OpenExcel(ExcelPathDetails);
        if (GUILayout.Button("📂 对话表", EditorStyles.toolbarButton, GUILayout.Width(70))) OpenExcel(ExcelPathTalk);
        if (GUILayout.Button("📂 语言表", EditorStyles.toolbarButton, GUILayout.Width(70))) OpenExcel(ExcelPathLanguage);
        GUILayout.FlexibleSpace();
        GUILayout.Label(GetChangeSummary(), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 打开指定 Excel 文件
    /// </summary>
    private void OpenExcel(string path)
    {
        if (File.Exists(path))
            Application.OpenURL("file:///" + path);
        else
            EditorUtility.DisplayDialog("提示", $"文件不存在:\n{path}", "确定");
    }

    /// <summary>
    /// 是否存在未保存变更
    /// </summary>
    private bool HasAnyChange()
    {
        return deletedStoryIds.Count > 0 || deletedStepIds.Count > 0 || deletedTalkIds.Count > 0
            || allStories.Exists(s => s.IsDirty) || allSteps.Exists(s => s.IsDirty) || allTalks.Exists(t => t.IsDirty);
    }

    /// <summary>
    /// 变更统计文本(工具栏右侧显示)
    /// </summary>
    private string GetChangeSummary()
    {
        if (!HasAnyChange())
            return "无变更 | 英文及其他语种请到语言表补录";
        int addS = allStories.FindAll(s => s.isNew).Count, modS = allStories.FindAll(s => !s.isNew && s.IsDirty).Count;
        int addD = allSteps.FindAll(s => s.isNew).Count, modD = allSteps.FindAll(s => !s.isNew && s.IsDirty).Count;
        int addT = allTalks.FindAll(t => t.isNew).Count, modT = allTalks.FindAll(t => !t.isNew && t.IsDirty).Count;
        return $"变更: 故事+{addS}/改{modS}/删{deletedStoryIds.Count} 步骤+{addD}/改{modD}/删{deletedStepIds.Count} 对话+{addT}/改{modT}/删{deletedTalkIds.Count}";
    }

    /// <summary>
    /// 左栏:故事列表(搜索/选择/新增/删除)
    /// </summary>
    private void DrawStoryListColumn()
    {
        EditorGUILayout.BeginVertical("HelpBox", GUILayout.Width(250), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("故事列表", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        scrollStoryList = EditorGUILayout.BeginScrollView(scrollStoryList);
        foreach (var story in allStories)
        {
            if (!string.IsNullOrEmpty(searchText) && !story.id.ToString().Contains(searchText) && !(story.nameCn ?? "").Contains(searchText) && !(story.remark ?? "").Contains(searchText))
                continue;
            bool isSelected = selectedStory == story;
            GUI.backgroundColor = isSelected ? new Color(0.55f, 0.75f, 1f) : (story.valid ? Color.white : Color.gray);
            string label = $"[{story.id}] {story.nameCn}{(story.isNew ? " (新)" : "")}{(story.valid ? "" : " (无效)")}";
            if (GUILayout.Button(label, GUILayout.Height(22)))
                selectedStory = story;
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndScrollView();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("+ 新增故事"))
            AddStory();
        GUI.backgroundColor = new Color(0.9f, 0.45f, 0.45f);
        using (new EditorGUI.DisabledScope(selectedStory == null))
        {
            if (GUILayout.Button("- 删除选中故事"))
                DeleteStory(selectedStory);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 中栏:选中故事的字段编辑
    /// </summary>
    private void DrawStoryFieldColumn()
    {
        EditorGUILayout.BeginVertical("HelpBox", GUILayout.Width(340), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("故事字段", EditorStyles.boldLabel);
        if (selectedStory == null)
        {
            EditorGUILayout.HelpBox("左侧选择或新增一个故事", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        scrollStoryField = EditorGUILayout.BeginScrollView(scrollStoryField);
        var story = selectedStory;
        EditorGUILayout.LabelField("故事ID", story.id.ToString());
        story.nameCn = EditorGUILayout.TextField(new GUIContent("名字(中文)", "写回语言表 StoryInfo sheet 的 content_cn;textId 约定=故事ID;英文及其他语种请到语言表补录"), story.nameCn);
        story.triggerType = (int)(StoryTriggerTypeEnum)EditorGUILayout.EnumPopup(new GUIContent("触发类型", "第一期只有引导;剧情为预留"), (StoryTriggerTypeEnum)story.triggerType);
        story.sceneType = (int)(StorySceneTypeEnum)EditorGUILayout.EnumPopup(new GUIContent("演出场景", "播放演出时所在场景"), (StorySceneTypeEnum)story.sceneType);
        story.triggerCondition = (int)(StoryTriggerConditionEnum)EditorGUILayout.EnumPopup(new GUIContent("触发条件", "新增条件需同步扩展 StoryHandler"), (StoryTriggerConditionEnum)story.triggerCondition);
        story.priority = EditorGUILayout.IntField(new GUIContent("优先级", "同条件多个未播故事时,值小先播"), story.priority);
        story.isOnce = EditorGUILayout.Toggle(new GUIContent("只播一次", "播完记录到独立存档 UserStory_{slot}(UserStoryBean.dicPlayedStory)"), story.isOnce);
        story.valid = EditorGUILayout.Toggle(new GUIContent("有效", "无效的故事不导出到运行时 JSON"), story.valid);
        EditorGUILayout.LabelField("备注");
        story.remark = EditorGUILayout.TextArea(story.remark, GUILayout.Height(40));
        EditorGUILayout.Space(6);
        var steps = GetStepsForStory(story.id);
        int talkCount = 0;
        foreach (var s in steps)
            if (s.stepType == (int)StoryStepTypeEnum.Talk)
                talkCount += ParseLongList(s.param1).Count;
        EditorGUILayout.LabelField($"步骤统计: 共 {steps.Count} 步 / 对话 {talkCount} 句", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 步骤编排栏
    /// <summary>
    /// 右栏:选中故事的步骤编排(排序/增删/类型与参数编辑)
    /// </summary>
    private void DrawStepListColumn()
    {
        EditorGUILayout.BeginVertical("HelpBox", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("步骤编排(按序号升序执行)", EditorStyles.boldLabel);
        if (selectedStory == null)
        {
            EditorGUILayout.HelpBox("左侧选择或新增一个故事", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        var steps = GetStepsForStory(selectedStory.id);
        int moveIndex = -1, moveDir = 0, insertIndex = -1;
        StepRow pendingDelete = null;
        scrollStepList = EditorGUILayout.BeginScrollView(scrollStepList);
        for (int i = 0; i < steps.Count; i++)
        {
            DrawStepRow(steps[i], i, steps.Count, ref moveIndex, ref moveDir, ref insertIndex, ref pendingDelete);
        }
        EditorGUILayout.EndScrollView();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("+ 添加步骤(末尾)"))
            AddStep(selectedStory);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
        //延迟执行结构变更(避免遍历中改集合)
        if (insertIndex >= 0)
            InsertStepBefore(selectedStory, insertIndex);
        if (moveIndex >= 0)
            MoveStep(selectedStory.id, moveIndex, moveDir);
        if (pendingDelete != null)
            DeleteStep(pendingDelete);
    }

    /// <summary>
    /// 绘制单个步骤行(折叠头:序号/类型/并发/插入/上下移/删除;展开后按类型画参数)
    /// </summary>
    private void DrawStepRow(StepRow step, int index, int count, ref int moveIndex, ref int moveDir, ref int insertIndex, ref StepRow pendingDelete)
    {
        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.BeginHorizontal();
        step.foldout = EditorGUILayout.Foldout(step.foldout, $"步骤 {step.stepOrder}{(step.isNew ? " (新)" : "")}", true);
        var newStepType = (StoryStepTypeEnum)EditorGUILayout.EnumPopup((StoryStepTypeEnum)step.stepType, GUILayout.Width(90));
        if (newStepType != (StoryStepTypeEnum)step.stepType)
            step.stepType = (int)newStepType;
        step.isAsync = EditorGUILayout.ToggleLeft(new GUIContent("并发", "勾选=发起后立即执行下一步(如镜头移动与对话同时进行);不勾=等本步完成"), step.isAsync, GUILayout.Width(50));
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button(new GUIContent("➕", "在此步骤之前插入新步骤"), GUILayout.Width(24)))
            insertIndex = index;
        GUI.backgroundColor = Color.white;
        using (new EditorGUI.DisabledScope(index == 0))
            if (GUILayout.Button("↑", GUILayout.Width(24))) { moveIndex = index; moveDir = -1; }
        using (new EditorGUI.DisabledScope(index == count - 1))
            if (GUILayout.Button("↓", GUILayout.Width(24))) { moveIndex = index; moveDir = 1; }
        GUI.backgroundColor = new Color(0.9f, 0.45f, 0.45f);
        if (GUILayout.Button("×", GUILayout.Width(24)))
            pendingDelete = step;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        if (step.foldout)
        {
            EditorGUI.indentLevel++;
            DrawStepParams(step);
            EditorGUILayout.LabelField("备注", GUILayout.Width(110));
            step.remark = EditorGUILayout.TextField(step.remark);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 按步骤类型绘制参数编辑(标签语义与 StoryDetailsInfoBeanPartial 注释保持一致)
    /// </summary>
    private void DrawStepParams(StepRow step)
    {
        switch ((StoryStepTypeEnum)step.stepType)
        {
            case StoryStepTypeEnum.Talk:
                DrawTalkStepParams(step);
                break;
            case StoryStepTypeEnum.CameraMove:
                DrawMarkerField(new GUIContent("目标标记", "back=回演出起始位;基地=self/core/portal/gashapon/juicer/altar/vat/achievement/council;战斗=core"), step, 1);
                DrawFloatField(new GUIContent("时长(秒)", "默认1"), step, 2, 1f);
                DrawIntField(new GUIContent("缓动序号", "0=DOTween默认缓动,其余按 DG.Tweening.Ease 强转"), step, 3, 0);
                break;
            case StoryStepTypeEnum.Wait:
                DrawFloatField(new GUIContent("等待(秒)", "实时,不受 timeScale 影响"), step, 1, 0f);
                break;
            case StoryStepTypeEnum.Effect:
                DrawLongField(new GUIContent("特效ID", "EffectInfo.id"), step, 1);
                DrawMarkerField(new GUIContent("目标标记(可空)", "空=战斗防守核心/基地魔王位"), step, 2);
                DrawFloatField(new GUIContent("尺寸倍率", "默认1"), step, 3, 1f);
                break;
            case StoryStepTypeEnum.Audio:
                DrawLongField(new GUIContent("音效ID", "AudioInfo.id"), step, 1);
                break;
            case StoryStepTypeEnum.Fade:
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("方向", "out=淡出变黑 / in=淡入"), GUILayout.Width(110));
                int fadeIndex = step.param1 == "out" ? 0 : (step.param1 == "in" ? 1 : -1);
                int newFadeIndex = EditorGUILayout.Popup(fadeIndex, new[] { "out(淡出变黑)", "in(淡入)" });
                if (newFadeIndex >= 0 && newFadeIndex != fadeIndex)
                    step.param1 = newFadeIndex == 0 ? "out" : "in";
                EditorGUILayout.EndHorizontal();
                DrawFloatField(new GUIContent("时长(秒)", "默认0.5"), step, 2, 0.5f);
                break;
        }
    }

    /// <summary>
    /// 通用文本参数行
    /// </summary>
    private void DrawMarkerField(GUIContent label, StepRow step, int paramIndex)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(110));
        string value = GetStepParam(step, paramIndex);
        string newValue = EditorGUILayout.TextField(value);
        int markerIndex = System.Array.FindIndex(CameraMarkers, m => string.Equals(m, newValue, System.StringComparison.OrdinalIgnoreCase));
        int newMarkerIndex = EditorGUILayout.Popup(markerIndex, CameraMarkers, GUILayout.Width(110));
        if (newMarkerIndex != markerIndex && newMarkerIndex >= 0)
            newValue = CameraMarkers[newMarkerIndex];
        if (newValue != value)
            SetStepParam(step, paramIndex, newValue);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 通用 float 参数行(写回字符串参数列,定点格式)
    /// </summary>
    private void DrawFloatField(GUIContent label, StepRow step, int paramIndex, float defaultValue)
    {
        string value = GetStepParam(step, paramIndex);
        float number = float.TryParse(value, out float v) ? v : defaultValue;
        float newNumber = EditorGUILayout.FloatField(label, number);
        if (newNumber != number || (value == "" && newNumber != defaultValue))
            SetStepParam(step, paramIndex, newNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 通用 int 参数行
    /// </summary>
    private void DrawIntField(GUIContent label, StepRow step, int paramIndex, int defaultValue)
    {
        string value = GetStepParam(step, paramIndex);
        int number = int.TryParse(value, out int v) ? v : defaultValue;
        int newNumber = EditorGUILayout.IntField(label, number);
        if (newNumber != number)
            SetStepParam(step, paramIndex, newNumber.ToString());
    }

    /// <summary>
    /// 通用 long 参数行
    /// </summary>
    private void DrawLongField(GUIContent label, StepRow step, int paramIndex)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(110));
        string value = GetStepParam(step, paramIndex);
        long number = long.TryParse(value, out long v) ? v : 0;
        long newNumber = EditorGUILayout.LongField(number);
        if (newNumber != number)
            SetStepParam(step, paramIndex, newNumber.ToString());
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 取步骤参数字符串
    /// </summary>
    private string GetStepParam(StepRow step, int paramIndex)
    {
        switch (paramIndex)
        {
            case 1: return step.param1;
            case 2: return step.param2;
            case 3: return step.param3;
            case 4: return step.param4;
            default: return "";
        }
    }

    /// <summary>
    /// 写步骤参数字符串
    /// </summary>
    private void SetStepParam(StepRow step, int paramIndex, string value)
    {
        switch (paramIndex)
        {
            case 1: step.param1 = value; break;
            case 2: step.param2 = value; break;
            case 3: step.param3 = value; break;
            case 4: step.param4 = value; break;
        }
    }
    #endregion

    #region 对话步骤编辑
    /// <summary>
    /// 对话步骤参数:对话ID串编辑 + 下拉追加(从右侧对话列表选择) + 引用对话只读预览(对话的增删改统一在右侧对话列表)
    /// </summary>
    private void DrawTalkStepParams(StepRow step)
    {
        BuildTalkOptions(selectedStory.id);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("对话ID(&连播)", "同一步内顺序连播多句,每句各等一次点击;对话的编辑/新增/删除统一在右侧对话列表"), GUILayout.Width(110));
        step.param1 = EditorGUILayout.TextField(step.param1);
        int pick = EditorGUILayout.Popup(new GUIContent("", "从本故事对话列表选择追加(story_id=0 的通用对话也会显示);需要引用其它故事的对话可直接在左侧文本框手输ID"), 0, talkLabels, GUILayout.Width(180));
        if (pick > 0)
            AppendTalkId(step, filteredTalks[pick - 1].id);
        EditorGUILayout.EndHorizontal();
        //引用对话的只读预览(编辑统一在右侧对话列表)
        BuildNpcOptions();
        var talkIds = ParseLongList(step.param1);
        foreach (var talkId in talkIds)
        {
            var talk = allTalks.Find(t => t.id == talkId);
            if (talk == null)
            {
                EditorGUILayout.HelpBox($"对话 id:{talkId} 不存在(保存校验会拦截)", MessageType.Error);
                continue;
            }
            int npcIndex = System.Array.IndexOf(npcIds, talk.npcId);
            string npcLabel = npcIndex > 0 ? npcLabels[npcIndex] : "旁白";
            string cnPreview = (talk.contentCn ?? "").Replace("\n", " ");
            if (cnPreview.Length > 24) cnPreview = cnPreview.Substring(0, 24) + "…";
            EditorGUILayout.LabelField($"   [{talk.id}] {npcLabel}: {cnPreview}", EditorStyles.miniLabel);
        }
    }
    #endregion

    #region 对话列表面板
    /// <summary>
    /// 右栏:本故事对话列表(含说话NPC;自由新增/编辑/删除;步骤编排只负责从这里选择引用,不混在一起)
    /// </summary>
    private void DrawTalkListColumn()
    {
        EditorGUILayout.BeginVertical("HelpBox", GUILayout.Width(400), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("对话列表(本故事+通用)", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        using (new EditorGUI.DisabledScope(selectedStory == null))
        {
            if (GUILayout.Button("+ 新增对话", GUILayout.Width(100)))
                AddTalk();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        if (selectedStory == null)
        {
            EditorGUILayout.HelpBox("左侧选择或新增一个故事", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        var talks = GetTalksForStory(selectedStory.id);
        TalkRow pendingDelete = null;
        scrollTalkList = EditorGUILayout.BeginScrollView(scrollTalkList);
        foreach (var talk in talks)
        {
            if (DrawTalkEditor(talk))
                pendingDelete = talk;
        }
        if (talks.Count == 0)
            EditorGUILayout.HelpBox("本故事还没有对话,点上方「+ 新增对话」创建。", MessageType.Info);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        //延迟执行删除(避免遍历中改集合)
        if (pendingDelete != null)
            DeleteTalk(pendingDelete);
    }

    /// <summary>
    /// 单条对话的编辑器(npc下拉/中文/备注/删除);返回 true 表示点了删除(由调用方延迟处理)
    /// </summary>
    private bool DrawTalkEditor(TalkRow talk)
    {
        bool wantDelete = false;
        BuildNpcOptions();
        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"对话 [{talk.id}]{(talk.isNew ? " (新)" : "")}", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField(new GUIContent($"所属故事: {(talk.storyId == 0 ? "通用" : talk.storyId.ToString())}", "新增对话自动绑定当前故事;通用对话(story_id=0)在所有故事的下拉中都可见"), EditorStyles.miniLabel, GUILayout.Width(90));
        int npcIndex = System.Array.IndexOf(npcIds, talk.npcId);
        if (npcIndex < 0) npcIndex = 0;
        int newNpcIndex = EditorGUILayout.Popup(npcIndex, npcLabels);
        if (newNpcIndex != npcIndex)
            talk.npcId = npcIds[newNpcIndex];
        GUI.backgroundColor = new Color(0.9f, 0.45f, 0.45f);
        if (GUILayout.Button("×", GUILayout.Width(24)))
            wantDelete = true;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("内容(中文)");
        talk.contentCn = EditorGUILayout.TextArea(talk.contentCn, GUILayout.Height(34));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("备注", GUILayout.Width(60));
        talk.remark = EditorGUILayout.TextField(talk.remark);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        return wantDelete;
    }

    /// <summary>
    /// 获取指定故事可见的对话列表(本故事 + story_id=0 通用对话,按 id 升序)
    /// </summary>
    private List<TalkRow> GetTalksForStory(long storyId)
    {
        return allTalks.FindAll(t => t.storyId == storyId || t.storyId == 0);
    }

    /// <summary>
    /// 新增对话(id=最大id+1,默认旁白,绑定当前故事)
    /// </summary>
    private void AddTalk()
    {
        long newId = 1;
        foreach (var t in allTalks)
            if (t.id >= newId) newId = t.id + 1;
        var talk = new TalkRow { id = newId, storyId = selectedStory.id, npcId = 0, remark = "新对话", isNew = true };
        allTalks.Add(talk);
        talkLabels = null;
    }

    /// <summary>
    /// 删除对话(二次确认;若被步骤引用,确认后自动从各步骤 param_1 移除该引用)
    /// </summary>
    private void DeleteTalk(TalkRow talk)
    {
        var referencing = new List<string>();
        foreach (var s in allSteps)
        {
            if (s.stepType == (int)StoryStepTypeEnum.Talk && ParseLongList(s.param1).Contains(talk.id))
                referencing.Add($"故事[{s.storyId}]步骤{s.stepOrder}");
        }
        string refTip = referencing.Count > 0 ? $"\n它被 {referencing.Count} 个步骤引用: {string.Join("、", referencing)}\n删除后将自动从这些步骤移除引用。" : "";
        if (!EditorUtility.DisplayDialog("删除对话", $"确定删除对话 [{talk.id}] 吗？{refTip}", "删除", "取消"))
            return;
        //从所有步骤的引用中移除
        foreach (var s in allSteps)
        {
            if (s.stepType != (int)StoryStepTypeEnum.Talk) continue;
            var ids = ParseLongList(s.param1);
            if (ids.Contains(talk.id))
            {
                ids.Remove(talk.id);
                s.param1 = string.Join("&", ids);
            }
        }
        allTalks.Remove(talk);
        if (!talk.isNew)
            deletedTalkIds.Add(talk.id);
        talkLabels = null;
    }
    #endregion

    #region 对话步骤辅助

    /// <summary>
    /// 追加对话ID到步骤 param_1(& 分隔)
    /// </summary>
    private void AppendTalkId(StepRow step, long talkId)
    {
        step.param1 = string.IsNullOrEmpty(step.param1) ? talkId.ToString() : step.param1 + "&" + talkId;
    }

    /// <summary>
    /// 解析 & 分隔的 long 列表(非法片段跳过)
    /// </summary>
    private List<long> ParseLongList(string text)
    {
        var list = new List<long>();
        if (string.IsNullOrEmpty(text))
            return list;
        var parts = text.Split('&');
        foreach (var part in parts)
        {
            if (long.TryParse(part.Trim(), out long value))
                list.Add(value);
        }
        return list;
    }
    #endregion

    #region 结构操作(增删故事/步骤/对话,步骤排序)
    /// <summary>
    /// 获取指定故事的步骤列表(按 step_order 升序)
    /// </summary>
    private List<StepRow> GetStepsForStory(long storyId)
    {
        var steps = allSteps.FindAll(s => s.storyId == storyId);
        steps.Sort((a, b) => a.stepOrder.CompareTo(b.stepOrder));
        return steps;
    }

    /// <summary>
    /// 新增故事(id=最大id+1,默认引导类型/基地场景/首次进基地)
    /// </summary>
    private void AddStory()
    {
        long newId = 1;
        foreach (var s in allStories)
            if (s.id >= newId) newId = s.id + 1;
        var story = new StoryRow { id = newId, nameCn = "新故事", remark = "新故事", isNew = true };
        allStories.Add(story);
        selectedStory = story;
    }

    /// <summary>
    /// 删除故事(级联删除其步骤;仅被该故事引用的对话列为孤儿提示,不删除)
    /// </summary>
    private void DeleteStory(StoryRow story)
    {
        if (story == null)
            return;
        var storySteps = GetStepsForStory(story.id);
        //统计仅被本故事引用的孤儿对话
        var orphanTalks = new List<long>();
        var usedElsewhere = new HashSet<long>();
        foreach (var s in allSteps)
            if (s.storyId != story.id && s.stepType == (int)StoryStepTypeEnum.Talk)
                foreach (var id in ParseLongList(s.param1)) usedElsewhere.Add(id);
        foreach (var s in storySteps)
        {
            if (s.stepType != (int)StoryStepTypeEnum.Talk) continue;
            foreach (var id in ParseLongList(s.param1))
                if (!usedElsewhere.Contains(id) && !orphanTalks.Contains(id)) orphanTalks.Add(id);
        }
        //绑定到本故事的对话也算孤儿(删除后在其它故事的对话下拉中不可见)
        foreach (var t in allTalks)
            if (t.storyId == story.id && !usedElsewhere.Contains(t.id) && !orphanTalks.Contains(t.id))
                orphanTalks.Add(t.id);
        string orphanTip = orphanTalks.Count > 0 ? $"\n以下对话仅被本故事引用,删除后将成为孤儿(不会删除,可手动清理): {string.Join(",", orphanTalks)}" : "";
        if (!EditorUtility.DisplayDialog("删除故事", $"确定删除故事 [{story.id}] {story.nameCn} 及其 {storySteps.Count} 个步骤吗？{orphanTip}", "删除", "取消"))
            return;
        foreach (var s in storySteps)
            DeleteStep(s);
        allStories.Remove(story);
        if (!story.isNew)
            deletedStoryIds.Add(story.id);
        if (selectedStory == story)
            selectedStory = allStories.Count > 0 ? allStories[0] : null;
    }

    /// <summary>
    /// 添加步骤到末尾(step_order=末尾,id 取本故事最大id+1保持号段内聚)
    /// </summary>
    private void AddStep(StoryRow story)
    {
        var steps = GetStepsForStory(story.id);
        var step = new StepRow { id = GetNextStepId(story.id), storyId = story.id, stepOrder = steps.Count + 1, isNew = true, foldout = true };
        allSteps.Add(step);
    }

    /// <summary>
    /// 在指定下标之前插入步骤(该位置及之后的步骤 step_order 顺延 +1)
    /// </summary>
    private void InsertStepBefore(StoryRow story, int index)
    {
        var steps = GetStepsForStory(story.id);
        int newOrder = index + 1;
        foreach (var s in steps)
            if (s.stepOrder >= newOrder) s.stepOrder++;
        var step = new StepRow { id = GetNextStepId(story.id), storyId = story.id, stepOrder = newOrder, isNew = true, foldout = true };
        allSteps.Add(step);
        NormalizeStepOrder(story.id);
    }

    /// <summary>
    /// 取下一个步骤 id(本故事现有步骤最大id+1,保持 story_id*1000 号段内聚;无步骤时取 story_id*1000+1)
    /// </summary>
    private long GetNextStepId(long storyId)
    {
        long maxId = 0;
        foreach (var s in allSteps)
            if (s.storyId == storyId && s.id > maxId) maxId = s.id;
        return maxId > 0 ? maxId + 1 : storyId * 1000 + 1;
    }

    /// <summary>
    /// 删除步骤(非新增行登记 deletedStepIds,保存时删行;删除后重排序号)
    /// </summary>
    private void DeleteStep(StepRow step)
    {
        allSteps.Remove(step);
        if (!step.isNew)
            deletedStepIds.Add(step.id);
        NormalizeStepOrder(step.storyId);
    }

    /// <summary>
    /// 上下移动步骤(交换 step_order 后归一化为 1..N)
    /// </summary>
    private void MoveStep(long storyId, int index, int direction)
    {
        var steps = GetStepsForStory(storyId);
        int target = index + direction;
        if (target < 0 || target >= steps.Count)
            return;
        (steps[index].stepOrder, steps[target].stepOrder) = (steps[target].stepOrder, steps[index].stepOrder);
        NormalizeStepOrder(storyId);
    }

    /// <summary>
    /// 步骤序号归一化(排序后重写为 1..N)
    /// </summary>
    private void NormalizeStepOrder(long storyId)
    {
        var steps = GetStepsForStory(storyId);
        for (int i = 0; i < steps.Count; i++)
            steps[i].stepOrder = i + 1;
    }

    #endregion

    #region 下拉选项构建
    /// <summary>
    /// 构建 NPC 下拉选项(0=旁白 + NpcInfo 全表,名字取 Language_NpcInfo_cn)
    /// </summary>
    private void BuildNpcOptions()
    {
        if (npcIds != null)
            return;
        var ids = new List<long> { 0 };
        var labels = new List<string> { "[0] 旁白(无立绘)" };
        var nameMap = LoadLanguageCnMap("NpcInfo");
        var npcArray = NpcInfoCfg.GetAllArrayData();
        for (int i = 0; i < npcArray.Length; i++)
        {
            var npc = npcArray[i];
            ids.Add(npc.id);
            nameMap.TryGetValue(npc.name, out string cn);
            labels.Add($"[{npc.id}] {cn}");
        }
        npcIds = ids.ToArray();
        npcLabels = labels.ToArray();
    }

    /// <summary>
    /// 构建对话选择下拉选项(按故事过滤:仅当前故事的对话 + story_id=0 的通用对话;首项为占位,与 filteredTalks 下标+1 对齐)
    /// </summary>
    private void BuildTalkOptions(long storyId)
    {
        if (talkLabels != null && talkLabelsForStoryId == storyId)
            return;
        BuildNpcOptions();
        filteredTalks = GetTalksForStory(storyId);
        var labels = new List<string> { "选择对话追加…" };
        foreach (var talk in filteredTalks)
        {
            int npcIndex = System.Array.IndexOf(npcIds, talk.npcId);
            string npcLabel = npcIndex > 0 ? npcLabels[npcIndex] : "旁白";
            string cnPreview = (talk.contentCn ?? "").Replace("\n", " ");
            if (cnPreview.Length > 16) cnPreview = cnPreview.Substring(0, 16) + "…";
            string commonTag = talk.storyId == 0 ? "[通用]" : "";
            labels.Add($"[{talk.id}]{commonTag} {npcLabel}: {cnPreview}");
        }
        talkLabels = labels.ToArray();
        talkLabelsForStoryId = storyId;
    }

    /// <summary>
    /// 读取指定语言 sheet 的中文 JSON 产物,构建 textId->中文 映射(文件不存在返回空表)
    /// </summary>
    private Dictionary<long, string> LoadLanguageCnMap(string sheetName)
    {
        var map = new Dictionary<long, string>();
        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Resources/JsonText/Language_{sheetName}_cn.txt");
        if (textAsset == null)
            return map;
        var rows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LanguageBean>>(textAsset.text);
        if (rows != null)
            foreach (var r in rows)
                if (!map.ContainsKey(r.id)) map.Add(r.id, r.content);
        return map;
    }
    #endregion

    #region 保存与校验
    /// <summary>
    /// 保存全部变更:校验 -> 确认 -> 四个 xlsx 各自单会话写回 -> 重新导出 JSON -> 提交快照
    /// </summary>
    private void SaveAll()
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        ValidateAll(errors, warnings);
        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("校验失败", string.Join("\n", errors), "确定");
            return;
        }
        if (!HasAnyChange())
        {
            EditorUtility.DisplayDialog("提示", "没有检测到变更。", "确定");
            return;
        }
        if (warnings.Count > 0 && !EditorUtility.DisplayDialog("校验警告", string.Join("\n", warnings) + "\n\n仍要保存吗？", "保存", "取消"))
            return;
        if (!EditorUtility.DisplayDialog("确认保存", GetChangeSummary() + "\n确定写入 Excel 并重新导出 JSON 吗？", "保存", "取消"))
            return;
        try
        {
            SaveStoryFile();
            SaveDetailsFile();
            SaveTalkFile();
            SaveLanguageFile();
            //重新导出运行时 JSON(业务表 + 语言表全部 sheet)
            ExcelUtil.ExcelToJsonItem(ExcelPathStory);
            ExcelUtil.ExcelToJsonItem(ExcelPathDetails);
            ExcelUtil.ExcelToJsonItem(ExcelPathTalk);
            ExcelUtil.ExcelToJsonItem(ExcelPathLanguage);
            AssetDatabase.Refresh();
            //提交快照并清空删除登记
            foreach (var s in allStories) s.CommitSnapshot();
            foreach (var s in allSteps) s.CommitSnapshot();
            foreach (var t in allTalks) t.CommitSnapshot();
            deletedStoryIds.Clear();
            deletedStepIds.Clear();
            deletedTalkIds.Clear();
            EditorUtility.DisplayDialog("完成", "已保存到 Excel，并重新导出了 StoryInfo/StoryDetailsInfo/StoryTalkInfo 与语言表 JSON。", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}\n(请确认 Excel 文件未被占用)", "确定");
            LogUtil.LogError($"故事演出编辑器保存失败: {e}");
        }
    }

    /// <summary>
    /// 写回故事表(StoryInfo):删除(行号降序) -> 修改 -> 新增;name[language] 约定写故事 id
    /// </summary>
    private void SaveStoryFile()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathStory)))
        {
            var sheet = pack.Workbook.Worksheets[SheetStory];
            var colMap = BuildColMap(sheet);
            DeleteRowsByIds(sheet, deletedStoryIds);
            foreach (var story in allStories)
            {
                if (!story.isNew && !story.IsDirty)
                    continue;
                int row = story.isNew ? sheet.Dimension.End.Row + 1 : FindRowById(sheet, story.id);
                if (row <= 0) continue;
                SetCellLong(sheet, row, colMap, "id", story.id);
                SetCellLong(sheet, row, colMap, "name[language]", story.id);
                SetCellLong(sheet, row, colMap, "trigger_type", story.triggerType);
                SetCellLong(sheet, row, colMap, "scene_type", story.sceneType);
                SetCellLong(sheet, row, colMap, "trigger_condition", story.triggerCondition);
                SetCellLong(sheet, row, colMap, "priority", story.priority);
                SetCellLong(sheet, row, colMap, "is_once", story.isOnce ? 1 : 0);
                SetCellLong(sheet, row, colMap, "valid", story.valid ? 1 : 0);
                SetCellText(sheet, row, colMap, "remark", story.remark);
            }
            pack.Save();
        }
    }

    /// <summary>
    /// 写回步骤表(StoryDetailsInfo)
    /// </summary>
    private void SaveDetailsFile()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathDetails)))
        {
            var sheet = pack.Workbook.Worksheets[SheetDetails];
            var colMap = BuildColMap(sheet);
            DeleteRowsByIds(sheet, deletedStepIds);
            foreach (var step in allSteps)
            {
                if (!step.isNew && !step.IsDirty)
                    continue;
                int row = step.isNew ? sheet.Dimension.End.Row + 1 : FindRowById(sheet, step.id);
                if (row <= 0) continue;
                SetCellLong(sheet, row, colMap, "id", step.id);
                SetCellLong(sheet, row, colMap, "story_id", step.storyId);
                SetCellLong(sheet, row, colMap, "step_order", step.stepOrder);
                SetCellLong(sheet, row, colMap, "step_type", step.stepType);
                SetCellLong(sheet, row, colMap, "is_async", step.isAsync ? 1 : 0);
                SetCellText(sheet, row, colMap, "param_1", step.param1);
                SetCellText(sheet, row, colMap, "param_2", step.param2);
                SetCellText(sheet, row, colMap, "param_3", step.param3);
                SetCellText(sheet, row, colMap, "param_4", step.param4);
                SetCellText(sheet, row, colMap, "remark", step.remark);
            }
            pack.Save();
        }
    }

    /// <summary>
    /// 写回对话表(StoryTalkInfo);content[language] 约定写对话 id
    /// </summary>
    private void SaveTalkFile()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathTalk)))
        {
            var sheet = pack.Workbook.Worksheets[SheetTalk];
            var colMap = BuildColMap(sheet);
            DeleteRowsByIds(sheet, deletedTalkIds);
            foreach (var talk in allTalks)
            {
                if (!talk.isNew && !talk.IsDirty)
                    continue;
                int row = talk.isNew ? sheet.Dimension.End.Row + 1 : FindRowById(sheet, talk.id);
                if (row <= 0) continue;
                SetCellLong(sheet, row, colMap, "id", talk.id);
                SetCellLong(sheet, row, colMap, "story_id", talk.storyId);
                SetCellLong(sheet, row, colMap, "npc_id", talk.npcId);
                SetCellLong(sheet, row, colMap, "content[language]", talk.id);
                SetCellText(sheet, row, colMap, "remark", talk.remark);
            }
            pack.Save();
        }
    }

    /// <summary>
    /// 写回语言表(excel_language):StoryInfo sheet 写故事名中文,StoryTalkInfo sheet 写对话内容中文;同步清理已删行
    /// (英文及其他语种不在编辑器维护——不写 content_en 等列,Excel 里已有的其它语种内容保持不变)
    /// </summary>
    private void SaveLanguageFile()
    {
        using (var pack = new ExcelPackage(new FileInfo(ExcelPathLanguage)))
        {
            var sheetStory = pack.Workbook.Worksheets[SheetStory];
            var colMapStory = BuildColMap(sheetStory);
            DeleteRowsByIds(sheetStory, deletedStoryIds);
            foreach (var story in allStories)
            {
                int row = FindRowById(sheetStory, story.id);
                if (row <= 0) row = sheetStory.Dimension.End.Row + 1;
                SetCellLong(sheetStory, row, colMapStory, "id", story.id);
                SetCellText(sheetStory, row, colMapStory, "content_cn", story.nameCn);
            }
            var sheetTalk = pack.Workbook.Worksheets[SheetTalk];
            var colMapTalk = BuildColMap(sheetTalk);
            DeleteRowsByIds(sheetTalk, deletedTalkIds);
            foreach (var talk in allTalks)
            {
                int row = FindRowById(sheetTalk, talk.id);
                if (row <= 0) row = sheetTalk.Dimension.End.Row + 1;
                SetCellLong(sheetTalk, row, colMapTalk, "id", talk.id);
                SetCellText(sheetTalk, row, colMapTalk, "content_cn", talk.contentCn);
            }
            pack.Save();
        }
    }

    /// <summary>
    /// 按 id 集合删除数据行(行号降序删防漂移)
    /// </summary>
    private void DeleteRowsByIds(ExcelWorksheet sheet, HashSet<long> ids)
    {
        if (ids.Count == 0)
            return;
        var rows = new List<int>();
        foreach (var id in ids)
        {
            int row = FindRowById(sheet, id);
            if (row > 0)
                rows.Add(row);
        }
        rows.Sort((a, b) => b.CompareTo(a));
        foreach (var row in rows)
            sheet.DeleteRow(row);
    }

    /// <summary>
    /// 保存前校验(错误阻断,警告可继续)
    /// </summary>
    private void ValidateAll(List<string> errors, List<string> warnings)
    {
        foreach (var story in allStories)
        {
            //触发条件与演出场景一致性
            if (story.triggerCondition == (int)StoryTriggerConditionEnum.EnterBaseSceneFirst && story.sceneType != (int)StorySceneTypeEnum.Base)
                errors.Add($"故事[{story.id}] 触发条件=首次进基地,但演出场景不是基地");
            if ((story.triggerCondition == (int)StoryTriggerConditionEnum.EnterFightSceneFirst || story.triggerCondition == (int)StoryTriggerConditionEnum.FightFirstDropCrystal) && story.sceneType != (int)StorySceneTypeEnum.Fight)
                errors.Add($"故事[{story.id}] 触发条件是战斗类,但演出场景不是战斗");
            var steps = GetStepsForStory(story.id);
            if (steps.Count == 0)
                warnings.Add($"故事[{story.id}] 没有任何步骤");
            if (string.IsNullOrEmpty(story.nameCn))
                warnings.Add($"故事[{story.id}] 中文名为空");
            foreach (var step in steps)
                ValidateStep(story, step, errors);
        }
    }

    /// <summary>
    /// 校验单个步骤的参数合法性
    /// </summary>
    private void ValidateStep(StoryRow story, StepRow step, List<string> errors)
    {
        string prefix = $"故事[{story.id}]步骤{step.stepOrder}";
        switch ((StoryStepTypeEnum)step.stepType)
        {
            case StoryStepTypeEnum.Talk:
                var talkIds = ParseLongList(step.param1);
                if (talkIds.Count == 0)
                    errors.Add($"{prefix}: 对话步骤没有配置对话ID");
                foreach (var id in talkIds)
                    if (allTalks.Find(t => t.id == id) == null)
                        errors.Add($"{prefix}: 对话 {id} 不存在");
                break;
            case StoryStepTypeEnum.CameraMove:
                if (System.Array.FindIndex(CameraMarkers, m => string.Equals(m, step.param1, System.StringComparison.OrdinalIgnoreCase)) < 0)
                    errors.Add($"{prefix}: 非法镜头目标标记 \"{step.param1}\"(可选: {string.Join("/", CameraMarkers)})");
                break;
            case StoryStepTypeEnum.Wait:
                if (!float.TryParse(step.param1, out float waitTime) || waitTime < 0)
                    errors.Add($"{prefix}: 等待秒数非法 \"{step.param1}\"");
                break;
            case StoryStepTypeEnum.Effect:
                if (!long.TryParse(step.param1, out long effectId) || effectId <= 0)
                    errors.Add($"{prefix}: 特效ID非法 \"{step.param1}\"");
                else if (!string.IsNullOrEmpty(step.param2) && System.Array.FindIndex(CameraMarkers, m => string.Equals(m, step.param2, System.StringComparison.OrdinalIgnoreCase)) < 0)
                    errors.Add($"{prefix}: 特效目标标记非法 \"{step.param2}\"");
                break;
            case StoryStepTypeEnum.Audio:
                if (!long.TryParse(step.param1, out long audioId) || audioId <= 0)
                    errors.Add($"{prefix}: 音效ID非法 \"{step.param1}\"");
                break;
            case StoryStepTypeEnum.Fade:
                if (step.param1 != "out" && step.param1 != "in")
                    errors.Add($"{prefix}: 淡入淡出方向只能是 out/in \"{step.param1}\"");
                break;
            default:
                errors.Add($"{prefix}: 未知步骤类型 {step.stepType}");
                break;
        }
    }
    #endregion
}
