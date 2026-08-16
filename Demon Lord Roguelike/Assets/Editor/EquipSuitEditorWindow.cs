using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 装备套装配置窗口
/// 用于可视化编辑 excel_equip_suit_info[装备套装] 表(EquipSuitInfo)：
/// 一行=一套手动搭配的套装(帽/衣/裤/鞋/鼻环/戒指/武器 7槽位, 0=空槽)，按种族模组(creature_model_id, 0=通用)归属物种；
/// 套装被 CreatureRandomInfo 的套装池(random_type=2, 由「游戏/皮肤随机池配置」窗口编辑池组合)引用，
/// NPC 配置 equip_random 指向套装池后, 创建时从池内整套随机(见 CreatureBean.InitRandomEquipForSuit)。
/// 左侧编辑当前套装的物种/备注/各槽位道具，右侧为当前槽位的候选装备列表(按槽位类型+套装物种过滤, 点选填入)；
/// 支持新建/删除套装；保存时删除/新增/修改统一在一个 EPPlus 会话写回 Excel(数字列写数值类型, 不走 SetExcelData 以免 long 列变文本)，
/// 最后 ExcelToJsonItem 同步导出 JSON(依赖 EquipSuitInfoBean 已由 ExcelEditorWindow 生成, 否则导出会报实体类缺失)。
/// 装备图标按 icon_res 从 AtlasForItems 图集取 sprite。
/// </summary>
public class EquipSuitEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/装备套装配置
    /// </summary>
    [MenuItem("游戏/装备套装配置")]
    private static void CreateWindow()
    {
        var window = GetWindow<EquipSuitEditorWindow>();
        window.titleContent = new GUIContent("装备套装配置");
        window.minSize = new Vector2(980, 560);
        window.Show();
    }

    #endregion

    #region 内部数据结构

    /// <summary>单个套装编辑数据</summary>
    private class SuitRow
    {
        public long id;
        public int modelId;                                  //种族模组ID(0=通用)
        public string remark;
        public Dictionary<int, long> slots = new Dictionary<int, long>(); //key=ItemTypeEnum值, value=道具ID(0=空槽)
        public bool isNew;                                   //新建未保存
        public string originalSnapshot;                      //原始快照串(脏检查用)

        /// <summary>当前内容快照串(物种|备注|各槽位道具)</summary>
        public string Snapshot()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(modelId).Append('|').Append(remark ?? "");
            foreach (var slot in SlotDefs)
                sb.Append('|').Append(slots.TryGetValue(slot.itemType, out long v) ? v : 0);
            return sb.ToString();
        }

        /// <summary>是否有未保存变更(新建套装恒为true)</summary>
        public bool IsDirty => isNew || Snapshot() != originalSnapshot;

        /// <summary>刷新原始快照为当前内容</summary>
        public void CommitSnapshot()
        {
            isNew = false;
            originalSnapshot = Snapshot();
        }
    }

    /// <summary>槽位定义(道具类型 + 表列名 + 中文名)</summary>
    private class SlotDef
    {
        public int itemType;
        public string column;
        public string label;
        public SlotDef(int itemType, string column, string label)
        {
            this.itemType = itemType;
            this.column = column;
            this.label = label;
        }
    }

    /// <summary>单个装备道具(ItemsInfo)数据</summary>
    private class ItemInfoItem
    {
        public long id;
        public int itemType;     //道具类型(ItemTypeEnum)
        public int weaponType;   //武器类型(ItemTypeWeaponEnum, 仅武器有效)
        public int modelId;      //种族模组ID(creature_model_id, 0=通用)
        public string iconRes;
        public string remark;
    }

    /// <summary>套装槽位定义表(与 EquipSuitInfo 列一一对应)</summary>
    private static readonly SlotDef[] SlotDefs =
    {
        new SlotDef((int)ItemTypeEnum.Hat, "hat", "帽子"),
        new SlotDef((int)ItemTypeEnum.Clothes, "clothes", "衣服"),
        new SlotDef((int)ItemTypeEnum.Pants, "pants", "裤子"),
        new SlotDef((int)ItemTypeEnum.Shoe, "shoe", "鞋子"),
        new SlotDef((int)ItemTypeEnum.NoseRing, "nose_ring", "鼻环"),
        new SlotDef((int)ItemTypeEnum.FingerRing, "finger_ring", "戒指"),
        new SlotDef((int)ItemTypeEnum.Weapon, "weapon", "武器"),
    };

    #endregion

    #region 常量

    /// <summary>装备套装工作表名</summary>
    private const string SheetSuit = "EquipSuitInfo";

    /// <summary>生物模型(物种)工作表名</summary>
    private const string SheetModel = "CreatureModel";

    /// <summary>道具信息工作表名</summary>
    private const string SheetItems = "ItemsInfo";

    /// <summary>装备图标图集路径(Items图集)</summary>
    private const string ItemsAtlasPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForItems.spriteatlas";

    /// <summary>列表行高(带图标)</summary>
    private const float RowHeight = 40f;

    /// <summary>可填入套装的道具类型(帽/衣/裤/鞋/鼻环/戒指/武器)</summary>
    private static readonly HashSet<int> EquipItemTypes = new HashSet<int>
    {
        (int)ItemTypeEnum.Hat, (int)ItemTypeEnum.Clothes, (int)ItemTypeEnum.Pants, (int)ItemTypeEnum.Shoe,
        (int)ItemTypeEnum.NoseRing, (int)ItemTypeEnum.FingerRing, (int)ItemTypeEnum.Weapon,
    };

    #endregion

    #region 成员变量

    /// <summary>装备套装 Excel 路径</summary>
    private string excelPathSuit;

    /// <summary>生物模型(物种) Excel 路径</summary>
    private string excelPathModel;

    /// <summary>道具信息 Excel 路径</summary>
    private string excelPathItems;

    /// <summary>全部套装</summary>
    private readonly List<SuitRow> allSuits = new List<SuitRow>();

    /// <summary>套装下拉项标签(与 allSuits 对齐)</summary>
    private string[] suitLabels;

    /// <summary>当前选中套装索引</summary>
    private int selectSuitIndex = 0;

    /// <summary>待保存时物理删除的套装id(删除即时从列表移除, 保存才落盘)</summary>
    private readonly List<long> deletedIds = new List<long>();

    /// <summary>新建套装的id输入(默认建议为最大id+1, 可手动改)</summary>
    private string newSuitIdInput = "";

    /// <summary>全部装备道具(ItemsInfo中装备类型道具)</summary>
    private readonly List<ItemInfoItem> allItemInfos = new List<ItemInfoItem>();

    /// <summary>道具ID -> 装备道具数据</summary>
    private readonly Dictionary<long, ItemInfoItem> itemInfoMap = new Dictionary<long, ItemInfoItem>();

    /// <summary>物种(模组)ID -> 物种名</summary>
    private readonly Dictionary<long, string> modelNameMap = new Dictionary<long, string>();

    /// <summary>物种下拉可选id(0=通用 + 官方物种; 与 modelLabels 对齐)</summary>
    private readonly List<int> modelIdValues = new List<int>();

    /// <summary>物种下拉标签</summary>
    private string[] modelLabels;

    /// <summary>装备图标图集(懒加载)</summary>
    private SpriteAtlas itemsAtlas;

    /// <summary>装备图标sprite缓存(key=icon_res)</summary>
    private readonly Dictionary<string, Sprite> itemSpriteCache = new Dictionary<string, Sprite>();

    /// <summary>当前挑选候选的槽位(ItemTypeEnum值, 默认帽子)</summary>
    private int currentPickSlot = (int)ItemTypeEnum.Hat;

    /// <summary>右侧候选展示列表(套用筛选后)</summary>
    private readonly List<ItemInfoItem> candidateShowList = new List<ItemInfoItem>();

    /// <summary>搜索关键字(匹配 id/icon_res/remark)</summary>
    private string searchKey = "";

    /// <summary>左编辑区滚动位置</summary>
    private Vector2 scrollPosEdit = Vector2.zero;

    /// <summary>右候选列表滚动位置</summary>
    private Vector2 scrollPosCandidate = Vector2.zero;

    /// <summary>数据已加载标记</summary>
    private bool dataLoaded = false;

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    private GUIStyle sectionHeaderStyle;
    private GUIStyle slotHeaderStyle;
    private GUIStyle rowLabelStyle;
    private GUIStyle idLabelStyle;
    private GUIStyle warnLabelStyle;

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时初始化路径并加载数据
    /// </summary>
    private void OnEnable()
    {
        excelPathSuit = Application.dataPath + "/Data/Excel/excel_equip_suit_info[装备套装].xlsx";
        excelPathModel = Application.dataPath + "/Data/Excel/excel_creature_model[生物模型信息].xlsx";
        excelPathItems = Application.dataPath + "/Data/Excel/excel_items_info[道具信息].xlsx";
        LoadAllData();
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
            EditorGUILayout.HelpBox("未能从 Excel 加载数据，请确认文件存在且已关闭。", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawSuitEditor();
        DrawCandidateList();
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化自定义 UI 样式
    /// </summary>
    private void InitializeStyles()
    {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        slotHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11 };
        slotHeaderStyle.normal.textColor = new Color(0.55f, 0.75f, 1f);
        rowLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
        idLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        warnLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        warnLabelStyle.normal.textColor = new Color(1f, 0.45f, 0.4f);
        stylesInitialized = true;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载全部数据(套装 + 装备全集 + 物种名)
    /// </summary>
    private void LoadAllData()
    {
        dataLoaded = false;
        deletedIds.Clear();
        LoadModelNames();
        LoadItemInfos();
        LoadSuits();
        RefreshNewSuitIdSuggestion();
        RebuildCandidates();
        dataLoaded = true;
    }

    /// <summary>
    /// 刷新新建id建议值(当前最大id+1, 套装id起始段200001[6位, 与装备池8位段错开])
    /// </summary>
    private void RefreshNewSuitIdSuggestion()
    {
        long maxId = 200000;
        foreach (var suit in allSuits)
        {
            if (suit.id > maxId)
                maxId = suit.id;
        }
        newSuitIdInput = (maxId + 1).ToString();
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
    /// 构建表头 -> 列号映射(表头在第1行)
    /// </summary>
    private Dictionary<string, int> BuildColMap(ExcelWorksheet sheet)
    {
        Dictionary<string, int> colMap = new Dictionary<string, int>();
        for (int col = 1; col <= sheet.Dimension.End.Column; col++)
        {
            string header = sheet.Cells[1, col].Text;
            if (!string.IsNullOrEmpty(header) && !colMap.ContainsKey(header))
                colMap[header] = col;
        }
        return colMap;
    }

    /// <summary>
    /// 加载物种名映射与物种下拉项(只收有备注的官方物种, Mod物种remark为空不进下拉)
    /// </summary>
    private void LoadModelNames()
    {
        modelNameMap.Clear();
        modelIdValues.Clear();
        modelIdValues.Add(0); //通用
        List<string> labels = new List<string> { "通用(0)" };
        if (!File.Exists(excelPathModel))
        {
            LogUtil.LogError($"生物模型 Excel 不存在: {excelPathModel}");
            modelLabels = labels.ToArray();
            return;
        }
        ExcelUtil.GetExcelPackage(new FileInfo(excelPathModel), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetModel];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetModel}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;
                string remark = GetCell(sheet, colMap, "remark", row);
                string markName = GetCell(sheet, colMap, "mark_name", row);
                string name = !string.IsNullOrEmpty(remark) ? remark : markName;
                modelNameMap[id] = string.IsNullOrEmpty(name) ? $"模组{id}" : name;
                //物种下拉只列官方物种(有备注的)
                if (!string.IsNullOrEmpty(remark) && id < 100000)
                {
                    modelIdValues.Add((int)id);
                    labels.Add($"{remark}({id})");
                }
            }
        });
        modelLabels = labels.ToArray();
    }

    /// <summary>
    /// 加载装备道具全集(ItemsInfo中装备类型的道具)
    /// </summary>
    private void LoadItemInfos()
    {
        allItemInfos.Clear();
        itemInfoMap.Clear();
        if (!File.Exists(excelPathItems))
        {
            LogUtil.LogError($"道具信息 Excel 不存在: {excelPathItems}");
            return;
        }
        ExcelUtil.GetExcelPackage(new FileInfo(excelPathItems), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetItems];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetItems}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;
                int.TryParse(GetCell(sheet, colMap, "item_type", row), out int itemType);
                //只收录装备类型道具(帽/衣/裤/鞋/鼻环/戒指/武器)
                if (!EquipItemTypes.Contains(itemType))
                    continue;
                int.TryParse(GetCell(sheet, colMap, "item_weapon_type", row), out int weaponType);
                int.TryParse(GetCell(sheet, colMap, "creature_model_id", row), out int modelId);
                var item = new ItemInfoItem
                {
                    id = id,
                    itemType = itemType,
                    weaponType = weaponType,
                    modelId = modelId,
                    iconRes = GetCell(sheet, colMap, "icon_res", row),
                    remark = GetCell(sheet, colMap, "remark", row)
                };
                allItemInfos.Add(item);
                itemInfoMap[id] = item;
            }
        });
        allItemInfos.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 加载全部套装(EquipSuitInfo表)
    /// </summary>
    private void LoadSuits()
    {
        allSuits.Clear();
        if (!File.Exists(excelPathSuit))
        {
            LogUtil.LogError($"装备套装 Excel 不存在: {excelPathSuit}");
            return;
        }
        ExcelUtil.GetExcelPackage(new FileInfo(excelPathSuit), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetSuit];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetSuit}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;
                var suit = new SuitRow { id = id };
                int.TryParse(GetCell(sheet, colMap, "creature_model_id", row), out suit.modelId);
                suit.remark = GetCell(sheet, colMap, "remark", row);
                foreach (var slot in SlotDefs)
                {
                    long.TryParse(GetCell(sheet, colMap, slot.column, row), out long itemId);
                    suit.slots[slot.itemType] = itemId;
                }
                suit.CommitSnapshot();
                allSuits.Add(suit);
            }
        });
        allSuits.Sort((a, b) => a.id.CompareTo(b.id));
        RebuildSuitLabels();
    }

    /// <summary>
    /// 重建套装下拉标签
    /// </summary>
    private void RebuildSuitLabels()
    {
        List<string> labels = new List<string>();
        foreach (var suit in allSuits)
        {
            string modelName = suit.modelId == 0 ? "通用" : GetModelName(suit.modelId);
            string newTag = suit.isNew ? "[新]" : "";
            labels.Add($"{newTag}{suit.id} | {modelName} | {suit.remark}");
        }
        suitLabels = labels.ToArray();
        if (selectSuitIndex >= allSuits.Count)
            selectSuitIndex = allSuits.Count - 1;
    }

    /// <summary>
    /// 当前选中套装
    /// </summary>
    private SuitRow CurrentSuit
    {
        get
        {
            if (selectSuitIndex >= 0 && selectSuitIndex < allSuits.Count)
                return allSuits[selectSuitIndex];
            return null;
        }
    }

    /// <summary>
    /// 物种名(按 model_id 取，未知回退id)
    /// </summary>
    private string GetModelName(int modelId)
    {
        if (modelNameMap.TryGetValue(modelId, out string name))
            return name;
        return $"模组{modelId}";
    }

    /// <summary>
    /// 是否存在任意未保存变更
    /// </summary>
    private bool HasAnyDirty()
    {
        if (deletedIds.Count > 0)
            return true;
        foreach (var suit in allSuits)
        {
            if (suit.IsDirty)
                return true;
        }
        return false;
    }

    #endregion

    #region 装备图标懒加载

    /// <summary>
    /// 获取装备图标sprite(懒加载AtlasForItems图集并缓存)
    /// </summary>
    private Sprite GetItemSprite(string iconRes)
    {
        if (string.IsNullOrEmpty(iconRes))
            return null;
        if (itemSpriteCache.TryGetValue(iconRes, out Sprite cached))
            return cached;
        if (itemsAtlas == null)
            itemsAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ItemsAtlasPath);
        Sprite sprite = itemsAtlas != null ? itemsAtlas.GetSprite(iconRes) : null;
        itemSpriteCache[iconRes] = sprite;
        return sprite;
    }

    /// <summary>
    /// 绘制装备图标(按图集sprite的贴图区域绘制; 无图标时画灰色占位块)
    /// </summary>
    private void DrawItemIcon(ItemInfoItem item)
    {
        Rect iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
        Sprite sprite = item != null ? GetItemSprite(item.iconRes) : null;
        if (sprite != null && sprite.texture != null)
        {
            var tex = sprite.texture;
            var tr = sprite.textureRect;
            Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(iconRect, tex, uv, true);
        }
        else
        {
            EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.3f, 0.3f, 0.3f));
        }
    }

    #endregion

    #region 候选列表

    /// <summary>
    /// 重建右侧候选装备列表(当前槽位类型 + 套装物种过滤 + 搜索)
    /// </summary>
    private void RebuildCandidates()
    {
        candidateShowList.Clear();
        SuitRow suit = CurrentSuit;
        string key = string.IsNullOrEmpty(searchKey) ? null : searchKey.Trim();
        foreach (var item in allItemInfos)
        {
            //按当前槽位类型过滤
            if (item.itemType != currentPickSlot)
                continue;
            //按套装物种过滤(通用装备0总是可见; 套装为通用0时不限物种)
            if (suit != null && suit.modelId != 0 && item.modelId != 0 && item.modelId != suit.modelId)
                continue;
            if (key != null
                && !item.id.ToString().Contains(key)
                && (string.IsNullOrEmpty(item.iconRes) || item.iconRes.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0)
                && (string.IsNullOrEmpty(item.remark) || item.remark.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            candidateShowList.Add(item);
        }
    }

    #endregion

    #region UI 绘制 - 工具栏

    /// <summary>
    /// 绘制顶部工具栏(套装下拉 + 操作按钮)
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"装备套装配置   共 {allSuits.Count} 套 | 装备全集 {allItemInfos.Count} 件", sectionHeaderStyle);
        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("套装:", GUILayout.Width(36));
        if (suitLabels != null && suitLabels.Length > 0 && selectSuitIndex >= 0)
        {
            int newIndex = EditorGUILayout.Popup(selectSuitIndex, suitLabels, GUILayout.Width(280));
            if (newIndex != selectSuitIndex)
            {
                selectSuitIndex = newIndex;
                RebuildCandidates();
            }
        }
        else
        {
            EditorGUILayout.LabelField("(无数据)", GUILayout.Width(280));
        }

        EditorGUILayout.LabelField("新id:", GUILayout.Width(32));
        newSuitIdInput = EditorGUILayout.TextField(newSuitIdInput, GUILayout.Width(70));
        if (GUILayout.Button("新建套装", GUILayout.Width(70), GUILayout.Height(20)))
            CreateNewSuit();

        //删除按钮仅在选中套装时可用
        GUI.enabled = CurrentSuit != null;
        if (GUILayout.Button("删除套装", GUILayout.Width(70), GUILayout.Height(20)))
            DeleteCurrentSuit();
        GUI.enabled = true;

        if (GUILayout.Button("打开Excel", GUILayout.Width(80), GUILayout.Height(20)))
            OpenSuitExcel();

        if (GUILayout.Button("刷新", GUILayout.Width(60), GUILayout.Height(20)))
        {
            if (!HasAnyDirty() || EditorUtility.DisplayDialog("确认刷新", "存在未保存的变更，刷新将丢弃这些变更，确定吗？", "丢弃并刷新", "取消"))
                LoadAllData();
        }

        GUILayout.FlexibleSpace();

        if (HasAnyDirty())
            EditorGUILayout.LabelField("● 有未保存变更", warnLabelStyle, GUILayout.Width(100));

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.30f, 0.55f, 0.90f);
        if (GUILayout.Button("保存到Excel并同步JSON", GUILayout.Width(180), GUILayout.Height(20)))
            SaveData();
        GUI.backgroundColor = prev;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("一套=一行手动搭配(0=空槽)。左侧编辑套装的物种/备注/槽位，点槽位「选择」后右侧列出该槽位候选装备(按套装物种过滤)，点候选填入。套装由「游戏/皮肤随机池配置」的套装池(random_type=2)引用参与NPC随机。物种不匹配的件会以红色警告(运行时该套将被整套过滤)。", MessageType.None);

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - 套装编辑区

    /// <summary>
    /// 绘制左侧套装编辑区(物种/备注/7槽位)
    /// </summary>
    private void DrawSuitEditor()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2f - 8));
        SuitRow suit = CurrentSuit;
        if (suit == null)
        {
            EditorGUILayout.LabelField("(无套装, 点「新建套装」创建)", rowLabelStyle);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField($"当前套装: {suit.id}{(suit.isNew ? " (新建未保存)" : "")}", EditorStyles.boldLabel);

        //物种下拉
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("物种:", GUILayout.Width(36));
        int modelIndex = modelIdValues.IndexOf(suit.modelId);
        if (modelIndex < 0) modelIndex = 0;
        int newModelIndex = EditorGUILayout.Popup(modelIndex, modelLabels ?? new[] { "通用(0)" }, GUILayout.Width(160));
        if (newModelIndex != modelIndex && newModelIndex >= 0 && newModelIndex < modelIdValues.Count)
        {
            suit.modelId = modelIdValues[newModelIndex];
            RebuildSuitLabels();
            RebuildCandidates();
        }
        EditorGUILayout.LabelField("备注:", GUILayout.Width(36));
        string newRemark = EditorGUILayout.TextField(suit.remark ?? "", GUILayout.MinWidth(120));
        if (newRemark != suit.remark)
        {
            suit.remark = newRemark;
            RebuildSuitLabels();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        //7槽位行
        scrollPosEdit = EditorGUILayout.BeginScrollView(scrollPosEdit);
        foreach (var slot in SlotDefs)
        {
            DrawSlotRow(suit, slot);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制单个槽位行(图标 + 道具信息 + 选择/清空按钮; 物种不匹配红色警告)
    /// </summary>
    private void DrawSlotRow(SuitRow suit, SlotDef slot)
    {
        suit.slots.TryGetValue(slot.itemType, out long itemId);
        bool hasItem = itemId > 0;
        ItemInfoItem item = hasItem && itemInfoMap.TryGetValue(itemId, out var info) ? info : null;

        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        //槽位名(当前挑选槽位高亮)
        bool isPicking = currentPickSlot == slot.itemType;
        EditorGUILayout.LabelField(isPicking ? $"▶ {slot.label}" : $"   {slot.label}", slotHeaderStyle, GUILayout.Width(56));

        DrawItemIcon(item);

        if (hasItem && item != null)
        {
            //物种不匹配警告(道具与套装物种均非0且不一致)
            bool speciesMismatch = suit.modelId != 0 && item.modelId != 0 && item.modelId != suit.modelId;
            var labelStyle = speciesMismatch ? warnLabelStyle : rowLabelStyle;
            EditorGUILayout.LabelField($"{itemId}", idLabelStyle, GUILayout.Width(80));
            string itemDesc = item.remark ?? "";
            if (slot.itemType == (int)ItemTypeEnum.Weapon && item.weaponType != 0)
                itemDesc += $"  [{(ItemTypeWeaponEnum)item.weaponType}]";
            if (speciesMismatch)
                itemDesc += $"  (物种不匹配:{GetModelName(item.modelId)})";
            EditorGUILayout.LabelField(itemDesc, labelStyle, GUILayout.MinWidth(100));
        }
        else if (hasItem)
        {
            EditorGUILayout.LabelField($"{itemId}", warnLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField("道具表不存在(悬空ID)", warnLabelStyle);
        }
        else
        {
            EditorGUILayout.LabelField("(空)", idLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField("", rowLabelStyle, GUILayout.MinWidth(100));
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("选择", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            currentPickSlot = slot.itemType;
            RebuildCandidates();
        }
        //清空按钮仅在有道具时可用
        GUI.enabled = hasItem;
        if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            suit.slots[slot.itemType] = 0;
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region UI 绘制 - 候选列表

    /// <summary>
    /// 绘制右侧候选装备列表(当前槽位 + 套装物种过滤)
    /// </summary>
    private void DrawCandidateList()
    {
        EditorGUILayout.BeginVertical("box");

        SuitRow suit = CurrentSuit;
        string slotName = GetSlotLabel(currentPickSlot);
        EditorGUILayout.LabelField($"候选装备 ({slotName}) ({candidateShowList.Count})", EditorStyles.boldLabel);

        // 筛选行(物种由套装限定, 不提供下拉)
        EditorGUILayout.BeginHorizontal();
        string speciesLabel = suit == null ? "-" : (suit.modelId == 0 ? "通用-不限定" : $"{GetModelName(suit.modelId)}({suit.modelId})");
        EditorGUILayout.LabelField($"物种: {speciesLabel}", rowLabelStyle, GUILayout.Width(140));
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(32));
        string newSearch = EditorGUILayout.TextField(searchKey, GUILayout.Width(140));
        if (newSearch != searchKey)
        {
            searchKey = newSearch;
            RebuildCandidates();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        scrollPosCandidate = EditorGUILayout.BeginScrollView(scrollPosCandidate);
        foreach (var item in candidateShowList)
        {
            DrawCandidateRow(suit, item);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制候选装备行(点击填入当前槽位)
    /// </summary>
    private void DrawCandidateRow(SuitRow suit, ItemInfoItem item)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        DrawItemIcon(item);
        EditorGUILayout.LabelField($"{item.id}", idLabelStyle, GUILayout.Width(80));
        EditorGUILayout.LabelField(item.modelId == 0 ? "通用" : GetModelName(item.modelId), rowLabelStyle, GUILayout.Width(56));
        string itemDesc = item.remark ?? "";
        if (item.itemType == (int)ItemTypeEnum.Weapon && item.weaponType != 0)
            itemDesc += $"  [{(ItemTypeWeaponEnum)item.weaponType}]";
        EditorGUILayout.LabelField(itemDesc, rowLabelStyle, GUILayout.MinWidth(100));

        GUILayout.FlexibleSpace();
        GUI.enabled = suit != null;
        if (GUILayout.Button("填入", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            suit.slots[currentPickSlot] = item.id;
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 槽位中文名
    /// </summary>
    private string GetSlotLabel(int itemType)
    {
        foreach (var slot in SlotDefs)
        {
            if (slot.itemType == itemType)
                return slot.label;
        }
        return $"类型{itemType}";
    }

    #endregion

    #region 新建与删除

    /// <summary>
    /// 新建套装(id取输入框值, 默认建议最大id+1可改; 校验正整数且未占用; 默认通用物种, 标记isNew保存时追加行)
    /// </summary>
    private void CreateNewSuit()
    {
        //解析输入id(空=取建议值)
        string input = (newSuitIdInput ?? "").Trim();
        if (input.Length == 0)
        {
            RefreshNewSuitIdSuggestion();
            input = newSuitIdInput;
        }
        if (!long.TryParse(input, out long newId) || newId <= 0)
        {
            EditorUtility.DisplayDialog("错误", $"套装ID格式错误: {input}(需为正整数)", "确定");
            return;
        }
        //id占用校验
        foreach (var suit in allSuits)
        {
            if (suit.id == newId)
            {
                EditorUtility.DisplayDialog("错误", $"套装ID {newId} 已存在(套装: {suit.remark})", "确定");
                return;
            }
        }
        var newSuit = new SuitRow
        {
            id = newId,
            modelId = 0,
            remark = "新套装",
            isNew = true
        };
        foreach (var slot in SlotDefs)
            newSuit.slots[slot.itemType] = 0;
        allSuits.Add(newSuit);
        selectSuitIndex = allSuits.Count - 1;
        RebuildSuitLabels();
        RefreshNewSuitIdSuggestion();
        RebuildCandidates();
    }

    /// <summary>
    /// 删除当前套装(即时从列表移除, 保存时才物理删行; 新建未保存的直接丢弃)
    /// </summary>
    private void DeleteCurrentSuit()
    {
        SuitRow suit = CurrentSuit;
        if (suit == null)
            return;
        if (!EditorUtility.DisplayDialog("确认删除", $"确定删除套装 {suit.id} | {suit.remark} 吗？(保存后生效)", "删除", "取消"))
            return;
        if (!suit.isNew)
            deletedIds.Add(suit.id);
        allSuits.RemoveAt(selectSuitIndex);
        RebuildSuitLabels();
        RefreshNewSuitIdSuggestion();
        RebuildCandidates();
    }

    #endregion

    #region 保存逻辑

    /// <summary>
    /// 保存全部变更到 Excel(删除/新增/修改统一一个EPPlus会话, 数字列写数值类型)，并重新导出 JSON
    /// </summary>
    private void SaveData()
    {
        List<SuitRow> added = new List<SuitRow>();
        List<SuitRow> modified = new List<SuitRow>();
        foreach (var suit in allSuits)
        {
            if (suit.isNew)
                added.Add(suit);
            else if (suit.IsDirty)
                modified.Add(suit);
        }

        if (added.Count == 0 && modified.Count == 0 && deletedIds.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到套装变更。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认保存",
            $"变更：新增 {added.Count} 套，修改 {modified.Count} 套，删除 {deletedIds.Count} 套。\n确定写入 Excel 并重新导出 JSON 吗？", "保存", "取消"))
            return;

        try
        {
            using (ExcelPackage pack = new ExcelPackage(new FileInfo(excelPathSuit)))
            {
                ExcelWorksheet sheet = pack.Workbook.Worksheets[SheetSuit];
                var colMap = BuildColMap(sheet);

                // 1) 删除(按id定位行, 行号降序删防漂移)
                List<int> deleteRows = new List<int>();
                foreach (var deletedId in deletedIds)
                {
                    int row = FindRowById(sheet, deletedId);
                    if (row > 0)
                        deleteRows.Add(row);
                }
                deleteRows.Sort((a, b) => b.CompareTo(a));
                foreach (var row in deleteRows)
                    sheet.DeleteRow(row);

                // 2) 修改(按id定位行, 逐列覆写)
                foreach (var suit in modified)
                {
                    int row = FindRowById(sheet, suit.id);
                    if (row > 0)
                        WriteSuitRow(sheet, colMap, row, suit);
                }

                // 3) 新增(追加到末尾)
                foreach (var suit in added)
                {
                    int row = sheet.Dimension.End.Row + 1;
                    WriteSuitRow(sheet, colMap, row, suit);
                }

                pack.Save();
            }

            // 4) 重新导出该表的运行时 JSON(依赖 EquipSuitInfoBean 已生成)
            ExcelUtil.ExcelToJsonItem(excelPathSuit);

            AssetDatabase.Refresh();

            // 5) 刷新快照与删除记录，标记为已保存
            deletedIds.Clear();
            foreach (var suit in allSuits)
                suit.CommitSnapshot();
            RebuildSuitLabels();

            EditorUtility.DisplayDialog("完成", "已保存套装变更到 Excel，并重新导出了 EquipSuitInfo.txt。", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}\n(请确认 Excel 文件未被占用)", "确定");
            LogUtil.LogError($"装备套装保存失败: {e}");
        }
    }

    /// <summary>
    /// 按id定位数据行(未找到返回-1)
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
    /// 把套装写入指定行(数字列写数值, 空槽写0, 备注写文本)
    /// </summary>
    private void WriteSuitRow(ExcelWorksheet sheet, Dictionary<string, int> colMap, int row, SuitRow suit)
    {
        sheet.Cells[row, colMap["id"]].Value = suit.id;
        sheet.Cells[row, colMap["creature_model_id"]].Value = suit.modelId;
        foreach (var slot in SlotDefs)
        {
            suit.slots.TryGetValue(slot.itemType, out long itemId);
            sheet.Cells[row, colMap[slot.column]].Value = itemId;
        }
        sheet.Cells[row, colMap["remark"]].Value = suit.remark ?? "";
    }

    /// <summary>
    /// 打开装备套装 Excel 表格
    /// </summary>
    private void OpenSuitExcel()
    {
        if (File.Exists(excelPathSuit))
            System.Diagnostics.Process.Start(excelPathSuit);
        else
            EditorUtility.DisplayDialog("错误", $"装备套装 Excel 文件不存在:\n{excelPathSuit}", "确定");
    }

    #endregion
}
