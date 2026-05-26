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
        window.minSize = new Vector2(600, 700);
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

    /// <summary>滚动位置</summary>
    private Vector2 scrollPos = Vector2.zero;

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    /// <summary>分区标题样式</summary>
    private GUIStyle sectionHeaderStyle;

    /// <summary>分组框样式</summary>
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
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";

        LoadWorldData();
        LoadNpcInfoData();
        LoadFightSceneData();
        LoadAllConfigFromExcel();
    }

    /// <summary>
    /// GUI 渲染入口
    /// </summary>
    private void OnGUI()
    {
        if (!stylesInitialized)
        {
            InitializeStyles();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 快捷操作按钮
        DrawOpenNpcInfoButton();

        GUILayout.Space(10);

        // 顶部选择区域
        DrawSelectionArea();

        GUILayout.Space(10);

        // 数据编辑区域
        if (dataLoaded && currentBean != null)
        {
            DrawDataEditArea();

            GUILayout.Space(10);

            DrawActionButtons();
        }
        else if (dataLoaded && currentBean == null)
        {
            EditorGUILayout.HelpBox($"未找到世界ID {GetSelectedWorldId()} 难度 {selectedDifficulty} 的数据", MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化所有自定义 UI 样式
    /// </summary>
    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            fixedHeight = 28,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 0, 8, 8),
            normal = { textColor = EditorGUIUtility.isProSkin ?
                new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f) }
        };

        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(15, 15, 15, 15),
            margin = new RectOffset(5, 5, 10, 10)
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
        foreach (var bean in allConfigList)
        {
            if (bean.world_id == worldId && bean.level == selectedDifficulty)
            {
                currentBean = bean;
                break;
            }
        }

        if (currentBean != null)
        {
            // 深拷贝一份原始数据用于对比
            originalBean = JsonConvert.DeserializeObject<FightTypeConquerInfoBean>(JsonConvert.SerializeObject(currentBean));
        }

        dataLoaded = true;
    }

    #endregion

    #region UI 绘制 - 选择区域

    /// <summary>
    /// 绘制顶部选择区域
    /// </summary>
    private void DrawSelectionArea()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("选择世界与难度", sectionHeaderStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        // 世界选择
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        EditorGUILayout.LabelField("世界:", EditorStyles.boldLabel);
        selectedWorldIndex = EditorGUILayout.Popup(selectedWorldIndex, worldNameList.ToArray(), GUILayout.Height(25));
        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // 难度选择
        EditorGUILayout.BeginVertical(GUILayout.Width(120));
        EditorGUILayout.LabelField("难度:", EditorStyles.boldLabel);
        selectedDifficulty = EditorGUILayout.IntSlider(selectedDifficulty, 1, 10, GUILayout.Height(25));
        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // 加载按钮
        EditorGUILayout.BeginVertical(GUILayout.Width(100));
        EditorGUILayout.LabelField(" ", EditorStyles.boldLabel);
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.20f, 0.75f, 0.35f);
        if (GUILayout.Button("加载数据", GUILayout.Height(25)))
        {
            LoadData();
        }
        GUI.backgroundColor = prevColor;
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (dataLoaded && currentBean != null)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField($"当前: ID={currentBean.id} | {currentBean.remark}", EditorStyles.miniLabel);
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
        EditorGUILayout.LabelField("数据编辑", sectionHeaderStyle);
        GUILayout.Space(10);

        // 基础信息
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID:", GUILayout.Width(120));
        EditorGUILayout.LongField(currentBean.id);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("世界ID:", GUILayout.Width(120));
        EditorGUILayout.LongField(currentBean.world_id);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("难度:", GUILayout.Width(120));
        EditorGUILayout.IntField(currentBean.level);
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(5);
        Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 场景配置
        EditorGUILayout.LabelField("场景配置", EditorStyles.boldLabel);
        currentBean.fight_scene_ids = DrawIdListField("战斗场景列表", currentBean.fight_scene_ids, "fight_scene_ids");
        currentBean.fight_scene_boss_ids = DrawIdListField("Boss战斗场景列表", currentBean.fight_scene_boss_ids, "fight_scene_boss_ids");

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 敌人配置
        EditorGUILayout.LabelField("敌人配置", EditorStyles.boldLabel);
        currentBean.enemy_ids = DrawIdListField("敌人列表", currentBean.enemy_ids, "enemy_ids");
        currentBean.enemy_boss_ids = DrawIdListField("Boss列表", currentBean.enemy_boss_ids, "enemy_boss_ids");
        currentBean.attack_start_num = DrawIntField("第一关敌人数量", currentBean.attack_start_num);
        currentBean.attack_show_time = DrawFloatField("进攻时间(秒)", currentBean.attack_show_time);
        currentBean.attack_num_addrate = DrawFloatField("每关敌人倍数", currentBean.attack_num_addrate);
        currentBean.attack_num_add = DrawIntField("每关增加敌人数量", currentBean.attack_num_add);

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 关卡配置
        EditorGUILayout.LabelField("关卡配置", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        currentBean.fight_num_min = DrawIntField("关卡次数-最小", currentBean.fight_num_min);
        currentBean.fight_num_max = DrawIntField("关卡次数-最大", currentBean.fight_num_max);
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical();
        currentBean.road_num_min = DrawIntField("道路数量-最小", currentBean.road_num_min);
        currentBean.road_num_max = DrawIntField("道路数量-最大", currentBean.road_num_max);
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical();
        currentBean.road_length_min = DrawIntField("道路长度-最小", currentBean.road_length_min);
        currentBean.road_length_max = DrawIntField("道路长度-最大", currentBean.road_length_max);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 难度与奖励
        EditorGUILayout.LabelField("难度与奖励", EditorStyles.boldLabel);
        currentBean.level_add = DrawFloatField("难度数值加成", currentBean.level_add);
        currentBean.drop_crystal = DrawIntField("掉落魔晶", currentBean.drop_crystal);
        currentBean.reward_crystal = DrawIntField("奖励-魔晶", currentBean.reward_crystal);
        currentBean.reward_equip_rarity = DrawIntField("奖励-装备稀有度", currentBean.reward_equip_rarity);
        currentBean.reward_equip_attribute_add = DrawIntField("奖励-装备属性加成", currentBean.reward_equip_attribute_add);

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 备注
        currentBean.remark = DrawStringField("备注", currentBean.remark);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制字符串字段
    /// </summary>
    private string DrawStringField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
        string result = EditorGUILayout.TextField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制ID列表字段（支持列表编辑和文本编辑两种模式）
    /// </summary>
    private string DrawIdListField(string label, string value, string fieldKey)
    {
        // 初始化状态
        if (!listFoldoutStates.ContainsKey(fieldKey))
            listFoldoutStates[fieldKey] = true;
        if (!listEditMode.ContainsKey(fieldKey))
            listEditMode[fieldKey] = true;
        if (!newIdInputs.ContainsKey(fieldKey))
            newIdInputs[fieldKey] = 0;

        // 解析当前值为列表
        List<long> idList = ParseIdList(value);

        EditorGUILayout.BeginVertical();

        // 标签行 + 编辑模式切换
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));

        string modeLabel = listEditMode[fieldKey] ? "切换到文本编辑" : "切换到列表编辑";
        if (GUILayout.Button(modeLabel, EditorStyles.miniButton, GUILayout.Width(100)))
        {
            listEditMode[fieldKey] = !listEditMode[fieldKey];
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);

        if (listEditMode[fieldKey])
        {
            // 列表编辑模式
            listFoldoutStates[fieldKey] = EditorGUILayout.Foldout(listFoldoutStates[fieldKey], $"  共 {idList.Count} 个ID", true);

            if (listFoldoutStates[fieldKey])
            {
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
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField($"[{i + 1}]", GUILayout.Width(30));

                    long newId = EditorGUILayout.LongField(idList[i], GUILayout.Width(100));
                    if (newId != idList[i])
                    {
                        idList[i] = newId;
                        value = BuildIdString(idList);
                    }

                    // 显示名字（场景字段查场景映射，敌人字段查NPC映射）
                    string displayName = GetDisplayName(idList[i], fieldKey);
                    EditorGUILayout.LabelField(displayName, EditorStyles.miniLabel, GUILayout.MinWidth(50));

                    Color prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
                    {
                        pendingRemoveIndex = i;
                        pendingRemoveFieldKey = fieldKey;
                    }
                    GUI.backgroundColor = prevColor;
                    EditorGUILayout.EndHorizontal();
                }

                if (idList.Count == 0)
                {
                    EditorGUILayout.LabelField("  （空列表）", EditorStyles.centeredGreyMiniLabel);
                }

                GUILayout.Space(4);

                // 添加新ID行
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.LabelField("新ID:", GUILayout.Width(40));
                newIdInputs[fieldKey] = EditorGUILayout.LongField(newIdInputs[fieldKey]);

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
            }
        }
        else
        {
            // 文本编辑模式
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
    /// 绘制整数字段
    /// </summary>
    private int DrawIntField(string label, int value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
        int result = EditorGUILayout.IntField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    /// <summary>
    /// 绘制浮点数字段
    /// </summary>
    private float DrawFloatField(string label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
        float result = EditorGUILayout.FloatField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    #endregion

    #region UI 绘制 - 操作按钮

    /// <summary>
    /// 绘制保存和重置按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 保存按钮
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.30f, 0.55f, 0.90f);
        if (GUILayout.Button("保存到Excel并生成Json", GUILayout.Width(200), GUILayout.Height(35)))
        {
            SaveData();
        }
        GUI.backgroundColor = prevColor;

        GUILayout.Space(15);

        // 重置按钮
        if (GUILayout.Button("重置", GUILayout.Width(80), GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要重置当前数据吗？未保存的修改将丢失。", "确定", "取消"))
            {
                LoadData();
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制打开NpcInfo Excel按钮
    /// </summary>
    private void DrawOpenNpcInfoButton()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("快捷操作", sectionHeaderStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.90f, 0.55f, 0.25f);
        if (GUILayout.Button("打开 NpcInfo Excel 表格", GUILayout.Width(220), GUILayout.Height(35)))
        {
            OpenNpcInfoExcel();
        }
        GUI.backgroundColor = prevColor;

        GUILayout.Space(10);

        prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.65f, 0.90f);
        if (GUILayout.Button("打开 战斗场景 Excel 表格", GUILayout.Width(220), GUILayout.Height(35)))
        {
            OpenFightSceneExcel();
        }
        GUI.backgroundColor = prevColor;

        GUILayout.Space(10);

        prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.65f, 0.35f, 0.90f);
        if (GUILayout.Button("打开 战斗模式难度 Excel 表格", GUILayout.Width(220), GUILayout.Height(35)))
        {
            OpenFightTypeConquerExcel();
        }
        GUI.backgroundColor = prevColor;

        GUILayout.Space(15);

        // 重新加载Excel按钮
        prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.55f, 0.55f, 0.55f);
        if (GUILayout.Button("刷新数据", GUILayout.Width(120), GUILayout.Height(35)))
        {
            LoadAllConfigFromExcel();
            LoadData();
            EditorUtility.DisplayDialog("完成", "已从Excel重新加载数据", "确定");
        }
        GUI.backgroundColor = prevColor;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
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

    #region 打开NpcInfo Excel

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

    #endregion
}
