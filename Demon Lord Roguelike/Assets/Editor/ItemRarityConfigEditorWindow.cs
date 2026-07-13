using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 道具稀有度配置窗口
/// 用于可视化编辑 excel_items_info[道具信息] 表的 reward_rarity 列（奖励可出稀有度白名单）。
/// 左侧列出所有道具(图标+名字，同名相邻)，右侧以稀有度枚举勾选形式配置白名单。
/// 列表虚拟化渲染 + 图标懒加载，兼顾大量道具下的性能。
/// </summary>
public class ItemRarityConfigEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/道具稀有度配置
    /// </summary>
    [MenuItem("游戏/道具稀有度配置")]
    private static void CreateWindow()
    {
        var window = GetWindow<ItemRarityConfigEditorWindow>();
        window.titleContent = new GUIContent("道具稀有度配置");
        window.minSize = new Vector2(760, 560);
        window.Show();
    }

    #endregion

    #region 内部数据结构

    /// <summary>单条道具编辑行数据</summary>
    private class ItemRow
    {
        public long id;
        public int itemType;
        public long creatureModelId;     //所属物种(生物模组)ID，0=通用
        public string iconRes;
        public string name;              //显示名(优先多语言 content，回退 remark)
        public HashSet<int> raritySet;   //当前编辑中的稀有度白名单
        public string originalRarity;    //原始 reward_rarity 字符串(用于对比变更)
    }

    #endregion

    #region 常量

    /// <summary>工作表名称</summary>
    private const string SheetName = "ItemsInfo";

    /// <summary>行高(像素)，虚拟化按此高度计算可视范围</summary>
    private const float RowHeight = 42f;

    /// <summary>列表区顶部工具栏预留高度(用于估算可视行数)</summary>
    private const float TopAreaHeight = 140f;

    /// <summary>道具图标默认所在目录</summary>
    private const string ItemIconFolder = "Assets/LoadResources/Textures/Items";

    /// <summary>稀有度枚举项(顺序即列表展示顺序)</summary>
    private static readonly RarityEnum[] RarityOptions =
    {
        RarityEnum.N, RarityEnum.R, RarityEnum.SR, RarityEnum.SSR, RarityEnum.UR, RarityEnum.L
    };

    #endregion

    #region 成员变量

    /// <summary>道具信息 Excel 路径</summary>
    private string excelPath;

    /// <summary>JSON 输出目录</summary>
    private string jsonFolderPath;

    /// <summary>多语言(cn) JSON 路径</summary>
    private string languageCnPath;

    /// <summary>生物模组(物种) JSON 路径</summary>
    private string creatureModelPath;

    /// <summary>全部道具行</summary>
    private readonly List<ItemRow> allRows = new List<ItemRow>();

    /// <summary>过滤/排序后的展示行</summary>
    private readonly List<ItemRow> showRows = new List<ItemRow>();

    /// <summary>仅经类型/物种/名字过滤(未套用稀有度筛选)的基准行——供统计条与稀有度筛选取值，保证统计数字稳定</summary>
    private readonly List<ItemRow> baseRows = new List<ItemRow>();

    /// <summary>名字ID -> 多语言文本</summary>
    private readonly Dictionary<long, string> languageMap = new Dictionary<long, string>();

    /// <summary>生物模组(物种)ID -> 物种名</summary>
    private readonly Dictionary<long, string> creatureModelMap = new Dictionary<long, string>();

    /// <summary>图标缓存(懒加载，key=图标名去后缀)</summary>
    private readonly Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

    /// <summary>搜索关键字(按名字模糊)</summary>
    private string searchKey = "";

    /// <summary>道具类型筛选(0=全部)</summary>
    private int filterItemType = 0;

    /// <summary>类型下拉项标签</summary>
    private string[] itemTypeLabels;

    /// <summary>类型下拉项对应的 item_type 值(与 itemTypeLabels 对齐)</summary>
    private List<int> itemTypeValues = new List<int>();

    /// <summary>类型下拉当前选中索引</summary>
    private int filterTypeIndex = 0;

    /// <summary>物种下拉项标签</summary>
    private string[] speciesLabels;

    /// <summary>物种下拉项对应的 creature_model_id 值(与 speciesLabels 对齐；-1=全部)</summary>
    private List<long> speciesValues = new List<long>();

    /// <summary>物种下拉当前选中索引</summary>
    private int filterSpeciesIndex = 0;

    /// <summary>稀有度筛选(0=不筛选；否则=稀有度ID，命中"含该稀有度或全适配"的道具)</summary>
    private int filterRarity = 0;

    /// <summary>滚动位置</summary>
    private Vector2 scrollPos = Vector2.zero;

    /// <summary>数据已加载标记</summary>
    private bool dataLoaded = false;

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    private GUIStyle sectionHeaderStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle idLabelStyle;
    private GUIStyle rowBoxEvenStyle;
    private GUIStyle rowBoxOddStyle;

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时初始化路径并加载数据
    /// </summary>
    private void OnEnable()
    {
        excelPath = Application.dataPath + "/Data/Excel/excel_items_info[道具信息].xlsx";
        jsonFolderPath = Application.dataPath + "/Resources/JsonText";
        languageCnPath = jsonFolderPath + "/Language_ItemsInfo_cn.txt";
        creatureModelPath = jsonFolderPath + "/CreatureModel.txt";

        LoadLanguageMap();
        LoadCreatureModelMap();
        LoadItemsFromExcel();
        RebuildShowRows();
    }

    /// <summary>
    /// GUI 渲染入口
    /// </summary>
    private void OnGUI()
    {
        if (!stylesInitialized)
            InitializeStyles();

        DrawToolbar();

        if (!dataLoaded)
        {
            EditorGUILayout.HelpBox("未能从 Excel 加载道具数据，请确认文件存在且已关闭。", MessageType.Warning);
            return;
        }

        DrawListHeader();
        DrawVirtualizedList();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化自定义 UI 样式
    /// </summary>
    private void InitializeStyles()
    {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        nameLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
        idLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };

        rowBoxEvenStyle = new GUIStyle();
        rowBoxEvenStyle.normal.background = MakeTex(new Color(0f, 0f, 0f, 0f));
        rowBoxOddStyle = new GUIStyle();
        rowBoxOddStyle.normal.background = MakeTex(new Color(1f, 1f, 1f, 0.04f));

        stylesInitialized = true;
    }

    /// <summary>
    /// 生成纯色 1x1 贴图(用于行背景交替)
    /// </summary>
    private Texture2D MakeTex(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载多语言(cn)映射：名字ID -> 文本
    /// </summary>
    private void LoadLanguageMap()
    {
        languageMap.Clear();
        if (!File.Exists(languageCnPath))
            return;
        try
        {
            string jsonText = File.ReadAllText(languageCnPath);
            LanguageItem[] arr = JsonConvert.DeserializeObject<LanguageItem[]>(jsonText);
            if (arr != null)
            {
                foreach (var item in arr)
                    languageMap[item.id] = item.content;
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载道具多语言失败: {e.Message}");
        }
    }

    /// <summary>多语言条目(用于反序列化 Language_*.txt)</summary>
    [Serializable]
    private class LanguageItem
    {
        public long id;
        public string content;
    }

    /// <summary>
    /// 加载生物模组(物种)映射：模组ID -> 物种名(优先 remark，回退 mark_name)
    /// </summary>
    private void LoadCreatureModelMap()
    {
        creatureModelMap.Clear();
        if (!File.Exists(creatureModelPath))
            return;
        try
        {
            string jsonText = File.ReadAllText(creatureModelPath);
            CreatureModelItem[] arr = JsonConvert.DeserializeObject<CreatureModelItem[]>(jsonText);
            if (arr != null)
            {
                foreach (var item in arr)
                {
                    string name = !string.IsNullOrEmpty(item.remark) ? item.remark : item.mark_name;
                    creatureModelMap[item.id] = string.IsNullOrEmpty(name) ? $"模组{item.id}" : name;
                }
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"加载生物模组(物种)失败: {e.Message}");
        }
    }

    /// <summary>生物模组条目(用于反序列化 CreatureModel.txt)</summary>
    [Serializable]
    private class CreatureModelItem
    {
        public long id;
        public string mark_name;
        public string remark;
    }

    /// <summary>
    /// 物种名(按 creature_model_id 取；0=通用，未知回退id)
    /// </summary>
    private string GetSpeciesName(long modelId)
    {
        if (modelId == 0)
            return "通用";
        if (creatureModelMap.TryGetValue(modelId, out string name))
            return name;
        return $"模组{modelId}";
    }

    /// <summary>
    /// 从 Excel 加载全部道具行
    /// </summary>
    private void LoadItemsFromExcel()
    {
        allRows.Clear();
        dataLoaded = false;

        if (!File.Exists(excelPath))
        {
            LogUtil.LogError($"道具 Excel 不存在: {excelPath}");
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

            // 建立表头 -> 列号映射
            Dictionary<string, int> colMap = new Dictionary<string, int>();
            for (int col = 1; col <= columnCount; col++)
            {
                string header = sheet.Cells[1, col].Text;
                if (!string.IsNullOrEmpty(header) && !colMap.ContainsKey(header))
                    colMap[header] = col;
            }

            // reward_rarity 列缺失说明尚未加列/未刷新
            if (!colMap.ContainsKey("reward_rarity"))
            {
                LogUtil.LogError("Excel 缺少 reward_rarity 列，请先在道具表添加该列。");
            }

            for (int row = 4; row <= rowCount; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;

                int.TryParse(GetCell(sheet, colMap, "item_type", row), out int itemType);
                long.TryParse(GetCell(sheet, colMap, "creature_model_id", row), out long creatureModelId);
                string iconRes = GetCell(sheet, colMap, "icon_res", row);
                string remark = GetCell(sheet, colMap, "remark", row);
                long.TryParse(GetCell(sheet, colMap, "name[language]", row), out long nameId);
                string rewardRarity = GetCell(sheet, colMap, "reward_rarity", row);

                string displayName = languageMap.TryGetValue(nameId, out string content) && !string.IsNullOrEmpty(content)
                    ? content : (string.IsNullOrEmpty(remark) ? $"id:{id}" : remark);

                allRows.Add(new ItemRow
                {
                    id = id,
                    itemType = itemType,
                    creatureModelId = creatureModelId,
                    iconRes = iconRes,
                    name = displayName,
                    raritySet = ParseRaritySet(rewardRarity),
                    originalRarity = NormalizeRarityString(rewardRarity)
                });
            }
        });

        BuildItemTypeFilter();
        BuildSpeciesFilter();
        dataLoaded = allRows.Count > 0;
    }

    /// <summary>
    /// 读取单元格文本(按表头名取列，列不存在返回空)
    /// </summary>
    private string GetCell(ExcelWorksheet sheet, Dictionary<string, int> colMap, string header, int row)
    {
        if (colMap.TryGetValue(header, out int col))
            return sheet.Cells[row, col].Text;
        return "";
    }

    /// <summary>
    /// 构建道具类型筛选下拉项
    /// </summary>
    private void BuildItemTypeFilter()
    {
        itemTypeValues.Clear();
        List<string> labels = new List<string> { "全部类型" };
        itemTypeValues.Add(0);

        HashSet<int> typeSet = new HashSet<int>();
        foreach (var row in allRows)
            typeSet.Add(row.itemType);
        List<int> sortedTypes = new List<int>(typeSet);
        sortedTypes.Sort();
        foreach (var t in sortedTypes)
        {
            labels.Add($"{GetItemTypeName(t)}({t})");
            itemTypeValues.Add(t);
        }
        itemTypeLabels = labels.ToArray();
        if (filterTypeIndex >= itemTypeLabels.Length)
            filterTypeIndex = 0;
    }

    /// <summary>
    /// 构建物种(生物模组)筛选下拉项(按物种名排序，0=通用置前)
    /// </summary>
    private void BuildSpeciesFilter()
    {
        speciesValues.Clear();
        List<string> labels = new List<string> { "全部物种" };
        speciesValues.Add(-1);

        HashSet<long> modelSet = new HashSet<long>();
        foreach (var row in allRows)
            modelSet.Add(row.creatureModelId);
        List<long> sortedModels = new List<long>(modelSet);
        // 通用(0)置前，其余按物种名排序
        sortedModels.Sort((a, b) =>
        {
            if (a == 0 && b != 0) return -1;
            if (b == 0 && a != 0) return 1;
            return string.Compare(GetSpeciesName(a), GetSpeciesName(b), StringComparison.Ordinal);
        });
        foreach (var m in sortedModels)
        {
            labels.Add($"{GetSpeciesName(m)}({m})");
            speciesValues.Add(m);
        }
        speciesLabels = labels.ToArray();
        if (filterSpeciesIndex >= speciesLabels.Length)
            filterSpeciesIndex = 0;
    }

    /// <summary>
    /// 道具类型中文名(用于筛选下拉展示)
    /// </summary>
    private string GetItemTypeName(int itemType)
    {
        switch (itemType)
        {
            case 1: return "帽子";
            case 2: return "衣服";
            case 3: return "裤子";
            case 4: return "鞋子";
            case 5: return "鼻环";
            case 6: return "戒指";
            case 10: return "武器";
            case 101: return "立绘";
            case 1000: return "魔晶";
            default: return "其他";
        }
    }

    /// <summary>
    /// 解析 reward_rarity 逗号串为集合
    /// </summary>
    private HashSet<int> ParseRaritySet(string value)
    {
        HashSet<int> set = new HashSet<int>();
        if (string.IsNullOrEmpty(value))
            return set;
        foreach (var part in value.Split(','))
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed) && int.TryParse(trimmed, out int r))
                set.Add(r);
        }
        return set;
    }

    /// <summary>
    /// 将集合规范化为升序逗号串(用于对比与写回)
    /// </summary>
    private string RaritySetToString(HashSet<int> set)
    {
        if (set == null || set.Count == 0)
            return "";
        List<int> list = new List<int>(set);
        list.Sort();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(list[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 规范化原始字符串(去空格并排序)，保证与编辑结果可对比
    /// </summary>
    private string NormalizeRarityString(string value)
    {
        return RaritySetToString(ParseRaritySet(value));
    }

    /// <summary>
    /// 按搜索/类型过滤并排序(同名相邻：先按名字，再按id)
    /// </summary>
    private void RebuildShowRows()
    {
        baseRows.Clear();
        showRows.Clear();
        int typeFilter = (filterTypeIndex >= 0 && filterTypeIndex < itemTypeValues.Count) ? itemTypeValues[filterTypeIndex] : 0;
        long speciesFilter = (filterSpeciesIndex >= 0 && filterSpeciesIndex < speciesValues.Count) ? speciesValues[filterSpeciesIndex] : -1;
        string key = string.IsNullOrEmpty(searchKey) ? null : searchKey.Trim();

        // 先套用类型/物种/名字的前置过滤，得到基准集(统计与稀有度筛选都基于它)
        foreach (var row in allRows)
        {
            if (typeFilter != 0 && row.itemType != typeFilter)
                continue;
            if (speciesFilter != -1 && row.creatureModelId != speciesFilter)
                continue;
            if (key != null && (string.IsNullOrEmpty(row.name) || row.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            baseRows.Add(row);
        }

        // 再套用稀有度筛选：命中"含该稀有度"或"全适配(空)"的道具(与全适配 +1 到每个稀有度的统计语义一致)
        foreach (var row in baseRows)
        {
            if (filterRarity != 0 && !(row.raritySet.Count == 0 || row.raritySet.Contains(filterRarity)))
                continue;
            showRows.Add(row);
        }

        showRows.Sort((a, b) =>
        {
            int c = string.Compare(a.name, b.name, StringComparison.Ordinal);
            if (c != 0) return c;
            return a.id.CompareTo(b.id);
        });
    }

    #endregion

    #region 图标懒加载

    /// <summary>
    /// 获取道具图标贴图(懒加载并缓存；优先道具目录，其次全局按名搜索)
    /// </summary>
    private Texture2D GetIconTexture(string iconRes)
    {
        if (string.IsNullOrEmpty(iconRes))
            return null;
        // 解析 "name,AtlasTag" -> name
        string name = iconRes;
        int commaIndex = iconRes.LastIndexOf(',');
        if (commaIndex > 0)
            name = iconRes.Substring(0, commaIndex);

        if (iconCache.TryGetValue(name, out Texture2D cached))
            return cached;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ItemIconFolder}/{name}.png");
        if (tex == null)
        {
            // 回退：全局按名搜索(如 UI 图集内的魔晶图标不在道具目录)
            string[] guids = AssetDatabase.FindAssets($"{name} t:Texture2D");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    break;
                }
            }
        }
        iconCache[name] = tex;
        return tex;
    }

    #endregion

    #region UI 绘制 - 工具栏

    /// <summary>
    /// 绘制顶部工具栏(搜索/类型筛选/操作按钮)
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"道具稀有度白名单配置   共 {allRows.Count} 个道具 | 当前显示 {showRows.Count}", sectionHeaderStyle);
        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("搜索名字:", GUILayout.Width(60));
        string newSearch = EditorGUILayout.TextField(searchKey, GUILayout.Width(180));
        if (newSearch != searchKey)
        {
            searchKey = newSearch;
            RebuildShowRows();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("类型:", GUILayout.Width(36));
        int newTypeIndex = EditorGUILayout.Popup(filterTypeIndex, itemTypeLabels ?? new[] { "全部类型" }, GUILayout.Width(120));
        if (newTypeIndex != filterTypeIndex)
        {
            filterTypeIndex = newTypeIndex;
            RebuildShowRows();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("物种:", GUILayout.Width(36));
        int newSpeciesIndex = EditorGUILayout.Popup(filterSpeciesIndex, speciesLabels ?? new[] { "全部物种" }, GUILayout.Width(140));
        if (newSpeciesIndex != filterSpeciesIndex)
        {
            filterSpeciesIndex = newSpeciesIndex;
            RebuildShowRows();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("打开道具 Excel", GUILayout.Width(110), GUILayout.Height(20)))
            OpenItemExcel();

        if (GUILayout.Button("刷新", GUILayout.Width(60), GUILayout.Height(20)))
        {
            LoadLanguageMap();
            LoadCreatureModelMap();
            LoadItemsFromExcel();
            RebuildShowRows();
        }

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.30f, 0.55f, 0.90f);
        if (GUILayout.Button("保存到Excel并同步JSON", GUILayout.Width(180), GUILayout.Height(20)))
            SaveData();
        GUI.backgroundColor = prev;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("留空=全稀有度适配；勾选后该道具仅在勾选的稀有度奖励中产出。稀有度: N=1 R=2 SR=3 SSR=4 UR=5 L=6", MessageType.None);

        DrawStatsBar();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制稀有度统计/筛选条：每个稀有度显示「可产出该稀有度的道具数」(全适配道具计入每个稀有度)，
    /// 点击即按该稀有度一键筛选(叠加类型/物种/名字前置过滤)，再次点击或点「全部」取消。
    /// 统计基于 baseRows(前置过滤内)，故数字稳定不随稀有度筛选变化。
    /// </summary>
    private void DrawStatsBar()
    {
        int[] rarityCount = new int[RarityOptions.Length];
        foreach (var row in baseRows)
        {
            bool isAll = row.raritySet.Count == 0; //全适配=对每个稀有度都 +1
            for (int i = 0; i < RarityOptions.Length; i++)
            {
                if (isAll || row.raritySet.Contains((int)RarityOptions[i]))
                    rarityCount[i]++;
            }
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("稀有度筛选/统计:", EditorStyles.miniBoldLabel, GUILayout.Width(96));

        // 「全部」= 取消稀有度筛选
        bool allActive = filterRarity == 0;
        bool newAllActive = GUILayout.Toggle(allActive, "全部", EditorStyles.miniButton, GUILayout.Width(48), GUILayout.Height(20));
        if (newAllActive && !allActive)
        {
            filterRarity = 0;
            RebuildShowRows();
        }

        for (int i = 0; i < RarityOptions.Length; i++)
        {
            int rarityId = (int)RarityOptions[i];
            bool active = filterRarity == rarityId;
            bool newActive = GUILayout.Toggle(active, $"{RarityOptions[i]} {rarityCount[i]}", EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(20));
            if (newActive != active)
            {
                filterRarity = newActive ? rarityId : 0; //再次点击取消
                RebuildShowRows();
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制列表列头
    /// </summary>
    private void DrawListHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(44);
        EditorGUILayout.LabelField("道具", EditorStyles.miniBoldLabel, GUILayout.Width(260));
        EditorGUILayout.LabelField("稀有度白名单(勾选)", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 虚拟化列表

    /// <summary>
    /// 绘制虚拟化道具列表(仅渲染可视范围内的行 + 上下占位，图标懒加载)
    /// </summary>
    private void DrawVirtualizedList()
    {
        int total = showRows.Count;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 计算可视范围
        float viewHeight = Mathf.Max(RowHeight, position.height - TopAreaHeight);
        int first = Mathf.Max(0, Mathf.FloorToInt(scrollPos.y / RowHeight) - 2);
        int visibleCount = Mathf.CeilToInt(viewHeight / RowHeight) + 4;
        int last = Mathf.Min(total, first + visibleCount);

        // 顶部占位
        if (first > 0)
            GUILayout.Space(first * RowHeight);

        // 上一行名字(用于同名分组交替底色)
        string prevName = first > 0 ? showRows[first - 1].name : null;
        int groupParity = 0;
        // 统计 first 之前(索引 0..first-1)的分组切换次数确定初始奇偶，保证滚动时底色稳定不闪烁
        for (int i = 1; i < first && i < total; i++)
        {
            if (showRows[i].name != showRows[i - 1].name)
                groupParity ^= 1;
        }

        for (int i = first; i < last; i++)
        {
            var row = showRows[i];
            if (prevName != null && row.name != prevName)
                groupParity ^= 1;
            prevName = row.name;
            DrawRow(row, groupParity == 1);
        }

        // 底部占位
        if (last < total)
            GUILayout.Space((total - last) * RowHeight);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制单行(图标 + 名字/ID + 稀有度勾选)
    /// </summary>
    private void DrawRow(ItemRow row, bool oddGroup)
    {
        EditorGUILayout.BeginHorizontal(oddGroup ? rowBoxOddStyle : rowBoxEvenStyle, GUILayout.Height(RowHeight));

        // 图标
        Rect iconRect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36), GUILayout.Height(36));
        Texture2D tex = GetIconTexture(row.iconRes);
        if (tex != null)
            GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
        else
            EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.3f, 0.3f, 0.3f));

        GUILayout.Space(6);

        // 名字 + ID
        EditorGUILayout.BeginVertical(GUILayout.Width(210));
        GUILayout.Space(3);
        EditorGUILayout.LabelField(row.name, nameLabelStyle);
        EditorGUILayout.LabelField($"id:{row.id}  {GetSpeciesName(row.creatureModelId)}  type:{GetItemTypeName(row.itemType)}({row.itemType})", idLabelStyle);
        EditorGUILayout.EndVertical();

        GUILayout.Space(6);

        // 稀有度勾选(枚举形式)
        for (int i = 0; i < RarityOptions.Length; i++)
        {
            int rarityId = (int)RarityOptions[i];
            bool has = row.raritySet.Contains(rarityId);
            bool newHas = GUILayout.Toggle(has, RarityOptions[i].ToString(), "Button", GUILayout.Width(46), GUILayout.Height(24));
            if (newHas != has)
            {
                if (newHas) row.raritySet.Add(rarityId);
                else row.raritySet.Remove(rarityId);
            }
        }

        GUILayout.Space(8);
        // 快捷：全清(等价全稀有度适配)
        if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(40), GUILayout.Height(22)))
            row.raritySet.Clear();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 保存逻辑

    /// <summary>
    /// 保存变更到 Excel，并同步补丁 JSON 的 reward_rarity 字段
    /// </summary>
    private void SaveData()
    {
        List<ExcelUtil.ExcelChangeData> changeList = new List<ExcelUtil.ExcelChangeData>();
        Dictionary<long, string> changedValues = new Dictionary<long, string>();

        foreach (var row in allRows)
        {
            string current = RaritySetToString(row.raritySet);
            if (current != row.originalRarity)
            {
                changeList.Add(new ExcelUtil.ExcelChangeData(row.id, "reward_rarity", current));
                changedValues[row.id] = current;
            }
        }

        if (changeList.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到稀有度配置变更。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认保存",
            $"检测到 {changeList.Count} 个道具的稀有度白名单变更，确定写入 Excel 并同步 JSON 吗？", "保存", "取消"))
            return;

        try
        {
            // 1) 写回 Excel(唯一真实源)
            ExcelUtil.SetExcelData(excelPath, SheetName, changeList);

            // 2) 定向补丁 JSON 的 reward_rarity(不触碰其它字段，避开 name[language] 特殊处理)
            PatchJsonRewardRarity(changedValues);

            AssetDatabase.Refresh();

            // 3) 刷新原始值，标记为已保存
            foreach (var row in allRows)
                row.originalRarity = RaritySetToString(row.raritySet);

            EditorUtility.DisplayDialog("完成",
                $"已保存 {changeList.Count} 项到 Excel，并同步了 JSON。\n\n" +
                "注意：若 ItemsInfoBean 尚未包含 reward_rarity 字段，请先在 Unity 对 ItemsInfo 执行一次「生成 Entity」使字段生效。",
                "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}", "确定");
            LogUtil.LogError($"道具稀有度保存失败: {e}");
        }
    }

    /// <summary>
    /// 定向补丁 ItemsInfo.txt 的 reward_rarity 字段(按 id 匹配，逐条 set/add，保留其它字段)
    /// </summary>
    private void PatchJsonRewardRarity(Dictionary<long, string> changedValues)
    {
        string jsonPath = jsonFolderPath + "/ItemsInfo.txt";
        if (!File.Exists(jsonPath))
        {
            LogUtil.LogError($"未找到 JSON: {jsonPath}，跳过 JSON 同步(可在 Unity 重新导出)。");
            return;
        }

        string text = File.ReadAllText(jsonPath);
        var arr = Newtonsoft.Json.Linq.JArray.Parse(text);
        foreach (var token in arr)
        {
            if (token is Newtonsoft.Json.Linq.JObject obj)
            {
                var idToken = obj["id"];
                if (idToken == null) continue;
                long id = (long)idToken;
                if (changedValues.TryGetValue(id, out string value))
                    obj["reward_rarity"] = value;
            }
        }
        File.WriteAllText(jsonPath, arr.ToString(Formatting.None));
    }

    /// <summary>
    /// 打开道具 Excel 表格
    /// </summary>
    private void OpenItemExcel()
    {
        if (File.Exists(excelPath))
            System.Diagnostics.Process.Start(excelPath);
        else
            EditorUtility.DisplayDialog("错误", $"道具 Excel 文件不存在:\n{excelPath}", "确定");
    }

    #endregion
}
