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
        window.minSize = new Vector2(900, 700);
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
    private const float CompareColumnWidth = 70f;

    /// <summary>字段标签固定宽度</summary>
    private const float FieldLabelWidth = 150f;

    /// <summary>对比单元格样式(只读灰字)</summary>
    private GUIStyle compareCellStyle;

    /// <summary>对比单元格样式(差异高亮)</summary>
    private GUIStyle compareCellDiffStyle;

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
        worldInfoExcelPath = Application.dataPath + "/Data/Excel/excel_game_world_info[游戏世界信息].xlsx";
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

        // 对比列头(前3难度 | 当前 | 后3难度)
        DrawCompareHeader();

        GUILayout.Space(4);

        // 整难度一键复制
        DrawCopyAllButtons();

        GUILayout.Space(6);

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
        //世界ID 右侧：打开世界配置表
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("打开世界配置表", GUILayout.Width(110), GUILayout.Height(18)))
        {
            OpenWorldInfoExcel();
        }
        EditorGUI.BeginDisabledGroup(true);
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
        currentBean.attack_boss_num = DrawStringField("Boss数量(x或x-y)", currentBean.attack_boss_num, "attack_boss_num");
        currentBean.attack_start_num = DrawIntField("第一关敌人数量", currentBean.attack_start_num, "attack_start_num");
        currentBean.attack_show_time = DrawFloatField("进攻时间(秒)", currentBean.attack_show_time, "attack_show_time");
        currentBean.attack_num_addrate = DrawFloatField("每关敌人倍数", currentBean.attack_num_addrate, "attack_num_addrate");
        currentBean.attack_num_add = DrawIntField("每关增加敌人数量", currentBean.attack_num_add, "attack_num_add");
        currentBean.attack_intensity_baserate = DrawFloatField("基础强度倍率(默认1,每关都生效)", currentBean.attack_intensity_baserate, "attack_intensity_baserate");
        currentBean.attack_intensity_addrate = DrawFloatField("每关强度倍率(HP/护甲/攻击)", currentBean.attack_intensity_addrate, "attack_intensity_addrate");

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 关卡配置(单值"x"或区间"x-y")
        EditorGUILayout.LabelField("关卡配置", EditorStyles.boldLabel);
        currentBean.fight_num = DrawStringField("关卡次数(x或x-y)", currentBean.fight_num, "fight_num");
        currentBean.road_num = DrawStringField("道路数量(x或x-y)", currentBean.road_num, "road_num");
        currentBean.road_length = DrawStringField("道路长度(x或x-y)", currentBean.road_length, "road_length");

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 难度与奖励
        EditorGUILayout.LabelField("难度与奖励", EditorStyles.boldLabel);
        currentBean.drop_crystal = DrawIntField("掉落魔晶", currentBean.drop_crystal, "drop_crystal");
        //奖励魔晶: 由Excel类型行决定(重生成Bean后为string), 统一按字符串编辑, 支持单值"200"或区间"100-200"
        string rewardCrystalStr = DrawRewardCrystalField("奖励-魔晶(x或x-y)", "reward_crystal");
        SetFieldValueAsString(currentBean, "reward_crystal", rewardCrystalStr);
        currentBean.reward_equip_rarity = DrawIntField("奖励-装备稀有度", currentBean.reward_equip_rarity, "reward_equip_rarity");

        GUILayout.Space(5);
        lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 0.3f));
        GUILayout.Space(5);

        // 备注
        currentBean.remark = DrawStringField("备注", currentBean.remark, "remark");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制对比列头：前3难度 | 当前难度 | 后3难度
    /// </summary>
    private void DrawCompareHeader()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);
        // 前3难度(远→近排列，贴近当前列的是level-1)
        for (int i = CompareRange; i >= 1; i--)
        {
            EditorGUILayout.LabelField($"难度 {selectedDifficulty - i}", headerStyle, GUILayout.Width(CompareColumnWidth));
        }
        EditorGUILayout.LabelField($"◀ 当前 难度 {selectedDifficulty} ▶", headerStyle);
        // 后3难度(近→远排列)
        for (int i = 1; i <= CompareRange; i++)
        {
            EditorGUILayout.LabelField($"难度 {selectedDifficulty + i}", headerStyle, GUILayout.Width(CompareColumnWidth));
        }
        EditorGUILayout.EndHorizontal();

        // 缺失提示行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth + 4);
        for (int i = CompareRange - 1; i >= 0; i--)
        {
            EditorGUILayout.LabelField(prevBeans[i] == null ? "(无)" : "", compareCellStyle, GUILayout.Width(CompareColumnWidth));
        }
        GUILayout.FlexibleSpace();
        for (int i = 0; i < CompareRange; i++)
        {
            EditorGUILayout.LabelField(nextBeans[i] == null ? "(无)" : "", compareCellStyle, GUILayout.Width(CompareColumnWidth));
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
    /// 绘制字符串字段（左=前3难度 / 中=当前可编辑 / 右=后3难度）
    /// </summary>
    private string DrawStringField(string label, string value, string fieldName)
    {
        string result = value;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, value, true);
        result = EditorGUILayout.TextField(result);
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
    private string DrawRewardCrystalField(string label, string fieldName)
    {
        string result = GetFieldValueStr(currentBean, fieldName);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, result, true);
        result = EditorGUILayout.TextField(result);
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

        // 场景字段：在列表旁提供「打开场景配置表」按钮（打开战斗场景 Excel）
        if (fieldKey.Contains("scene"))
        {
            GUILayout.Space(5);
            if (GUILayout.Button("打开场景配置表", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                OpenFightSceneExcel();
            }
        }
        // 敌人字段：在列表旁提供「打开NpcInfo配置表」按钮（打开NpcInfo Excel）
        else if (fieldKey.Contains("enemy"))
        {
            GUILayout.Space(5);
            if (GUILayout.Button("打开NpcInfo配置表", EditorStyles.miniButton, GUILayout.Width(120)))
            {
                OpenNpcInfoExcel();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 前后3难度对比行(只读+点击复制，与上方列头对齐；长ID串截断展示，完整值见tooltip)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FieldLabelWidth);
        var clickedPrev = DrawCompareCells(prevBeans, fieldKey, value, true);
        GUILayout.FlexibleSpace();
        var clickedNext = DrawCompareCells(nextBeans, fieldKey, value, false);
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
    /// 绘制整数字段（左=前3难度 / 中=当前可编辑 / 右=后3难度）
    /// </summary>
    private int DrawIntField(string label, int value, string fieldName)
    {
        int result = value;
        string valueStr = value.ToString();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, valueStr, true);
        result = EditorGUILayout.IntField(result);
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
    private float DrawFloatField(string label, float value, string fieldName)
    {
        float result = value;
        string valueStr = value.ToString();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(FieldLabelWidth));
        var clickedPrev = DrawCompareCells(prevBeans, fieldName, valueStr, true);
        result = EditorGUILayout.FloatField(result);
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

        GUILayout.Space(10);

        prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.35f, 0.80f, 0.55f);
        if (GUILayout.Button("打开 世界配置 Excel 表格", GUILayout.Width(220), GUILayout.Height(35)))
        {
            OpenWorldInfoExcel();
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

        GUILayout.Space(10);

        // 导出JSON按钮（不需要数据变更，直接从当前Excel重新生成Json）
        prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.30f, 0.55f, 0.90f);
        if (GUILayout.Button("导出 JSON", GUILayout.Width(120), GUILayout.Height(35)))
        {
            ExportJsonOnly();
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
