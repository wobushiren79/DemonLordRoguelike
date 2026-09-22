using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗模式编辑工具 - 无尽模式页签
/// 用于可视化编辑 excel_fight_type_infinite_info[战斗-无尽模式] 表：
/// 每行=一个世界一个难度的无尽配置(无难度1)，round_intensity_addrate 为每轮强度倍率(第N轮怪物HP/护甲/攻击力×该值^(N-1), 含BOSS)；
/// 怪物池/数量/时长/场景/魔晶掉落均复用同难度征服行(excel_fight_type_conquer_info)，本表只配轮次强度递增。
/// 本页签以「配置行列表」为主组织编辑（行下拉 + 字段编辑 + 新增/删除/保存/导出Json）。
/// 宿主窗口见 FightModeEditorWindow（菜单：游戏/战斗模式编辑）
/// </summary>
public class FightModeEditorTabInfinite : FightModeEditorTabBase
{
    #region 成员变量

    /// <summary>无尽模式 Excel 文件路径</summary>
    private string excelPath;

    /// <summary>工作表名称</summary>
    private const string SheetName = "FightTypeInfiniteInfo";

    /// <summary>当前可见的配置行列表（按 id 升序）</summary>
    private List<FightTypeInfiniteInfoBean> visibleList = new List<FightTypeInfiniteInfoBean>();

    /// <summary>行下拉当前选中索引（对应 visibleList）</summary>
    private int selectedRowIndex = 0;

    /// <summary>当前编辑的数据</summary>
    private FightTypeInfiniteInfoBean currentBean;

    /// <summary>原始Bean用于对比变更</summary>
    private FightTypeInfiniteInfoBean originalBean;

    /// <summary>所有配置数据（用于查找）</summary>
    private List<FightTypeInfiniteInfoBean> allConfigList = new List<FightTypeInfiniteInfoBean>();

    /// <summary>数据已加载标记</summary>
    private bool dataLoaded = false;

    /// <summary>滚动位置</summary>
    private Vector2 scrollPos = Vector2.zero;

    /// <summary>字段标签固定宽度</summary>
    private const float FieldLabelWidth = 190f;

    /// <summary>字段已修改时的编辑框背景色(淡黄)</summary>
    private static readonly Color ModifiedBgColor = new Color(1f, 0.93f, 0.55f);

    /// <summary>世界ID→世界中文名映射（直读 GameWorldInfo.txt + Language_GameWorldInfo_cn.txt，不走 TextHandler 单例避免编辑器态污染）</summary>
    private Dictionary<long, string> worldNameMap = new Dictionary<long, string>();

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 宿主窗口启用时初始化路径和加载数据
    /// </summary>
    public override void Init()
    {
        excelPath = Application.dataPath + "/Data/Excel/excel_fight_type_infinite_info[战斗-无尽模式].xlsx";
        LoadAllConfigFromExcel();
        LoadWorldNameMap();
        RecomputeVisibleList();
        LoadData();
    }

    /// <summary>
    /// GUI 渲染入口：顶部工具栏与选择区固定不滚动，中间编辑区滚动，底部保存栏固定
    /// </summary>
    public override void OnGUI()
    {
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
        else
        {
            EditorGUILayout.HelpBox("配置表没有任何数据行，可点击「新增行」创建。", MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();

        // 底部保存栏(固定，始终可见)
        if (dataLoaded && currentBean != null)
        {
            DrawActionButtons();
        }
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 从 Excel 加载全部配置行（按表头名列反射映射到 Bean 字段，空数值单元格按 0 处理）
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
                FightTypeInfiniteInfoBean bean = new FightTypeInfiniteInfoBean();
                for (int col = 1; col <= columnCount; col++)
                {
                    string fieldName = sheet.Cells[1, col].Text;
                    string cellText = sheet.Cells[row, col].Text;

                    FieldInfo fieldInfo = typeof(FightTypeInfiniteInfoBean).GetField(fieldName);
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
    /// 加载世界中文名映射（直读 JsonText 产物：GameWorldInfo.txt 取 id→name文本ID/remark，Language_GameWorldInfo_cn.txt 取文本ID→中文；缺文件时留空，下拉退化为"世界{id}"）
    /// </summary>
    private void LoadWorldNameMap()
    {
        worldNameMap.Clear();

        string jsonFolderPath = Application.dataPath + "/Resources/JsonText";
        string worldJsonPath = jsonFolderPath + "/GameWorldInfo.txt";
        if (!File.Exists(worldJsonPath))
            return;

        try
        {
            // 语言表：文本ID→中文名
            Dictionary<long, string> languageMap = new Dictionary<long, string>();
            string languagePath = jsonFolderPath + "/Language_GameWorldInfo_cn.txt";
            if (File.Exists(languagePath))
            {
                List<LanguageBean> languageRows = JsonConvert.DeserializeObject<List<LanguageBean>>(File.ReadAllText(languagePath));
                if (languageRows != null)
                {
                    foreach (var row in languageRows)
                    {
                        if (!languageMap.ContainsKey(row.id)) languageMap.Add(row.id, row.content);
                    }
                }
            }

            // 世界表：世界ID→中文名（语言表缺失时退化用 remark）
            GameWorldInfoBean[] worldArray = JsonConvert.DeserializeObject<GameWorldInfoBean[]>(File.ReadAllText(worldJsonPath));
            if (worldArray == null)
                return;
            foreach (var worldInfo in worldArray)
            {
                string worldName;
                if (!languageMap.TryGetValue(worldInfo.name, out worldName) || string.IsNullOrEmpty(worldName))
                    worldName = worldInfo.remark;
                if (!string.IsNullOrEmpty(worldName))
                    worldNameMap[worldInfo.id] = worldName;
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载世界名映射失败: {e.Message}");
        }
    }

    /// <summary>
    /// 取世界显示名（无映射时退化为"世界{id}"）
    /// </summary>
    private string GetWorldName(long worldId)
    {
        if (worldNameMap.TryGetValue(worldId, out string worldName) && !string.IsNullOrEmpty(worldName))
            return worldName;
        return $"世界{worldId}";
    }

    /// <summary>
    /// 重算可见行列表（数据刷新后调用；不影响已加载的编辑数据）
    /// </summary>
    private void RecomputeVisibleList()
    {
        visibleList.Clear();
        visibleList.AddRange(allConfigList);
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
            originalBean = JsonConvert.DeserializeObject<FightTypeInfiniteInfoBean>(JsonConvert.SerializeObject(currentBean));
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
        FieldInfo[] fields = typeof(FightTypeInfiniteInfoBean).GetFields();
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
        FieldInfo f = typeof(FightTypeInfiniteInfoBean).GetField(fieldName);
        if (f == null) return false;
        object cur = f.GetValue(currentBean);
        object ori = f.GetValue(originalBean);
        return !Equals(cur, ori);
    }

    #endregion

    #region UI 绘制 - 顶部工具栏

    /// <summary>
    /// 绘制顶部工具栏（刷新/导出/打开 Excel 表，单行小按钮固定显示）
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("刷新数据", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            LoadAllConfigFromExcel();
            LoadWorldNameMap();
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
        if (GUILayout.Button("无尽模式配置", EditorStyles.toolbarButton, GUILayout.Width(76)))
        {
            OpenExcel(excelPath, "无尽模式配置");
        }
        if (GUILayout.Button("征服模式配置", EditorStyles.toolbarButton, GUILayout.Width(76)))
        {
            OpenExcel(Application.dataPath + "/Data/Excel/excel_fight_type_conquer_info[战斗-征服模式].xlsx", "征服模式配置");
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 选择区域

    /// <summary>
    /// 绘制顶部选择区域（配置行下拉 + 新增/删除按钮，附当前编辑状态行）
    /// </summary>
    private void DrawSelectionArea()
    {
        EditorGUILayout.BeginVertical("HelpBox");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("配置行", GUILayout.Width(50));

        string[] options = new string[visibleList.Count];
        for (int i = 0; i < visibleList.Count; i++)
        {
            var b = visibleList[i];
            options[i] = $"[{b.id}] {GetWorldName(b.world_id)} 难度{b.level} (倍率{b.round_intensity_addrate})";
        }

        int newIndex = EditorGUILayout.Popup(selectedRowIndex, options);
        if (newIndex != selectedRowIndex)
        {
            selectedRowIndex = newIndex;
            LoadData();
        }

        // 新增行按钮（以当前编辑行/表内末行为模板）
        if (GUILayout.Button("新增行", GUILayout.Width(64)))
        {
            CreateNewRow();
        }
        // 删除行按钮（仅已加载数据时可用）
        using (new EditorGUI.DisabledScope(currentBean == null))
        {
            if (GUILayout.Button("删除行", GUILayout.Width(64)))
            {
                DeleteCurrentRow();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 当前编辑状态行
        if (currentBean != null)
        {
            int changes = CountChanges();
            string stateText = changes > 0 ? $"未保存变更: {changes} 个字段" : "无未保存变更";
            EditorGUILayout.LabelField($"当前编辑: ID {currentBean.id}（{currentBean.remark}）", stateText, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - 数据编辑区

    /// <summary>
    /// 绘制数据编辑区（id 只读，其余字段可编辑，修改过的字段淡黄高亮）
    /// </summary>
    private void DrawDataEditArea()
    {
        EditorGUILayout.BeginVertical("HelpBox");

        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);

        // id 只读
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.LongField("配置ID(id)", currentBean.id);
        }

        // world_id
        DrawLongField("世界ID(world_id)", "world_id", ref currentBean.world_id);
        // level
        DrawIntField("难度等级(level, 2~10)", "level", ref currentBean.level);
        // round_intensity_addrate
        DrawFloatField("每轮强度倍率(round_intensity_addrate)", "round_intensity_addrate", ref currentBean.round_intensity_addrate);
        // remark
        DrawStringField("备注(remark)", "remark", ref currentBean.remark);

        EditorGUILayout.Space(6);
        // 强度公式说明
        EditorGUILayout.HelpBox(
            "强度公式：第N轮怪物强度 = 同难度征服行 attack_intensity_baserate × round_intensity_addrate^(N-1)（×终焉议会强度议案）。\n" +
            "怪物池/每轮数量/进攻时长/BOSS/魔晶掉落均复用同难度征服行配置（excel_fight_type_conquer_info）。",
            MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制 long 字段编辑框（修改态淡黄高亮）
    /// </summary>
    private void DrawLongField(string label, string fieldName, ref long value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(FieldLabelWidth));
        Color oldColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        string text = EditorGUILayout.TextField(value.ToString());
        GUI.backgroundColor = oldColor;
        if (long.TryParse(text, out long v)) value = v;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制 int 字段编辑框（修改态淡黄高亮）
    /// </summary>
    private void DrawIntField(string label, string fieldName, ref int value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(FieldLabelWidth));
        Color oldColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        string text = EditorGUILayout.TextField(value.ToString());
        GUI.backgroundColor = oldColor;
        if (int.TryParse(text, out int v)) value = v;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制 float 字段编辑框（修改态淡黄高亮）
    /// </summary>
    private void DrawFloatField(string label, string fieldName, ref float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(FieldLabelWidth));
        Color oldColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        string text = EditorGUILayout.TextField(value.ToString());
        GUI.backgroundColor = oldColor;
        if (float.TryParse(text, out float v)) value = v;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制 string 字段编辑框（修改态淡黄高亮）
    /// </summary>
    private void DrawStringField(string label, string fieldName, ref string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(FieldLabelWidth));
        Color oldColor = GUI.backgroundColor;
        if (IsFieldModified(fieldName)) GUI.backgroundColor = ModifiedBgColor;
        value = EditorGUILayout.TextField(value ?? "");
        GUI.backgroundColor = oldColor;
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 底部操作栏

    /// <summary>
    /// 绘制底部操作栏（保存按钮，附变更数提示）
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        int changes = CountChanges();
        using (new EditorGUI.DisabledScope(changes == 0))
        {
            if (GUILayout.Button($"保存到Excel并重导Json ({changes}个变更)", GUILayout.Height(26), GUILayout.Width(220)))
            {
                SaveData();
            }
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    #endregion

    #region 保存/新增/删除逻辑

    /// <summary>
    /// 保存数据到Excel并重新生成Json（单个 EPPlus 会话按 id 定位行整行覆写）
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

            // 保存后重载：尽量保持选中同一行
            long keepId = currentBean.id;
            LoadAllConfigFromExcel();
            RecomputeVisibleList();
            int keepIndex = visibleList.FindIndex(b => b.id == keepId);
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
    /// 新增一行配置：id=当前最大id+1，其余字段复制模板行（优先当前编辑行/表内末行），备注自动生成
    /// </summary>
    private void CreateNewRow()
    {
        // 模板：优先当前编辑行，其次表内末行
        FightTypeInfiniteInfoBean template = currentBean;
        if (template == null && allConfigList.Count > 0) template = allConfigList[allConfigList.Count - 1];

        long newId = 0;
        foreach (var bean in allConfigList)
        {
            if (bean.id > newId) newId = bean.id;
        }
        newId++;

        string templateDesc = template != null ? $"ID {template.id}（{template.remark}）" : "内置默认值";
        if (!EditorUtility.DisplayDialog("确认新增",
            $"将新增一行无尽模式配置：\nID = {newId}\n模板 = {templateDesc}\n\n确定写入Excel并重新生成Json吗？",
            "新增", "取消"))
        {
            return;
        }

        try
        {
            FightTypeInfiniteInfoBean newBean = new FightTypeInfiniteInfoBean();
            if (template != null)
            {
                // 复制模板全部字段（id 随后覆盖）
                FieldInfo[] fields = typeof(FightTypeInfiniteInfoBean).GetFields();
                foreach (FieldInfo f in fields)
                {
                    if (f.Name == "id") continue;
                    f.SetValue(newBean, f.GetValue(template));
                }
            }
            else
            {
                // 表内无任何行时的兜底默认值
                newBean.world_id = 1;
                newBean.level = 2;
                newBean.round_intensity_addrate = 1.1f;
            }
            newBean.id = newId;
            newBean.remark = $"无尽模式-配置{newId}";

            using (ExcelPackage pack = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet sheet = pack.Workbook.Worksheets[SheetName];
                int row = sheet.Dimension.End.Row + 1;
                WriteRowCells(sheet, row, newBean);
                pack.Save();
            }

            // 重新生成Json并刷新资源
            RegenerateJson();

            // 重载并选中新行
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
    private void WriteRowCells(ExcelWorksheet sheet, int row, FightTypeInfiniteInfoBean bean)
    {
        int columnCount = sheet.Dimension.End.Column;
        for (int col = 1; col <= columnCount; col++)
        {
            string fieldName = sheet.Cells[1, col].Text;
            FieldInfo fieldInfo = typeof(FightTypeInfiniteInfoBean).GetField(fieldName);
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
