using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 终焉议会编辑工具（主窗口）
/// 以页签方式承载议会相关配置编辑：
/// - 议案：编辑 excel_doom_council_info[终焉议会信息]，见 DoomCouncilEditorTabBill
/// - 议员评级：编辑 excel_doom_council_ratings_info[终焉议会议员等级信息]，见 DoomCouncilEditorTabRatings
/// </summary>
public class DoomCouncilEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/终焉议会编辑
    /// </summary>
    [MenuItem("游戏/终焉议会编辑")]
    private static void CreateWindow()
    {
        var window = GetWindow<DoomCouncilEditorWindow>();
        window.titleContent = new GUIContent("终焉议会编辑工具");
        window.minSize = new Vector2(920, 600);
        window.Show();
    }

    #endregion

    #region 成员变量

    /// <summary>页签名称（索引与 tab 实例一一对应）</summary>
    private static readonly string[] TabNames = { "议案", "议员评级" };

    /// <summary>当前选中的页签索引</summary>
    private int selectedTab = 0;

    /// <summary>议案页签</summary>
    private DoomCouncilEditorTabBill billTab;

    /// <summary>议员评级页签</summary>
    private DoomCouncilEditorTabRatings ratingsTab;

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时创建并初始化全部页签（切页签不丢各自编辑状态）
    /// </summary>
    private void OnEnable()
    {
        billTab = new DoomCouncilEditorTabBill();
        billTab.Init();
        ratingsTab = new DoomCouncilEditorTabRatings();
        ratingsTab.Init();
    }

    /// <summary>
    /// GUI 渲染入口：顶部页签栏固定，下面委托给当前页签绘制
    /// </summary>
    private void OnGUI()
    {
        selectedTab = GUILayout.Toolbar(selectedTab, TabNames, GUILayout.Height(28));
        switch (selectedTab)
        {
            case 0:
                billTab.OnGUI();
                break;
            case 1:
                ratingsTab.OnGUI();
                break;
        }
    }

    #endregion
}

/// <summary>
/// 终焉议会编辑工具的页签基类（非泛型部分）：宿主窗口统一调度入口
/// </summary>
public abstract class DoomCouncilEditorTabBase
{
    /// <summary>
    /// 宿主窗口 OnEnable 时调用：初始化路径与加载数据
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// 宿主窗口 OnGUI 时调用：绘制本页签全部内容
    /// </summary>
    public abstract void OnGUI();
}

/// <summary>
/// 终焉议会编辑工具页签泛型基类：封装 Excel 读写、变更对比、字段高亮、列表选择等通用逻辑，
/// 子类只需声明 Excel/Sheet 名并实现字段编辑区的绘制
/// </summary>
/// <typeparam name="T">配置表 Bean 类型</typeparam>
public abstract class DoomCouncilEditorTabBase<T> : DoomCouncilEditorTabBase where T : BaseBean, new()
{
    #region 抽象成员（子类声明）

    /// <summary>Excel 文件名（含扩展名与中文备注，如 excel_doom_council_info[终焉议会信息].xlsx）</summary>
    protected abstract string ExcelFileName { get; }

    /// <summary>工作表名称（与 Bean 前缀一致，如 DoomCouncilInfo）</summary>
    protected abstract string SheetName { get; }

    /// <summary>多语言 JSON 文件名（如 Language_DoomCouncilInfo_cn.txt）</summary>
    protected abstract string LanguageFileName { get; }

    /// <summary>获取列表项的显示名（默认取 name 语言ID对应中文，取不到回退 remark）</summary>
    protected abstract string GetItemDisplayName(T bean);

    /// <summary>绘制右侧字段编辑区（子类按表结构分区绘制各字段）</summary>
    protected abstract void DrawEditFields();

    #endregion

    #region 成员变量

    /// <summary>Excel 文件路径</summary>
    protected string excelPath;

    /// <summary>Json 输出目录</summary>
    protected string jsonFolderPath;

    /// <summary>所有配置数据（从 Excel 加载）</summary>
    protected List<T> allConfigList = new List<T>();

    /// <summary>当前编辑的数据</summary>
    protected T currentBean;

    /// <summary>原始Bean用于对比变更</summary>
    protected T originalBean;

    /// <summary>左侧列表当前选中索引</summary>
    protected int selectedIndex = -1;

    /// <summary>左侧列表滚动位置</summary>
    private Vector2 listScrollPos = Vector2.zero;

    /// <summary>右侧编辑区滚动位置</summary>
    protected Vector2 editScrollPos = Vector2.zero;

    /// <summary>语言ID → (content, content_1) 映射（用于文本预览与列表显示名）</summary>
    protected Dictionary<long, LanguageJsonItem> languageMap = new Dictionary<long, LanguageJsonItem>();

    /// <summary>左侧列表宽度</summary>
    protected const float ListWidth = 250f;

    /// <summary>字段标签固定宽度</summary>
    protected const float FieldLabelWidth = 110f;

    /// <summary>字段已修改时的编辑框背景色(淡黄)</summary>
    protected static readonly Color ModifiedBgColor = new Color(1f, 0.93f, 0.55f);

    /// <summary>列表项选中态背景色(蓝)</summary>
    protected static readonly Color SelectedBgColor = new Color(0.30f, 0.60f, 0.95f);

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    /// <summary>分组框样式</summary>
    protected GUIStyle boxStyle;

    /// <summary>未保存提示样式(橙色小字)</summary>
    protected GUIStyle dirtyHintStyle;

    /// <summary>文本预览样式(灰色小字)</summary>
    protected GUIStyle previewTextStyle;

    /// <summary>参数提示样式(青色小字，自动换行)</summary>
    protected GUIStyle paramHintStyle;

    #endregion

    #region 语言 JSON 结构

    /// <summary>
    /// 多语言 JSON 行结构（content=主文本，content_1=副文本，缺字段时反序列化为 null）
    /// </summary>
    [Serializable]
    protected class LanguageJsonItem
    {
        /// <summary>文本ID</summary>
        public long id;
        /// <summary>主文本（议案名/评级名）</summary>
        public string content;
        /// <summary>副文本（议案描述）</summary>
        public string content_1;
    }

    #endregion

    #region 初始化与样式

    /// <summary>
    /// 宿主窗口启用时初始化路径和加载数据
    /// </summary>
    public override void Init()
    {
        excelPath = Application.dataPath + "/Data/Excel/" + ExcelFileName;
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";
        LoadLanguageData();
        LoadAllConfigFromExcel();
        // 默认选中第一条，打开窗口即可编辑
        if (allConfigList.Count > 0)
        {
            SelectBean(0);
        }
    }

    /// <summary>
    /// 初始化所有自定义 UI 样式（需 OnGUI 线程）
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 4, 4)
        };

        dirtyHintStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(1f, 0.70f, 0.30f) : new Color(0.85f, 0.45f, 0.0f) }
        };

        previewTextStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            normal = { textColor = new Color(0.60f, 0.60f, 0.60f) }
        };

        paramHintStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(0.55f, 0.85f, 0.85f) : new Color(0.10f, 0.50f, 0.50f) }
        };

        stylesInitialized = true;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载多语言 JSON（Language_XXX_cn.txt → id 到文本行的映射，用于预览）
    /// </summary>
    protected void LoadLanguageData()
    {
        languageMap.Clear();
        string languagePath = jsonFolderPath + "/" + LanguageFileName;
        if (!File.Exists(languagePath))
            return;

        try
        {
            string jsonText = File.ReadAllText(languagePath);
            LanguageJsonItem[] itemArray = JsonConvert.DeserializeObject<LanguageJsonItem[]>(jsonText);
            if (itemArray == null)
                return;
            foreach (var item in itemArray)
            {
                languageMap[item.id] = item;
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载多语言数据失败({LanguageFileName}): {e.Message}");
        }
    }

    /// <summary>
    /// 从 Excel 加载所有配置数据（第1行字段名，第4行起为数据行，反射填充 Bean）
    /// </summary>
    protected void LoadAllConfigFromExcel()
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
                T bean = new T();
                for (int col = 1; col <= columnCount; col++)
                {
                    string fieldName = sheet.Cells[1, col].Text;
                    string cellText = sheet.Cells[row, col].Text;

                    FieldInfo fieldInfo = typeof(T).GetField(fieldName);
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
    /// 选中指定索引的配置进行编辑（深拷贝原始数据用于变更对比）
    /// </summary>
    protected void SelectBean(int index)
    {
        if (index < 0 || index >= allConfigList.Count)
        {
            selectedIndex = -1;
            currentBean = null;
            originalBean = null;
            return;
        }
        selectedIndex = index;
        currentBean = allConfigList[index];
        originalBean = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(currentBean));
    }

    #endregion

    #region 变更对比

    /// <summary>
    /// 统计当前数据相对原始数据的变更字段数（用于未保存提示与保存按钮状态）
    /// </summary>
    protected int CountChanges()
    {
        if (currentBean == null || originalBean == null) return 0;
        int count = 0;
        FieldInfo[] fields = typeof(T).GetFields();
        foreach (FieldInfo field in fields)
        {
            if (field.Name == "id") continue;
            if (!Equals(field.GetValue(currentBean), field.GetValue(originalBean))) count++;
        }
        return count;
    }

    /// <summary>
    /// 判断指定字段当前值是否与原始值不同（用于编辑框淡黄高亮）
    /// </summary>
    protected bool IsFieldModified(string fieldName)
    {
        if (currentBean == null || originalBean == null) return false;
        return GetFieldValueStr(currentBean, fieldName) != GetFieldValueStr(originalBean, fieldName);
    }

    /// <summary>
    /// 通过反射读取指定Bean字段的字符串值（用于变更对比）
    /// </summary>
    protected string GetFieldValueStr(T bean, string fieldName)
    {
        if (bean == null) return "";
        FieldInfo f = typeof(T).GetField(fieldName);
        if (f == null) return "";
        object v = f.GetValue(bean);
        return v?.ToString() ?? "";
    }

    /// <summary>
    /// 取语言ID对应的中文预览文本（content 主文本 / content_1 副文本，取不到返回提示）
    /// </summary>
    protected string GetLanguagePreview(long languageId, bool subContent)
    {
        if (languageMap.TryGetValue(languageId, out LanguageJsonItem item))
        {
            string text = subContent ? item.content_1 : item.content;
            if (!string.IsNullOrEmpty(text))
                return text;
        }
        return "(未找到文本)";
    }

    #endregion

    #region UI 绘制 - 整体布局

    /// <summary>
    /// GUI 渲染入口：顶部工具栏固定，中间左列表+右编辑区，底部保存栏固定
    /// </summary>
    public override void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawItemList();
        editScrollPos = EditorGUILayout.BeginScrollView(editScrollPos);
        if (currentBean != null)
        {
            DrawEditArea();
        }
        else
        {
            EditorGUILayout.HelpBox("左侧选择一条数据开始编辑", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();

        if (currentBean != null)
        {
            DrawActionButtons();
        }
    }

    /// <summary>
    /// 绘制顶部工具栏（刷新/导出 JSON/打开表格）
    /// </summary>
    protected virtual void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("刷新数据", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            ReloadData();
            EditorUtility.DisplayDialog("完成", "已从Excel重新加载数据", "确定");
        }

        if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            ExportJsonOnly();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("打开表格", EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            OpenExcel();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 重新加载 Excel 与语言数据，并尽量保持当前选中项（按 id 找回）
    /// </summary>
    protected void ReloadData()
    {
        long currentId = currentBean != null ? currentBean.id : -1;
        LoadLanguageData();
        LoadAllConfigFromExcel();
        int newIndex = allConfigList.FindIndex(b => b.id == currentId);
        SelectBean(newIndex >= 0 ? newIndex : (allConfigList.Count > 0 ? 0 : -1));
    }

    /// <summary>
    /// 绘制左侧数据列表（[id] 显示名，选中态蓝色，点击切换编辑目标）
    /// </summary>
    protected void DrawItemList()
    {
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField($"共 {allConfigList.Count} 条", EditorStyles.miniBoldLabel);

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);
        for (int i = 0; i < allConfigList.Count; i++)
        {
            T bean = allConfigList[i];
            bool isSelected = i == selectedIndex;
            Color prevBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = SelectedBgColor;

            string displayName = GetItemDisplayName(bean);
            GUIContent content = new GUIContent($"[{bean.id}] {displayName}", $"{SheetName} ID={bean.id}");
            if (GUILayout.Button(content, EditorStyles.miniButtonLeft, GUILayout.Height(22)))
            {
                // 有未保存修改时提示，避免误切换丢失编辑
                if (CountChanges() > 0 && i != selectedIndex)
                {
                    if (EditorUtility.DisplayDialog("未保存修改",
                        $"当前数据有 {CountChanges()} 项未保存修改，切换后将丢失。\n确定切换吗？", "切换", "取消"))
                    {
                        SelectBean(i);
                    }
                }
                else
                {
                    SelectBean(i);
                }
            }
            GUI.backgroundColor = prevBg;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制右侧编辑区（状态行 + 子类字段区）
    /// </summary>
    protected void DrawEditArea()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        // 状态行：当前编辑信息 + 未保存提示
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"当前编辑: ID={currentBean.id}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        int changes = CountChanges();
        if (changes > 0)
        {
            EditorGUILayout.LabelField($"● {changes} 项未保存修改", dirtyHintStyle);
        }
        EditorGUILayout.EndHorizontal();

        DrawEditFields();

        GUILayout.Space(6);
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - 通用字段

    /// <summary>
    /// 绘制分区标题（加粗文字 + 下方细分隔线）
    /// </summary>
    protected void DrawSectionTitle(string title)
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(4);
    }

    /// <summary>
    /// 绘制字符串字段（值与原始值不同则淡黄背景标记"已修改"）
    /// </summary>
    protected string DrawStringField(GUIContent labelContent, string value, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        string result = EditorGUILayout.TextField(value ?? "");
        GUI.backgroundColor = prevColor;
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制整数字段（值与原始值不同则淡黄背景标记"已修改"）
    /// </summary>
    protected int DrawIntField(GUIContent labelContent, int value, string fieldName)
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
    /// 绘制长整数字段（值与原始值不同则淡黄背景标记"已修改"）
    /// </summary>
    protected long DrawLongField(GUIContent labelContent, long value, string fieldName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelContent, GUILayout.Width(FieldLabelWidth));
        Color prevColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        long result = EditorGUILayout.LongField(value);
        GUI.backgroundColor = prevColor;
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制浮点数字段（值与原始值不同则淡黄背景标记"已修改"）
    /// </summary>
    protected float DrawFloatField(GUIContent labelContent, float value, string fieldName)
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

    /// <summary>
    /// 绘制多语言文本ID字段（可编辑ID + 下方灰色中文预览）
    /// </summary>
    /// <param name="subContent">true=预览 content_1（描述），false=预览 content（名字）</param>
    protected long DrawLanguageIdField(GUIContent labelContent, long value, string fieldName, bool subContent)
    {
        long result = DrawLongField(labelContent, value, fieldName);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);
        EditorGUILayout.LabelField(GetLanguagePreview(result, subContent), previewTextStyle);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制只读 ID 字段
    /// </summary>
    protected void DrawIdField()
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(FieldLabelWidth));
        EditorGUILayout.LongField(currentBean.id, GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    #endregion

    #region UI 绘制 - 操作按钮

    /// <summary>
    /// 绘制底部固定保存栏（保存按钮显示变更数、无变更时禁用；附重置按钮）
    /// </summary>
    protected void DrawActionButtons()
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
                SelectBean(selectedIndex);
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    #endregion

    #region 保存逻辑

    /// <summary>
    /// 保存数据到 Excel 并重新生成 Json
    /// </summary>
    protected void SaveData()
    {
        if (currentBean == null) return;

        // 对比所有字段收集变更
        List<ExcelUtil.ExcelChangeData> changeDataList = new List<ExcelUtil.ExcelChangeData>();
        FieldInfo[] fields = typeof(T).GetFields();
        foreach (FieldInfo field in fields)
        {
            if (field.Name == "id") continue;

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

            // 重新生成Json并刷新资源（与 ExcelEditorWindow 导出逻辑一致）
            ExcelUtil.ExcelToJsonItem(excelPath);

            // 重新加载数据（保持当前选中项）
            ReloadData();

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
    protected void ExportJsonOnly()
    {
        if (!File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在:\n{excelPath}", "确定");
            return;
        }

        try
        {
            ExcelUtil.ExcelToJsonItem(excelPath);
            EditorUtility.DisplayDialog("完成", "已从 Excel 重新导出 Json 文件", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
            LogUtil.LogError($"导出失败: {e}");
        }
    }

    /// <summary>
    /// 用系统默认程序打开本页签的 Excel 表格
    /// </summary>
    protected void OpenExcel()
    {
        if (File.Exists(excelPath))
        {
            System.Diagnostics.Process.Start(excelPath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在:\n{excelPath}", "确定");
        }
    }

    #endregion
}
