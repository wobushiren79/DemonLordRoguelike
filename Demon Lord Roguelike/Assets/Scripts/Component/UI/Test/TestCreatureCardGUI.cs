using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.Reflection;
using static ExcelUtil;
#endif

/// <summary>
/// 生物卡片编辑器（GUI版，纯代码控制面板 + 真实卡片预制体，不依赖任何测试预制）
/// 自由设置稀有度/等级/生物ID/NPC ID（或下拉选择生物/NPC）实时查看 UIViewCreatureCardItem 与 UIViewCreatureCardDetails 的显示效果；
/// 支持自定义稀有度板色(主板/副板,支持双色渐变)与等级字体颜色实时预览，并可写回 excel_rarity_info / excel_level_info 配置表(同步再生JSON并清Cfg缓存立即生效)。
/// 由 LauncherTest.StartForCreatureCardEditor 挂到空物体上启动。
/// </summary>
public class TestCreatureCardGUI : MonoBehaviour
{
    #region 常量
    private const string PathCardItemPrefab = "UI/Common/UIViewCreatureCardItem";       //卡片项预制(Resources路径)
    private const string PathCardDetailsPrefab = "UI/Common/UIViewCreatureCardDetails"; //卡片详情预制(Resources路径)
    private const string PathRarityExcel = "Assets/Data/Excel/excel_rarity_info[稀有度].xlsx";    //稀有度配置表(写回用)
    private const string PathLevelExcel = "Assets/Data/Excel/excel_level_info[等级信息].xlsx";    //等级配置表(写回用)
    private const int LevelMax = 10;        //等级滑条上限(LevelInfo 配置 1~10 级颜色, 0级固定白色)
    private const int CanvasSortOrder = 5000;//覆盖层Canvas层级(压过游戏UI)
    private const float PanelWidth = 400;   //左侧IMGUI控制面板宽度
    #endregion

    #region 数据字段
    /// <summary>当前面板实例(供入口防重复创建, 面板销毁时置空)</summary>
    public static TestCreatureCardGUI Instance;

    private Canvas canvas;                          //卡片显示用覆盖层Canvas(随面板销毁)
    private UIViewCreatureCardItem cardItem;        //小卡实例(真实预制体)
    private UIViewCreatureCardDetails cardDetails;  //大卡详情实例(真实预制体)

    private int sourceType;                         //数据来源: 0=生物 1=NPC
    private long currentCreatureId = 2001;          //当前选中的生物id
    private long currentNpcId = 1010010001;         //当前选中的NPC id
    private string inputManualId = "";              //手动输入id(非空且合法时优先于下拉, 作用于当前来源类型)
    private int rarity = 1;                         //稀有度(1~6, 含999魔王配色档)
    private int level;                              //等级(0~10)
    private float cardScale = 1f;                   //卡片整体缩放

    //自定义颜色(勾选启用后覆盖配置色显示; 板色支持渐变: 起点色+终点色)
    private bool customBoardColor, customOtherColor, customLevelColor;
    private bool boardGradient, otherGradient;
    private readonly ColorEditState boardStartState = new ColorEditState();   //主板色-起点
    private readonly ColorEditState boardEndState = new ColorEditState();     //主板色-终点(渐变)
    private readonly ColorEditState otherStartState = new ColorEditState();   //副板色-起点
    private readonly ColorEditState otherEndState = new ColorEditState();     //副板色-终点(渐变)
    private readonly ColorEditState levelColorState = new ColorEditState();   //等级字体色

    //下拉候选(懒加载)
    private List<SelectItem> listCreatureOptions, listNpcOptions, listRarityOptions;
    private bool isTargetDropdownOpen, isRarityDropdownOpen;
    private Vector2 scrollTargetDropdown, scrollRarityDropdown;
    private string creatureDropdownLabel = "请选择生物", npcDropdownLabel = "请选择NPC", rarityDropdownLabel = "稀有度";

    private long lastDataKey = -1;                  //上一帧的数据参数指纹(来源/id/稀有度/等级), 变更才重建生物数据
    private string saveHint = "";                   //保存结果提示
    private float saveHintTime;                     //保存提示显示截止时间
    #endregion

    #region GUI样式
    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, hintStyle, buttonLeftStyle;
    private Vector2 scrollMain;
    #endregion

    /// <summary>下拉候选项(id + 显示名)</summary>
    private struct SelectItem
    {
        public long id;
        public string label;
        public SelectItem(long id, string label)
        {
            this.id = id;
            this.label = label;
        }
    }

    /// <summary>单个颜色的编辑状态(颜色值 + RGB数值/Hex 文本框内容)，文本框与颜色值双向同步</summary>
    private class ColorEditState
    {
        /// <summary>当前颜色值(A固定为1)</summary>
        public Color color = Color.white;
        /// <summary>RGB 数值输入框内容(0~255)</summary>
        public string rInput = "255", gInput = "255", bInput = "255";
        /// <summary>Hex 输入框内容(#RRGGBB)</summary>
        public string hexInput = "#FFFFFF";

        /// <summary>设置颜色并同步全部文本框(滑条/调色盘/读取配置后调用)</summary>
        public void SetColor(Color newColor)
        {
            color = newColor;
            color.a = 1f;
            rInput = $"{Mathf.RoundToInt(color.r * 255)}";
            gInput = $"{Mathf.RoundToInt(color.g * 255)}";
            bInput = $"{Mathf.RoundToInt(color.b * 255)}";
            hexInput = $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        /// <summary>RGB 数值输入合法时应用(同步 Hex 框, 保留 RGB 三框原文防打断输入)</summary>
        public void ApplyRgbInput()
        {
            if (int.TryParse(rInput, out int r) && int.TryParse(gInput, out int g) && int.TryParse(bInput, out int b))
            {
                color = new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f);
                hexInput = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            }
        }

        /// <summary>Hex 输入合法时应用(同步 RGB 三框, 保留 Hex 框原文防打断输入)</summary>
        public void ApplyHexInput()
        {
            string normalizedHex = hexInput.StartsWith("#") ? hexInput : $"#{hexInput}";
            if (ColorUtility.TryParseHtmlString(normalizedHex, out Color hexColor))
            {
                color = hexColor;
                color.a = 1f;
                rInput = $"{Mathf.RoundToInt(color.r * 255)}";
                gInput = $"{Mathf.RoundToInt(color.g * 255)}";
                bInput = $"{Mathf.RoundToInt(color.b * 255)}";
            }
        }
    }

    #region 生命周期
    /// <summary>
    /// 初始化：创建覆盖层Canvas并实例化真实卡片预制体，随后按初始参数刷新卡片
    /// </summary>
    private void Start()
    {
        Instance = this;
        CreateCards();
        ReadConfigColors(false, false, false);
        RefreshCards();
    }

    /// <summary>
    /// 销毁时置空实例引用(Canvas为子物体随之销毁)
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 设置初始数据(由 LauncherTest 传入 GameTestEditor 面板的生物/NPC ID)
    /// </summary>
    /// <param name="creatureId">生物ID, >0 时默认生物模式</param>
    /// <param name="npcInfoId">NPC ID, 生物ID为0时默认NPC模式</param>
    public void SetInitData(long creatureId, long npcInfoId)
    {
        if (creatureId > 0)
        {
            sourceType = 0;
            currentCreatureId = creatureId;
        }
        else
        {
            sourceType = 1;
            currentNpcId = npcInfoId;
        }
    }

    /// <summary>
    /// 创建覆盖层Canvas并实例化小卡/大卡详情两个真实预制体，摆到屏幕中央左右
    /// </summary>
    private void CreateCards()
    {
        GameObject canvasObj = new GameObject("CreatureCardTestCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;
        //与游戏UI一致的适配方式: 1920x1080 参考分辨率宽高匹配
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        cardItem = Instantiate(Resources.Load<GameObject>(PathCardItemPrefab), canvasObj.transform).GetComponent<UIViewCreatureCardItem>();
        NormalizeRoot(cardItem.transform as RectTransform, new Vector2(-220, 0));
        cardDetails = Instantiate(Resources.Load<GameObject>(PathCardDetailsPrefab), canvasObj.transform).GetComponent<UIViewCreatureCardDetails>();
        NormalizeRoot(cardDetails.transform as RectTransform, new Vector2(300, 0));
    }

    /// <summary>
    /// 规整预制体根节点：固化当前尺寸并改为居中锚点，避免拉伸型根节点铺满整个Canvas
    /// </summary>
    /// <param name="rt">预制体根RectTransform</param>
    /// <param name="anchoredPos">目标锚点坐标</param>
    private void NormalizeRoot(RectTransform rt, Vector2 anchoredPos)
    {
        Vector2 size = rt.rect.size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (size.x > 1 && size.y > 1)
            rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one * cardScale;
    }
    #endregion

    #region 卡片数据构建与刷新
    /// <summary>
    /// 重建生物数据并刷新小卡/大卡显示(数据参数变更时调用)，随后应用自定义颜色覆盖
    /// </summary>
    private void RefreshCards()
    {
        if (cardItem == null || cardDetails == null) return;
        CreatureBean creatureData = BuildCreature();
        if (creatureData == null) return;
        //ShowNoPopup: 测试面板无需详情气泡交互, 禁用悬停弹窗按钮
        cardItem.SetData(creatureData, CardUseStateEnum.ShowNoPopup);
        cardDetails.SetData(creatureData);
        ApplyCustomColors();
    }

    /// <summary>
    /// 按当前来源类型/id/稀有度/等级构建测试生物(Play模式单例齐全, 可走真实构造链)
    /// </summary>
    private CreatureBean BuildCreature()
    {
        long targetId = GetCurrentId();
        CreatureBean creatureData;
        if (sourceType == 1)
        {
            NpcInfoBean npcInfo = NpcInfoCfg.GetItemData(targetId);
            if (npcInfo == null) return null;
            creatureData = new CreatureBean(npcInfo);
        }
        else
        {
            if (CreatureInfoCfg.GetItemData(targetId) == null) return null;
            creatureData = new CreatureBean(targetId);
        }
        creatureData.rarity = rarity;
        creatureData.level = level;
        creatureData.AddSkinForBase();
        return creatureData;
    }

    /// <summary>
    /// 取当前生效的id：手动输入优先(需存在于对应配置表)，空/非法输入回退下拉选择
    /// </summary>
    private long GetCurrentId()
    {
        if (!inputManualId.IsNull() && long.TryParse(inputManualId, out long manualId))
        {
            bool isValid = sourceType == 1 ? NpcInfoCfg.GetItemData(manualId) != null : CreatureInfoCfg.GetItemData(manualId) != null;
            if (isValid) return manualId;
        }
        return sourceType == 1 ? currentNpcId : currentCreatureId;
    }

    /// <summary>
    /// 计算数据参数指纹(来源/id/稀有度/等级)，用于变更检测(颜色/缩放变更不重建生物数据)
    /// </summary>
    private long ComputeDataKey()
    {
        return sourceType * 100000000L + GetCurrentId() * 1000 + rarity * 100 + level;
    }
    #endregion

    #region 颜色工具
    /// <summary>
    /// 从配置表读取当前稀有度/等级的颜色填入编辑器；forceAll=false 时只读取未勾选自定义的组(保留用户正在编辑的颜色)
    /// </summary>
    private void ReadConfigColors(bool forceBoard, bool forceOther, bool forceLevel)
    {
        RarityInfoBean rarityInfo = RarityInfoCfg.GetItemData(rarity);
        if (rarityInfo != null)
        {
            if (forceBoard || !customBoardColor)
                ParseColorStr(rarityInfo.ui_board_color, boardStartState, boardEndState, ref boardGradient);
            if (forceOther || !customOtherColor)
                ParseColorStr(rarityInfo.ui_board_other_color, otherStartState, otherEndState, ref otherGradient);
        }
        if (forceLevel || !customLevelColor)
            levelColorState.SetColor(LevelInfoCfg.GetLevelColor(level));
    }

    /// <summary>
    /// 解析配置颜色串(支持 "#xxx" 单色与 "#xxx,#yyy" 双色渐变)到起止色编辑状态(同步文本框)
    /// </summary>
    private void ParseColorStr(string colorStr, ColorEditState startState, ColorEditState endState, ref bool isGradient)
    {
        if (colorStr.IsNull()) return;
        string[] colors = colorStr.Split(',');
        if (ColorUtility.TryParseHtmlString(colors[0].Trim(), out Color parsedStart))
            startState.SetColor(parsedStart);
        isGradient = colors.Length >= 2;
        if (isGradient && ColorUtility.TryParseHtmlString(colors[1].Trim(), out Color parsedEnd))
            endState.SetColor(parsedEnd);
        else
            endState.SetColor(startState.color);
    }

    /// <summary>
    /// 按渐变开关把起止色拼回配置格式字符串(单色 "#xxx" / 渐变 "#xxx,#yyy")
    /// </summary>
    private string ToColorStr(Color startColor, bool isGradient, Color endColor)
    {
        string startStr = $"#{ColorUtility.ToHtmlStringRGB(startColor)}";
        return isGradient ? $"{startStr},#{ColorUtility.ToHtmlStringRGB(endColor)}" : startStr;
    }

    /// <summary>
    /// 把启用的自定义颜色覆盖到卡片显示(主板色: 小卡底板+大卡底板/场景底; 副板色: 小卡图标底+大卡稀有度条; 等级色: 两者等级字体)
    /// </summary>
    private void ApplyCustomColors()
    {
        if (cardItem == null || cardDetails == null) return;
        if (customBoardColor)
        {
            string colorStr = ToColorStr(boardStartState.color, boardGradient, boardEndState.color);
            GameUIUtil.SetGradientColor(cardItem.ui_CardBgBorad, colorStr);
            GameUIUtil.SetGradientColor(cardDetails.ui_CardBgBoard, colorStr);
            GameUIUtil.SetGradientColor(cardDetails.ui_CardSceneBg, colorStr);
        }
        if (customOtherColor)
        {
            string colorStr = ToColorStr(otherStartState.color, otherGradient, otherEndState.color);
            GameUIUtil.SetGradientColor(cardItem.ui_IconContent, colorStr);
            GameUIUtil.SetGradientColor(cardDetails.ui_CardRate, colorStr);
        }
        if (customLevelColor)
        {
            cardItem.ui_LevelText.color = levelColorState.color;
            cardDetails.ui_LevelText.color = levelColorState.color;
        }
    }

    /// <summary>
    /// 应用卡片整体缩放(小卡/大卡同步)
    /// </summary>
    private void ApplyCardScale()
    {
        if (cardItem != null) cardItem.transform.localScale = Vector3.one * cardScale;
        if (cardDetails != null) cardDetails.transform.localScale = Vector3.one * cardScale;
    }

    /// <summary>
    /// 保存当前编辑器颜色到配置表(当前稀有度行的主板/副板色 + 当前等级行的等级色)并再生JSON、清Cfg缓存立即生效
    /// </summary>
    private void SaveColorsToExcel()
    {
#if UNITY_EDITOR
        string boardStr = ToColorStr(boardStartState.color, boardGradient, boardEndState.color);
        string otherStr = ToColorStr(otherStartState.color, otherGradient, otherEndState.color);
        List<ExcelChangeData> rarityChanges = new List<ExcelChangeData>
        {
            new ExcelChangeData(rarity, "ui_board_color", boardStr),
            new ExcelChangeData(rarity, "ui_board_other_color", otherStr),
        };
        ExcelUtil.SetExcelData(PathRarityExcel, "RarityInfo", rarityChanges);
        ExcelUtil.ExcelToJsonItem(PathRarityExcel);
        ClearCfgCache(typeof(RarityInfoCfg));

        //0级无颜色配置(固定白色), 仅 1~10 级写回 level_color
        string levelColorStr = $"#{ColorUtility.ToHtmlStringRGB(levelColorState.color)}";
        if (level >= 1)
        {
            List<ExcelChangeData> levelChanges = new List<ExcelChangeData>
            {
                new ExcelChangeData(level, "level_color", levelColorStr),
            };
            ExcelUtil.SetExcelData(PathLevelExcel, "LevelInfo", levelChanges);
            ExcelUtil.ExcelToJsonItem(PathLevelExcel);
            ClearCfgCache(typeof(LevelInfoCfg));
        }

        RefreshCards();
        saveHint = $"已保存: 稀有度{rarity} 主板{boardStr} 副板{otherStr}" + (level >= 1 ? $" + {level}级等级色" : "(0级无等级色配置)");
        saveHintTime = Time.unscaledTime + 5f;
        LogUtil.Log($"[卡片编辑器] 颜色已写回配置表: 稀有度{rarity}({boardStr}/{otherStr})" + (level >= 1 ? $", {level}级等级色 {levelColorStr}" : ""));
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 反射清理 Cfg 的静态缓存(dicData/arrayData)，使再生JSON后的配置立即生效(运行时 asm 无法引用编辑器程序集的 GameTestEditor.ClearCfgBaseStaticCache, 此处就地实现)
    /// </summary>
    private void ClearCfgCache(System.Type cfgType)
    {
        cfgType.GetField("dicData", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
        cfgType.BaseType?.GetField("arrayData", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
    }
#endif
    #endregion

    #region 下拉候选列表
    /// <summary>
    /// 懒加载全部下拉候选(生物/NPC/稀有度)，并按当前选中项初始化下拉按钮显示文本
    /// </summary>
    private void EnsureOptions()
    {
        if (listCreatureOptions == null)
        {
            listCreatureOptions = new List<SelectItem>();
            foreach (var creatureInfo in CreatureInfoCfg.GetAllArrayData())
            {
                string name = creatureInfo.name_language;
                listCreatureOptions.Add(new SelectItem(creatureInfo.id, name.IsNull() ? $"{creatureInfo.id}" : $"{creatureInfo.id} {name}"));
            }
            creatureDropdownLabel = FindOptionLabel(listCreatureOptions, currentCreatureId, creatureDropdownLabel);
        }
        if (listNpcOptions == null)
        {
            listNpcOptions = new List<SelectItem>();
            foreach (var npcInfo in NpcInfoCfg.GetAllArrayData())
            {
                //随机议员等无名字NPC回退显示备注, 均无则只显示id
                string name = npcInfo.name_language;
                if (name.IsNull()) name = npcInfo.remark;
                listNpcOptions.Add(new SelectItem(npcInfo.id, name.IsNull() ? $"{npcInfo.id}" : $"{npcInfo.id} {name}"));
            }
            npcDropdownLabel = FindOptionLabel(listNpcOptions, currentNpcId, npcDropdownLabel);
        }
        if (listRarityOptions == null)
        {
            listRarityOptions = new List<SelectItem>();
            foreach (var rarityInfo in RarityInfoCfg.GetAllArrayData())
            {
                string name = rarityInfo.name_language;
                if (name.IsNull()) name = rarityInfo.remark;
                listRarityOptions.Add(new SelectItem(rarityInfo.id, name.IsNull() ? $"稀有度{rarityInfo.id}" : $"{rarityInfo.id} {name}"));
            }
            rarityDropdownLabel = FindOptionLabel(listRarityOptions, rarity, rarityDropdownLabel);
        }
    }

    /// <summary>
    /// 在候选列表中查找指定id的显示名(找不到时保留原文本)
    /// </summary>
    private string FindOptionLabel(List<SelectItem> options, long id, string defaultLabel)
    {
        foreach (var option in options)
        {
            if (option.id == id) return option.label;
        }
        return defaultLabel;
    }
    #endregion

    #region GUI绘制
    /// <summary>
    /// IMGUI入口，绘制纯代码创建的卡片编辑器控制面板
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();
        EnsureOptions();

        float panelWidth = Mathf.Min(PanelWidth, Screen.width - 20);
        GUILayout.BeginArea(new Rect(10, 10, panelWidth, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);
        GUILayout.Label("生物卡片编辑器", titleStyle);
        GUILayout.Space(4);

        DrawSourceSelector();
        GUILayout.Space(6);
        DrawRaritySelector();
        DrawLevelSlider();
        DrawScaleSlider();
        GUILayout.Space(6);
        DrawColorEditors();
        GUILayout.Space(6);
        DrawSaveAndClose();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        //变更检测: 数据参数变更重建生物刷新卡片; 颜色/缩放变更只做轻量覆盖(不重建Spine)
        if (GUI.changed)
        {
            long newDataKey = ComputeDataKey();
            if (newDataKey != lastDataKey)
            {
                lastDataKey = newDataKey;
                ReadConfigColors(false, false, false);
                RefreshCards();
            }
            ApplyCardScale();
            ApplyCustomColors();
        }
    }

    /// <summary>
    /// 绘制数据来源选择(生物/NPC 切换 + 下拉 + 手动ID)
    /// </summary>
    private void DrawSourceSelector()
    {
        //来源类型切换
        GUILayout.BeginHorizontal();
        GUILayout.Label("来源", labelStyle, GUILayout.Width(60));
        int newSourceType = GUILayout.Toolbar(sourceType, new string[] { "生物", "NPC" }, GUILayout.Height(26));
        if (newSourceType != sourceType)
        {
            sourceType = newSourceType;
            isTargetDropdownOpen = false;
        }
        GUILayout.EndHorizontal();

        //手动ID(空=用下拉)
        GUILayout.BeginHorizontal();
        GUILayout.Label("手动ID", labelStyle, GUILayout.Width(60));
        inputManualId = GUILayout.TextField(inputManualId, GUILayout.Height(26), GUILayout.Width(140));
        GUILayout.Label("(空=用下拉)", hintStyle);
        GUILayout.EndHorizontal();

        //目标下拉(生物或NPC)
        bool isNpc = sourceType == 1;
        string targetTypeLabel = isNpc ? "NPC" : "生物";
        string currentLabel = isNpc ? npcDropdownLabel : creatureDropdownLabel;
        List<SelectItem> targetOptions = isNpc ? listNpcOptions : listCreatureOptions;
        GUILayout.BeginHorizontal();
        GUILayout.Label(targetTypeLabel, labelStyle, GUILayout.Width(60));
        if (GUILayout.Button(currentLabel, buttonLeftStyle, GUILayout.Height(26)))
        {
            isTargetDropdownOpen = !isTargetDropdownOpen;
            isRarityDropdownOpen = false;
        }
        GUILayout.EndHorizontal();

        //下拉展开候选列表(当前选中项高亮)
        if (isTargetDropdownOpen)
        {
            long selectedId = isNpc ? currentNpcId : currentCreatureId;
            scrollTargetDropdown = GUILayout.BeginScrollView(scrollTargetDropdown, GUI.skin.box, GUILayout.Height(300));
            foreach (var option in targetOptions)
            {
                bool isCurrent = option.id == selectedId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    if (isNpc) { currentNpcId = option.id; npcDropdownLabel = option.label; }
                    else { currentCreatureId = option.id; creatureDropdownLabel = option.label; }
                    isTargetDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }

        //手动输入了非法/不存在的id时提示
        if (!inputManualId.IsNull())
        {
            bool isValidManual = long.TryParse(inputManualId, out long manualId)
                && (isNpc ? NpcInfoCfg.GetItemData(manualId) != null : CreatureInfoCfg.GetItemData(manualId) != null);
            if (!isValidManual)
                GUILayout.Label("⚠ 手动ID无效或不在配置表，将使用下拉选择", hintStyle);
        }
    }

    /// <summary>
    /// 绘制稀有度下拉(含999魔王配色档, 取自稀有度配置表全量行)
    /// </summary>
    private void DrawRaritySelector()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("稀有度", labelStyle, GUILayout.Width(60));
        if (GUILayout.Button(rarityDropdownLabel, buttonLeftStyle, GUILayout.Height(26)))
        {
            isRarityDropdownOpen = !isRarityDropdownOpen;
            isTargetDropdownOpen = false;
        }
        GUILayout.EndHorizontal();

        if (isRarityDropdownOpen)
        {
            scrollRarityDropdown = GUILayout.BeginScrollView(scrollRarityDropdown, GUI.skin.box, GUILayout.Height(200));
            foreach (var option in listRarityOptions)
            {
                bool isCurrent = option.id == rarity;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    rarity = (int)option.id;
                    rarityDropdownLabel = option.label;
                    isRarityDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 绘制等级滑条(0~10级, 0级无颜色配置固定白色)
    /// </summary>
    private void DrawLevelSlider()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("等级", labelStyle, GUILayout.Width(60));
        level = (int)GUILayout.HorizontalSlider(level, 0, LevelMax, GUILayout.Height(26));
        GUILayout.Label($"{level}", labelStyle, GUILayout.Width(30));
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制卡片缩放滑条(0.5~2倍)
    /// </summary>
    private void DrawScaleSlider()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("缩放", labelStyle, GUILayout.Width(60));
        cardScale = GUILayout.HorizontalSlider(cardScale, 0.5f, 2f, GUILayout.Height(26));
        GUILayout.Label($"{cardScale:F2}x", labelStyle, GUILayout.Width(46));
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制颜色编辑区(稀有度主板/副板色支持渐变, 等级色单色; 勾选自定义后覆盖配置色显示)
    /// </summary>
    private void DrawColorEditors()
    {
        GUILayout.Label("—— 显示颜色（勾选后覆盖配置色）——", hintStyle);
        DrawBoardColorEditor("主板色", ref customBoardColor, boardStartState, boardEndState, ref boardGradient);
        DrawBoardColorEditor("副板色", ref customOtherColor, otherStartState, otherEndState, ref otherGradient);
        DrawLevelColorEditor();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("读取配置色", GUILayout.Height(26)))
        {
            ReadConfigColors(true, true, true);
            RefreshCards();
        }
        GUILayout.Label("放弃修改, 还原为配置表颜色", hintStyle);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制一组板色编辑器(启用开关 + 渐变开关 + 起点/终点颜色编辑)
    /// </summary>
    private void DrawBoardColorEditor(string title, ref bool customEnabled, ColorEditState startState, ColorEditState endState, ref bool isGradient)
    {
        GUILayout.BeginHorizontal();
        customEnabled = GUILayout.Toggle(customEnabled, $" {title}", labelStyle, GUILayout.Width(90));
        isGradient = GUILayout.Toggle(isGradient, " 渐变", labelStyle, GUILayout.Width(60));
        //色块预览(渐变时显示起止两个色块)
        DrawColorBlock(startState.color);
        if (isGradient) DrawColorBlock(endState.color);
        GUILayout.EndHorizontal();
        if (customEnabled)
        {
            DrawColorEditor(isGradient ? "起点" : "颜色", startState);
            if (isGradient)
                DrawColorEditor("终点", endState);
        }
    }

    /// <summary>
    /// 绘制等级颜色编辑器(单色; 0级无配置不可编辑)
    /// </summary>
    private void DrawLevelColorEditor()
    {
        GUILayout.BeginHorizontal();
        bool canEdit = level >= 1;
        GUI.enabled = canEdit;
        customLevelColor = GUILayout.Toggle(customLevelColor && canEdit, " 等级色", labelStyle, GUILayout.Width(90));
        GUI.enabled = true;
        DrawColorBlock(levelColorState.color);
        if (!canEdit) GUILayout.Label("(0级固定白色)", hintStyle);
        GUILayout.EndHorizontal();
        if (customLevelColor && canEdit)
        {
            DrawColorEditor("颜色", levelColorState);
        }
    }

    /// <summary>
    /// 绘制单个颜色的完整编辑区：Hex 输入行 + RGB 滑条/数值输入行 + 调色盘
    /// </summary>
    private void DrawColorEditor(string title, ColorEditState state)
    {
        //行1: 标题 + 色块预览 + Hex 输入(支持带不带#前缀, 合法即应用)
        GUILayout.BeginHorizontal();
        GUILayout.Label(title, hintStyle, GUILayout.Width(36));
        DrawColorBlock(state.color);
        GUILayout.Label("Hex", hintStyle, GUILayout.Width(26));
        string newHex = GUILayout.TextField(state.hexInput, GUILayout.Height(20), GUILayout.Width(72));
        if (newHex != state.hexInput)
        {
            state.hexInput = newHex;
            state.ApplyHexInput();
        }
        GUILayout.EndHorizontal();
        //行2: RGB 滑条 + 数值输入(0~255)
        DrawColorChannelEditor("R", state.color.r, state.rInput, (newValue, newText) =>
        {
            state.rInput = newText;
            if (newValue.HasValue) { Color c = state.color; c.r = newValue.Value; state.SetColor(c); }
            else state.ApplyRgbInput();
        });
        DrawColorChannelEditor("G", state.color.g, state.gInput, (newValue, newText) =>
        {
            state.gInput = newText;
            if (newValue.HasValue) { Color c = state.color; c.g = newValue.Value; state.SetColor(c); }
            else state.ApplyRgbInput();
        });
        DrawColorChannelEditor("B", state.color.b, state.bInput, (newValue, newText) =>
        {
            state.bInput = newText;
            if (newValue.HasValue) { Color c = state.color; c.b = newValue.Value; state.SetColor(c); }
            else state.ApplyRgbInput();
        });
        //行3: 调色盘
        DrawColorPalette(state);
    }

    /// <summary>
    /// 绘制单个颜色通道的滑条+数值输入组合控件(两个控件恒绘制, 防事件间控件数不一致报 Mismatched LayoutGroup)
    /// </summary>
    /// <param name="channelName">通道名(R/G/B)</param>
    /// <param name="channelValue">当前通道值(0~1)</param>
    /// <param name="channelText">当前数值输入框文本(0~255)</param>
    /// <param name="onChange">变更回调: 滑条拖动传 newValue(数值框同步); 文本输入传 newText(解析后应用)</param>
    private void DrawColorChannelEditor(string channelName, float channelValue, string channelText, System.Action<float?, string> onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(36);
        GUILayout.Label(channelName, hintStyle, GUILayout.Width(12));
        //滑条(0~1)
        float newValue = GUILayout.HorizontalSlider(channelValue, 0f, 1f, GUILayout.Height(20));
        bool sliderChanged = !Mathf.Approximately(newValue, channelValue);
        //数值输入框(0~255); 滑条刚拖动的帧强制显示滑条换算值, 避免文本框残留旧值
        string newText = GUILayout.TextField(sliderChanged ? $"{Mathf.RoundToInt(newValue * 255)}" : channelText, GUILayout.Height(20), GUILayout.Width(36));
        if (sliderChanged)
            onChange(newValue, newText);
        else if (newText != channelText)
            onChange(null, newText);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 调色盘区：16 色预设色块点选即应用(8列网格, 当前颜色所在色块显示✔)
    /// </summary>
    private void DrawColorPalette(ColorEditState state)
    {
        const int paletteColumns = 8;
        for (int i = 0; i < paletteColors.Length; i++)
        {
            if (i % paletteColumns == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(36);
            }
            Color paletteColor = paletteColors[i];
            bool isCurrentColor = IsApproximatelyColor(state.color, paletteColor);
            //用backgroundColor给空按钮着色成色块
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = paletteColor;
            if (GUILayout.Button(isCurrentColor ? "✔" : "", GUILayout.Width(30), GUILayout.Height(24)))
            {
                GUI.backgroundColor = oldBgColor;
                state.SetColor(paletteColor);
            }
            GUI.backgroundColor = oldBgColor;
            if (i % paletteColumns == paletteColumns - 1 || i == paletteColors.Length - 1)
                GUILayout.EndHorizontal();
        }
    }

    /// <summary>调色盘预设色(与 NpcCreateEditorWindow 皮肤调色盘同一套16色)</summary>
    private static readonly Color[] paletteColors =
    {
        new Color(1.00f, 1.00f, 1.00f), //白
        new Color(0.75f, 0.75f, 0.78f), //银灰
        new Color(0.45f, 0.45f, 0.48f), //灰
        new Color(0.10f, 0.10f, 0.12f), //黑
        new Color(0.35f, 0.20f, 0.10f), //深棕
        new Color(0.55f, 0.35f, 0.18f), //棕
        new Color(0.85f, 0.65f, 0.35f), //亚麻
        new Color(0.95f, 0.85f, 0.45f), //金
        new Color(0.85f, 0.30f, 0.15f), //橙红
        new Color(0.70f, 0.15f, 0.15f), //红
        new Color(0.55f, 0.10f, 0.20f), //酒红
        new Color(0.95f, 0.55f, 0.65f), //粉
        new Color(0.60f, 0.30f, 0.70f), //紫
        new Color(0.25f, 0.35f, 0.75f), //蓝
        new Color(0.30f, 0.70f, 0.80f), //青
        new Color(0.25f, 0.60f, 0.35f), //绿
    };

    /// <summary>
    /// 判断两个颜色RGB是否近似相等（用于调色盘当前色高亮）
    /// </summary>
    private bool IsApproximatelyColor(Color colorA, Color colorB)
    {
        return Mathf.Approximately(colorA.r, colorB.r)
            && Mathf.Approximately(colorA.g, colorB.g)
            && Mathf.Approximately(colorA.b, colorB.b);
    }

    /// <summary>
    /// 绘制颜色预览色块
    /// </summary>
    private void DrawColorBlock(Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUILayout.Label(GUIContent.none, GUI.skin.box, GUILayout.Width(28), GUILayout.Height(20));
        GUI.color = oldColor;
    }

    /// <summary>
    /// 绘制保存与关闭按钮区
    /// </summary>
    private void DrawSaveAndClose()
    {
#if UNITY_EDITOR
        GUI.backgroundColor = new Color(0.9f, 0.75f, 0.4f);
        if (GUILayout.Button("💾 保存颜色到配置表", GUILayout.Height(30)))
            SaveColorsToExcel();
        GUI.backgroundColor = Color.white;
        GUILayout.Label("写回当前稀有度行(主板/副板色)与当前等级行(等级色), 再生JSON后立即生效", hintStyle);
        if (!saveHint.IsNull() && Time.unscaledTime < saveHintTime)
            GUILayout.Label($"✔ {saveHint}", hintStyle);
        GUILayout.Space(4);
#endif
        if (GUILayout.Button("关闭", GUILayout.Height(26)))
            Destroy(gameObject);
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitGUIStyle()
    {
        if (guiStyleInited) return;
        guiStyleInited = true;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        //按钮文本左对齐(下拉按钮/选项按钮显示 id+名字 长文本用)
        buttonLeftStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 0, 0) };
    }
    #endregion
}
