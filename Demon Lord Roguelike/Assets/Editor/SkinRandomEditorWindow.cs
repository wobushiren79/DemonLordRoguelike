using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 皮肤/装备/套装随机池配置窗口
/// 用于可视化编辑 excel_creature_random_info[生物随机信息] 表的随机池：
/// 皮肤池(random_type=0)：编辑 skin_random_data 列（随机皮肤池）。顶部下拉选择随机池（并展示其具体内容），
///   左侧列出已加入随机的皮肤部件，右侧列出未加入的部件，点选即可加入/移除；
/// 装备池(random_type=1)：编辑 equip_random_data 列（随机装备池，池内为 ItemsInfo 的道具ID），
///   右侧候选取自 excel_items_info[道具信息]，按池内已有装备的物种(creature_model_id)自动过滤；
/// 套装池(random_type=2)：编辑 equip_random_data 列（套装池，池内为 EquipSuitInfo 的套装ID，多套等概率整套随机），
///   右侧候选取自 excel_equip_suit_info[装备套装]，按池内已有套装的物种(creature_model_id)自动过滤，
///   套装内容本身由「游戏/装备套装配置」窗口(EquipSuitEditorWindow)编辑。
/// 保存时把 ID 集合压缩为区间串（如 1030001-1030003,...) 写回 Excel 并同步导出 JSON。
/// 部件全集取自 excel_creature_model_info[生物模型详情信息]，物种名取自 excel_creature_model[生物模型信息]。
/// 规则：右侧未加入列表按池内已有部件的物种自动过滤（如选了人类池只列人类皮肤）；
/// 装备/武器类部位（part_type>=50，及装备驱动的身体部位如鼻环 NoseRing=9）不展示（皮肤由装备道具驱动）；每行按
/// {mark_name}_Atlas_{res_name(/→_)} 约定加载 Textures/Skins 下的皮肤图标（与游戏内 UITestNpcCreate 同约定）；
/// 装备图标按 icon_res 从 AtlasForItems 图集取 sprite。
/// </summary>
public class SkinRandomEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/皮肤随机池配置
    /// </summary>
    [MenuItem("游戏/皮肤随机池配置")]
    private static void CreateWindow()
    {
        var window = GetWindow<SkinRandomEditorWindow>();
        window.titleContent = new GUIContent("皮肤/装备/套装随机池配置");
        window.minSize = new Vector2(960, 560);
        window.Show();
    }

    #endregion

    #region 内部数据结构

    /// <summary>单个随机池编辑数据</summary>
    private class PoolRow
    {
        public long id;
        public int randomType;          //随机类型(0=皮肤池 1=装备池 2=套装池, CreatureRandomTypeEnum)
        public string remark;
        public HashSet<long> skinSet = new HashSet<long>();  //当前编辑中的皮肤部件ID集合(皮肤池)
        public HashSet<long> equipSet = new HashSet<long>(); //当前编辑中的ID集合(装备池=ItemsInfo道具ID, 套装池=EquipSuitInfo套装ID)
        public string originalData;                          //原始 skin_random_data 的规范化串(用于对比变更)
        public string originalEquipData;                     //原始 equip_random_data 的规范化串(用于对比变更)

        /// <summary>是否装备池</summary>
        public bool IsEquipPool => randomType == (int)CreatureRandomTypeEnum.Equip;

        /// <summary>是否套装池(池内为EquipSuitInfo套装ID, 与装备池同存equip_random_data列)</summary>
        public bool IsSuitPool => randomType == (int)CreatureRandomTypeEnum.Suit;
    }

    /// <summary>单个皮肤部件(生物模型详情)数据</summary>
    private class ModelInfoItem
    {
        public long id;
        public int modelId;      //所属物种(模组)ID
        public int partType;     //部位类型(CreatureSkinTypeEnum)
        public string resName;
        public string remark;
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

    /// <summary>单个装备套装(EquipSuitInfo)数据</summary>
    private class SuitInfoItem
    {
        public long id;
        public int modelId;      //种族模组ID(creature_model_id, 0=通用)
        public int itemCount;    //套内道具件数(非空槽位数)
        public string remark;
    }

    #endregion

    #region 常量

    /// <summary>随机皮肤池工作表名</summary>
    private const string SheetRandom = "CreatureRandomInfo";

    /// <summary>生物模型详情工作表名</summary>
    private const string SheetModelInfo = "CreatureModelInfo";

    /// <summary>生物模型(物种)工作表名</summary>
    private const string SheetModel = "CreatureModel";

    /// <summary>道具信息工作表名</summary>
    private const string SheetItems = "ItemsInfo";

    /// <summary>装备套装工作表名</summary>
    private const string SheetSuit = "EquipSuitInfo";

    /// <summary>套装表槽位列名(统计件数用)</summary>
    private static readonly string[] SuitSlotHeaders = { "hat", "clothes", "pants", "shoe", "nose_ring", "finger_ring", "weapon" };

    /// <summary>装备图标图集路径(Items图集)</summary>
    private const string ItemsAtlasPath = "Assets/LoadResources/Textures/SpriteAtlas/AtlasForItems.spriteatlas";

    /// <summary>列表行高(带图标)</summary>
    private const float RowHeight = 40f;

    /// <summary>皮肤图标所在目录(GameDataEditor.SpineAllSkinInit 抽取产物)</summary>
    private const string SkinIconFolder = "Assets/LoadResources/Textures/Skins";

    /// <summary>可进入装备池的道具类型(帽/衣/裤/鞋/鼻环/戒指/武器)</summary>
    private static readonly HashSet<int> EquipItemTypes = new HashSet<int>
    {
        (int)ItemTypeEnum.Hat,       //1 帽子
        (int)ItemTypeEnum.Clothes,   //2 衣服
        (int)ItemTypeEnum.Pants,     //3 裤子
        (int)ItemTypeEnum.Shoe,      //4 鞋子
        (int)ItemTypeEnum.NoseRing,  //5 鼻环
        (int)ItemTypeEnum.FingerRing,//6 戒指
        (int)ItemTypeEnum.Weapon,    //10 武器
    };

    /// <summary>
    /// 排除展示的部位统一列表(装备/武器类：皮肤由装备道具驱动,不进入随机池编辑展示)。
    /// 含穿戴(帽子/衣服/裤子/鞋/腰带/手套)、武器线/武器左右手/双手武器，及装备驱动的身体部位鼻环。
    /// 新增装备类部位时直接往此列表添加。
    /// </summary>
    private static readonly HashSet<int> ExcludePartTypes = new HashSet<int>
    {
        (int)CreatureSkinTypeEnum.NoseRing,    //9  鼻环(虽在身体段,但由鼻环装备经 creature_model_info_id 对接驱动)
        (int)CreatureSkinTypeEnum.Hat,         //50 帽子
        (int)CreatureSkinTypeEnum.Clothes,     //51 衣服
        (int)CreatureSkinTypeEnum.Pants,       //52 裤子
        (int)CreatureSkinTypeEnum.Shoe,        //53 鞋子
        (int)CreatureSkinTypeEnum.Belt,        //54 腰带
        (int)CreatureSkinTypeEnum.Gloves,      //55 手套
        (int)CreatureSkinTypeEnum.Weapon_Line, //80 武器线
        (int)CreatureSkinTypeEnum.Weapon_L,    //90 武器左手
        (int)CreatureSkinTypeEnum.Weapon_R,    //91 武器右手
        92,                                    //92 双手武器(枚举未定义,部件表内存在)
    };

    #endregion

    #region 成员变量

    /// <summary>随机皮肤池 Excel 路径(注意文件名含空格)</summary>
    private string excelPathRandom;

    /// <summary>生物模型详情 Excel 路径(注意文件名含空格)</summary>
    private string excelPathModelInfo;

    /// <summary>生物模型(物种) Excel 路径</summary>
    private string excelPathModel;

    /// <summary>道具信息 Excel 路径</summary>
    private string excelPathItems;

    /// <summary>装备套装 Excel 路径</summary>
    private string excelPathSuit;

    /// <summary>全部随机池</summary>
    private readonly List<PoolRow> allPools = new List<PoolRow>();

    /// <summary>池下拉项标签(与 allPools 对齐)</summary>
    private string[] poolLabels;

    /// <summary>当前选中池索引</summary>
    private int selectPoolIndex = 0;

    /// <summary>全部皮肤部件(生物模型详情)</summary>
    private readonly List<ModelInfoItem> allModelInfos = new List<ModelInfoItem>();

    /// <summary>部件ID -> 部件数据</summary>
    private readonly Dictionary<long, ModelInfoItem> modelInfoMap = new Dictionary<long, ModelInfoItem>();

    /// <summary>全部装备道具(ItemsInfo中装备类型道具)</summary>
    private readonly List<ItemInfoItem> allItemInfos = new List<ItemInfoItem>();

    /// <summary>道具ID -> 装备道具数据</summary>
    private readonly Dictionary<long, ItemInfoItem> itemInfoMap = new Dictionary<long, ItemInfoItem>();

    /// <summary>全部装备套装(EquipSuitInfo)</summary>
    private readonly List<SuitInfoItem> allSuitInfos = new List<SuitInfoItem>();

    /// <summary>套装ID -> 套装数据</summary>
    private readonly Dictionary<long, SuitInfoItem> suitInfoMap = new Dictionary<long, SuitInfoItem>();

    /// <summary>装备图标图集(懒加载)</summary>
    private SpriteAtlas itemsAtlas;

    /// <summary>装备图标sprite缓存(key=icon_res)</summary>
    private readonly Dictionary<string, Sprite> itemSpriteCache = new Dictionary<string, Sprite>();

    /// <summary>物种(模组)ID -> 物种名</summary>
    private readonly Dictionary<long, string> modelNameMap = new Dictionary<long, string>();

    /// <summary>物种(模组)ID -> mark_name(拼皮肤图标名用)</summary>
    private readonly Dictionary<long, string> modelMarkNameMap = new Dictionary<long, string>();

    /// <summary>皮肤图标缓存(懒加载，key=图标名)</summary>
    private readonly Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

    /// <summary>已加入池的展示列表(按部位、ID排序；含无效ID的悬空项)</summary>
    private readonly List<long> inPoolShowList = new List<long>();

    /// <summary>未加入池的展示列表(套用筛选后)</summary>
    private readonly List<ModelInfoItem> notInPoolShowList = new List<ModelInfoItem>();

    /// <summary>装备池: 已加入池的展示列表(按道具类型、ID排序；含无效ID的悬空项)</summary>
    private readonly List<long> inPoolEquipShowList = new List<long>();

    /// <summary>装备池: 未加入池的展示列表(套用筛选后)</summary>
    private readonly List<ItemInfoItem> notInPoolEquipShowList = new List<ItemInfoItem>();

    /// <summary>套装池: 已加入池的展示列表(按物种、ID排序；含无效ID的悬空项)</summary>
    private readonly List<long> inPoolSuitShowList = new List<long>();

    /// <summary>套装池: 未加入池的展示列表(套用筛选后)</summary>
    private readonly List<SuitInfoItem> notInPoolSuitShowList = new List<SuitInfoItem>();

    /// <summary>部位下拉当前选中索引</summary>
    private int filterPartIndex = 0;

    /// <summary>部位下拉标签</summary>
    private string[] partLabels;

    /// <summary>部位下拉对应的 part_type 值(与 partLabels 对齐；0=全部)</summary>
    private readonly List<int> partValues = new List<int>();

    /// <summary>装备池: 道具类型下拉当前选中索引</summary>
    private int filterItemTypeIndex = 0;

    /// <summary>装备池: 道具类型下拉标签</summary>
    private string[] itemTypeLabels;

    /// <summary>装备池: 道具类型下拉对应的 item_type 值(与 itemTypeLabels 对齐；0=全部)</summary>
    private readonly List<int> itemTypeValues = new List<int>();

    /// <summary>搜索关键字(匹配 id/res_name/remark)</summary>
    private string searchKey = "";

    /// <summary>左列表滚动位置</summary>
    private Vector2 scrollPosIn = Vector2.zero;

    /// <summary>右列表滚动位置</summary>
    private Vector2 scrollPosOut = Vector2.zero;

    /// <summary>数据已加载标记</summary>
    private bool dataLoaded = false;

    /// <summary>样式初始化标记</summary>
    private bool stylesInitialized = false;

    private GUIStyle sectionHeaderStyle;
    private GUIStyle partHeaderStyle;
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
        excelPathRandom = Application.dataPath + "/Data/Excel/excel_creature_random_info[生物随机信息] .xlsx";
        excelPathModelInfo = Application.dataPath + "/Data/Excel/excel_creature_model_info[生物模型详情信息] .xlsx";
        excelPathModel = Application.dataPath + "/Data/Excel/excel_creature_model[生物模型信息].xlsx";
        excelPathItems = Application.dataPath + "/Data/Excel/excel_items_info[道具信息].xlsx";
        excelPathSuit = Application.dataPath + "/Data/Excel/excel_equip_suit_info[装备套装].xlsx";
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

        DrawPoolContent();
        EditorGUILayout.Space(4);
        DrawTwoColumns();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化自定义 UI 样式
    /// </summary>
    private void InitializeStyles()
    {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        partHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11 };
        partHeaderStyle.normal.textColor = new Color(0.55f, 0.75f, 1f);
        rowLabelStyle = new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
        idLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        warnLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        warnLabelStyle.normal.textColor = new Color(1f, 0.45f, 0.4f);
        stylesInitialized = true;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 加载全部数据(随机池 + 部件全集 + 物种名)
    /// </summary>
    private void LoadAllData()
    {
        dataLoaded = false;
        LoadModelNames();
        LoadModelInfos();
        LoadItemInfos();
        LoadSuitInfos();
        LoadPools();
        BuildPartFilter();
        BuildItemTypeFilter();
        RebuildShowLists();
        dataLoaded = allPools.Count > 0 && allModelInfos.Count > 0;
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
    /// 加载物种名映射(生物模型表：模组ID -> 物种名，优先 remark 回退 mark_name)
    /// </summary>
    private void LoadModelNames()
    {
        modelNameMap.Clear();
        modelMarkNameMap.Clear();
        if (!File.Exists(excelPathModel))
        {
            LogUtil.LogError($"生物模型 Excel 不存在: {excelPathModel}");
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
                modelMarkNameMap[id] = markName ?? "";
            }
        });
    }

    /// <summary>
    /// 加载皮肤部件全集(生物模型详情表)
    /// </summary>
    private void LoadModelInfos()
    {
        allModelInfos.Clear();
        modelInfoMap.Clear();
        if (!File.Exists(excelPathModelInfo))
        {
            LogUtil.LogError($"生物模型详情 Excel 不存在: {excelPathModelInfo}");
            return;
        }
        ExcelUtil.GetExcelPackage(new FileInfo(excelPathModelInfo), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetModelInfo];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetModelInfo}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;
                int.TryParse(GetCell(sheet, colMap, "model_id", row), out int modelId);
                int.TryParse(GetCell(sheet, colMap, "part_type", row), out int partType);
                var item = new ModelInfoItem
                {
                    id = id,
                    modelId = modelId,
                    partType = partType,
                    resName = GetCell(sheet, colMap, "res_name", row),
                    remark = GetCell(sheet, colMap, "remark", row)
                };
                allModelInfos.Add(item);
                modelInfoMap[id] = item;
            }
        });
        allModelInfos.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 加载随机皮肤池(随机信息表)
    /// </summary>
    private void LoadPools()
    {
        allPools.Clear();
        if (!File.Exists(excelPathRandom))
        {
            LogUtil.LogError($"随机皮肤池 Excel 不存在: {excelPathRandom}");
            return;
        }
        ExcelUtil.GetExcelPackage(new FileInfo(excelPathRandom), (ep) =>
        {
            ExcelWorksheet sheet = ep.Workbook.Worksheets[SheetRandom];
            if (sheet == null)
            {
                LogUtil.LogError($"未找到工作表: {SheetRandom}");
                return;
            }
            var colMap = BuildColMap(sheet);
            for (int row = 4; row <= sheet.Dimension.End.Row; row++)
            {
                string idText = GetCell(sheet, colMap, "id", row);
                if (string.IsNullOrEmpty(idText) || !long.TryParse(idText, out long id))
                    continue;
                string skinData = GetCell(sheet, colMap, "skin_random_data", row);
                string equipData = GetCell(sheet, colMap, "equip_random_data", row);
                int.TryParse(GetCell(sheet, colMap, "random_type", row), out int randomType);
                var pool = new PoolRow
                {
                    id = id,
                    randomType = randomType,
                    remark = GetCell(sheet, colMap, "remark", row),
                    skinSet = ParseSkinSet(skinData),
                    equipSet = ParseSkinSet(equipData)
                };
                pool.originalData = CompressSkinSet(pool.skinSet);
                pool.originalEquipData = CompressSkinSet(pool.equipSet);
                allPools.Add(pool);
            }
        });
        allPools.Sort((a, b) => a.id.CompareTo(b.id));

        // 重建下拉标签(带池类型前缀)
        List<string> labels = new List<string>();
        foreach (var pool in allPools)
        {
            string typeTag = pool.IsEquipPool ? "[装备]" : (pool.IsSuitPool ? "[套装]" : "[皮肤]");
            labels.Add(string.IsNullOrEmpty(pool.remark) ? $"{typeTag}{pool.id}" : $"{typeTag}{pool.id} | {pool.remark}");
        }
        poolLabels = labels.ToArray();
        if (selectPoolIndex >= allPools.Count)
            selectPoolIndex = 0;
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
                string modelIdText = GetCell(sheet, colMap, "creature_model_id", row);
                int.TryParse(modelIdText, out int modelId);
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
    /// 加载装备套装全集(EquipSuitInfo表; 套装内容由「装备套装配置」窗口编辑, 此处仅作套装池候选)
    /// </summary>
    private void LoadSuitInfos()
    {
        allSuitInfos.Clear();
        suitInfoMap.Clear();
        if (!File.Exists(excelPathSuit))
        {
            //套装表缺失不阻断皮肤/装备池编辑(仅套装池无候选)
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
                int.TryParse(GetCell(sheet, colMap, "creature_model_id", row), out int modelId);
                //统计套内非空槽位件数
                int itemCount = 0;
                foreach (var slotHeader in SuitSlotHeaders)
                {
                    if (long.TryParse(GetCell(sheet, colMap, slotHeader, row), out long slotItemId) && slotItemId > 0)
                        itemCount++;
                }
                var item = new SuitInfoItem
                {
                    id = id,
                    modelId = modelId,
                    itemCount = itemCount,
                    remark = GetCell(sheet, colMap, "remark", row)
                };
                allSuitInfos.Add(item);
                suitInfoMap[id] = item;
            }
        });
        allSuitInfos.Sort((a, b) => a.id.CompareTo(b.id));
    }

    /// <summary>
    /// 构建装备池的道具类型筛选下拉项(取装备全集中出现过的道具类型)
    /// </summary>
    private void BuildItemTypeFilter()
    {
        itemTypeValues.Clear();
        List<string> labels = new List<string> { "全部类型" };
        itemTypeValues.Add(0);

        HashSet<int> typeSet = new HashSet<int>();
        foreach (var item in allItemInfos)
        {
            typeSet.Add(item.itemType);
        }
        List<int> sorted = new List<int>(typeSet);
        sorted.Sort();
        foreach (var t in sorted)
        {
            labels.Add($"{GetItemTypeName(t)}({t})");
            itemTypeValues.Add(t);
        }
        itemTypeLabels = labels.ToArray();
        if (filterItemTypeIndex >= itemTypeLabels.Length)
            filterItemTypeIndex = 0;
    }

    /// <summary>
    /// 构建部位筛选下拉项(取部件全集中出现过的身体部位；装备/武器类不列出)
    /// </summary>
    private void BuildPartFilter()
    {
        partValues.Clear();
        List<string> labels = new List<string> { "全部部位" };
        partValues.Add(0);

        HashSet<int> partSet = new HashSet<int>();
        foreach (var item in allModelInfos)
        {
            if (IsEquipPart(item.partType))
                continue;
            partSet.Add(item.partType);
        }
        List<int> sorted = new List<int>(partSet);
        sorted.Sort();
        foreach (var p in sorted)
        {
            labels.Add($"{GetPartTypeName(p)}({p})");
            partValues.Add(p);
        }
        partLabels = labels.ToArray();
        if (filterPartIndex >= partLabels.Length)
            filterPartIndex = 0;
    }

    #endregion

    #region 数据解析与压缩

    /// <summary>
    /// 解析 skin_random_data 为部件ID集合(与运行时 SplitForListLong(',','-') 同规则：逗号分段、'-'区间展开)
    /// </summary>
    private HashSet<long> ParseSkinSet(string data)
    {
        HashSet<long> set = new HashSet<long>();
        if (string.IsNullOrEmpty(data))
            return set;
        foreach (var segment in data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = segment.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && long.TryParse(parts[0], out long start)
                && long.TryParse(parts[1], out long end))
            {
                for (long n = start; n <= end; n++)
                    set.Add(n);
            }
            else if (long.TryParse(segment, out long single))
            {
                set.Add(single);
            }
        }
        return set;
    }

    /// <summary>
    /// 把部件ID集合压缩为规范区间串(升序，连续段压缩为 a-b，逗号连接)——与表内原有书写格式一致
    /// </summary>
    private string CompressSkinSet(HashSet<long> set)
    {
        if (set == null || set.Count == 0)
            return "";
        List<long> ids = new List<long>(set);
        ids.Sort();
        StringBuilder sb = new StringBuilder();
        int i = 0;
        bool first = true;
        while (i < ids.Count)
        {
            long start = ids[i];
            long end = start;
            while (i + 1 < ids.Count && ids[i + 1] == end + 1)
            {
                i++;
                end = ids[i];
            }
            if (!first)
                sb.Append(',');
            sb.Append(start == end ? $"{start}" : $"{start}-{end}");
            first = false;
            i++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 当前选中池
    /// </summary>
    private PoolRow CurrentPool
    {
        get
        {
            if (selectPoolIndex >= 0 && selectPoolIndex < allPools.Count)
                return allPools[selectPoolIndex];
            return null;
        }
    }

    /// <summary>
    /// 部位类型中文名(优先取 CreatureSkinTypeEnum 枚举名)
    /// </summary>
    private string GetPartTypeName(int partType)
    {
        if (Enum.IsDefined(typeof(CreatureSkinTypeEnum), partType))
            return ((CreatureSkinTypeEnum)partType).ToString();
        return $"类型{partType}";
    }

    /// <summary>
    /// 是否排除展示的部位(装备/武器类,统一由 ExcludePartTypes 列表判定；此类皮肤由装备道具驱动)
    /// </summary>
    private bool IsEquipPart(int partType)
    {
        return ExcludePartTypes.Contains(partType);
    }

    /// <summary>
    /// 池内已有部件推导出的物种集合(用于右侧列表自动限定同物种；空池返回空集合=不限定)
    /// </summary>
    private HashSet<int> GetPoolSpecies(PoolRow pool)
    {
        HashSet<int> species = new HashSet<int>();
        if (pool == null)
            return species;
        foreach (var id in pool.skinSet)
        {
            if (modelInfoMap.TryGetValue(id, out var info))
                species.Add(info.modelId);
        }
        return species;
    }

    /// <summary>
    /// 装备池内已有装备推导出的物种集合(creature_model_id, 跳过0=通用装备；空池返回空集合=不限定)
    /// </summary>
    private HashSet<int> GetPoolEquipSpecies(PoolRow pool)
    {
        HashSet<int> species = new HashSet<int>();
        if (pool == null)
            return species;
        foreach (var id in pool.equipSet)
        {
            if (itemInfoMap.TryGetValue(id, out var info) && info.modelId != 0)
                species.Add(info.modelId);
        }
        return species;
    }

    /// <summary>
    /// 套装池内已有套装推导出的物种集合(套装的creature_model_id, 跳过0=通用套装；空池返回空集合=不限定)
    /// </summary>
    private HashSet<int> GetPoolSuitSpecies(PoolRow pool)
    {
        HashSet<int> species = new HashSet<int>();
        if (pool == null)
            return species;
        foreach (var id in pool.equipSet)
        {
            if (suitInfoMap.TryGetValue(id, out var info) && info.modelId != 0)
                species.Add(info.modelId);
        }
        return species;
    }

    /// <summary>
    /// 池物种的展示文本(如 "骷髅(2)"；多物种拼接；空池为 "空池-不限定")
    /// </summary>
    private string GetPoolSpeciesLabel(PoolRow pool)
    {
        //装备池按装备道具的种族模组推导物种, 套装池按套装的种族模组推导
        var species = pool != null && pool.IsEquipPool ? GetPoolEquipSpecies(pool)
            : (pool != null && pool.IsSuitPool ? GetPoolSuitSpecies(pool) : GetPoolSpecies(pool));
        if (species.Count == 0)
            return "空池-不限定";
        List<int> sorted = new List<int>(species);
        sorted.Sort();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i > 0) sb.Append('、');
            sb.Append($"{GetModelName(sorted[i])}({sorted[i]})");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 道具类型中文名
    /// </summary>
    private string GetItemTypeName(int itemType)
    {
        switch ((ItemTypeEnum)itemType)
        {
            case ItemTypeEnum.Hat: return "帽子";
            case ItemTypeEnum.Clothes: return "衣服";
            case ItemTypeEnum.Pants: return "裤子";
            case ItemTypeEnum.Shoe: return "鞋子";
            case ItemTypeEnum.NoseRing: return "鼻环";
            case ItemTypeEnum.FingerRing: return "戒指";
            case ItemTypeEnum.Weapon: return "武器";
            default: return $"类型{itemType}";
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
    /// 池是否有未保存变更(皮肤池比对皮肤集, 装备池/套装池比对equip集)
    /// </summary>
    private bool IsPoolDirty(PoolRow pool)
    {
        if (pool == null)
            return false;
        if (pool.IsEquipPool || pool.IsSuitPool)
            return CompressSkinSet(pool.equipSet) != pool.originalEquipData;
        return CompressSkinSet(pool.skinSet) != pool.originalData;
    }

    /// <summary>
    /// 是否存在任意未保存变更
    /// </summary>
    private bool HasAnyDirty()
    {
        foreach (var pool in allPools)
        {
            if (IsPoolDirty(pool))
                return true;
        }
        return false;
    }

    #endregion

    #region 皮肤图标懒加载

    /// <summary>
    /// 获取皮肤部件图标贴图(懒加载并缓存；命名约定与游戏内一致：{mark_name}_Atlas_{res_name的/换成_}，
    /// 图标由 GameDataEditor.SpineAllSkinInit 从 Spine 抽取到 Textures/Skins；找不到时回退全局按名搜索)
    /// </summary>
    private Texture2D GetSkinIconTexture(ModelInfoItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.resName))
            return null;
        if (!modelMarkNameMap.TryGetValue(item.modelId, out string markName) || string.IsNullOrEmpty(markName))
            return null;
        string iconName = $"{markName}_Atlas_{item.resName.Replace("/", "_")}";

        if (iconCache.TryGetValue(iconName, out Texture2D cached))
            return cached;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SkinIconFolder}/{iconName}.png");
        if (tex == null)
        {
            // 回退：全局按名搜索
            string[] guids = AssetDatabase.FindAssets($"{iconName} t:Texture2D");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == iconName)
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    break;
                }
            }
        }
        iconCache[iconName] = tex;
        return tex;
    }

    /// <summary>
    /// 绘制皮肤图标(无图标时画灰色占位块)
    /// </summary>
    private void DrawSkinIcon(ModelInfoItem item)
    {
        Rect iconRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
        Texture2D tex = GetSkinIconTexture(item);
        if (tex != null)
            GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
        else
            EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.3f, 0.3f, 0.3f));
    }

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

    #region 列表重建

    /// <summary>
    /// 重建左右展示列表(当前池变更或筛选变更后调用)
    /// </summary>
    private void RebuildShowLists()
    {
        inPoolShowList.Clear();
        notInPoolShowList.Clear();
        inPoolEquipShowList.Clear();
        notInPoolEquipShowList.Clear();
        inPoolSuitShowList.Clear();
        notInPoolSuitShowList.Clear();
        PoolRow pool = CurrentPool;
        if (pool == null)
            return;
        //装备池走装备展示列表
        if (pool.IsEquipPool)
        {
            RebuildEquipShowLists(pool);
            return;
        }
        //套装池走套装展示列表
        if (pool.IsSuitPool)
        {
            RebuildSuitShowLists(pool);
            return;
        }

        // 左列表：池内ID，隐藏装备/武器类部件(仅不展示，数据仍保留在池内)，按(部位, ID)排序；无效ID(模型表不存在)排最后
        foreach (var id in pool.skinSet)
        {
            if (modelInfoMap.TryGetValue(id, out var info) && IsEquipPart(info.partType))
                continue;
            inPoolShowList.Add(id);
        }
        inPoolShowList.Sort((a, b) =>
        {
            bool hasA = modelInfoMap.TryGetValue(a, out var infoA);
            bool hasB = modelInfoMap.TryGetValue(b, out var infoB);
            if (hasA && hasB)
            {
                int c = infoA.partType.CompareTo(infoB.partType);
                if (c != 0) return c;
                return a.CompareTo(b);
            }
            if (hasA != hasB)
                return hasA ? -1 : 1;
            return a.CompareTo(b);
        });

        // 右列表：未加入池的部件——仅身体部位(排除装备/武器类)、仅池同物种(池推导不出物种时不限定)，再套用部位/搜索筛选
        int partFilter = (filterPartIndex >= 0 && filterPartIndex < partValues.Count) ? partValues[filterPartIndex] : 0;
        HashSet<int> poolSpecies = GetPoolSpecies(pool);
        string key = string.IsNullOrEmpty(searchKey) ? null : searchKey.Trim();

        foreach (var item in allModelInfos)
        {
            if (pool.skinSet.Contains(item.id))
                continue;
            if (IsEquipPart(item.partType))
                continue;
            if (poolSpecies.Count > 0 && !poolSpecies.Contains(item.modelId))
                continue;
            if (partFilter != 0 && item.partType != partFilter)
                continue;
            if (key != null
                && !item.id.ToString().Contains(key)
                && (string.IsNullOrEmpty(item.resName) || item.resName.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0)
                && (string.IsNullOrEmpty(item.remark) || item.remark.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            notInPoolShowList.Add(item);
        }
    }

    /// <summary>
    /// 重建装备池的左右展示列表(左=已加入的装备, 右=未加入的装备; 按池物种自动过滤)
    /// </summary>
    private void RebuildEquipShowLists(PoolRow pool)
    {
        // 左列表：池内装备ID，按(道具类型, ID)排序；无效ID(道具表不存在)排最后
        foreach (var id in pool.equipSet)
        {
            inPoolEquipShowList.Add(id);
        }
        inPoolEquipShowList.Sort((a, b) =>
        {
            bool hasA = itemInfoMap.TryGetValue(a, out var infoA);
            bool hasB = itemInfoMap.TryGetValue(b, out var infoB);
            if (hasA && hasB)
            {
                int c = infoA.itemType.CompareTo(infoB.itemType);
                if (c != 0) return c;
                return a.CompareTo(b);
            }
            if (hasA != hasB)
                return hasA ? -1 : 1;
            return a.CompareTo(b);
        });

        // 右列表：未加入池的装备——仅池同物种(creature_model_id, 通用装备0总是可见)，再套用道具类型/搜索筛选
        int typeFilter = (filterItemTypeIndex >= 0 && filterItemTypeIndex < itemTypeValues.Count) ? itemTypeValues[filterItemTypeIndex] : 0;
        HashSet<int> poolSpecies = GetPoolEquipSpecies(pool);
        string key = string.IsNullOrEmpty(searchKey) ? null : searchKey.Trim();

        foreach (var item in allItemInfos)
        {
            if (pool.equipSet.Contains(item.id))
                continue;
            if (item.modelId != 0 && poolSpecies.Count > 0 && !poolSpecies.Contains(item.modelId))
                continue;
            if (typeFilter != 0 && item.itemType != typeFilter)
                continue;
            if (key != null
                && !item.id.ToString().Contains(key)
                && (string.IsNullOrEmpty(item.iconRes) || item.iconRes.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0)
                && (string.IsNullOrEmpty(item.remark) || item.remark.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            notInPoolEquipShowList.Add(item);
        }
    }

    /// <summary>
    /// 重建套装池的左右展示列表(左=已加入的套装, 右=未加入的套装; 按池物种自动过滤, 通用套装0总是可见)
    /// </summary>
    private void RebuildSuitShowLists(PoolRow pool)
    {
        // 左列表：池内套装ID，按(物种, ID)排序；无效ID(套装表不存在)排最后
        foreach (var id in pool.equipSet)
        {
            inPoolSuitShowList.Add(id);
        }
        inPoolSuitShowList.Sort((a, b) =>
        {
            bool hasA = suitInfoMap.TryGetValue(a, out var infoA);
            bool hasB = suitInfoMap.TryGetValue(b, out var infoB);
            if (hasA && hasB)
            {
                int c = infoA.modelId.CompareTo(infoB.modelId);
                if (c != 0) return c;
                return a.CompareTo(b);
            }
            if (hasA != hasB)
                return hasA ? -1 : 1;
            return a.CompareTo(b);
        });

        // 右列表：未加入池的套装——仅池同物种(creature_model_id, 通用套装0总是可见)，再套用搜索筛选
        HashSet<int> poolSpecies = GetPoolSuitSpecies(pool);
        string key = string.IsNullOrEmpty(searchKey) ? null : searchKey.Trim();

        foreach (var item in allSuitInfos)
        {
            if (pool.equipSet.Contains(item.id))
                continue;
            if (item.modelId != 0 && poolSpecies.Count > 0 && !poolSpecies.Contains(item.modelId))
                continue;
            if (key != null
                && !item.id.ToString().Contains(key)
                && (string.IsNullOrEmpty(item.remark) || item.remark.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0))
                continue;
            notInPoolSuitShowList.Add(item);
        }
    }

    #endregion

    #region UI 绘制 - 工具栏与池内容

    /// <summary>
    /// 绘制顶部工具栏(随机池下拉 + 操作按钮)
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"皮肤/装备随机池配置   共 {allPools.Count} 个随机池 | 部件全集 {allModelInfos.Count} 个 | 装备全集 {allItemInfos.Count} 件", sectionHeaderStyle);
        GUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("随机池:", GUILayout.Width(48));
        if (poolLabels != null && poolLabels.Length > 0)
        {
            int newIndex = EditorGUILayout.Popup(selectPoolIndex, poolLabels, GUILayout.Width(260));
            if (newIndex != selectPoolIndex)
            {
                selectPoolIndex = newIndex;
                RebuildShowLists();
            }
        }
        else
        {
            EditorGUILayout.LabelField("(无数据)", GUILayout.Width(260));
        }

        GUILayout.Space(10);
        if (GUILayout.Button("打开Excel", GUILayout.Width(80), GUILayout.Height(20)))
            OpenRandomExcel();

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

        EditorGUILayout.HelpBox("左=已加入随机(点「移除」移出池)；右=未加入(点「加入」进池)，按池内物种自动过滤。皮肤池([皮肤])编辑皮肤部件；装备池([装备])编辑装备道具(取自道具表)；套装池([套装])编辑套装组合(取自装备套装表, 套装内容由「游戏/装备套装配置」窗口编辑)。保存时自动压缩为区间串(如 1030001-1030003)写回 Excel 并同步导出 JSON。", MessageType.None);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制当前池的具体内容(压缩串 + 统计)
    /// </summary>
    private void DrawPoolContent()
    {
        PoolRow pool = CurrentPool;
        if (pool == null)
            return;

        EditorGUILayout.BeginVertical("box");
        string typeTag = pool.IsEquipPool ? "装备池" : (pool.IsSuitPool ? "套装池" : "皮肤池");
        EditorGUILayout.LabelField($"当前池: [{typeTag}] {pool.id} | {pool.remark}", EditorStyles.boldLabel);

        if (pool.IsSuitPool)
        {
            // 套装池统计：物种覆盖数 / 无效ID数
            HashSet<int> speciesSet = new HashSet<int>();
            int invalidSuitCount = 0;
            foreach (var id in pool.equipSet)
            {
                if (suitInfoMap.TryGetValue(id, out var info))
                    speciesSet.Add(info.modelId);
                else
                    invalidSuitCount++;
            }
            EditorGUILayout.LabelField($"套装总数: {pool.equipSet.Count}    覆盖物种: {speciesSet.Count}    {(invalidSuitCount > 0 ? $"无效ID: {invalidSuitCount}(套装表不存在)" : "")}",
                invalidSuitCount > 0 ? warnLabelStyle : idLabelStyle);

            // 当前编辑中的 equip_random_data 内容(只读展示)
            GUI.enabled = false;
            EditorGUILayout.TextArea(CompressSkinSet(pool.equipSet), GUILayout.MinHeight(34));
            GUI.enabled = true;
        }
        else if (pool.IsEquipPool)
        {
            // 装备池统计：道具类型覆盖数 / 无效ID数
            HashSet<int> typeSet = new HashSet<int>();
            int invalidCount = 0;
            foreach (var id in pool.equipSet)
            {
                if (itemInfoMap.TryGetValue(id, out var info))
                    typeSet.Add(info.itemType);
                else
                    invalidCount++;
            }
            EditorGUILayout.LabelField($"装备总数: {pool.equipSet.Count}    覆盖道具类型: {typeSet.Count}    {(invalidCount > 0 ? $"无效ID: {invalidCount}(道具表不存在)" : "")}",
                invalidCount > 0 ? warnLabelStyle : idLabelStyle);

            // 当前编辑中的 equip_random_data 内容(只读展示)
            GUI.enabled = false;
            EditorGUILayout.TextArea(CompressSkinSet(pool.equipSet), GUILayout.MinHeight(34));
            GUI.enabled = true;
        }
        else
        {
            // 统计：部位覆盖数 / 无效ID数
            HashSet<int> partSet = new HashSet<int>();
            int invalidCount = 0;
            foreach (var id in pool.skinSet)
            {
                if (modelInfoMap.TryGetValue(id, out var info))
                    partSet.Add(info.partType);
                else
                    invalidCount++;
            }
            EditorGUILayout.LabelField($"部件总数: {pool.skinSet.Count}    覆盖部位: {partSet.Count}    {(invalidCount > 0 ? $"无效ID: {invalidCount}(模型表不存在)" : "")}",
                invalidCount > 0 ? warnLabelStyle : idLabelStyle);

            // 当前编辑中的 skin_random_data 内容(只读展示)
            GUI.enabled = false;
            EditorGUILayout.TextArea(CompressSkinSet(pool.skinSet), GUILayout.MinHeight(34));
            GUI.enabled = true;
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - 双列表

    /// <summary>
    /// 绘制左右双列表(已加入 / 未加入)
    /// </summary>
    private void DrawTwoColumns()
    {
        PoolRow pool = CurrentPool;
        if (pool == null)
            return;
        //装备池走装备双列表
        if (pool.IsEquipPool)
        {
            DrawTwoColumnsForEquip(pool);
            return;
        }
        //套装池走套装双列表
        if (pool.IsSuitPool)
        {
            DrawTwoColumnsForSuit(pool);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // ---------------- 左：已加入随机 ----------------
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2f - 8));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"已加入随机 ({inPoolShowList.Count})", EditorStyles.boldLabel, GUILayout.Width(150));
        // 池内被隐藏的装备/武器类部件数量(数据保留，仅不展示)
        int hiddenEquipCount = pool.skinSet.Count - inPoolShowList.Count;
        if (hiddenEquipCount > 0)
            EditorGUILayout.LabelField($"(已隐藏装备/武器部件 {hiddenEquipCount} 个)", warnLabelStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("全部移除", EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(18)))
        {
            if (EditorUtility.DisplayDialog("确认", $"确定清空池 {pool.id} 的全部 {pool.skinSet.Count} 个部件吗？", "清空", "取消"))
            {
                pool.skinSet.Clear();
                RebuildShowLists();
            }
        }
        EditorGUILayout.EndHorizontal();

        scrollPosIn = EditorGUILayout.BeginScrollView(scrollPosIn);
        int lastPart = int.MinValue;
        int partCount = 0;
        foreach (var id in inPoolShowList)
        {
            bool valid = modelInfoMap.TryGetValue(id, out var info);
            int part = valid ? info.partType : int.MaxValue;
            if (part != lastPart)
            {
                // 部位分组头(统计该组数量)
                partCount = 0;
                foreach (var other in inPoolShowList)
                {
                    int otherPart = modelInfoMap.TryGetValue(other, out var o) ? o.partType : int.MaxValue;
                    if (otherPart == part)
                        partCount++;
                }
                string partName = part == int.MaxValue ? "无效ID" : $"{GetPartTypeName(part)}({part})";
                EditorGUILayout.LabelField($"── {partName} ×{partCount}", partHeaderStyle);
                lastPart = part;
            }
            DrawInPoolRow(pool, id, info, valid);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ---------------- 右：未加入随机 ----------------
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"未加入随机 ({notInPoolShowList.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入全部(筛选结果)", EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(18)))
        {
            foreach (var item in notInPoolShowList)
                pool.skinSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();

        // 筛选行(物种由池自动限定，不提供下拉)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"物种: {GetPoolSpeciesLabel(pool)}", rowLabelStyle, GUILayout.Width(130));
        EditorGUILayout.LabelField("部位:", GUILayout.Width(32));
        int newPartIndex = EditorGUILayout.Popup(filterPartIndex, partLabels ?? new[] { "全部部位" }, GUILayout.Width(110));
        if (newPartIndex != filterPartIndex)
        {
            filterPartIndex = newPartIndex;
            RebuildShowLists();
        }
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(32));
        string newSearch = EditorGUILayout.TextField(searchKey, GUILayout.Width(120));
        if (newSearch != searchKey)
        {
            searchKey = newSearch;
            RebuildShowLists();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        scrollPosOut = EditorGUILayout.BeginScrollView(scrollPosOut);
        foreach (var item in notInPoolShowList)
            DrawNotInPoolRow(pool, item);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制左列表行(已加入：部件信息 + 移除按钮)
    /// </summary>
    private void DrawInPoolRow(PoolRow pool, long id, ModelInfoItem info, bool valid)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        if (valid)
        {
            DrawSkinIcon(info);
            EditorGUILayout.LabelField($"{id}", idLabelStyle, GUILayout.Width(70));
            EditorGUILayout.LabelField(info.resName ?? "", rowLabelStyle, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField(info.remark ?? "", idLabelStyle, GUILayout.MinWidth(50));
        }
        else
        {
            EditorGUILayout.LabelField($"{id}", warnLabelStyle, GUILayout.Width(70));
            EditorGUILayout.LabelField("模型表不存在(悬空ID)", warnLabelStyle);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.skinSet.Remove(id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制右列表行(未加入：部件信息 + 加入按钮)
    /// </summary>
    private void DrawNotInPoolRow(PoolRow pool, ModelInfoItem item)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        DrawSkinIcon(item);
        EditorGUILayout.LabelField($"{item.id}", idLabelStyle, GUILayout.Width(70));
        EditorGUILayout.LabelField($"{GetPartTypeName(item.partType)}", rowLabelStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField(item.resName ?? "", rowLabelStyle, GUILayout.MinWidth(100));
        EditorGUILayout.LabelField(item.remark ?? "", idLabelStyle, GUILayout.MinWidth(50));

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.skinSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制装备池的左右双列表(已加入 / 未加入装备)
    /// </summary>
    private void DrawTwoColumnsForEquip(PoolRow pool)
    {
        EditorGUILayout.BeginHorizontal();

        // ---------------- 左：已加入随机 ----------------
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2f - 8));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"已加入随机 ({inPoolEquipShowList.Count})", EditorStyles.boldLabel, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("全部移除", EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(18)))
        {
            if (EditorUtility.DisplayDialog("确认", $"确定清空池 {pool.id} 的全部 {pool.equipSet.Count} 件装备吗？", "清空", "取消"))
            {
                pool.equipSet.Clear();
                RebuildShowLists();
            }
        }
        EditorGUILayout.EndHorizontal();

        scrollPosIn = EditorGUILayout.BeginScrollView(scrollPosIn);
        int lastType = int.MinValue;
        int typeCount = 0;
        foreach (var id in inPoolEquipShowList)
        {
            bool valid = itemInfoMap.TryGetValue(id, out var info);
            int itemType = valid ? info.itemType : int.MaxValue;
            if (itemType != lastType)
            {
                // 道具类型分组头(统计该组数量)
                typeCount = 0;
                foreach (var other in inPoolEquipShowList)
                {
                    int otherType = itemInfoMap.TryGetValue(other, out var o) ? o.itemType : int.MaxValue;
                    if (otherType == itemType)
                        typeCount++;
                }
                string typeName = itemType == int.MaxValue ? "无效ID" : $"{GetItemTypeName(itemType)}({itemType})";
                EditorGUILayout.LabelField($"── {typeName} ×{typeCount}", partHeaderStyle);
                lastType = itemType;
            }
            DrawInPoolEquipRow(pool, id, info, valid);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ---------------- 右：未加入随机 ----------------
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"未加入随机 ({notInPoolEquipShowList.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入全部(筛选结果)", EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(18)))
        {
            foreach (var item in notInPoolEquipShowList)
                pool.equipSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();

        // 筛选行(物种由池自动限定，不提供下拉)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"物种: {GetPoolSpeciesLabel(pool)}", rowLabelStyle, GUILayout.Width(130));
        EditorGUILayout.LabelField("类型:", GUILayout.Width(32));
        int newTypeIndex = EditorGUILayout.Popup(filterItemTypeIndex, itemTypeLabels ?? new[] { "全部类型" }, GUILayout.Width(110));
        if (newTypeIndex != filterItemTypeIndex)
        {
            filterItemTypeIndex = newTypeIndex;
            RebuildShowLists();
        }
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(32));
        string newSearch = EditorGUILayout.TextField(searchKey, GUILayout.Width(120));
        if (newSearch != searchKey)
        {
            searchKey = newSearch;
            RebuildShowLists();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        scrollPosOut = EditorGUILayout.BeginScrollView(scrollPosOut);
        foreach (var item in notInPoolEquipShowList)
            DrawNotInPoolEquipRow(pool, item);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制装备池左列表行(已加入：装备信息 + 移除按钮)
    /// </summary>
    private void DrawInPoolEquipRow(PoolRow pool, long id, ItemInfoItem info, bool valid)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        if (valid)
        {
            DrawItemIcon(info);
            EditorGUILayout.LabelField($"{id}", idLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField(info.iconRes ?? "", rowLabelStyle, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField(info.remark ?? "", idLabelStyle, GUILayout.MinWidth(50));
        }
        else
        {
            EditorGUILayout.LabelField($"{id}", warnLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField("道具表不存在(悬空ID)", warnLabelStyle);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.equipSet.Remove(id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制装备池右列表行(未加入：装备信息 + 加入按钮)
    /// </summary>
    private void DrawNotInPoolEquipRow(PoolRow pool, ItemInfoItem item)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        DrawItemIcon(item);
        EditorGUILayout.LabelField($"{item.id}", idLabelStyle, GUILayout.Width(80));
        EditorGUILayout.LabelField($"{GetItemTypeName(item.itemType)}", rowLabelStyle, GUILayout.Width(50));
        EditorGUILayout.LabelField(item.iconRes ?? "", rowLabelStyle, GUILayout.MinWidth(100));
        EditorGUILayout.LabelField(item.remark ?? "", idLabelStyle, GUILayout.MinWidth(50));

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.equipSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制套装池的左右双列表(已加入 / 未加入套装; 套装内容本身由「装备套装配置」窗口编辑)
    /// </summary>
    private void DrawTwoColumnsForSuit(PoolRow pool)
    {
        EditorGUILayout.BeginHorizontal();

        // ---------------- 左：已加入随机 ----------------
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2f - 8));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"已加入随机 ({inPoolSuitShowList.Count})", EditorStyles.boldLabel, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("全部移除", EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(18)))
        {
            if (EditorUtility.DisplayDialog("确认", $"确定清空池 {pool.id} 的全部 {pool.equipSet.Count} 套套装吗？", "清空", "取消"))
            {
                pool.equipSet.Clear();
                RebuildShowLists();
            }
        }
        EditorGUILayout.EndHorizontal();

        scrollPosIn = EditorGUILayout.BeginScrollView(scrollPosIn);
        int lastModel = int.MinValue;
        int modelCount = 0;
        foreach (var id in inPoolSuitShowList)
        {
            bool valid = suitInfoMap.TryGetValue(id, out var info);
            int modelId = valid ? info.modelId : int.MaxValue;
            if (modelId != lastModel)
            {
                // 物种分组头(统计该组数量)
                modelCount = 0;
                foreach (var other in inPoolSuitShowList)
                {
                    int otherModel = suitInfoMap.TryGetValue(other, out var o) ? o.modelId : int.MaxValue;
                    if (otherModel == modelId)
                        modelCount++;
                }
                string modelName = modelId == int.MaxValue ? "无效ID" : (modelId == 0 ? "通用(0)" : $"{GetModelName(modelId)}({modelId})");
                EditorGUILayout.LabelField($"── {modelName} ×{modelCount}", partHeaderStyle);
                lastModel = modelId;
            }
            DrawInPoolSuitRow(pool, id, info, valid);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ---------------- 右：未加入随机 ----------------
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"未加入随机 ({notInPoolSuitShowList.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入全部(筛选结果)", EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(18)))
        {
            foreach (var item in notInPoolSuitShowList)
                pool.equipSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();

        // 筛选行(物种由池自动限定，不提供下拉)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"物种: {GetPoolSpeciesLabel(pool)}", rowLabelStyle, GUILayout.Width(130));
        EditorGUILayout.LabelField("搜索:", GUILayout.Width(32));
        string newSearch = EditorGUILayout.TextField(searchKey, GUILayout.Width(120));
        if (newSearch != searchKey)
        {
            searchKey = newSearch;
            RebuildShowLists();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        scrollPosOut = EditorGUILayout.BeginScrollView(scrollPosOut);
        foreach (var item in notInPoolSuitShowList)
            DrawNotInPoolSuitRow(pool, item);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制套装池左列表行(已加入：套装信息 + 移除按钮)
    /// </summary>
    private void DrawInPoolSuitRow(PoolRow pool, long id, SuitInfoItem info, bool valid)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        if (valid)
        {
            EditorGUILayout.LabelField($"{id}", idLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField($"{info.itemCount}件", rowLabelStyle, GUILayout.Width(40));
            EditorGUILayout.LabelField(info.remark ?? "", rowLabelStyle, GUILayout.MinWidth(100));
        }
        else
        {
            EditorGUILayout.LabelField($"{id}", warnLabelStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField("套装表不存在(悬空ID)", warnLabelStyle);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.equipSet.Remove(id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制套装池右列表行(未加入：套装信息 + 加入按钮)
    /// </summary>
    private void DrawNotInPoolSuitRow(PoolRow pool, SuitInfoItem item)
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Height(RowHeight));

        EditorGUILayout.LabelField($"{item.id}", idLabelStyle, GUILayout.Width(80));
        EditorGUILayout.LabelField(item.modelId == 0 ? "通用" : GetModelName(item.modelId), rowLabelStyle, GUILayout.Width(60));
        EditorGUILayout.LabelField($"{item.itemCount}件", rowLabelStyle, GUILayout.Width(40));
        EditorGUILayout.LabelField(item.remark ?? "", rowLabelStyle, GUILayout.MinWidth(100));

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("加入", EditorStyles.miniButton, GUILayout.Width(44), GUILayout.Height(18)))
        {
            pool.equipSet.Add(item.id);
            RebuildShowLists();
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 保存逻辑

    /// <summary>
    /// 保存全部变更池到 Excel，并重新导出 JSON
    /// </summary>
    private void SaveData()
    {
        List<ExcelUtil.ExcelChangeData> changeList = new List<ExcelUtil.ExcelChangeData>();
        foreach (var pool in allPools)
        {
            if (!IsPoolDirty(pool))
                continue;
            //按池类型写回对应列(皮肤池写skin_random_data, 装备池/套装池写equip_random_data)
            if (pool.IsEquipPool || pool.IsSuitPool)
                changeList.Add(new ExcelUtil.ExcelChangeData(pool.id, "equip_random_data", CompressSkinSet(pool.equipSet)));
            else
                changeList.Add(new ExcelUtil.ExcelChangeData(pool.id, "skin_random_data", CompressSkinSet(pool.skinSet)));
        }

        if (changeList.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到随机池变更。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认保存",
            $"检测到 {changeList.Count} 个随机池的变更，确定写入 Excel 并重新导出 JSON 吗？", "保存", "取消"))
            return;

        try
        {
            // 1) 写回 Excel(唯一真实源)
            ExcelUtil.SetExcelData(excelPathRandom, SheetRandom, changeList);

            // 2) 重新导出该表的运行时 JSON(该 Excel 仅含 CreatureRandomInfo 单表，整体再生安全)
            ExcelUtil.ExcelToJsonItem(excelPathRandom);

            AssetDatabase.Refresh();

            // 3) 刷新原始值，标记为已保存
            foreach (var pool in allPools)
            {
                pool.originalData = CompressSkinSet(pool.skinSet);
                pool.originalEquipData = CompressSkinSet(pool.equipSet);
            }

            EditorUtility.DisplayDialog("完成", $"已保存 {changeList.Count} 个随机池到 Excel，并重新导出了 CreatureRandomInfo.txt。", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}\n(请确认 Excel 文件未被占用)", "确定");
            LogUtil.LogError($"皮肤随机池保存失败: {e}");
        }
    }

    /// <summary>
    /// 打开随机皮肤池 Excel 表格
    /// </summary>
    private void OpenRandomExcel()
    {
        if (File.Exists(excelPathRandom))
            System.Diagnostics.Process.Start(excelPathRandom);
        else
            EditorUtility.DisplayDialog("错误", $"随机皮肤池 Excel 文件不存在:\n{excelPathRandom}", "确定");
    }

    #endregion
}
