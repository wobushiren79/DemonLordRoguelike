using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 研究模块编辑窗口
/// 顶部页签选择研究类型，中间可拖动画布显示节点（可拖动改坐标），下方编辑选中研究的全部配置（含多语言与解锁配置）
/// </summary>
public class ResearchEditorWindow : EditorWindow
{
    #region 菜单项与窗口创建

    /// <summary>
    /// 菜单项：游戏/研究模块编辑
    /// </summary>
    [MenuItem("游戏/研究模块编辑")]
    private static void OpenWindow()
    {
        var window = GetWindow<ResearchEditorWindow>();
        window.titleContent = new GUIContent("研究模块编辑");
        window.minSize = new Vector2(1000, 800);
        window.Show();
    }

    #endregion

    #region 常量

    /// <summary>研究信息 Excel 路径</summary>
    private const string RESEARCH_EXCEL = "Assets/Data/Excel/excel_research_info[研究信息].xlsx";
    /// <summary>解锁信息 Excel 路径</summary>
    private const string UNLOCK_EXCEL = "Assets/Data/Excel/excel_unlock_info[解锁信息].xlsx";
    /// <summary>多语言 Excel 路径</summary>
    private const string LANGUAGE_EXCEL = "Assets/Data/Excel/excel_language[多语言_FrameWork].xlsx";

    /// <summary>研究信息工作表名</summary>
    private const string RESEARCH_SHEET = "ResearchInfo";
    /// <summary>解锁信息工作表名</summary>
    private const string UNLOCK_SHEET = "UnlockInfo";

    /// <summary>研究信息 JSON 路径</summary>
    private const string RESEARCH_JSON = "Assets/Resources/JsonText/ResearchInfo.txt";
    /// <summary>解锁信息 JSON 路径</summary>
    private const string UNLOCK_JSON = "Assets/Resources/JsonText/UnlockInfo.txt";
    /// <summary>研究多语言中文 JSON 路径</summary>
    private const string LANG_CN_JSON = "Assets/Resources/JsonText/Language_ResearchInfo_cn.txt";
    /// <summary>研究多语言英文 JSON 路径</summary>
    private const string LANG_EN_JSON = "Assets/Resources/JsonText/Language_ResearchInfo_en.txt";

    /// <summary>节点尺寸</summary>
    private const float NODE_SIZE = 70f;
    /// <summary>画布最小高度</summary>
    private const float CANVAS_MIN_HEIGHT = 360f;

    /// <summary>研究信息 Excel 字段顺序（写回 Excel 时使用，与表头列名一致）</summary>
    private static readonly string[] RESEARCH_FIELDS = new string[]
    {
        "research_type", "icon_res", "level_max", "position_x", "position_y",
        "unlock_id", "pre_unlock_ids", "pay_crystal", "name", "remark",
    };

    /// <summary>解锁信息 Excel 字段顺序</summary>
    private static readonly string[] UNLOCK_FIELDS = new string[]
    {
        "unlock_type", "remark",
    };

    #endregion

    #region 数据字段

    /// <summary>所有研究数据（运行时编辑用）</summary>
    private List<ResearchInfoBean> allResearch = new List<ResearchInfoBean>();
    /// <summary>原始研究数据快照（用于 diff）</summary>
    private Dictionary<long, ResearchInfoBean> originalResearch = new Dictionary<long, ResearchInfoBean>();

    /// <summary>所有解锁数据</summary>
    private Dictionary<long, UnlockInfoBean> allUnlock = new Dictionary<long, UnlockInfoBean>();
    /// <summary>原始解锁数据快照</summary>
    private Dictionary<long, UnlockInfoBean> originalUnlock = new Dictionary<long, UnlockInfoBean>();

    /// <summary>研究多语言中文（id → content）</summary>
    private Dictionary<long, string> langCn = new Dictionary<long, string>();
    /// <summary>研究多语言英文</summary>
    private Dictionary<long, string> langEn = new Dictionary<long, string>();
    /// <summary>多语言中文原始快照</summary>
    private Dictionary<long, string> originalLangCn = new Dictionary<long, string>();
    /// <summary>多语言英文原始快照</summary>
    private Dictionary<long, string> originalLangEn = new Dictionary<long, string>();

    /// <summary>当前选中的研究类型</summary>
    private ResearchInfoTypeEnum selectedType = ResearchInfoTypeEnum.Building;
    /// <summary>当前选中的研究节点（下方编辑区使用）</summary>
    private ResearchInfoBean selectedResearch;

    /// <summary>画布世界平移偏移</summary>
    private Vector2 panOffset = Vector2.zero;
    /// <summary>画布缩放</summary>
    private float zoom = 0.5f;

    /// <summary>当前交互状态</summary>
    private enum InteractionMode { None, DraggingNode, Panning }
    private InteractionMode interactionMode = InteractionMode.None;
    /// <summary>正在拖动的节点</summary>
    private ResearchInfoBean draggingNode;
    /// <summary>拖动开始时的鼠标位置（窗口坐标）</summary>
    private Vector2 dragStartMousePos;
    /// <summary>拖动开始时的节点世界坐标</summary>
    private Vector2 dragStartNodeWorldPos;
    /// <summary>平移开始时的平移偏移</summary>
    private Vector2 dragStartPanOffset;

    /// <summary>底部编辑区滚动位置</summary>
    private Vector2 bottomScroll = Vector2.zero;
    /// <summary>当前画布高度</summary>
    private float currentCanvasHeight = CANVAS_MIN_HEIGHT;

    #endregion

    #region 样式

    /// <summary>样式是否已初始化</summary>
    private bool stylesInitialized = false;
    private GUIStyle sectionHeaderStyle;
    private GUIStyle boxStyle;
    private GUIStyle nodeBoxStyle;
    private GUIStyle nodeLabelStyle;

    #endregion

    #region 资源缓存

    /// <summary>1x1 白纹理（用于线条）</summary>
    private Texture2D whiteTex;

    /// <summary>icon_res → Sprite 缓存（编辑器预览用）</summary>
    private Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

    #endregion

    #region Unity 生命周期

    /// <summary>
    /// 窗口启用时加载数据
    /// </summary>
    private void OnEnable()
    {
        LoadAllData();
    }

    /// <summary>
    /// 窗口禁用时释放纹理
    /// </summary>
    private void OnDisable()
    {
        if (whiteTex != null)
        {
            DestroyImmediate(whiteTex);
            whiteTex = null;
        }
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

        // 顶部：研究类型 Tab
        DrawTabBar();

        GUILayout.Space(4);

        // 中间：画布
        DrawCanvas();

        GUILayout.Space(6);

        // 下方：选中研究的编辑区
        DrawEditArea();

        // 最底：保存/重置按钮
        DrawActionButtons();
    }

    #endregion

    #region 样式初始化

    /// <summary>
    /// 初始化全部自定义样式
    /// </summary>
    private void InitializeStyles()
    {
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            fixedHeight = 24,
            alignment = TextAnchor.MiddleLeft,
        };

        boxStyle = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(4, 4, 4, 4),
        };

        nodeBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(2, 2, 2, 2),
            fontSize = 9,
            wordWrap = true,
        };

        nodeLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            normal = { textColor = Color.white },
            wordWrap = true,
        };

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();

        stylesInitialized = true;
    }

    #endregion

    #region 数据加载

    /// <summary>
    /// 从 JSON 文件加载全部数据并建立快照
    /// </summary>
    private void LoadAllData()
    {
        LoadResearch();
        LoadUnlock();
        LoadLanguage(LANG_CN_JSON, langCn, originalLangCn);
        LoadLanguage(LANG_EN_JSON, langEn, originalLangEn);

        // 重置选中
        selectedResearch = null;
        // 还原视图状态
        zoom = 0.5f;
        CenterViewOnCurrentType();
    }

    /// <summary>
    /// 从 ResearchInfo.txt 加载研究数据并建立深拷贝快照
    /// </summary>
    private void LoadResearch()
    {
        allResearch.Clear();
        originalResearch.Clear();
        if (!File.Exists(RESEARCH_JSON))
        {
            Debug.LogError($"未找到 {RESEARCH_JSON}");
            return;
        }
        string json = File.ReadAllText(RESEARCH_JSON);
        var array = JsonConvert.DeserializeObject<ResearchInfoBean[]>(json);
        if (array == null) return;
        foreach (var item in array)
        {
            allResearch.Add(item);
            // 深拷贝快照
            var snap = JsonConvert.DeserializeObject<ResearchInfoBean>(JsonConvert.SerializeObject(item));
            originalResearch[item.id] = snap;
        }
    }

    /// <summary>
    /// 从 UnlockInfo.txt 加载解锁数据
    /// </summary>
    private void LoadUnlock()
    {
        allUnlock.Clear();
        originalUnlock.Clear();
        if (!File.Exists(UNLOCK_JSON))
        {
            Debug.LogError($"未找到 {UNLOCK_JSON}");
            return;
        }
        string json = File.ReadAllText(UNLOCK_JSON);
        var array = JsonConvert.DeserializeObject<UnlockInfoBean[]>(json);
        if (array == null) return;
        foreach (var item in array)
        {
            allUnlock[item.id] = item;
            var snap = JsonConvert.DeserializeObject<UnlockInfoBean>(JsonConvert.SerializeObject(item));
            originalUnlock[item.id] = snap;
        }
    }

    /// <summary>
    /// 加载单个语言 JSON（格式 [{id, content}]）
    /// </summary>
    private void LoadLanguage(string path, Dictionary<long, string> target, Dictionary<long, string> snap)
    {
        target.Clear();
        snap.Clear();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"未找到 {path}");
            return;
        }
        string json = File.ReadAllText(path);
        var array = JsonConvert.DeserializeObject<LanguageItem[]>(json);
        if (array == null) return;
        foreach (var item in array)
        {
            target[item.id] = item.content ?? "";
            snap[item.id] = item.content ?? "";
        }
    }

    /// <summary>
    /// 语言项序列化结构
    /// </summary>
    [Serializable]
    private class LanguageItem
    {
        public long id;
        public string content;
    }

    #endregion

    #region UI 绘制 - 顶部 Tab

    /// <summary>
    /// 绘制顶部研究类型 Tab
    /// </summary>
    private void DrawTabBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawTabButton("设施", ResearchInfoTypeEnum.Building);
        DrawTabButton("强化", ResearchInfoTypeEnum.Strengthen);
        DrawTabButton("魔物", ResearchInfoTypeEnum.Creature);
        DrawTabButton("世界", ResearchInfoTypeEnum.World);
        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField($"缩放: {zoom:0.00}", GUILayout.Width(90));
        if (GUILayout.Button("重置视图", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            panOffset = Vector2.zero;
            zoom = 0.5f;
        }
        if (GUILayout.Button("重新加载", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("确认", "重新加载将丢弃未保存的修改，确定吗？", "确定", "取消"))
            {
                LoadAllData();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制单个 Tab 按钮
    /// </summary>
    private void DrawTabButton(string label, ResearchInfoTypeEnum type)
    {
        bool isSelected = selectedType == type;
        Color prev = GUI.backgroundColor;
        if (isSelected)
        {
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
        }
        if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (selectedType != type)
            {
                selectedType = type;
                selectedResearch = null;
                CenterViewOnCurrentType();
            }
        }
        GUI.backgroundColor = prev;
    }

    /// <summary>
    /// 切换类型时将视图自动居中到当前类型节点的几何中心
    /// </summary>
    private void CenterViewOnCurrentType()
    {
        var list = GetCurrentTypeResearch();
        if (list.Count == 0) return;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var r in list)
        {
            minX = Mathf.Min(minX, r.position_x);
            maxX = Mathf.Max(maxX, r.position_x);
            minY = Mathf.Min(minY, r.position_y);
            maxY = Mathf.Max(maxY, r.position_y);
        }
        panOffset = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    #endregion

    #region UI 绘制 - 画布

    /// <summary>
    /// 绘制中间画布（含节点与连线）
    /// </summary>
    private void DrawCanvas()
    {
        // 动态计算画布高度：窗口剩余高度的一半左右
        currentCanvasHeight = Mathf.Max(CANVAS_MIN_HEIGHT, position.height * 0.5f);

        Rect canvasRect = GUILayoutUtility.GetRect(0, currentCanvasHeight, GUILayout.ExpandWidth(true));
        // 背景
        EditorGUI.DrawRect(canvasRect, new Color(0.13f, 0.13f, 0.15f, 1f));
        // 边框
        DrawRectBorder(canvasRect, new Color(0.05f, 0.05f, 0.05f), 1f);

        var currentList = GetCurrentTypeResearch();
        Vector2 canvasCenter = canvasRect.center;

        // 1) 网格 + 节点 + 图标 走 BeginClip（这些都是 EditorGUI.DrawRect / GUI.DrawTextureWithTexCoords，被剪裁规则正确处理）
        GUI.BeginClip(canvasRect);
        {
            Rect localRect = new Rect(0, 0, canvasRect.width, canvasRect.height);
            Vector2 localCenter = localRect.center;

            DrawGrid(localRect);

            foreach (var r in currentList)
            {
                DrawNode(r, localCenter, localRect);
            }
        }
        GUI.EndClip();

        // 2) 连线在剪裁外画（用全局坐标），手动用 Cohen–Sutherland 把线段裁到 canvasRect 内，避免 Handles/GL 不遵守 GUI.BeginClip 导致溢出画布
        foreach (var r in currentList)
        {
            DrawConnections(r, canvasCenter, canvasRect);
        }

        // 视图信息（左下角）：在剪裁之外用全局坐标绘制
        var infoRect = new Rect(canvasRect.x + 8, canvasRect.yMax - 22, 400, 18);
        EditorGUI.LabelField(infoRect, $"节点数:{currentList.Count}  平移:({panOffset.x:0},{panOffset.y:0})  类型:{selectedType}", EditorStyles.miniLabel);

        // 事件处理：使用全局 canvasRect 与全局鼠标坐标
        HandleCanvasEvents(canvasRect, currentList);
    }

    /// <summary>
    /// 绘制画布网格
    /// </summary>
    private void DrawGrid(Rect canvasRect)
    {
        var center = canvasRect.center;
        float spacing = 100f * zoom;
        if (spacing < 8f) spacing = 8f;
        Color minorColor = new Color(1f, 1f, 1f, 0.04f);
        Color axisColor = new Color(1f, 1f, 1f, 0.18f);

        float offsetX = (-panOffset.x * zoom) % spacing;
        float offsetY = (panOffset.y * zoom) % spacing;

        // 竖直
        for (float x = canvasRect.x + (center.x - canvasRect.x + offsetX) % spacing; x < canvasRect.xMax; x += spacing)
        {
            DrawLine(new Vector2(x, canvasRect.y), new Vector2(x, canvasRect.yMax), minorColor, 1f);
        }
        // 水平
        for (float y = canvasRect.y + (center.y - canvasRect.y + offsetY) % spacing; y < canvasRect.yMax; y += spacing)
        {
            DrawLine(new Vector2(canvasRect.x, y), new Vector2(canvasRect.xMax, y), minorColor, 1f);
        }
        // 原点坐标轴（如可见）
        Vector2 origin = WorldToScreen(Vector2.zero, center);
        if (canvasRect.Contains(origin))
        {
            DrawLine(new Vector2(canvasRect.x, origin.y), new Vector2(canvasRect.xMax, origin.y), axisColor, 1f);
            DrawLine(new Vector2(origin.x, canvasRect.y), new Vector2(origin.x, canvasRect.yMax), axisColor, 1f);
        }
    }

    /// <summary>
    /// 世界坐标 → 屏幕坐标（Y 轴翻转）
    /// </summary>
    private Vector2 WorldToScreen(Vector2 worldPos, Vector2 canvasCenter)
    {
        return new Vector2(
            canvasCenter.x + (worldPos.x - panOffset.x) * zoom,
            canvasCenter.y - (worldPos.y - panOffset.y) * zoom
        );
    }

    /// <summary>
    /// 屏幕坐标 → 世界坐标（Y 轴翻转）
    /// </summary>
    private Vector2 ScreenToWorld(Vector2 screenPos, Vector2 canvasCenter)
    {
        return new Vector2(
            panOffset.x + (screenPos.x - canvasCenter.x) / zoom,
            panOffset.y - (screenPos.y - canvasCenter.y) / zoom
        );
    }

    /// <summary>
    /// 绘制研究节点的前置连线
    /// </summary>
    private void DrawConnections(ResearchInfoBean info, Vector2 canvasCenter, Rect canvasRect)
    {
        if (string.IsNullOrEmpty(info.pre_unlock_ids))
            return;

        Vector2 targetScreen = WorldToScreen(new Vector2(info.position_x, info.position_y), canvasCenter);
        // 不再用早剔（只判 target 会把"从画内指向画外"的连线整条砍掉），改由后续 Cohen–Sutherland 逐线段裁剪

        // 解析前置（兼容 , | 拍平）
        var preIds = new List<long>();
        var split = info.pre_unlock_ids.Split(',');
        foreach (var part in split)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (part.Contains('|'))
            {
                foreach (var sub in part.Split('|'))
                {
                    if (long.TryParse(sub, out long sid)) preIds.Add(sid);
                }
            }
            else
            {
                if (long.TryParse(part, out long id)) preIds.Add(id);
            }
        }

        foreach (var preUnlockId in preIds)
        {
            var preResearch = FindResearchByUnlockId(preUnlockId);
            if (preResearch == null) continue;
            // 仅同类型才画线（与运行时一致）
            if (preResearch.research_type != info.research_type) continue;

            Vector2 preScreen = WorldToScreen(new Vector2(preResearch.position_x, preResearch.position_y), canvasCenter);
            Color lineColor = new Color(0.4f, 0.7f, 1f, 0.9f);

            // Cohen–Sutherland 裁剪到 canvasRect 内，避免连线超出画布
            Vector2 a = preScreen, b = targetScreen;
            if (ClipSegmentToRect(canvasRect, ref a, ref b))
            {
                DrawLine(a, b, lineColor, 2f);
            }
        }
    }

    /// <summary>
    /// Cohen–Sutherland 直线裁剪。把线段 (a,b) 裁剪到 rect 内；返回 true 表示线段至少有一部分在 rect 中。
    /// </summary>
    private bool ClipSegmentToRect(Rect rect, ref Vector2 a, ref Vector2 b)
    {
        const int INSIDE = 0, LEFT = 1, RIGHT = 2, BOTTOM = 4, TOP = 8;
        float xmin = rect.xMin, xmax = rect.xMax, ymin = rect.yMin, ymax = rect.yMax;

        int Code(Vector2 p)
        {
            int c = INSIDE;
            if (p.x < xmin) c |= LEFT;
            else if (p.x > xmax) c |= RIGHT;
            if (p.y < ymin) c |= TOP;       // 屏幕坐标 y 向下，此处沿用 Rect.yMin/yMax 语义
            else if (p.y > ymax) c |= BOTTOM;
            return c;
        }

        int ca = Code(a), cb = Code(b);
        while (true)
        {
            if ((ca | cb) == 0) return true;            // 两端都在内部
            if ((ca & cb) != 0) return false;           // 两端都在同一外部区域，必定在外
            int co = ca != 0 ? ca : cb;
            float x = 0, y = 0;
            if ((co & BOTTOM) != 0)
            {
                x = a.x + (b.x - a.x) * (ymax - a.y) / (b.y - a.y);
                y = ymax;
            }
            else if ((co & TOP) != 0)
            {
                x = a.x + (b.x - a.x) * (ymin - a.y) / (b.y - a.y);
                y = ymin;
            }
            else if ((co & RIGHT) != 0)
            {
                y = a.y + (b.y - a.y) * (xmax - a.x) / (b.x - a.x);
                x = xmax;
            }
            else if ((co & LEFT) != 0)
            {
                y = a.y + (b.y - a.y) * (xmin - a.x) / (b.x - a.x);
                x = xmin;
            }
            if (co == ca) { a = new Vector2(x, y); ca = Code(a); }
            else { b = new Vector2(x, y); cb = Code(b); }
        }
    }

    /// <summary>
    /// 简易圆形/矩形相交检测（用于剔除画布外的节点连线）
    /// </summary>
    private bool RectIntersectsCircle(Rect rect, Vector2 center, float radius)
    {
        float dx = Mathf.Max(rect.xMin - center.x, 0, center.x - rect.xMax);
        float dy = Mathf.Max(rect.yMin - center.y, 0, center.y - rect.yMax);
        return (dx * dx + dy * dy) <= (radius * radius);
    }

    /// <summary>
    /// 绘制单个研究节点
    /// </summary>
    private void DrawNode(ResearchInfoBean info, Vector2 canvasCenter, Rect canvasRect)
    {
        Vector2 screen = WorldToScreen(new Vector2(info.position_x, info.position_y), canvasCenter);
        float size = NODE_SIZE * Mathf.Max(0.5f, zoom);
        Rect nodeRect = new Rect(screen.x - size * 0.5f, screen.y - size * 0.5f, size, size);

        // 画布外剔除（仅完全不相交才跳过；剪裁由外层 GUI.BeginClip 完成）
        if (!nodeRect.Overlaps(canvasRect))
            return;

        // 颜色：选中=橙色 / 普通=深蓝
        bool isSelected = selectedResearch != null && selectedResearch.id == info.id;
        Color bg = isSelected ? new Color(1f, 0.6f, 0.2f, 0.9f) : new Color(0.25f, 0.4f, 0.7f, 0.9f);
        EditorGUI.DrawRect(nodeRect, bg);

        // 图标叠加（如果 icon_res 存在）
        var iconSprite = GetIconSprite(info.icon_res);
        if (iconSprite != null && iconSprite.texture != null)
        {
            float padding = Mathf.Max(2f, size * 0.08f);
            Rect iconRect = new Rect(nodeRect.x + padding, nodeRect.y + padding,
                nodeRect.width - 2 * padding, nodeRect.height - 2 * padding);
            DrawSpriteInRect(iconRect, iconSprite);
        }
        else if (!string.IsNullOrEmpty(info.icon_res))
        {
            // 找不到资源时，在节点中央显示问号占位
            var qStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.5f, 0.5f, 0.9f) },
                fontSize = Mathf.RoundToInt(size * 0.35f),
            };
            EditorGUI.LabelField(nodeRect, "?", qStyle);
        }

        DrawRectBorder(nodeRect, isSelected ? Color.yellow : new Color(0.1f, 0.1f, 0.1f, 0.8f), isSelected ? 2f : 1f);

        // 名字标签：放到节点下方，避免遮挡图标
        string label;
        if (langCn.TryGetValue(info.name, out string cnName) && !string.IsNullOrEmpty(cnName))
        {
            label = cnName;
        }
        else
        {
            label = $"#{info.id}";
        }
        var labelRect = new Rect(nodeRect.x - 20, nodeRect.yMax + 1, nodeRect.width + 40, 14);
        EditorGUI.LabelField(labelRect, label, nodeLabelStyle);
    }

    /// <summary>
    /// 根据 icon_res 加载 Sprite（带缓存）。
    /// 支持 "name,AtlasType" 格式（与 IconHandler.ParseIconName 一致），按文件名匹配。
    /// </summary>
    private Sprite GetIconSprite(string iconRes)
    {
        if (string.IsNullOrEmpty(iconRes)) return null;
        if (iconCache.TryGetValue(iconRes, out var cached)) return cached;

        string actualName = iconRes;
        int commaIdx = iconRes.LastIndexOf(',');
        if (commaIdx > 0 && commaIdx < iconRes.Length - 1)
        {
            actualName = iconRes.Substring(0, commaIdx);
        }

        Sprite sprite = null;
        var guids = AssetDatabase.FindAssets($"{actualName} t:Sprite");
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 1) 单 Sprite 资源：文件名等于 actualName
            if (Path.GetFileNameWithoutExtension(assetPath) == actualName)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null) break;
            }
            // 2) Sprite Sheet：扫描表征资源，按 sprite.name 匹配
            var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var sub in subs)
            {
                if (sub is Sprite s && s.name == actualName)
                {
                    sprite = s;
                    break;
                }
            }
            if (sprite != null) break;
        }
        iconCache[iconRes] = sprite;
        return sprite;
    }

    /// <summary>
    /// 在指定矩形内等比绘制 Sprite（处理图集子区域 uv）
    /// </summary>
    private void DrawSpriteInRect(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;
        Rect texRect = sprite.textureRect;
        Rect uv = new Rect(
            texRect.x / sprite.texture.width,
            texRect.y / sprite.texture.height,
            texRect.width / sprite.texture.width,
            texRect.height / sprite.texture.height
        );

        // 等比缩放到 rect 内（contain）
        float spriteAspect = texRect.width / texRect.height;
        float rectAspect = rect.width / rect.height;
        Rect drawRect;
        if (spriteAspect > rectAspect)
        {
            float h = rect.width / spriteAspect;
            drawRect = new Rect(rect.x, rect.y + (rect.height - h) * 0.5f, rect.width, h);
        }
        else
        {
            float w = rect.height * spriteAspect;
            drawRect = new Rect(rect.x + (rect.width - w) * 0.5f, rect.y, w, rect.height);
        }
        GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv);
    }

    #endregion

    #region 画布交互（拖动 / 平移 / 缩放）

    /// <summary>
    /// 处理画布上的鼠标事件
    /// </summary>
    private void HandleCanvasEvents(Rect canvasRect, List<ResearchInfoBean> currentList)
    {
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        Vector2 canvasCenter = canvasRect.center;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (canvasRect.Contains(mousePos) && e.button == 0)
                {
                    var hitNode = FindNodeAtScreen(mousePos, canvasCenter, currentList);
                    if (hitNode != null)
                    {
                        selectedResearch = hitNode;
                        draggingNode = hitNode;
                        dragStartMousePos = mousePos;
                        dragStartNodeWorldPos = new Vector2(hitNode.position_x, hitNode.position_y);
                        interactionMode = InteractionMode.DraggingNode;
                        GUI.FocusControl(null);
                        e.Use();
                        Repaint();
                    }
                    else
                    {
                        // 空白处 → 平移
                        interactionMode = InteractionMode.Panning;
                        dragStartMousePos = mousePos;
                        dragStartPanOffset = panOffset;
                        e.Use();
                    }
                }
                else if (canvasRect.Contains(mousePos) && (e.button == 1 || e.button == 2))
                {
                    // 右键 / 中键：平移
                    interactionMode = InteractionMode.Panning;
                    dragStartMousePos = mousePos;
                    dragStartPanOffset = panOffset;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (interactionMode == InteractionMode.DraggingNode && draggingNode != null)
                {
                    Vector2 delta = mousePos - dragStartMousePos;
                    // 屏幕 delta → 世界 delta（Y 翻转）
                    float worldDx = delta.x / zoom;
                    float worldDy = -delta.y / zoom;
                    draggingNode.position_x = dragStartNodeWorldPos.x + worldDx;
                    draggingNode.position_y = dragStartNodeWorldPos.y + worldDy;
                    e.Use();
                    Repaint();
                }
                else if (interactionMode == InteractionMode.Panning)
                {
                    Vector2 delta = mousePos - dragStartMousePos;
                    // 屏幕 delta → 世界 delta（Y 翻转，平移方向与鼠标相反）
                    panOffset = new Vector2(
                        dragStartPanOffset.x - delta.x / zoom,
                        dragStartPanOffset.y + delta.y / zoom
                    );
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (interactionMode == InteractionMode.DraggingNode)
                {
                    // 落点取整（与运行时坐标格式一致）
                    if (draggingNode != null)
                    {
                        draggingNode.position_x = Mathf.Round(draggingNode.position_x);
                        draggingNode.position_y = Mathf.Round(draggingNode.position_y);
                    }
                }
                interactionMode = InteractionMode.None;
                draggingNode = null;
                Repaint();
                break;

            case EventType.ScrollWheel:
                if (canvasRect.Contains(mousePos))
                {
                    float oldZoom = zoom;
                    zoom = Mathf.Clamp(zoom * (e.delta.y > 0 ? 0.9f : 1.1f), 0.1f, 2.0f);
                    // 让缩放围绕鼠标位置：保持鼠标指向的世界点不变
                    if (oldZoom > 0 && zoom > 0)
                    {
                        Vector2 mouseWorldOld = new Vector2(
                            panOffset.x + (mousePos.x - canvasCenter.x) / oldZoom,
                            panOffset.y - (mousePos.y - canvasCenter.y) / oldZoom
                        );
                        Vector2 mouseWorldNew = ScreenToWorld(mousePos, canvasCenter);
                        panOffset += (mouseWorldOld - mouseWorldNew);
                    }
                    e.Use();
                    Repaint();
                }
                break;
        }
    }

    /// <summary>
    /// 根据屏幕坐标查找命中的节点
    /// </summary>
    private ResearchInfoBean FindNodeAtScreen(Vector2 screenPos, Vector2 canvasCenter, List<ResearchInfoBean> list)
    {
        float size = NODE_SIZE * Mathf.Max(0.5f, zoom);
        // 倒序查找（让后绘制的节点优先命中）
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var info = list[i];
            Vector2 nodeScreen = WorldToScreen(new Vector2(info.position_x, info.position_y), canvasCenter);
            Rect r = new Rect(nodeScreen.x - size * 0.5f, nodeScreen.y - size * 0.5f, size, size);
            if (r.Contains(screenPos))
                return info;
        }
        return null;
    }

    #endregion

    #region UI 绘制 - 编辑区域

    /// <summary>
    /// 绘制下方编辑区域
    /// </summary>
    private void DrawEditArea()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("编辑区", sectionHeaderStyle);

        if (selectedResearch == null)
        {
            EditorGUILayout.HelpBox("请在上方画布中点击一个研究节点进行编辑", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        float editHeight = Mathf.Max(150, position.height - currentCanvasHeight - 200);
        bottomScroll = EditorGUILayout.BeginScrollView(bottomScroll, GUILayout.Height(editHeight));

        // ResearchInfo 字段
        EditorGUILayout.LabelField($"ResearchInfo ID: {selectedResearch.id}", EditorStyles.boldLabel);

        selectedResearch.research_type = (int)(ResearchInfoTypeEnum)EditorGUILayout.EnumPopup(
            new GUIContent("研究类型 research_type"), (ResearchInfoTypeEnum)selectedResearch.research_type);

        // 图标资源 + 预览
        EditorGUILayout.BeginHorizontal();
        selectedResearch.icon_res = EditorGUILayout.TextField("图标资源 icon_res", selectedResearch.icon_res ?? "");
        var previewSprite = GetIconSprite(selectedResearch.icon_res);
        Rect previewRect = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
        EditorGUI.DrawRect(previewRect, new Color(0.18f, 0.18f, 0.18f, 1f));
        if (previewSprite != null && previewSprite.texture != null)
        {
            DrawSpriteInRect(new Rect(previewRect.x + 2, previewRect.y + 2, previewRect.width - 4, previewRect.height - 4), previewSprite);
        }
        else if (!string.IsNullOrEmpty(selectedResearch.icon_res))
        {
            var qStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.5f, 0.5f, 0.9f) },
            };
            GUI.Label(previewRect, "?", qStyle);
        }
        DrawRectBorder(previewRect, new Color(0f, 0f, 0f, 0.6f), 1f);
        EditorGUILayout.EndHorizontal();

        selectedResearch.level_max = EditorGUILayout.IntField("升级上限 level_max", selectedResearch.level_max);

        EditorGUILayout.BeginHorizontal();
        selectedResearch.position_x = EditorGUILayout.FloatField("位置 X position_x", selectedResearch.position_x);
        selectedResearch.position_y = EditorGUILayout.FloatField("位置 Y position_y", selectedResearch.position_y);
        EditorGUILayout.EndHorizontal();

        selectedResearch.unlock_id = EditorGUILayout.LongField("解锁ID unlock_id", selectedResearch.unlock_id);
        selectedResearch.pre_unlock_ids = EditorGUILayout.TextField("前置解锁 pre_unlock_ids", selectedResearch.pre_unlock_ids ?? "");
        selectedResearch.pay_crystal = EditorGUILayout.TextField("支付水晶 pay_crystal", selectedResearch.pay_crystal ?? "");
        selectedResearch.name = EditorGUILayout.LongField("名字文本ID name", selectedResearch.name);
        selectedResearch.remark = EditorGUILayout.TextField("备注 remark", selectedResearch.remark ?? "");

        GUILayout.Space(8);
        DrawSeparator();
        GUILayout.Space(4);

        // 多语言
        EditorGUILayout.LabelField($"多语言（textId = {selectedResearch.name}）", EditorStyles.boldLabel);
        if (!langCn.ContainsKey(selectedResearch.name))
        {
            EditorGUILayout.HelpBox($"多语言 ID {selectedResearch.name} 在中文表中不存在，保存后会新增该条目。", MessageType.Info);
        }
        string cnText = langCn.TryGetValue(selectedResearch.name, out var cn) ? cn : "";
        string newCn = EditorGUILayout.TextField("中文 cn", cnText);
        if (newCn != cnText) langCn[selectedResearch.name] = newCn;

        string enText = langEn.TryGetValue(selectedResearch.name, out var en) ? en : "";
        string newEn = EditorGUILayout.TextField("英文 en", enText);
        if (newEn != enText) langEn[selectedResearch.name] = newEn;

        GUILayout.Space(8);
        DrawSeparator();
        GUILayout.Space(4);

        // 对应 UnlockInfo
        EditorGUILayout.LabelField($"UnlockInfo（id = {selectedResearch.unlock_id}）", EditorStyles.boldLabel);
        if (!allUnlock.TryGetValue(selectedResearch.unlock_id, out var unlockBean))
        {
            EditorGUILayout.HelpBox($"未找到 UnlockInfo[{selectedResearch.unlock_id}]，保存时会新增。", MessageType.Warning);
            if (GUILayout.Button("创建对应 UnlockInfo 条目", GUILayout.Width(220)))
            {
                var newUnlock = new UnlockInfoBean { id = selectedResearch.unlock_id, unlock_type = 0, remark = "" };
                allUnlock[newUnlock.id] = newUnlock;
            }
        }
        else
        {
            unlockBean.unlock_type = EditorGUILayout.IntField("解锁类型 unlock_type (0研究 1扭蛋机)", unlockBean.unlock_type);
            unlockBean.remark = EditorGUILayout.TextField("备注 remark", unlockBean.remark ?? "");
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region UI 绘制 - 操作按钮

    /// <summary>
    /// 绘制保存与重置按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(selectedResearch == null);
        Color prev = GUI.backgroundColor;

        GUI.backgroundColor = new Color(1f, 0.55f, 0.2f);
        if (GUILayout.Button("还原选中研究", GUILayout.Width(140), GUILayout.Height(32)))
        {
            if (selectedResearch != null)
            {
                RestoreSelected();
            }
        }
        GUI.backgroundColor = prev;
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button("保存全部修改", GUILayout.Width(180), GUILayout.Height(32)))
        {
            SaveAll();
        }
        GUI.backgroundColor = prev;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    /// <summary>
    /// 还原选中研究到原始数据
    /// </summary>
    private void RestoreSelected()
    {
        if (selectedResearch == null) return;
        if (!originalResearch.TryGetValue(selectedResearch.id, out var snap)) return;
        // 字段复制
        selectedResearch.research_type = snap.research_type;
        selectedResearch.icon_res = snap.icon_res;
        selectedResearch.level_max = snap.level_max;
        selectedResearch.position_x = snap.position_x;
        selectedResearch.position_y = snap.position_y;
        selectedResearch.unlock_id = snap.unlock_id;
        selectedResearch.pre_unlock_ids = snap.pre_unlock_ids;
        selectedResearch.pay_crystal = snap.pay_crystal;
        selectedResearch.name = snap.name;
        selectedResearch.remark = snap.remark;
        // 还原对应多语言
        if (originalLangCn.TryGetValue(selectedResearch.name, out var cn)) langCn[selectedResearch.name] = cn;
        if (originalLangEn.TryGetValue(selectedResearch.name, out var en)) langEn[selectedResearch.name] = en;
        // 还原对应 UnlockInfo
        if (originalUnlock.TryGetValue(selectedResearch.unlock_id, out var u) && allUnlock.TryGetValue(selectedResearch.unlock_id, out var cur))
        {
            cur.unlock_type = u.unlock_type;
            cur.remark = u.remark;
        }
        Repaint();
    }

    #endregion

    #region 数据保存

    /// <summary>
    /// id → 字段名 → 字符串值 的修改集
    /// </summary>
    private class ChangeSet
    {
        public Dictionary<long, Dictionary<string, string>> data = new Dictionary<long, Dictionary<string, string>>();
        public void Add(long id, string field, string value)
        {
            if (!data.TryGetValue(id, out var dict))
            {
                dict = new Dictionary<string, string>();
                data[id] = dict;
            }
            dict[field] = value;
        }
        public int FieldCount
        {
            get { int c = 0; foreach (var kv in data) c += kv.Value.Count; return c; }
        }
    }

    /// <summary>
    /// 保存所有修改到 Excel 与 JSON
    /// </summary>
    private void SaveAll()
    {
        var researchChanges = CollectResearchChanges();
        var unlockChanges = CollectUnlockChanges();
        var langCnChanges = CollectLanguageChanges(langCn, originalLangCn);
        var langEnChanges = CollectLanguageChanges(langEn, originalLangEn);

        int totalChanges = researchChanges.FieldCount + unlockChanges.FieldCount + langCnChanges.Count + langEnChanges.Count;
        if (totalChanges == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有检测到任何修改", "确定");
            return;
        }

        string msg = $"将写入以下修改：\n" +
                     $"- ResearchInfo 字段: {researchChanges.FieldCount}（{researchChanges.data.Count} 条记录）\n" +
                     $"- UnlockInfo 字段: {unlockChanges.FieldCount}（{unlockChanges.data.Count} 条记录）\n" +
                     $"- 多语言 cn: {langCnChanges.Count}\n" +
                     $"- 多语言 en: {langEnChanges.Count}\n\n" +
                     "Excel 与 JSON 都会被更新。多语言会同时写入 excel_language Excel 的 ResearchInfo 工作表（如存在）。";
        if (!EditorUtility.DisplayDialog("确认保存", msg, "保存", "取消"))
            return;

        try
        {
            if (researchChanges.FieldCount > 0)
            {
                WriteChangesToExcel(RESEARCH_EXCEL, RESEARCH_SHEET, researchChanges);
            }
            if (unlockChanges.FieldCount > 0)
            {
                WriteChangesToExcel(UNLOCK_EXCEL, UNLOCK_SHEET, unlockChanges);
            }
            if (langCnChanges.Count + langEnChanges.Count > 0)
            {
                SaveLanguageExcel(langCnChanges, langEnChanges);
            }

            // 写 JSON
            WriteResearchJson();
            WriteUnlockJson();
            WriteLanguageJson(LANG_CN_JSON, langCn);
            WriteLanguageJson(LANG_EN_JSON, langEn);

            AssetDatabase.Refresh();

            // 重置快照
            RefreshSnapshots();

            EditorUtility.DisplayDialog("完成", "保存完成", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败：{e.Message}", "确定");
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// 收集 ResearchInfo 修改（与快照对比，按 id 分组以保证同 id 单行写入）
    /// </summary>
    private ChangeSet CollectResearchChanges()
    {
        var set = new ChangeSet();
        var type = typeof(ResearchInfoBean);
        foreach (var item in allResearch)
        {
            originalResearch.TryGetValue(item.id, out var snap);
            foreach (var field in RESEARCH_FIELDS)
            {
                var fi = type.GetField(field);
                if (fi == null) continue;
                object cur = fi.GetValue(item);
                object old = snap != null ? fi.GetValue(snap) : null;
                if (snap == null || !Equals(cur, old))
                {
                    set.Add(item.id, field, FormatExcelValue(cur));
                }
            }
        }
        return set;
    }

    /// <summary>
    /// 收集 UnlockInfo 修改
    /// </summary>
    private ChangeSet CollectUnlockChanges()
    {
        var set = new ChangeSet();
        var type = typeof(UnlockInfoBean);
        foreach (var kv in allUnlock)
        {
            var item = kv.Value;
            originalUnlock.TryGetValue(item.id, out var snap);
            foreach (var field in UNLOCK_FIELDS)
            {
                var fi = type.GetField(field);
                if (fi == null) continue;
                object cur = fi.GetValue(item);
                object old = snap != null ? fi.GetValue(snap) : null;
                if (snap == null || !Equals(cur, old))
                {
                    set.Add(item.id, field, FormatExcelValue(cur));
                }
            }
        }
        return set;
    }

    /// <summary>
    /// 收集多语言修改：仅记录 id → 新文本
    /// </summary>
    private Dictionary<long, string> CollectLanguageChanges(
        Dictionary<long, string> current, Dictionary<long, string> snap)
    {
        var dict = new Dictionary<long, string>();
        foreach (var kv in current)
        {
            string oldVal;
            bool inSnap = snap.TryGetValue(kv.Key, out oldVal);
            if (!inSnap || oldVal != kv.Value)
            {
                dict[kv.Key] = kv.Value ?? "";
            }
        }
        return dict;
    }

    /// <summary>
    /// 将分组的字段修改写入 Excel：每个 id 命中已有行则更新，未命中则在末尾新建一行
    /// </summary>
    private void WriteChangesToExcel(string excelPath, string sheetName, ChangeSet changes)
    {
        if (!File.Exists(excelPath))
        {
            Debug.LogError($"Excel 文件不存在：{excelPath}");
            return;
        }
        var file = new FileInfo(excelPath);
        using (var pack = new ExcelPackage(file))
        {
            var sheet = pack.Workbook.Worksheets[sheetName];
            if (sheet == null)
            {
                Debug.LogError($"Excel 工作表不存在：{sheetName}");
                return;
            }
            int columnCount = sheet.Dimension.End.Column;
            int rowCount = sheet.Dimension.End.Row;

            // 列名 → 列号
            var colIndex = new Dictionary<string, int>();
            for (int x = 1; x <= columnCount; x++)
            {
                colIndex[sheet.Cells[1, x].Text] = x;
            }

            // id → 行号
            var idRowMap = new Dictionary<long, int>();
            for (int y = 4; y <= rowCount; y++)
            {
                if (long.TryParse(sheet.Cells[y, 1].Text, out long rowId))
                    idRowMap[rowId] = y;
            }

            int nextRow = rowCount + 1;
            foreach (var kv in changes.data)
            {
                long id = kv.Key;
                if (!idRowMap.TryGetValue(id, out int row))
                {
                    row = nextRow++;
                    if (colIndex.TryGetValue("id", out int idColIdx))
                    {
                        sheet.Cells[row, idColIdx].Value = id;
                    }
                    else
                    {
                        sheet.Cells[row, 1].Value = id;
                    }
                    idRowMap[id] = row;
                }
                foreach (var fkv in kv.Value)
                {
                    if (!colIndex.TryGetValue(fkv.Key, out int col))
                    {
                        Debug.LogWarning($"Excel {sheetName} 中没有列 {fkv.Key}");
                        continue;
                    }
                    sheet.Cells[row, col].Value = fkv.Value;
                }
            }

            pack.Save();
        }
    }

    /// <summary>
    /// 写入多语言 Excel（excel_language[多语言_FrameWork].xlsx 的 ResearchInfo 工作表）
    /// 表头格式：id | content_cn | content_en | remark
    /// </summary>
    private void SaveLanguageExcel(Dictionary<long, string> cnChanges, Dictionary<long, string> enChanges)
    {
        if (!File.Exists(LANGUAGE_EXCEL))
        {
            Debug.LogWarning($"未找到 {LANGUAGE_EXCEL}，跳过 Excel 写入（JSON 仍会写入）");
            return;
        }
        var file = new FileInfo(LANGUAGE_EXCEL);
        using (var pack = new ExcelPackage(file))
        {
            var sheet = pack.Workbook.Worksheets[RESEARCH_SHEET];
            if (sheet == null)
            {
                Debug.LogWarning($"语言 Excel 中未找到 {RESEARCH_SHEET} 工作表，跳过 Excel 写入（JSON 仍会写入）");
                return;
            }
            int columnCount = sheet.Dimension.End.Column;
            int rowCount = sheet.Dimension.End.Row;

            var colIndex = new Dictionary<string, int>();
            for (int x = 1; x <= columnCount; x++)
            {
                colIndex[sheet.Cells[1, x].Text] = x;
            }
            var idRowMap = new Dictionary<long, int>();
            for (int y = 4; y <= rowCount; y++)
            {
                if (long.TryParse(sheet.Cells[y, 1].Text, out long rowId)) idRowMap[rowId] = y;
            }
            int nextRow = rowCount + 1;

            // 合并 cn / en 修改的 id 集合
            var allIds = new HashSet<long>(cnChanges.Keys);
            foreach (var id in enChanges.Keys) allIds.Add(id);

            foreach (var id in allIds)
            {
                if (!idRowMap.TryGetValue(id, out int row))
                {
                    row = nextRow++;
                    sheet.Cells[row, 1].Value = id;
                    idRowMap[id] = row;
                }
                if (cnChanges.TryGetValue(id, out var cnVal) && colIndex.TryGetValue("content_cn", out int cnCol))
                {
                    sheet.Cells[row, cnCol].Value = cnVal;
                }
                if (enChanges.TryGetValue(id, out var enVal) && colIndex.TryGetValue("content_en", out int enCol))
                {
                    sheet.Cells[row, enCol].Value = enVal;
                }
            }

            pack.Save();
        }
    }

    /// <summary>
    /// 写入 ResearchInfo.txt JSON（不含 partial 中的运行时字段）
    /// </summary>
    private void WriteResearchJson()
    {
        // 过滤运行时字段：直接序列化 bean 即可，partial 中的 preUnlockIds / arrayPayCrystal 会被序列化为 null（与原始一致）
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include,
        };
        string json = JsonConvert.SerializeObject(allResearch.ToArray(), settings);
        File.WriteAllText(RESEARCH_JSON, json);
    }

    /// <summary>
    /// 写入 UnlockInfo.txt JSON
    /// </summary>
    private void WriteUnlockJson()
    {
        // 保持原始排序：按 id 升序
        var array = allUnlock.Values.OrderBy(u => u.id).ToArray();
        string json = JsonConvert.SerializeObject(array, Formatting.None);
        File.WriteAllText(UNLOCK_JSON, json);
    }

    /// <summary>
    /// 写入单语言 JSON（保留原始顺序，新增 id 追加到末尾）
    /// </summary>
    private void WriteLanguageJson(string path, Dictionary<long, string> data)
    {
        var list = new List<LanguageItem>();
        foreach (var kv in data)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            list.Add(new LanguageItem { id = kv.Key, content = kv.Value });
        }
        list = list.OrderBy(i => i.id).ToList();
        string json = JsonConvert.SerializeObject(list.ToArray(), Formatting.None);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 保存后重建快照
    /// </summary>
    private void RefreshSnapshots()
    {
        originalResearch.Clear();
        foreach (var item in allResearch)
        {
            originalResearch[item.id] = JsonConvert.DeserializeObject<ResearchInfoBean>(JsonConvert.SerializeObject(item));
        }
        originalUnlock.Clear();
        foreach (var kv in allUnlock)
        {
            originalUnlock[kv.Key] = JsonConvert.DeserializeObject<UnlockInfoBean>(JsonConvert.SerializeObject(kv.Value));
        }
        originalLangCn = new Dictionary<long, string>(langCn);
        originalLangEn = new Dictionary<long, string>(langEn);
    }

    /// <summary>
    /// 把字段值转换为 Excel 写入字符串
    /// </summary>
    private string FormatExcelValue(object val)
    {
        if (val == null) return "";
        if (val is float f) return ((int)Mathf.Round(f)).ToString();
        if (val is double d) return ((int)Math.Round(d)).ToString();
        return val.ToString();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 获取当前类型下的全部研究
    /// </summary>
    private List<ResearchInfoBean> GetCurrentTypeResearch()
    {
        var result = new List<ResearchInfoBean>();
        foreach (var r in allResearch)
        {
            if ((ResearchInfoTypeEnum)r.research_type == selectedType)
            {
                result.Add(r);
            }
        }
        return result;
    }

    /// <summary>
    /// 按 unlock_id 查找研究（用于连线）
    /// </summary>
    private ResearchInfoBean FindResearchByUnlockId(long unlockId)
    {
        foreach (var r in allResearch)
        {
            if (r.unlock_id == unlockId) return r;
        }
        return null;
    }

    /// <summary>
    /// 绘制水平分割线
    /// </summary>
    private void DrawSeparator()
    {
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 0.5f));
    }

    /// <summary>
    /// 绘制矩形边框
    /// </summary>
    private void DrawRectBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    /// <summary>
    /// 绘制两点之间的线段。
    /// 使用 Handles.DrawAAPolyLine（走 GL 渲染），在 GUI.BeginClip 内能正确显示，不受 GUI.matrix 旋转副作用影响。
    /// 仅在 Repaint 事件中绘制，避免重复触发。
    /// </summary>
    private void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
    {
        if (Event.current == null || Event.current.type != EventType.Repaint)
            return;
        Color prev = Handles.color;
        Handles.color = color;
        Handles.DrawAAPolyLine(thickness, new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0));
        Handles.color = prev;
    }

    #endregion
}
