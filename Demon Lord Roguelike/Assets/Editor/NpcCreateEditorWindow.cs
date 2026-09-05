using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// NPC创建编辑器窗口（非运行态）。
/// 在非运行状态下创建/修改 excel_npc_info[NPC信息].xlsx 的 NPC 数据，
/// 提供完整的 NPC 创建 GUI 功能（外观皮肤/逐部位调色/装备/随机池/属性编辑），
/// Spine 预览复用 SpineWindow 动画预览页签的 PreviewRenderUtility 模式。
/// 编辑器安全约束（全窗口强制）：
/// 1) 禁止 new CreatureBean(npcInfo)（内部读 name_language → TextHandler.Instance 会在非Play模式污染场景），一律 BuildCreatureForEditor 手动装配；
/// 2) 禁止读任何 Bean 的 *_language 属性，中文名一律直读 JsonText/Language_{表}_cn.txt；
/// 3) 禁止 UIHandler 弹窗，统一 EditorUtility.DisplayDialog；
/// 4) Excel 为唯一真实源：编辑只改编辑副本，保存时 EPPlus 写回 xlsx 并调 ExcelUtil.ExcelToJsonItem 重导 JSON。
/// </summary>
public partial class NpcCreateEditorWindow : EditorWindow
{
    #region 路径与常量
    /// <summary>NPC业务表sheet名</summary>
    private const string SheetNpc = "NpcInfo";
    /// <summary>Excel目录</summary>
    private static string ExcelDir => Application.dataPath + "/Data/Excel";
    /// <summary>NPC配置表路径</summary>
    private static string ExcelPathNpc => ExcelDir + "/excel_npc_info[NPC信息].xlsx";
    /// <summary>多语言表路径（NPC中文名写其 NpcInfo sheet 的 content_cn 列）</summary>
    private static string ExcelPathLanguage => ExcelDir + "/excel_language[多语言_FrameWork].xlsx";
    /// <summary>参考模型生物ID（生物2001基础皮肤）</summary>
    private const long ReferenceCreatureId = 2001;
    /// <summary>编辑器预览生物的固定UUID（占位用，不入存档）</summary>
    private const string EditorPreviewUUId = "editor-preview";
    #endregion

    #region 编辑状态字段
    /// <summary>当前编辑副本（深拷贝自Cfg缓存，绝不直接改Cfg原值）</summary>
    private NpcInfoBean editingNpcInfo;
    /// <summary>当前编辑的中文名（写语言表 content_cn；不入 NpcInfoBean，故独立字段）</summary>
    private string editingNameCn = "";
    /// <summary>编辑副本序列化快照（脏判定基准）</summary>
    private string snapshotJson = "";
    /// <summary>中文名快照（脏判定基准）</summary>
    private string snapshotNameCn = "";
    /// <summary>当前编辑项是否为未落盘的新建NPC</summary>
    private bool isNewEntry;
    /// <summary>登记待删除的NPC id（保存时才真正删行，删除前可撤销登记）</summary>
    private readonly HashSet<long> deletedNpcIds = new HashSet<long>();
    #endregion

    #region 列表状态字段
    /// <summary>NPC全表缓存（ReloadAllCfg 时重建，来源 NpcInfoCfg）</summary>
    private List<NpcInfoBean> listAllNpc = new List<NpcInfoBean>();
    /// <summary>NPC中文名映射（直读 Language_NpcInfo_cn.txt，不走 TextHandler）</summary>
    private Dictionary<long, string> dicNpcNameCn = new Dictionary<long, string>();
    /// <summary>生物中文名映射（直读 Language_CreatureInfo_cn.txt）</summary>
    private Dictionary<long, string> dicCreatureNameCn = new Dictionary<long, string>();
    /// <summary>道具中文名映射（直读 Language_ItemsInfo_cn.txt）</summary>
    private Dictionary<long, string> dicItemNameCn = new Dictionary<long, string>();
    /// <summary>搜索文本（id包含 或 中文名模糊）</summary>
    private string searchText = "";
    /// <summary>类型筛选序号（0=全部，其余映射 filterNpcTypeValues）</summary>
    private int filterNpcTypeIndex;
    /// <summary>稀有度筛选序号（0=全部，1~6=N~L，rarity≤0按N计）</summary>
    private int filterRarityIndex;
    /// <summary>排序模式（0=id升序 1=id降序 2=稀有度降序）</summary>
    private int sortMode;
    private Vector2 scrollNpcList;
    /// <summary>类型筛选值映射（与显示名同序；首项-1=全部）</summary>
    private static readonly int[] filterNpcTypeValues = { -1, 0, 1, 2, 3 };
    /// <summary>类型筛选显示名</summary>
    private static readonly string[] filterNpcTypeLabels = { "全部类型", "0 默认", "1 战斗", "2 议会固定", "3 议会随机" };
    /// <summary>稀有度筛选显示名（序号即稀有度值，0=全部）</summary>
    private static readonly string[] filterRarityLabels = { "全部稀有度", "N", "R", "SR", "SSR", "UR", "L" };
    /// <summary>排序模式显示名（与 sortMode 一一对应）</summary>
    private static readonly string[] sortModeLabels = { "ID↑", "ID↓", "稀有度↓" };
    #endregion

    #region 新建面板字段
    /// <summary>新建面板是否展开</summary>
    private bool isCreatingNew;
    /// <summary>新建id输入</summary>
    private string inputNewId = "";
    /// <summary>新建中文名输入</summary>
    private string inputNewName = "";
    /// <summary>新建模板序号（0=空白模板，其余映射 listAllNpc[序号-1]）</summary>
    private int newTemplateIndex;
    #endregion

    #region 栏宽与分隔条
    /// <summary>左栏(NPC列表)宽</summary>
    private float widthNpcList = 260f;
    /// <summary>右栏(预览)宽</summary>
    private float widthPreview = 460f;
    private const float DefaultWidthNpcList = 260f;
    private const float DefaultWidthPreview = 460f;
    private const float MinWidthNpcList = 180f;
    private const float MinWidthEdit = 320f;
    private const float MinWidthPreview = 320f;
    private const float SplitterWidth = 5f;
    /// <summary>选择面板(皮肤/装备候选)固定宽</summary>
    private const float WidthSelectPanel = 300f;
    /// <summary>正在拖拽的分隔条(-1=未拖拽;0=列表右 1=预览左)</summary>
    private int draggingSplitter = -1;
    #endregion

    #region GUI样式
    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, sectionStyle;
    #endregion

    #region 窗口生命周期
    /// <summary>
    /// 打开NPC创建编辑器窗口
    /// </summary>
    [MenuItem("游戏/NPC创建编辑")]
    private static void CreateWindow()
    {
        var window = GetWindow<NpcCreateEditorWindow>();
        window.titleContent = new GUIContent("NPC创建编辑");
        window.minSize = new Vector2(1300, 700);
        window.Show();
    }

    /// <summary>
    /// 窗口启用：加载配置与名字映射，并初始化预览子系统
    /// </summary>
    private void OnEnable()
    {
        ReloadAllCfg();
        PreviewOnEnable();
    }

    /// <summary>
    /// 窗口禁用：释放预览子系统
    /// </summary>
    private void OnDisable()
    {
        PreviewOnDisable();
    }
    #endregion

    #region 数据加载
    /// <summary>
    /// 清空相关Cfg静态缓存并重新加载全部配置与名字映射
    /// </summary>
    private void ReloadAllCfg()
    {
        //清Cfg基类静态缓存（dicData/arrayData），保证读到磁盘最新JSON
        ClearCfgBaseStaticCache(typeof(NpcInfoCfg));
        ClearCfgBaseStaticCache(typeof(CreatureInfoCfg));
        ClearCfgBaseStaticCache(typeof(ItemsInfoCfg));
        ClearCfgBaseStaticCache(typeof(CreatureModelInfoCfg));
        ClearCfgBaseStaticCache(typeof(CreatureModelCfg));
        ClearCfgBaseStaticCache(typeof(CreatureRandomInfoCfg));
        ClearCfgBaseStaticCache(typeof(AttackModeExtInfoCfg));
        ClearCfgBaseStaticCache(typeof(SpineAnimationStateCfg));
        //清各Cfg在Partial里额外声明的public静态缓存
        ItemsInfoCfg.dicDataForCreatureModel = null;
        CreatureModelInfoCfg.dicDetailsModelInfo = null;
        SpineAnimationStateCfg.dicSpineAnimData = null;

        var allData = NpcInfoCfg.GetAllArrayData();
        listAllNpc = allData != null ? new List<NpcInfoBean>(allData) : new List<NpcInfoBean>();
        dicNpcNameCn = LoadLanguageCnMap(NpcInfoCfg.fileName);
        dicCreatureNameCn = LoadLanguageCnMap(CreatureInfoCfg.fileName);
        dicItemNameCn = LoadLanguageCnMap(ItemsInfoCfg.fileName);
        //下拉候选依赖配置缓存，一并失效重建
        InvalidateOptionsCache();
    }

    /// <summary>
    /// 直读语言JSON产物构建 textId→中文 映射（文件不存在返回空表；不走 LanguageCfg/TextHandler，避免编辑器态触碰单例）
    /// </summary>
    private Dictionary<long, string> LoadLanguageCnMap(string cfgFileName)
    {
        var map = new Dictionary<long, string>();
        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/Resources/JsonText/Language_{cfgFileName}_cn.txt");
        if (textAsset == null)
            return map;
        var listData = JsonConvert.DeserializeObject<List<LanguageBean>>(textAsset.text);
        if (listData == null)
            return map;
        foreach (var itemData in listData)
            map[itemData.id] = itemData.content;
        return map;
    }

    /// <summary>
    /// 清空 Cfg 基类的 static 数据缓存（反射访问 NonPublic static 的 dicData/arrayData）：
    /// Cfg 的 static 缓存只加载一次（不随 JSON 重导失效），重导后若不清理且不触发域重载，编辑器读取的仍是旧数据
    /// </summary>
    private void ClearCfgBaseStaticCache(Type cfgType)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.FlattenHierarchy;
        cfgType.GetField("dicData", flags)?.SetValue(null, null);
        cfgType.GetField("arrayData", flags)?.SetValue(null, null);
    }

    /// <summary>
    /// 取NPC显示名（编辑器安全版：名字直读中文映射，不碰 name_language；随机议员无名字时显示评级占位）
    /// </summary>
    private string GetNpcNameCn(NpcInfoBean npcInfo)
    {
        if (npcInfo == null)
            return "";
        //正在编辑的新建项：语言行尚未存在，直接用窗口输入的名字
        if (isNewEntry && editingNpcInfo == npcInfo)
            return editingNameCn.IsNull() ? "(未命名)" : editingNameCn;
        if (npcInfo.name == 0)
        {
            return npcInfo.GetNpcType() == NpcTypeEnum.CouncilorRandom
                ? $"(随机议员·评级{npcInfo.GetCouncilorRatings()})"
                : "(无名字)";
        }
        if (dicNpcNameCn.TryGetValue(npcInfo.name, out string npcName))
            return npcName.IsNull() ? "未命名" : npcName;
        return "(未配置名字)";
    }

    /// <summary>
    /// 深拷贝NPC配置为编辑副本（JSON序列化；name_language 带 [JsonIgnore] 不会触发 TextHandler，protected 缓存字段不参与序列化）
    /// </summary>
    private NpcInfoBean DeepCopyNpc(NpcInfoBean source)
    {
        return JsonConvert.DeserializeObject<NpcInfoBean>(JsonConvert.SerializeObject(source));
    }

    /// <summary>
    /// 提交当前编辑现场为快照（保存/加载后调用，清除脏标记）
    /// </summary>
    private void CommitSnapshot()
    {
        snapshotJson = editingNpcInfo != null ? JsonConvert.SerializeObject(editingNpcInfo) : "";
        snapshotNameCn = editingNameCn;
    }
    #endregion

    #region 脏判定与切换保护
    /// <summary>
    /// 当前编辑副本相对快照是否有改动（新建项恒脏）
    /// </summary>
    private bool IsEditingDirty()
    {
        if (editingNpcInfo == null)
            return false;
        if (isNewEntry)
            return true;
        return JsonConvert.SerializeObject(editingNpcInfo) != snapshotJson || editingNameCn != snapshotNameCn;
    }

    /// <summary>
    /// 是否有任何待保存变更（编辑脏 或 有删除登记）
    /// </summary>
    private bool HasAnyChange()
    {
        return IsEditingDirty() || deletedNpcIds.Count > 0;
    }

    /// <summary>
    /// 脏数据三选保护（保存并继续/放弃修改/取消）；返回 true=可以继续后续操作
    /// </summary>
    private bool ConfirmDiscardIfDirty()
    {
        if (!IsEditingDirty())
            return true;
        int choice = EditorUtility.DisplayDialogComplex(
            "未保存的修改",
            $"NPC [{editingNpcInfo.id}] 有未保存的修改。",
            "保存并继续", "取消", "放弃修改");
        if (choice == 0)
            return SaveAll();
        return choice == 2;
    }
    #endregion

    #region 界面绘制
    /// <summary>
    /// 绘制窗口（工具栏 + 左NPC列表/中编辑区/右预览 三栏，栏间分隔条可拖拽调宽；皮肤/装备选择面板打开时在中右之间插入固定宽候选栏）
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();
        DrawToolbar();
        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        DrawNpcListColumn();
        DrawSplitter(0);
        DrawEditColumn();
        if (selectMode != SelectMode.None)
            DrawSelectPanelColumn();
        DrawSplitter(1);
        DrawPreviewColumn();
        EditorGUILayout.EndHorizontal();
        HandleSplitterDrag();
        //域重载后预览实例已销毁但序列化字段还在，自动重建一次
        EnsurePreviewAlive();
    }

    /// <summary>
    /// 绘制栏间分隔条（按住左右拖拽调节相邻栏宽，双击恢复该栏默认宽；中栏为弹性栏自动占满剩余宽度）
    /// </summary>
    private void DrawSplitter(int splitterIndex)
    {
        GUILayout.Box(GUIContent.none, GUI.skin.box, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
        Rect rect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            if (e.clickCount == 2)
                ResetSplitterWidth(splitterIndex);
            else
                draggingSplitter = splitterIndex;
            e.Use();
        }
    }

    /// <summary>
    /// 处理分隔条拖拽中/结束（拖拽中按鼠标位移调整相邻固定栏宽并夹取范围，保证弹性中栏不小于最小宽）
    /// </summary>
    private void HandleSplitterDrag()
    {
        if (draggingSplitter < 0)
            return;
        var e = Event.current;
        EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.ResizeHorizontal);
        if (e.type == EventType.MouseDrag)
        {
            float delta = e.delta.x;
            if (draggingSplitter == 0)
                widthNpcList = ClampSplitterWidth(widthNpcList + delta, MinWidthNpcList, widthPreview);
            else if (draggingSplitter == 1)
                widthPreview = ClampSplitterWidth(widthPreview - delta, MinWidthPreview, widthNpcList);
            e.Use();
            Repaint();
        }
        else if (e.rawType == EventType.MouseUp)
        {
            draggingSplitter = -1;
        }
    }

    /// <summary>
    /// 夹取栏宽（下限=该栏最小宽；上限=保证弹性中栏不小于最小宽，otherFixedWidth=另一个固定栏宽）
    /// </summary>
    private float ClampSplitterWidth(float width, float minWidth, float otherFixedWidth)
    {
        float selectWidth = selectMode != SelectMode.None ? WidthSelectPanel : 0f;
        float maxWidth = position.width - otherFixedWidth - selectWidth - MinWidthEdit - SplitterWidth * 2 - 20f;
        if (maxWidth < minWidth)
            maxWidth = minWidth;
        return Mathf.Clamp(width, minWidth, maxWidth);
    }

    /// <summary>
    /// 双击分隔条恢复对应栏默认宽
    /// </summary>
    private void ResetSplitterWidth(int splitterIndex)
    {
        if (splitterIndex == 0)
            widthNpcList = DefaultWidthNpcList;
        else if (splitterIndex == 1)
            widthPreview = DefaultWidthPreview;
    }

    /// <summary>
    /// 顶部工具栏：保存/刷新配置/打开配置表/未保存状态
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.enabled = HasAnyChange();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("💾 保存", EditorStyles.toolbarButton, GUILayout.Width(70)))
            SaveAll();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        if (GUILayout.Button("🔄 刷新配置", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (!HasAnyChange() || EditorUtility.DisplayDialog("放弃变更", "存在未保存的变更，刷新配置将丢失它们。继续？", "放弃变更", "取消"))
            {
                deletedNpcIds.Clear();
                isNewEntry = false;
                editingNpcInfo = null;
                CloseSelect();
                ReloadAllCfg();
                RebuildPreview();
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("📂 NPC表", EditorStyles.toolbarButton, GUILayout.Width(70)))
            OpenExcel(ExcelPathNpc);
        if (GUILayout.Button("📂 语言表", EditorStyles.toolbarButton, GUILayout.Width(70)))
            OpenExcel(ExcelPathLanguage);
        GUILayout.FlexibleSpace();
        if (HasAnyChange())
        {
            GUI.color = new Color(1f, 0.7f, 0.3f);
            GUILayout.Label("● 未保存修改", EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 用默认程序打开 Excel 配置表
    /// </summary>
    private void OpenExcel(string excelPath)
    {
        if (!System.IO.File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("打开失败", $"找不到配置表:\n{excelPath}", "确定");
            return;
        }
        Application.OpenURL("file:///" + excelPath.Replace("\\", "/"));
    }
    #endregion

    #region GUI样式
    /// <summary>
    /// 懒初始化GUI样式，只初始化一次
    /// </summary>
    private void InitGUIStyle()
    {
        if (guiStyleInited)
            return;
        guiStyleInited = true;
        titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
        sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
    }

    /// <summary>
    /// 绘制分节标题
    /// </summary>
    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, sectionStyle);
        var rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        EditorGUILayout.Space(2);
    }
    #endregion
}
