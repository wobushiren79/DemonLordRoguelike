using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using OfficeOpenXml;
#endif

/// <summary>
/// Mod 幻化药测试面板（GUI版，纯代码控制面板 + 真实卡片预制体，不依赖测试预制），四个页签：
/// 单个预览=下拉选药, 小卡=Chess基础形象/大卡=Avator高清/场景并排左基础右幻化(世界空间对比)；
/// 小卡列表/大卡列表/场景列表=分页网格一次展示多个幻化药, 悬停目标后滚轮改缩放/拖拽改位置; 项下小按钮[复制][粘贴]走参数剪贴板快速套用,
/// 小卡/大卡列表项另带[详情]按钮=打开游戏生物展示弹窗 UIDialogCreatureShow 查看幻化形象与骨架全部动画(点动画名即播放;
/// 小卡列表=Chess 基础形象(show 骨架, isShowChessSpine), 大卡列表=ui_show 高清形象),
/// 打开期间进入详情模式隐藏测试面板(覆盖层Canvas sortingOrder=5000 且 IMGUI 恒渲染在最上层, 不隐藏会遮挡弹窗), 弹窗销毁回调自动恢复;
/// 场景列表另有「粘贴剪贴板到全部场景项」按钮=一键把剪贴板 world_data 套用到当前筛选(Mod筛选)全部项(同MOD骨架大小相近, 免逐项粘贴)。
/// 实现=给基础生物设置 transformItemId 后走真实卡片 SetData / SetCreatureData 链（与魔物管理吃幻化药同路径）。
/// 调参(仅编辑器)：文本框精输 + 滑动条粗调(缩放对数映射) + 悬停滚轮/拖拽, 经 TransformPotionUITestOverride 覆盖层实时生效；
/// 数据键: show_data=小卡默认展示尺寸, ui_show_data=大卡详情尺寸, world_data=世界显示尺寸/偏移(战斗/基地等 SkeletonAnimation 消费),
/// show_brightness=场景亮度系数(1=原亮度,<1调暗,>1调亮,域(0,2]; 场景列表 Alt+滚轮调, 消费=SetCreatureData→SpineHandler.ApplySceneDimOverride, UI 不消费)。
/// 场景列表另带「测试场景」区: 循环加载 FightSceneCfg 各行真实场景预制体(森林/沙漠/皇宫/平原各变体)并还原光照(天空盒/雾/环境光/Details显隐,
/// 体积雾/景深不还原), 卸载时还原面板原环境, 关面板自动卸载。
/// 「保存全部修改」一键批量写回 Mod道具Excel(唯一真实源, 会话首写前备份到 Mod项目/ExcelBackup, 滚动复用.bak.1~3只留最近3份) + Mod项目与主项目部署副本两处 ItemsInfo.txt + 当前会话内存。
/// 由 LauncherTest.StartForTransformPotionTest 挂到空物体上启动。
/// </summary>
public class TestTransformPotionGUI : MonoBehaviour
{
    #region 常量

    private const string PathCardItemPrefab = "UI/Common/UIViewCreatureCardItem";       //卡片项预制(Resources路径)
    private const string PathCardDetailsPrefab = "UI/Common/UIViewCreatureCardDetails"; //卡片详情预制(Resources路径)
    private const long BaseCreatureId = 2001;           //基础生物id(幻化只换形象, 任意生物均可, 与卡片编辑器默认一致)
    private const int CanvasSortOrder = 5000;           //覆盖层Canvas层级(压过游戏UI)
    private const float PanelWidth = 400;               //左侧IMGUI控制面板宽度
    private const long ModIdDivisor = 100000000000000L; //ModID除数(10^14，与 BaseBean.CombineModId 拼接规则一致, 用于从完整id拆自ID)

    private static readonly Vector3 SceneCameraPos = new Vector3(0, 3.6f, -6f);  //场景展示相机位置(与卡片编辑器同机位: 侧视微俯视)
    private static readonly Vector3 SceneCameraLookAt = new Vector3(0, 2.9f, 0);  //场景展示相机注视点(把模型框到屏幕底部, 避开中间卡片)
    private const float SceneSpineOffsetX = 1.1f;   //场景模型相对中心点的横向偏移(基础在左/幻化在右)
    private const float BrightnessStep = 0.02f;     //亮度调节步进(场景列表 Alt+滚轮, show_brightness 系数)
    private const float BrightnessMin = 0.3f;       //亮度系数下限(防误滚到全黑)
    private const float BrightnessMax = 2f;         //亮度系数上限(1=原亮度, >1调亮; shader _Brightness 属性域 Range(0,2))

    //列表布局: 间距与横竖个数(用户可调, EditorPrefs 持久化跨会话保持, 见 DrawGridSizeRow 布局调整行)
    private const float DragThresholdPx = 4f;                                //拖拽生效阈值(防误触微小拖动)

    #endregion

    #region 数据字段

    /// <summary>当前面板实例(供入口防重复创建与 LauncherTest 清理, 面板销毁时置空)</summary>
    public static TestTransformPotionGUI Instance;

    private Canvas canvas;                          //卡片显示用覆盖层Canvas(随面板销毁)
    private UIViewCreatureCardItem cardItem;        //小卡实例(单个预览页, 真实预制体, 显示Chess基础形象)
    private UIViewCreatureCardDetails cardDetails;  //大卡详情实例(单个预览页, 真实预制体, 显示Avator高清形象)

    private GameObject sceneRoot;                   //场景Spine展示根节点(单个预览页, 世界空间, OnDestroy统一销毁)
    private GameObject scenePlaneObj;               //场景地平面(大小对比的地面基准)
    private SkeletonAnimation sceneSpineBase;       //场景显示-基础原形象(左, transformItemId=0 对比基准)
    private SkeletonAnimation sceneSpineTransform;  //场景显示-幻化形象(右, 当前选中药)
    private bool showSceneSpine = true;             //是否显示场景Spine展示

    private long initItemId;                        //入口传入的初始幻化药id(完整id, 0=默认第一个)
    private long currentPotionItemId;               //当前选中的幻化药id(完整id, 0=无幻化显示原形象)

    //下拉候选(懒加载)
    private List<SelectItem> listPotionOptions;     //单个预览下拉候选(首项=无幻化)
    private List<SelectItem> listAllPotions;        //列表页数据源(全部幻化药, 不含无幻化)
    private bool isDropdownOpen;
    private Vector2 scrollDropdown;
    private string potionDropdownLabel = "请选择幻化药";

    private long lastDataKey = -1;                  //上一帧的幻化药id指纹, 变更才重建生物数据刷新卡片

    private CreatureBean currentCreature;           //当前卡片显示的生物数据(调参直接应用尺寸到图标用)

    //页签与列表状态
    private PanelTab currentTab = PanelTab.Single;  //当前页签
    private int pageChess, pageShow, pageScene;     //各列表页当前页码(0起)
    private string pageJumpBuffer = "";             //页码跳转输入框缓冲
    private GameObject listRoot;                    //卡片列表容器(覆盖层Canvas子节点, 换页/换页签重建)
    private GameObject sceneListRoot;               //场景列表容器(世界空间, 换页/换页签重建)
    private GameObject testSceneObj;                //测试场景实例(场景列表页签加载的游戏场景prefab, 切换/卸载/关面板时销毁)
    private int testSceneIndex = -1;                //当前测试场景索引(-1=无场景, 否则 testSceneRows 下标)
    private bool testSceneLoading;                  //测试场景加载中(防连点重入)
    private List<TestSceneOption> testSceneRows;    //测试场景候选(首项=基地特殊档, 其后 FightSceneCfg 全量按 id 排序; 懒加载)
    private bool isSceneDropdownOpen;               //测试场景下拉展开状态
    private Vector2 scrollSceneDropdown;            //测试场景下拉滚动位置
    private bool hasCacheTestEnv;                   //是否已缓存面板环境(首个场景加载时缓存, 卸载还原用)
    private Color cacheEnvAmbient;                  //缓存: 原全局环境光
    private Material cacheEnvSkybox;                //缓存: 原天空盒材质
    private bool cacheEnvFog;                       //缓存: 原雾开关
    private Color cacheEnvFogColor;                 //缓存: 原雾颜色
    private float cacheEnvFogStart;                 //缓存: 原雾起始距离
    private float cacheEnvFogEnd;                   //缓存: 原雾结束距离
    private FogMode cacheEnvFogMode;                //缓存: 原雾模式
    private CameraClearFlags cacheEnvCamClearFlags; //缓存: 原主相机清屏标志
    private Color cacheEnvCamBgColor;               //缓存: 原主相机背景色
    private Light[] cacheEnvLights;                 //缓存: 面板场景的原有灯光(加载测试场景时禁用, 防与场景自带灯光叠加双份光照)
    private bool[] cacheEnvLightsEnabled;           //缓存: 各原有灯各自的开关状态(卸载时按原状恢复)
    private readonly List<ListItem> listItems = new List<ListItem>(); //当前列表页可见项
    private UIDialogCreatureShow creatureShowDialog;    //当前打开的生物展示弹窗(列表项[详情]按钮; 打开期间为详情模式=隐藏测试面板防遮挡, 弹窗销毁回调恢复)

    //列表布局/筛选设置(static=本次Play会话内重开面板保持, 项目内JSON持久化随git共享)
    private static int chessCols = 5, chessRows = 2;    //小卡列表 列x行
    private static int showCols = 2, showRows = 1;      //大卡列表 列x行
    private static int sceneCols = 6;                   //场景列表 列(每页数量)
    private static float chessCellX = 370, chessCellY = 500;   //小卡网格间距
    private static float showCellX = 920, showCellY = 980;     //大卡网格间距
    private static float sceneSpacing = 1.5f;                  //场景模型世界间距
    private static int modFilterIndex;                  //Mod筛选当前选中(0=全部,1=游戏本地,≥2=对应Mod)

    private List<SelectItem> listModFilterOptions;      //Mod筛选候选(全部/游戏本地/各已加载Mod)
    private bool isModFilterOpen;                       //Mod筛选下拉展开中

    #endregion

    #region 枚举与内部类

    /// <summary>面板页签</summary>
    private enum PanelTab { Single, ChessList, ShowList, SceneList }

    /// <summary>调参数据段(对应 other_data 三个尺寸键: show_data=默认展示小卡尺寸, ui_show_data=详情UI尺寸, world_data=世界显示)</summary>
    private enum DataKind { Show, UiShow, World }

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

    /// <summary>测试场景候选项（fightScene=null=基地特殊档）</summary>
    private class TestSceneOption
    {
        public string label;                //显示名（基地 / FightScene 行的 remark）
        public FightSceneBean fightScene;   //战斗场景配置行（null=基地）
    }

    /// <summary>列表可见项(卡片列表=卡根+图标, 场景列表=摆放根+Renderer子节点+基础缩放; 悬停交互与直接应用用)</summary>
    private class ListItem    {
        public long potionId;
        public string label;
        public RectTransform cardRoot;      //卡片列表: 卡根RectTransform
        public SkeletonGraphic cardIcon;    //卡片列表: 卡内生物图标
        public GameObject sceneObj;         //场景列表: 摆放根(脚下踩地)
        public Transform sceneRenderer;     //场景列表: Renderer子节点(缩放/偏移落点)
        public float sceneBaseScale;        //场景列表: 基础缩放(size_spine×体型倍率)
        public bool editable = true;        //可否编辑(大卡列表: 任一展示资源存在即可调——无 ui_show_res 时详情UI回落 show 形象, ui_show_data 仍被消费)
        public bool hasUiShowRes = true;    //是否配置 ui_show_res(仅标签提示用: 无Avator=详情UI显示回落的 show 形象)
        public bool hasShowRes = true;      //是否配置 show_res(场景列表用: 无则世界显示不幻化=world_data 不被真实链消费, 不可调并标注"(无世界幻化)")
    }

    #endregion

    #region GUI样式

    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, hintStyle, buttonLeftStyle, labelCenterStyle, smallButtonStyle;
    private Vector2 scrollMain;

    #endregion

#if UNITY_EDITOR
    #region 调参数据字段(仅编辑器)

    //调参编辑缓冲(文本框内容, 切换幻化药/保存/重置时从配置+覆盖层重载)
    private string editShowScale, editShowX, editShowY;  //小卡(Chess)调参缓冲
    private string editUiShowScale, editUiShowX, editUiShowY;     //详情(Avator)调参缓冲
    private string editWorldScale, editWorldX, editWorldY;  //场景(world_data)调参缓冲
    private string appliedShowData, appliedUiShowData, appliedWorldData; //已应用的「scale;x,y」(变更检测用)
    private bool hasUiShowRes;                              //当前药是否配置 ui_show_res
    private bool hasShowRes;                                //当前药是否配置 show_res(世界幻化资源; 场景/world_data 调参可用性——无则真实链 SetCreatureData 的 hasTransform 门控不消费 world_data)
    private bool hasDetailAdjust;                           //详情调参组可用性(ui_show_res 或 show_res 任一即可——无 ui_show_res 时详情UI回落 show 形象, ui_show_data 仍被消费)
    private string adjustError;                             //调参输入错误提示(红字)
    private string saveMessage;                             //保存结果提示
    private bool saveMessageIsError;                        //保存结果提示是否为错误(红/绿)
    private ListItem dragItem;                              //当前拖拽的列表项(null=未拖拽)
    private DataKind dragKind;                              //当前拖拽的数据段
    private bool dragActivated;                             //拖拽是否已过阈值生效(未过阈值不移动, 防误触)
    private Vector2 dragStartMouse, dragStartPos;           //拖拽起点(GUI坐标/起始数据pos)
    private static readonly HashSet<string> excelBackupDonePaths = new HashSet<string>(); //本次Play会话已备份过的Mod道具Excel路径(每次会话每文件首次写入前备份一次)

    //参数剪贴板(static=会话内保持; [复制]存入当前项参数, [粘贴]套用到其他同段项; clipKind=数据段标签, 跨段粘贴拒绝——show/ui_show=绝对UI缩放与坐标、world=相对倍率与世界坐标, 单位不同串段粘贴必错)
    private static bool clipHasValue;
    private static float clipScale;
    private static Vector2 clipPos;
    private static DataKind clipKind;

    //滑动条量程: 缩放=对数映射0.01~20(兼容小卡~3.75与详情~0.19两个量级, 手感与滚轮等比一致); 位置=线性±600(超出量程的文本值不被滑条覆盖, 抓握即拉回); 世界位置=线性±2(世界单位)
    private const float ScaleSliderMin = 0.01f;
    private const float ScaleSliderMax = 20f;
    private const float PosSliderMin = -600f;
    private const float PosSliderMax = 600f;
    private const float WorldPosSliderMin = -2f;
    private const float WorldPosSliderMax = 2f;

    #endregion
#endif

    #region 生命周期

    /// <summary>
    /// 初始化：创建覆盖层Canvas并实例化真实卡片预制体，创建场景对比spine，随后按初始幻化药刷新卡片
    /// </summary>
    private void Start()
    {
        Instance = this;
#if UNITY_EDITOR
        LoadLayoutPrefs();
#endif
        CreateCards();
        CreateSceneSpines();
        //入口未指定幻化药时默认选第一个候选(无幻化=0 不默认)
        EnsureOptions();
        currentPotionItemId = initItemId != 0 ? initItemId : (listPotionOptions.Count > 1 ? listPotionOptions[1].id : 0);
        SyncDropdownLabel();
#if UNITY_EDITOR
        LoadEditBuffers();
#endif
        RefreshCards();
        SetTab(PanelTab.Single);
    }

    /// <summary>
    /// 销毁时置空实例引用并清理场景Spine展示与列表容器(Canvas为子物体随之销毁)
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        //详情弹窗随面板关闭(防切换测试模块时弹窗残留; 其销毁回调里的恢复详情模式对已销毁canvas判空安全)
        if (creatureShowDialog != null)
        {
            creatureShowDialog.DestroyDialog();
            creatureShowDialog = null;
        }
        if (sceneRoot != null) Destroy(sceneRoot);
        if (scenePlaneObj != null) Destroy(scenePlaneObj);
        if (sceneListRoot != null) Destroy(sceneListRoot);
#if UNITY_EDITOR
        UnloadTestScene();
#endif
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 设置初始数据(由 LauncherTest 传入 GameTestEditor 面板选中的幻化药id)
    /// </summary>
    /// <param name="itemId">幻化药道具完整id(0=默认第一个候选)</param>
    public void SetInitData(long itemId)
    {
        initItemId = itemId;
    }

    /// <summary>
    /// 创建覆盖层Canvas并实例化小卡/大卡详情两个真实预制体，摆到屏幕中央左右(与卡片编辑器同款布局)
    /// </summary>
    private void CreateCards()
    {
        GameObject canvasObj = new GameObject("TransformPotionTestCanvas");
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
    }

    #endregion

    #region 页签切换

    /// <summary>
    /// 切换页签：单个预览页显示小卡/大卡/场景对比; 列表页重建对应网格(卡片列表容器在Canvas下, 场景列表容器在世界空间)
    /// </summary>
    private void SetTab(PanelTab tab)
    {
        currentTab = tab;
        bool isSingle = tab == PanelTab.Single;
        if (cardItem != null) cardItem.gameObject.SetActive(isSingle);
        if (cardDetails != null) cardDetails.gameObject.SetActive(isSingle);
        if (sceneRoot != null) sceneRoot.SetActive(isSingle && showSceneSpine);
        if (scenePlaneObj != null) scenePlaneObj.SetActive((isSingle && showSceneSpine) || tab == PanelTab.SceneList);
        DestroyListView();
        if (!isSingle) BuildListView();
    }

    /// <summary>
    /// 绘制页签栏(当前页签高亮)
    /// </summary>
    private void DrawTabBar()
    {
        GUILayout.BeginHorizontal();
        DrawTabButton(PanelTab.Single, "单个预览");
        DrawTabButton(PanelTab.ChessList, "小卡列表");
        DrawTabButton(PanelTab.ShowList, "大卡列表");
        DrawTabButton(PanelTab.SceneList, "场景列表");
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制单个页签按钮(选中绿色高亮)
    /// </summary>
    private void DrawTabButton(PanelTab tab, string name)
    {
        Color oldColor = GUI.color;
        if (currentTab == tab) GUI.color = Color.green;
        if (GUILayout.Button(name, GUILayout.Height(24)))
        {
            GUI.color = oldColor;
            SetTab(tab);
            return;
        }
        GUI.color = oldColor;
    }

    #endregion

    #region 场景Spine显示(单个预览)

    /// <summary>
    /// 创建场景Spine展示：地平面 + 左基础原形象(transformItemId=0 对比基准) + 主相机取景(与卡片编辑器同机位, 模型框到屏幕底部避开中间卡片)；
    /// 右幻化形象在首次刷新时懒创建(均走真实 SetCreatureData 链)
    /// </summary>
    private void CreateSceneSpines()
    {
        //地平面(大小对比的地面基准, 与卡片编辑器/特效测试同款10x10平面, 摆原点即顶面高度0)
        scenePlaneObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        scenePlaneObj.name = "TransformPotionTestPlane";
        //场景模型根节点(世界空间, 独立于覆盖层Canvas)
        sceneRoot = new GameObject("TransformPotionTestSceneRoot");
        CreatureBean baseCreature = BuildCreature(0);
        if (baseCreature == null) return;
        sceneSpineBase = CreateSceneSpine("Base", -SceneSpineOffsetX, baseCreature);
        //主相机: 隐藏虚拟相机、激活主相机并关闭切换动画(与卡片编辑器/特效测试同套逻辑), 再摆到同款取景位
        CameraManager cameraManager = CameraHandler.Instance.manager;
        if (cameraManager.mainCamera != null)
        {
            cameraManager.HideAllCM();
            cameraManager.mainCamera.gameObject.SetActive(true);
            cameraManager.SetMainCameraDefaultBlend(0);
            cameraManager.mainCamera.transform.position = SceneCameraPos;
            cameraManager.mainCamera.transform.LookAt(SceneCameraLookAt);
        }
    }

    /// <summary>
    /// 在场景根节点下创建一个世界空间生物Spine模型(走真实 SetCreatureData 链: 幻化整骨替换+皮肤+缩放=size_spine×体型倍率×world_data倍率)，并循环播放待机；
    /// 根节点负责摆放、SkeletonAnimation 挂子节点 Renderer(SetCreatureData 对 spine 节点的缩放/位置管理不干扰根节点摆放)
    /// </summary>
    /// <param name="name">节点名后缀</param>
    /// <param name="posX">横向摆放坐标(脚下踩地平面y=0)</param>
    /// <param name="creatureData">生物数据</param>
    private SkeletonAnimation CreateSceneSpine(string name, float posX, CreatureBean creatureData)
    {
        GameObject spineObj = new GameObject($"SceneSpine_{name}");
        spineObj.transform.SetParent(sceneRoot.transform, false);
        spineObj.transform.localPosition = new Vector3(posX, 0, 0);
        GameObject rendererObj = new GameObject("Renderer");
        rendererObj.transform.SetParent(spineObj.transform, false);
        SkeletonAnimation spine = SpineHandler.Instance.AddSkeletonAnimation(rendererObj, creatureData.creatureModel.res_name);
        CreatureHandler.Instance.SetCreatureData(spine, creatureData);
        SpineHandler.Instance.PlayAnim(spine, SpineAnimationStateEnum.Idle, creatureData, true);
        return spine;
    }

    /// <summary>
    /// 刷新场景对比显示(幻化药变更时随卡片一起调用)：左基础原形象固定不动, 只换右侧当前幻化药形象(换骨/换肤/重置缩放)
    /// </summary>
    /// <param name="transformCreature">当前幻化药生物数据(与卡片同一份)</param>
    private void RefreshSceneSpines(CreatureBean transformCreature)
    {
        if (sceneRoot == null || transformCreature == null) return;
        if (sceneSpineTransform == null)
        {
            sceneSpineTransform = CreateSceneSpine("Transform", SceneSpineOffsetX, transformCreature);
            return;
        }
        CreatureHandler.Instance.SetCreatureData(sceneSpineTransform, transformCreature);
        //幻化骨架不指定动画名, 由框架按目标骨架实际动画列表解析(缺失仅日志不播)
        SpineHandler.Instance.PlayAnim(sceneSpineTransform, SpineAnimationStateEnum.Idle, transformCreature, true);
    }

    /// <summary>
    /// 绘制场景Spine展示区(显示开关 + 左右位说明)
    /// </summary>
    private void DrawSceneSpineSection()
    {
        GUILayout.BeginHorizontal();
        bool newShow = GUILayout.Toggle(showSceneSpine, " 场景Spine", labelStyle, GUILayout.Width(100));
        if (newShow != showSceneSpine)
        {
            showSceneSpine = newShow;
            if (sceneRoot != null) sceneRoot.SetActive(showSceneSpine);
            if (scenePlaneObj != null) scenePlaneObj.SetActive(showSceneSpine);
        }
        GUILayout.Label("(左=基础原形象 右=幻化形象, 可悬停滚轮/拖拽调 world_data)", hintStyle);
        GUILayout.EndHorizontal();
    }

    #endregion

    #region 卡片数据构建与刷新

    /// <summary>
    /// 重建生物数据并刷新小卡/大卡/场景显示(幻化药变更时调用)：
    /// 小卡走 chess 段基础形象，大卡详情走 avator 段高清形象(含自带 ui_data_b 尺寸)，场景并排左基础右幻化，均为真实显示链
    /// </summary>
    private void RefreshCards()
    {
        if (cardItem == null || cardDetails == null) return;
        CreatureBean creatureData = BuildCreature();
        if (creatureData == null) return;
        currentCreature = creatureData;
        //ShowNoPopup: 测试面板无需详情气泡交互, 禁用悬停弹窗按钮
        cardItem.SetData(creatureData, CardUseStateEnum.ShowNoPopup);
        cardDetails.SetData(creatureData);
        RefreshSceneSpines(creatureData);
    }

    /// <summary>
    /// 构建基础生物并设置当前选中的幻化药(transformItemId)，0=无幻化显示原形象
    /// </summary>
    private CreatureBean BuildCreature()
    {
        return BuildCreature(currentPotionItemId);
    }

    /// <summary>
    /// 构建基础生物并设置指定幻化药(transformItemId)，0=无幻化显示原形象(场景基础对比位用)；
    /// 体型倍率恒钉为1——CreatureBean 构造时会按 CreatureInfo.body_size 区间随机 roll 一次并缓存(真实游戏的个体体型差异)，
    /// 面板调参/对比需要确定性基准，world_data 倍率应相对标准体型校准
    /// </summary>
    /// <param name="transformItemId">幻化药道具完整id(0=无幻化)</param>
    private CreatureBean BuildCreature(long transformItemId)
    {
        if (CreatureInfoCfg.GetItemData(BaseCreatureId) == null) return null;
        CreatureBean creatureData = new CreatureBean(BaseCreatureId);
        creatureData.rarity = 1;
        creatureData.level = 0;
        creatureData.AddSkinForBase();
        creatureData.bodySizeScale = 1; //钉死标准体型, 防构建随机 roll 导致同参数不同大小/每次重建大小都变
        creatureData.transformItemId = transformItemId;
        return creatureData;
    }

    #endregion

    #region 下拉候选列表

    /// <summary>
    /// 懒加载幻化药候选：listPotionOptions=首项「无幻化(原形象)」+全部幻化药(单个预览下拉用)，
    /// listAllPotions=全部幻化药(列表页数据源, 不含无幻化)，均按自ID排序，显示名=道具名[自ID]
    /// </summary>
    private void EnsureOptions()
    {
        if (listPotionOptions != null) return;
        listPotionOptions = new List<SelectItem>
        {
            new SelectItem(0, "无幻化（原形象）")
        };
        listAllPotions = new List<SelectItem>();
        var listPotions = new List<ItemsInfoBean>();
        foreach (var itemInfo in ItemsInfoCfg.GetAllArrayData())
        {
            if (itemInfo.GetItemType() == ItemTypeEnum.TransformPotion)
            {
                listPotions.Add(itemInfo);
            }
        }
        //按自ID排序(Mod道具完整id含modId前缀, 直接按完整id排即先按modId再按自ID)
        listPotions.Sort((a, b) => a.id.CompareTo(b.id));
        foreach (var itemInfo in listPotions)
        {
            string name = itemInfo.name_language;
            long selfId = itemInfo.id % ModIdDivisor;
            string label = name.IsNull() ? $"[{selfId}]" : $"{name} [{selfId}]";
            listPotionOptions.Add(new SelectItem(itemInfo.id, label));
            listAllPotions.Add(new SelectItem(itemInfo.id, label));
        }
        //Mod筛选候选: 全部/游戏本地/各已加载Mod(带ItemsInfo JsonText的Mod)
        listModFilterOptions = new List<SelectItem>
        {
            new SelectItem(-1, "全部"),
            new SelectItem(0, "游戏本地")
        };
        foreach (var (modId, modName, _) in ModHandler.Instance.manager.GetModJsonTextFileInfos("ItemsInfo"))
        {
            listModFilterOptions.Add(new SelectItem(modId, modName));
        }
    }

    /// <summary>
    /// 取当前Mod筛选后的幻化药列表(全部=不过滤; 游戏本地=无modId前缀; 指定Mod=按modId前缀过滤)
    /// </summary>
    private List<SelectItem> GetFilteredPotions()
    {
        if (listAllPotions == null) return null;
        if (listModFilterOptions == null || modFilterIndex <= 0 || modFilterIndex >= listModFilterOptions.Count) return listAllPotions;
        long filterModId = listModFilterOptions[modFilterIndex].id;
        List<SelectItem> filtered = new List<SelectItem>();
        foreach (var potion in listAllPotions)
        {
            if (potion.id / ModIdDivisor == filterModId) filtered.Add(potion);
        }
        return filtered;
    }

    /// <summary>
    /// 按当前选中幻化药同步下拉按钮显示文本(找不到时显示自ID)
    /// </summary>
    private void SyncDropdownLabel()
    {
        foreach (var option in listPotionOptions)
        {
            if (option.id == currentPotionItemId)
            {
                potionDropdownLabel = option.label;
                return;
            }
        }
        potionDropdownLabel = $"未知道具 [{currentPotionItemId % ModIdDivisor}]";
    }

    #endregion

    #region 列表视图构建与销毁

    /// <summary>
    /// 按当前页签、Mod筛选、布局设置与页码构建列表视图(小卡=列x行网格真实卡片, 大卡=列x行网格真实详情卡, 场景=世界空间spine一排)；
    /// 网格整体右移避开左侧面板(面板占屏宽按 scaleFactor 换算成 Canvas单位/世界单位)
    /// </summary>
    private void BuildListView()
    {
        DestroyListView();
        List<SelectItem> potions = GetFilteredPotions();
        if (potions == null || potions.Count == 0) return;
        //左侧面板占用宽换算成Canvas单位(屏幕像素÷scaleFactor): 网格右移让最左列完整露出面板
        float guardLeftCanvas = canvas != null && canvas.scaleFactor > 0 ? (PanelWidth + 20) / canvas.scaleFactor : 460f;
        if (currentTab == PanelTab.SceneList)
        {
            sceneListRoot = new GameObject("TransformPotionSceneListRoot");
            int page = Mathf.Clamp(pageScene, 0, GetPageCount(sceneCols) - 1);
            pageScene = page;
            int start = page * sceneCols;
            int count = Mathf.Min(sceneCols, potions.Count - start);
            //世界空间右移量: 面板占屏比 × 模型深度处可见世界半宽
            float worldShift = 0;
            Camera cam = CameraHandler.Instance.manager.mainCamera;
            if (cam != null)
            {
                float dist = Mathf.Abs(cam.transform.position.z);
                float visibleHalfWidth = dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
                worldShift = guardLeftCanvas / 1920f * visibleHalfWidth;
            }
            //最左固定基础样板(transformItemId=0 原形象, 每页常驻最左作对比基准; 不可编辑、不占「每页」数量), 幻化药依次排其右
            int visualCount = count + 1;
            CreateSceneListItem(0, "基础样板(原形象)", worldShift + (0 - (visualCount - 1) / 2f) * sceneSpacing, false);
            for (int i = 0; i < count; i++)
            {
                SelectItem option = potions[start + i];
                float posX = worldShift + ((i + 1) - (visualCount - 1) / 2f) * sceneSpacing;
                CreateSceneListItem(option.id, option.label, posX);
            }
            return;
        }
        //卡片列表(小卡/大卡共用容器与构建路径, 仅预制体/布局不同; 横竖个数用户可调)
        listRoot = new GameObject("TransformPotionCardListRoot");
        listRoot.transform.SetParent(canvas.transform, false);
        RectTransform listRootRT = listRoot.AddComponent<RectTransform>();
        listRootRT.anchorMin = listRootRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRootRT.pivot = new Vector2(0.5f, 0.5f);
        listRootRT.anchoredPosition = Vector2.zero;
        bool isChess = currentTab == PanelTab.ChessList;
        int cols = isChess ? chessCols : showCols;
        int rows = isChess ? chessRows : showRows;
        Vector2 cell = isChess ? new Vector2(chessCellX, chessCellY) : new Vector2(showCellX, showCellY);
        float cardEstWidth = isChess ? 340f : 720f; //网格右移计算的卡片估计宽
        int pageSize = cols * rows;
        int cardPage = Mathf.Clamp(isChess ? pageChess : pageShow, 0, GetPageCount(pageSize) - 1);
        if (isChess) pageChess = cardPage; else pageShow = cardPage;
        int cardStart = cardPage * pageSize;
        int cardCount = Mathf.Min(pageSize, potions.Count - cardStart);
        //网格整体右移: 网格中心x = max(0, 可用区左缘 + 网格半宽 + 余量)
        float gridWidth = (cols - 1) * cell.x + cardEstWidth;
        float centerX = Mathf.Max(0f, -960f + guardLeftCanvas + gridWidth / 2f + 20f);
        for (int i = 0; i < cardCount; i++)
        {
            SelectItem option = potions[cardStart + i];
            CreatureBean creatureData = BuildCreature(option.id);
            if (creatureData == null) continue;
            ListItem item = new ListItem { potionId = option.id, label = option.label };
            int col = i % cols;
            int row = i / cols;
            Vector2 pos = new Vector2(centerX + (col - (cols - 1) / 2f) * cell.x, ((rows - 1) / 2f - row) * cell.y);
            if (isChess)
            {
                UIViewCreatureCardItem card = Instantiate(Resources.Load<GameObject>(PathCardItemPrefab), listRootRT).GetComponent<UIViewCreatureCardItem>();
                NormalizeRoot(card.transform as RectTransform, pos);
                card.SetData(creatureData, CardUseStateEnum.ShowNoPopup);
                item.cardRoot = card.transform as RectTransform;
                item.cardIcon = card.ui_Icon;
            }
            else
            {
                UIViewCreatureCardDetails card = Instantiate(Resources.Load<GameObject>(PathCardDetailsPrefab), listRootRT).GetComponent<UIViewCreatureCardDetails>();
                NormalizeRoot(card.transform as RectTransform, pos);
                card.SetData(creatureData);
                //大卡图标点击原本也会打开生物展示弹窗(OnClickForIconShow), 但不走详情模式会被测试面板遮挡——禁用, 统一走项下[详情]按钮
                if (card.ui_IconBtn != null) card.ui_IconBtn.interactable = false;
                //大卡列表只展示卡面(底板+肖像+名字+稀有度+职业+等级), 隐藏详情UI区块(属性/好感/装备/BUFF/MP/备注等噪音)
                HideDetailsChrome(card);
                item.cardRoot = card.transform as RectTransform;
                item.cardIcon = card.ui_Icon;
                //大卡详情对带任一展示资源的药均可调: 无 ui_show_res 时详情UI回落 show 形象(SetCreatureData 的 isUIShow 分支),
                //ui_show_data 仍被 SetCreatureUIForDetails 消费(与有无 ui_show_res 无关)——仅基础药(如 Kuluoxierback)同样可调
                ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(option.id);
                TransformOtherData otherData = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
                item.hasUiShowRes = !otherData.uiShowRes.IsNull();
                item.editable = item.hasUiShowRes || !otherData.showRes.IsNull();
            }
            listItems.Add(item);
        }
    }

    /// <summary>
    /// 隐藏大卡详情的详情UI区块, 只保留卡面显示(底板+肖像+名字+稀有度+职业+等级)——大卡列表专用(调 ui_show_data 时属性/好感/装备/BUFF/MP/备注等均为噪音)
    /// </summary>
    private static void HideDetailsChrome(UIViewCreatureCardDetails card)
    {
        RectTransform[] chrome = {
            card.ui_DetailsBase, card.ui_Details, card.ui_Details_Child_1, card.ui_Relationship, card.ui_NameDoomCouncil,
            card.ui_Equip, card.ui_Buff, card.ui_MP, card.ui_RenmarkText, card.ui_NameTitle, card.ui_AttributeList_RectTransform,
            card.ui_ViewCreatureCardItemAttribute_Life, card.ui_ViewCreatureCardItemAttribute_Def, card.ui_ViewCreatureCardItemAttribute_Speed,
            card.ui_ViewCreatureCardItemAttribute_MP, card.ui_ViewCreatureCardItemAttribute_MPR, card.ui_ViewCreatureCardItemAttribute_Atk,
            card.ui_ViewCreatureCardItemAttribute_AddLife, card.ui_ViewCreatureCardItemAttribute_AddDef,
        };
        foreach (RectTransform rt in chrome)
        {
            if (rt != null) rt.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 创建场景列表单个spine项: 摆放根(脚下踩地)+Renderer子节点(承载缩放/偏移, 与游戏内实体一致)+Idle播放, 并登记到 listItems；
    /// 无 show_res(世界幻化资源)的药场景显示的是原生物, world_data 不被真实链(SetCreatureData 的 hasTransform 门控)消费,
    /// 强制 editable=false 防"调参假象生效、保存后弹回"(与大卡列表"(无Avator)"同套路)
    /// </summary>
    /// <param name="potionId">幻化药道具完整id(0=基础样板原形象, 跳过 show_res 判定)</param>
    /// <param name="label">项下名字标签</param>
    /// <param name="posX">世界空间X坐标</param>
    /// <param name="editable">可否编辑(基础样板=否, 仅作对比基准不响应悬停调参)</param>
    private void CreateSceneListItem(long potionId, string label, float posX, bool editable = true)
    {
        CreatureBean creatureData = BuildCreature(potionId);
        if (creatureData == null) return;
        bool hasShowRes = true;
        if (potionId != 0)
        {
            ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(potionId);
            hasShowRes = itemInfo != null && !CreatureBean.ParseTransformOtherData(itemInfo.other_data).showRes.IsNull();
        }
        GameObject spineObj = new GameObject(potionId == 0 ? "SceneListSpine_Base" : $"SceneListSpine_{potionId}");
        spineObj.transform.SetParent(sceneListRoot.transform, false);
        spineObj.transform.localPosition = new Vector3(posX, 0, 0);
        GameObject rendererObj = new GameObject("Renderer");
        rendererObj.transform.SetParent(spineObj.transform, false);
        SkeletonAnimation spine = SpineHandler.Instance.AddSkeletonAnimation(rendererObj, creatureData.creatureModel.res_name);
        CreatureHandler.Instance.SetCreatureData(spine, creatureData);
        SpineHandler.Instance.PlayAnim(spine, SpineAnimationStateEnum.Idle, creatureData, true);
        listItems.Add(new ListItem
        {
            potionId = potionId,
            label = label,
            sceneObj = spineObj,
            sceneRenderer = rendererObj.transform,
            sceneBaseScale = creatureData.creatureModel.size_spine * creatureData.GetBodySizeScale(),
            editable = editable && hasShowRes,
            hasShowRes = hasShowRes,
        });
    }

    /// <summary>
    /// 销毁列表视图(换页/换页签/回单个预览时调用)
    /// </summary>
    private void DestroyListView()
    {
        listItems.Clear();
        if (listRoot != null) { Destroy(listRoot); listRoot = null; }
        if (sceneListRoot != null) { Destroy(sceneListRoot); sceneListRoot = null; }
    }

    /// <summary>
    /// 取列表总页数(按当前Mod筛选后的数据)
    /// </summary>
    private int GetPageCount(int pageSize)
    {
        int total = GetFilteredPotions()?.Count ?? 0;
        if (total == 0) return 1;
        return Mathf.Max(1, Mathf.CeilToInt(total / (float)pageSize));
    }

    /// <summary>
    /// 绘制列表分页栏(◀ ▶ 翻页 + 页码显示 + 跳转输入; 页码变更即重建列表)
    /// </summary>
    private void DrawPager()
    {
        int pageSize = currentTab == PanelTab.ChessList ? chessCols * chessRows : currentTab == PanelTab.ShowList ? showCols * showRows : sceneCols;
        int pageCount = GetPageCount(pageSize);
        int page = currentTab == PanelTab.ChessList ? pageChess : currentTab == PanelTab.ShowList ? pageShow : pageScene;
        page = Mathf.Clamp(page, 0, pageCount - 1);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀", GUILayout.Width(36), GUILayout.Height(24)) && page > 0) page--;
        GUILayout.Label($"{page + 1}/{pageCount}", labelCenterStyle, GUILayout.Width(60), GUILayout.Height(24));
        if (GUILayout.Button("▶", GUILayout.Width(36), GUILayout.Height(24)) && page < pageCount - 1) page++;
        GUILayout.Label("跳页", hintStyle, GUILayout.Width(30));
        pageJumpBuffer = GUILayout.TextField(pageJumpBuffer, GUILayout.Width(50));
        if (GUILayout.Button("GO", GUILayout.Width(36), GUILayout.Height(24)) && int.TryParse(pageJumpBuffer, out int jump))
            page = Mathf.Clamp(jump - 1, 0, pageCount - 1);
        GUILayout.EndHorizontal();
        int oldPage = currentTab == PanelTab.ChessList ? pageChess : currentTab == PanelTab.ShowList ? pageShow : pageScene;
        if (page != oldPage)
        {
            if (currentTab == PanelTab.ChessList) pageChess = page;
            else if (currentTab == PanelTab.ShowList) pageShow = page;
            else pageScene = page;
            BuildListView();
        }
    }

    /// <summary>
    /// 绘制Mod筛选行(全部/游戏本地/各已加载Mod下拉; 变更即重置页码并重建列表)
    /// </summary>
    private void DrawModFilter()
    {
        if (listModFilterOptions == null || listModFilterOptions.Count <= 1) return;
        modFilterIndex = Mathf.Clamp(modFilterIndex, 0, listModFilterOptions.Count - 1);
        GUILayout.BeginHorizontal();
        GUILayout.Label("筛选", labelStyle, GUILayout.Width(40));
        if (GUILayout.Button(listModFilterOptions[modFilterIndex].label, buttonLeftStyle, GUILayout.Height(24)))
            isModFilterOpen = !isModFilterOpen;
        GUILayout.EndHorizontal();
        if (!isModFilterOpen) return;
        for (int i = 0; i < listModFilterOptions.Count; i++)
        {
            bool isCurrent = i == modFilterIndex;
            Color oldColor = GUI.color;
            if (isCurrent) GUI.color = Color.green;
            if (GUILayout.Button(isCurrent ? $"✔ {listModFilterOptions[i].label}" : listModFilterOptions[i].label, buttonLeftStyle, GUILayout.Height(22)))
            {
                GUI.color = oldColor;
                isModFilterOpen = false;
                if (i != modFilterIndex)
                {
                    modFilterIndex = i;
                    pageChess = pageShow = pageScene = 0;
                    BuildListView();
#if UNITY_EDITOR
                    SaveLayoutPrefs();
#endif
                }
                break;
            }
            GUI.color = oldColor;
        }
    }

    /// <summary>
    /// 绘制列表布局调整行(第一行=横竖个数(场景含间距), 第二行=间距XY; 分两行防超出面板宽度被裁掉; 变更即重建列表并写项目内JSON持久化)
    /// </summary>
    private void DrawGridSizeRow()
    {
        bool changed = false;
        GUILayout.BeginHorizontal();
        GUILayout.Label("布局", labelStyle, GUILayout.Width(40));
        if (currentTab == PanelTab.ChessList)
        {
            changed |= DrawStepper("列", ref chessCols, 1, 8);
            changed |= DrawStepper("行", ref chessRows, 1, 4);
        }
        else if (currentTab == PanelTab.ShowList)
        {
            changed |= DrawStepper("列", ref showCols, 1, 4);
            changed |= DrawStepper("行", ref showRows, 1, 2);
        }
        else
        {
            changed |= DrawStepper("每页", ref sceneCols, 1, 12);
            changed |= DrawFloatStepper("间距", ref sceneSpacing, 0.1f, 0.2f, 5f);
        }
        GUILayout.EndHorizontal();
        //卡片列表间距单独第二行(与行列数同行总宽444px超出400px面板会被裁掉)
        if (currentTab != PanelTab.SceneList)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("间距", labelStyle, GUILayout.Width(40));
            if (currentTab == PanelTab.ChessList)
            {
                changed |= DrawFloatStepper("X", ref chessCellX, 10, 100, 1200);
                changed |= DrawFloatStepper("Y", ref chessCellY, 10, 100, 1200);
            }
            else
            {
                changed |= DrawFloatStepper("X", ref showCellX, 10, 300, 1500);
                changed |= DrawFloatStepper("Y", ref showCellY, 10, 300, 1500);
            }
            GUILayout.EndHorizontal();
        }
        if (changed)
        {
            BuildListView();
#if UNITY_EDITOR
            SaveLayoutPrefs();
#endif
        }
    }

    /// <summary>
    /// 绘制 -/n/+ 步进控件(返回是否有变更)
    /// </summary>
    /// <param name="title">步进标签</param>
    /// <param name="value">当前值(直接改写)</param>
    /// <param name="min">下限</param>
    /// <param name="max">上限</param>
    private bool DrawStepper(string title, ref int value, int min, int max)
    {
        int old = value;
        GUILayout.Label(title, hintStyle, GUILayout.Width(title.Length > 1 ? 34 : 18));
        if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(20))) value = Mathf.Max(min, value - 1);
        GUILayout.Label(value.ToString(), labelCenterStyle, GUILayout.Width(22), GUILayout.Height(20));
        if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(20))) value = Mathf.Min(max, value + 1);
        return value != old;
    }

    /// <summary>
    /// 绘制浮点 -/n/+ 步进控件(返回是否有变更; 变更后按步长取整防浮点漂移)
    /// </summary>
    /// <param name="title">步进标签</param>
    /// <param name="value">当前值(直接改写)</param>
    /// <param name="step">步长</param>
    /// <param name="min">下限</param>
    /// <param name="max">上限</param>
    private bool DrawFloatStepper(string title, ref float value, float step, float min, float max)
    {
        float old = value;
        GUILayout.Label(title, hintStyle, GUILayout.Width(34));
        if (GUILayout.Button("-", GUILayout.Width(22), GUILayout.Height(20))) value = Mathf.Max(min, value - step);
        GUILayout.Label(value.ToString("0.##"), labelCenterStyle, GUILayout.Width(40), GUILayout.Height(20));
        if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(20))) value = Mathf.Min(max, value + step);
        if (value != old) value = Mathf.Round(value / step) * step;
        return value != old;
    }

    #endregion

    #region GUI绘制

    /// <summary>
    /// IMGUI入口，绘制幻化药测试控制面板
    /// </summary>
    private void OnGUI()
    {
        //详情模式(生物展示弹窗打开中): 跳过全部IMGUI绘制与悬停交互——IMGUI 恒渲染在最上层, 不跳过会遮挡弹窗
        if (creatureShowDialog != null) return;
        InitGUIStyle();

        float panelWidth = Mathf.Min(PanelWidth, Screen.width - 20);
        GUILayout.BeginArea(new Rect(10, 10, panelWidth, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);
        GUILayout.Label("Mod 幻化药测试", titleStyle);
        DrawTabBar();
        GUILayout.Space(6);

        if (currentTab == PanelTab.Single)
        {
            GUILayout.Label("小卡=Chess基础形象；大卡=Avator高清；场景=左基础右幻化(均可悬停滚轮/拖拽调参)", hintStyle);
            GUILayout.Space(4);
            DrawPotionDropdown();
            GUILayout.Space(6);
            DrawPotionInfo();
            GUILayout.Space(6);
#if UNITY_EDITOR
            DrawAdjustSection();
            GUILayout.Space(6);
#endif
            DrawSceneSpineSection();
        }
        else
        {
            string hint = currentTab == PanelTab.ChessList ? "小卡列表：悬停目标卡片，滚轮=改缩放，拖拽=改位置（show_data）"
                : currentTab == PanelTab.ShowList ? "大卡列表：悬停目标卡片，滚轮=改缩放，拖拽=改位置（ui_show_data；无Avator=调回落的show形象）"
                : "场景列表：最左=基础样板(原形象)对比基准(不可调)；悬停目标模型，滚轮=改大小，拖拽=改位置（world_data 世界显示），Alt+滚轮=调亮度（show_brightness 场景亮暗 30%~200%）";
            GUILayout.Label(hint, hintStyle);
            GUILayout.Label("项下小按钮：[还原]=恢复配置值(场景页含亮度) [0,0]=位置归零 [复制][粘贴]=参数快速套用", hintStyle);
#if UNITY_EDITOR
            if (clipHasValue)
                GUILayout.Label($"剪贴板[{KindName(clipKind)}]: {FmtNum(clipScale)};{FmtNum(clipPos.x)},{FmtNum(clipPos.y)}", hintStyle);
#endif
            GUILayout.Space(4);
            DrawModFilter();
            DrawGridSizeRow();
            DrawPager();
#if UNITY_EDITOR
            if (currentTab == PanelTab.SceneList)
            {
                //同MOD场景骨架大小相近: 一键把剪贴板 world_data 套用到当前筛选全部项, 免逐项粘贴
                if (GUILayout.Button("⇪ 粘贴剪贴板到全部场景项(当前筛选)", GUILayout.Height(22)))
                    PasteClipboardToAllScenes();
                if (GUILayout.Button("📋 输出场景诊断到控制台(按 ` 键查看)", GUILayout.Height(22)))
                    LogSceneListDiagnostics();
                DrawTestSceneSection();
            }
#endif
        }

        GUILayout.FlexibleSpace();
#if UNITY_EDITOR
        DrawSaveFooter();
#endif
        if (GUILayout.Button("关闭", GUILayout.Height(26)))
        {
            Destroy(gameObject);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

#if UNITY_EDITOR
        HandleInteraction();
        DrawListLabels();
#endif

        //变更检测: 幻化药变更才重建生物数据刷新卡片
        if (GUI.changed && currentPotionItemId != lastDataKey)
        {
            lastDataKey = currentPotionItemId;
            RefreshCards();
        }
    }

    /// <summary>
    /// 绘制幻化药下拉(当前选中项高亮; 无候选时提示)
    /// </summary>
    private void DrawPotionDropdown()
    {
        if (listPotionOptions.Count <= 1)
        {
            GUILayout.Label("⚠ 配置表中没有幻化药(请确认 Mod 已加载且 ItemsInfo 已合并)", hintStyle);
            return;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("幻化药", labelStyle, GUILayout.Width(60));
        if (GUILayout.Button("◀", GUILayout.Width(26), GUILayout.Height(26)))
        {
            SwitchPotionByOffset(-1);
        }
        if (GUILayout.Button(potionDropdownLabel, buttonLeftStyle, GUILayout.Height(26)))
        {
            isDropdownOpen = !isDropdownOpen;
        }
        if (GUILayout.Button("▶", GUILayout.Width(26), GUILayout.Height(26)))
        {
            SwitchPotionByOffset(1);
        }
        GUILayout.EndHorizontal();
        if (isDropdownOpen)
        {
            scrollDropdown = GUILayout.BeginScrollView(scrollDropdown, GUI.skin.box, GUILayout.Height(300));
            foreach (var option in listPotionOptions)
            {
                bool isCurrent = option.id == currentPotionItemId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                if (GUILayout.Button(isCurrent ? $"✔ {option.label}" : option.label, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    isDropdownOpen = false;
                    SwitchPotion(option.id);
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// 切换选中幻化药并立即刷新显示(下拉选项与◀▶左右切换共用)：同步下拉标签+重载调参缓冲+刷新卡片与场景
    /// </summary>
    private void SwitchPotion(long itemId)
    {
        if (itemId == currentPotionItemId) return;
        currentPotionItemId = itemId;
        lastDataKey = itemId;
        SyncDropdownLabel();
#if UNITY_EDITOR
        LoadEditBuffers();
#endif
        RefreshCards();
    }

    /// <summary>
    /// 按偏移量左右切换幻化药(◀=上一个 ▶=下一个, 含「无幻化」项, 两端到头停住不循环)
    /// </summary>
    private void SwitchPotionByOffset(int offset)
    {
        if (listPotionOptions == null || listPotionOptions.Count == 0) return;
        int index = listPotionOptions.FindIndex(o => o.id == currentPotionItemId);
        if (index < 0) index = 0;
        index = Mathf.Clamp(index + offset, 0, listPotionOptions.Count - 1);
        SwitchPotion(listPotionOptions[index].id);
    }

    /// <summary>
    /// 绘制当前幻化药的配置信息(other_data 键值解析 + spine 资源在 Mod 中的加载状态)
    /// </summary>
    private void DrawPotionInfo()
    {
        if (currentPotionItemId == 0)
        {
            GUILayout.Label("当前显示原形象（未幻化）", hintStyle);
            return;
        }
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(currentPotionItemId);
        if (itemInfo == null)
        {
            GUILayout.Label($"⚠ 找不到道具配置:{currentPotionItemId}(Mod移除或配置被删)", hintStyle);
            return;
        }
        TransformOtherData transformData = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
        GUILayout.Label($"other_data: {(itemInfo.other_data.IsNull() ? "(空)" : itemInfo.other_data)}", hintStyle);
        DrawResLoadState("Show(默认展示)", transformData.showRes);
        if (!transformData.uiShowRes.IsNull())
        {
            DrawResLoadState("UIShow(详情高清)", transformData.uiShowRes);
        }
        if (!transformData.uiShowSkin.IsNull())
        {
            GUILayout.Label($"UIShow皮肤: {transformData.uiShowSkin}", hintStyle);
        }
        if (!transformData.uiShowData.IsNull())
        {
            GUILayout.Label($"详情UI尺寸: {transformData.uiShowData}", hintStyle);
        }
        if (!transformData.showData.IsNull())
        {
            GUILayout.Label($"小卡UI尺寸: {transformData.showData}", hintStyle);
        }
        if (!transformData.worldData.IsNull())
        {
            GUILayout.Label($"世界显示: {transformData.worldData}", hintStyle);
        }
        if (!transformData.idleAnim.IsNull())
        {
            GUILayout.Label($"Show替代待机: {transformData.idleAnim}", hintStyle);
        }
        if (!transformData.uiShowIdleAnim.IsNull())
        {
            GUILayout.Label($"UIShow替代待机: {transformData.uiShowIdleAnim}", hintStyle);
        }
    }

    /// <summary>
    /// 绘制单个 spine 资源在已加载 Mod 中的命中状态(✓=某Mod catalog含此资源, ✗=未加载)
    /// </summary>
    private void DrawResLoadState(string title, string resName)
    {
        if (resName.IsNull())
        {
            GUILayout.Label($"{title}: (未配置)", hintStyle);
            return;
        }
        bool isLoaded = ModHandler.Instance.IsModAsset(resName);
        Color oldColor = GUI.color;
        GUI.color = isLoaded ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f);
        GUILayout.Label($"{title}: {(isLoaded ? "✓" : "✗")} {resName}", hintStyle);
        GUI.color = oldColor;
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
        //按钮文本左对齐(下拉按钮/选项按钮显示 名字+自ID 长文本用)
        buttonLeftStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 0, 0) };
        //列表项名称标签(卡片下方/模型脚下, 居中, 禁换行——过长由 TruncateLabel 截断加…, 防换行溢出显示不全)
        labelCenterStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        //列表项小按钮(还原/0,0, 44x18小尺寸)
        smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, padding = new RectOffset(2, 2, 0, 0) };
    }

    #endregion

#if UNITY_EDITOR

    #region 调参缓冲加载与应用

    /// <summary>
    /// 从道具配置(+未保存覆盖层)重载调参缓冲：切换幻化药/保存/重置时调用；覆盖层有值优先(未保存的调参切换后保留)；
    /// world_data 无配置时按默认 1;0,0 显示(首次改动才会创建该键)
    /// </summary>
    private void LoadEditBuffers()
    {
        adjustError = null;
        saveMessage = null;
        hasUiShowRes = false;
        hasShowRes = false;
        hasDetailAdjust = false;
        appliedShowData = null;
        appliedUiShowData = null;
        appliedWorldData = null;
        editShowScale = editShowX = editShowY = "";
        editUiShowScale = editUiShowX = editUiShowY = "";
        editWorldScale = editWorldX = editWorldY = "";
        if (currentPotionItemId == 0) return;
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(currentPotionItemId);
        if (itemInfo == null) return;
        TransformOtherData transformData = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
        hasUiShowRes = !transformData.uiShowRes.IsNull();
        hasShowRes = !transformData.showRes.IsNull();
        hasDetailAdjust = hasUiShowRes || hasShowRes;
        string showData = transformData.showData, uiData = transformData.uiShowData, worldData = transformData.worldData;
        if (TransformPotionUITestOverride.TryGetShowData(currentPotionItemId, out string ovShow)) showData = ovShow;
        if (TransformPotionUITestOverride.TryGetUiShowData(currentPotionItemId, out string ovUiShow)) uiData = ovUiShow;
        if (TransformPotionUITestOverride.TryGetWorldData(currentPotionItemId, out string ovWorld)) worldData = ovWorld;
        if (TryParseUIData(showData, out float chessScale, out Vector2 chessPos))
        {
            editShowScale = FmtNum(chessScale);
            editShowX = FmtNum(chessPos.x);
            editShowY = FmtNum(chessPos.y);
            appliedShowData = ComposeUIData(chessScale, chessPos);
        }
        if (TryParseUIData(uiData, out float showScale, out Vector2 showPos))
        {
            editUiShowScale = FmtNum(showScale);
            editUiShowX = FmtNum(showPos.x);
            editUiShowY = FmtNum(showPos.y);
            appliedUiShowData = ComposeUIData(showScale, showPos);
        }
        //world_data: 无配置按默认 1;0,0(缓冲与applied都置默认, 防首帧自动生成覆盖)
        if (!TryParseUIData(worldData, out float worldScale, out Vector2 worldPos))
        {
            worldScale = 1;
            worldPos = Vector2.zero;
        }
        editWorldScale = FmtNum(worldScale);
        editWorldX = FmtNum(worldPos.x);
        editWorldY = FmtNum(worldPos.y);
        appliedWorldData = ComposeUIData(worldScale, worldPos);
    }

    /// <summary>
    /// 把三组文本框缓冲写入覆盖层并应用到卡片图标/场景模型(任一字段为空=编辑中途静默跳过; 非空但非法=红字提示不写覆盖层)
    /// </summary>
    private void TryApplyEditBuffers()
    {
        adjustError = null;
        bool changed = false;
        changed |= TryApplyOneGroup(editShowScale, editShowX, editShowY, true, DataKind.Show, ref appliedShowData, "小卡");
        if (hasDetailAdjust)
            changed |= TryApplyOneGroup(editUiShowScale, editUiShowX, editUiShowY, true, DataKind.UiShow, ref appliedUiShowData, "详情");
        changed |= TryApplyOneGroup(editWorldScale, editWorldX, editWorldY, true, DataKind.World, ref appliedWorldData, "场景");
        if (changed) ApplyAdjustToIcons();
    }

    /// <summary>
    /// 应用单组调参缓冲(非空且合法才写覆盖层; 返回是否有变更)
    /// </summary>
    private bool TryApplyOneGroup(string sScale, string sX, string sY, bool enabled, DataKind kind, ref string appliedData, string title)
    {
        if (!enabled || sScale.IsNull() || sX.IsNull() || sY.IsNull()) return false;
        if (!TryParseFields(sScale, sX, sY, out float scale, out Vector2 pos) || scale <= 0)
        {
            adjustError = adjustError == null ? $"{title}参数非法(缩放需>0, 坐标需为数字)" : adjustError + $"；{title}参数非法";
            return false;
        }
        string composed = ComposeUIData(scale, pos);
        if (composed == appliedData) return false;
        appliedData = composed;
        SetOverrideData(currentPotionItemId, kind, scale, pos);
        return true;
    }

    /// <summary>
    /// 写入指定幻化药指定数据段的覆盖值
    /// </summary>
    private void SetOverrideData(long potionId, DataKind kind, float scale, Vector2 pos)
    {
        string composed = ComposeUIData(scale, pos);
        if (kind == DataKind.Show) TransformPotionUITestOverride.SetShowData(potionId, composed);
        else if (kind == DataKind.UiShow) TransformPotionUITestOverride.SetUiShowData(potionId, composed);
        else TransformPotionUITestOverride.SetWorldData(potionId, composed);
    }

    /// <summary>
    /// 取指定幻化药指定数据段的当前有效值(覆盖层优先, 无覆盖读配置; world_data 无配置默认 1,0,0)；
    /// 键缺失且给了列表项时卡片段回落读卡片图标当前显示的缩放/坐标作基线
    /// (仅基础药无 ui_show_data 键时大卡按原生物 ui_data_b 显示, 基线须从显示值起调防首次调整跳变)
    /// </summary>
    private void GetCurrentData(long potionId, DataKind kind, out float scale, out Vector2 pos, ListItem item = null)
    {
        scale = 1;
        pos = Vector2.zero;
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(potionId);
        if (itemInfo == null) return;
        TransformOtherData transformData = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
        string data = kind == DataKind.Show ? transformData.showData : kind == DataKind.UiShow ? transformData.uiShowData : transformData.worldData;
        if (kind == DataKind.Show && TransformPotionUITestOverride.TryGetShowData(potionId, out string ovC)) data = ovC;
        if (kind == DataKind.UiShow && TransformPotionUITestOverride.TryGetUiShowData(potionId, out string ovS)) data = ovS;
        if (kind == DataKind.World && TransformPotionUITestOverride.TryGetWorldData(potionId, out string ovW)) data = ovW;
        if (TryParseUIData(data, out scale, out pos)) return;
        //键缺失(如仅基础药无 ui_show_data): 卡片段回落读图标当前显示值作基线(场景段 1;0,0 即恒等基线无需回落)
        if (kind != DataKind.World && item != null && item.cardIcon != null)
        {
            scale = item.cardIcon.transform.localScale.x;
            pos = item.cardIcon.rectTransform.anchoredPosition;
        }
    }

    /// <summary>
    /// 把当前调参结果直接应用到单个预览的小卡/大卡图标与场景幻化模型的缩放/坐标
    /// (与 GameUIUtil.SetCreatureUIForSimple/Details 及 CreatureHandler.SetCreatureData 的尺寸段同逻辑, 不重建骨架不打断动画)
    /// </summary>
    private void ApplyAdjustToIcons()
    {
        if (currentCreature == null || cardItem == null || cardDetails == null) return;
        //小卡: 幻化 chess 尺寸优先, 无则原生物 ui_data_s
        if (currentCreature.GetTransformShowData(out float chessScale, out Vector2 chessPos))
        {
            cardItem.ui_Icon.transform.localScale = Vector3.one * chessScale;
            cardItem.ui_Icon.rectTransform.anchoredPosition = chessPos;
        }
        else
        {
            currentCreature.creatureModel.ChangeUISizeForS(cardItem.ui_Icon.rectTransform, 1);
        }
        //大卡: 幻化 ui_show 尺寸优先, 无则原生物 ui_data_b
        if (currentCreature.GetTransformUIShowData(out float showScale, out Vector2 showPos))
        {
            cardDetails.ui_Icon.transform.localScale = Vector3.one * showScale;
            cardDetails.ui_Icon.rectTransform.anchoredPosition = showPos;
        }
        else
        {
            currentCreature.creatureModel.ChangeUISizeForB(cardDetails.ui_Icon.rectTransform);
        }
        //场景幻化模型: world_data 尺寸/偏移(无则基础缩放+归零, 与 CreatureHandler 注入同逻辑)
        if (sceneSpineTransform != null)
        {
            float baseScale = currentCreature.creatureModel.size_spine * currentCreature.GetBodySizeScale();
            ApplyWorldToSceneSpine(sceneSpineTransform.transform, baseScale, currentPotionItemId);
        }
    }

    /// <summary>
    /// 把 world_data 当前有效值直接应用到场景 spine 的 Renderer 节点(缩放=基础×倍率, 偏移=(x,y,0))
    /// </summary>
    private void ApplyWorldToSceneSpine(Transform rendererTF, float baseScale, long potionId)
    {
        GetCurrentData(potionId, DataKind.World, out float worldScale, out Vector2 worldPos);
        rendererTF.localScale = Vector3.one * (baseScale * worldScale);
        rendererTF.localPosition = new Vector3(worldPos.x, worldPos.y, 0);
    }

    /// <summary>
    /// 重置当前药为配置值: 清该药覆盖层(三个数据段)并按配置重载缓冲/刷新卡片
    /// </summary>
    private void ResetAdjust()
    {
        TransformPotionUITestOverride.Clear(currentPotionItemId);
        LoadEditBuffers();
        RefreshCards();
    }

    /// <summary>
    /// 解析「scale;x,y」尺寸串(不变区域性, 与配置格式一致)
    /// </summary>
    private static bool TryParseUIData(string data, out float scale, out Vector2 pos)
    {
        scale = 1;
        pos = Vector2.zero;
        if (data.IsNull()) return false;
        string[] segs = data.Split(';');
        if (segs.Length < 2 || !float.TryParse(segs[0], NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
        {
            scale = 1;
            return false;
        }
        string[] xy = segs[1].Split(',');
        if (xy.Length < 2) return false;
        float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
        float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        pos = new Vector2(x, y);
        return true;
    }

    /// <summary>
    /// 解析三个文本框(缩放/X/Y)为数值
    /// </summary>
    private static bool TryParseFields(string sScale, string sX, string sY, out float scale, out Vector2 pos)
    {
        bool ok = float.TryParse(sScale, NumberStyles.Float, CultureInfo.InvariantCulture, out scale)
            & float.TryParse(sX, NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
            & float.TryParse(sY, NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        pos = new Vector2(x, y);
        return ok;
    }

    /// <summary>
    /// 组「scale;x,y」尺寸串(不变区域性, 与配置格式一致)
    /// </summary>
    private static string ComposeUIData(float scale, Vector2 pos)
    {
        return $"{FmtNum(scale)};{FmtNum(pos.x)},{FmtNum(pos.y)}";
    }

    /// <summary>
    /// 数字格式化(不变区域性, 最多4位小数去尾零)
    /// </summary>
    private static string FmtNum(float v)
    {
        return v.ToString("0.####", CultureInfo.InvariantCulture);
    }

    #endregion

    #region 调参与保存UI

    /// <summary>
    /// 绘制尺寸/位置调整区(小卡/详情/场景三组 + 重置按钮 + 错误提示)
    /// </summary>
    private void DrawAdjustSection()
    {
        if (currentPotionItemId == 0)
        {
            GUILayout.Label("选择幻化药后可调整其小卡/详情/场景的缩放与位置", hintStyle);
            return;
        }
        if (ItemsInfoCfg.GetItemData(currentPotionItemId) == null) return;
        bool dirty = TransformPotionUITestOverride.HasOverride(currentPotionItemId);
        GUILayout.Label(dirty ? "尺寸/位置调整（●已修改未保存）" : "尺寸/位置调整", labelStyle);
        GUILayout.Label("拖拽卡片/场景形象=改位置；滚轮=改缩放；或下方文本框精输/滑动条粗调", hintStyle);
        DrawAdjustFields("小卡(Show 默认展示形象)", true, DataKind.Show);
        DrawAdjustFields(hasDetailAdjust ? (hasUiShowRes ? "详情(UIShow 高清形象)" : "详情(无Avator 回落show形象)") : "详情(无展示资源 不可调)", hasDetailAdjust, DataKind.UiShow);
        DrawAdjustFields(hasShowRes ? "场景(world_data 世界显示)" : "场景(无 show_res 世界不幻化, world_data 不被消费不可调)", hasShowRes, DataKind.World);
        TryApplyEditBuffers();
        if (!adjustError.IsNull())
        {
            Color old = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.45f);
            GUILayout.Label(adjustError, hintStyle);
            GUI.color = old;
        }
        if (GUILayout.Button("重置当前药为配置值", GUILayout.Height(24))) ResetAdjust();
    }

    /// <summary>
    /// 绘制一组调参控件(缩放/X/Y 三行, 每行=标签+文本框+滑动条; 世界组位置量程±2且保留2位小数)
    /// </summary>
    /// <param name="title">组标题</param>
    /// <param name="enabled">该组是否可编辑(详情组=ui_show_res/show_res 任一存在即可, 均无才禁用)</param>
    /// <param name="kind">数据段(决定缓冲归属与位置滑条量程/取整)</param>
    private void DrawAdjustFields(string title, bool enabled, DataKind kind)
    {
        GUI.enabled = enabled;
        GUILayout.Label(title, labelStyle);
        if (kind == DataKind.Show)
        {
            editShowScale = DrawAdjustScaleRow("缩放", editShowScale);
            editShowX = DrawAdjustPosRow("X", editShowX, PosSliderMin, PosSliderMax, true);
            editShowY = DrawAdjustPosRow("Y", editShowY, PosSliderMin, PosSliderMax, true);
        }
        else if (kind == DataKind.UiShow)
        {
            editUiShowScale = DrawAdjustScaleRow("缩放", editUiShowScale);
            editUiShowX = DrawAdjustPosRow("X", editUiShowX, PosSliderMin, PosSliderMax, true);
            editUiShowY = DrawAdjustPosRow("Y", editUiShowY, PosSliderMin, PosSliderMax, true);
        }
        else
        {
            editWorldScale = DrawAdjustScaleRow("缩放", editWorldScale);
            editWorldX = DrawAdjustPosRow("X", editWorldX, WorldPosSliderMin, WorldPosSliderMax, false);
            editWorldY = DrawAdjustPosRow("Y", editWorldY, WorldPosSliderMin, WorldPosSliderMax, false);
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// 缩放调参行(标签+文本框+对数映射滑动条)：滑动条与文本框双向同步, 仅滑条值变化(=用户在拖滑条)才格式化写回文本缓冲, 空闲时滑条原样回传不干扰文本
    /// </summary>
    /// <param name="label">行标签</param>
    /// <param name="buffer">文本缓冲(当前缩放字符串)</param>
    /// <returns>新的文本缓冲</returns>
    private string DrawAdjustScaleRow(string label, string buffer)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, hintStyle, GUILayout.Width(30));
        string newBuffer = GUILayout.TextField(buffer, GUILayout.Width(60));
        float scale = float.TryParse(newBuffer, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0 ? v : 1f;
        float t = Mathf.InverseLerp(Mathf.Log10(ScaleSliderMin), Mathf.Log10(ScaleSliderMax), Mathf.Log10(Mathf.Clamp(scale, ScaleSliderMin, ScaleSliderMax)));
        float newT = GUILayout.HorizontalSlider(t, 0f, 1f);
        if (Mathf.Abs(newT - t) > 0.00001f)
            newBuffer = FmtNum(Mathf.Pow(10f, Mathf.Lerp(Mathf.Log10(ScaleSliderMin), Mathf.Log10(ScaleSliderMax), newT)));
        GUILayout.EndHorizontal();
        return newBuffer;
    }

    /// <summary>
    /// 位置调参行(标签+文本框+线性滑动条)：滑动条与文本框双向同步, 滑条按量程映射写回(UI坐标取整/世界坐标2位小数, 按舍入值比较防抖动)
    /// </summary>
    /// <param name="label">行标签</param>
    /// <param name="buffer">文本缓冲(当前坐标字符串)</param>
    /// <param name="min">滑条量程下限</param>
    /// <param name="max">滑条量程上限</param>
    /// <param name="roundToInt">true=写回取整(UI坐标惯例), false=保留2位小数(世界坐标)</param>
    /// <returns>新的文本缓冲</returns>
    private string DrawAdjustPosRow(string label, string buffer, float min, float max, bool roundToInt)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, hintStyle, GUILayout.Width(30));
        string newBuffer = GUILayout.TextField(buffer, GUILayout.Width(60));
        float pos = float.TryParse(newBuffer, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
        float newPos = GUILayout.HorizontalSlider(pos, min, max);
        bool changed = roundToInt
            ? Mathf.RoundToInt(newPos) != Mathf.RoundToInt(pos)
            : Mathf.Round(newPos * 100f) != Mathf.Round(pos * 100f);
        if (changed)
            newBuffer = roundToInt ? Mathf.RoundToInt(newPos).ToString() : FmtNum(Mathf.Round(newPos * 100f) / 100f);
        GUILayout.EndHorizontal();
        return newBuffer;
    }

    /// <summary>
    /// 绘制保存底栏(全部页签共用)：一键保存全部未保存修改 + 清空未保存修改 + 保存结果提示
    /// </summary>
    private void DrawSaveFooter()
    {
        List<long> dirtyIds = TransformPotionUITestOverride.GetAllDirtyIds();
        GUILayout.BeginHorizontal();
        GUI.enabled = dirtyIds.Count > 0;
        if (GUILayout.Button($"💾 保存全部修改({dirtyIds.Count})", GUILayout.Height(26))) SaveAllOverrides();
        if (GUILayout.Button("🗑 清空未保存修改", GUILayout.Height(26)))
        {
            TransformPotionUITestOverride.ClearAll();
            if (currentTab == PanelTab.Single) { LoadEditBuffers(); RefreshCards(); }
            else BuildListView();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        if (!saveMessage.IsNull())
        {
            Color old = GUI.color;
            GUI.color = saveMessageIsError ? new Color(1f, 0.45f, 0.45f) : new Color(0.45f, 1f, 0.55f);
            GUILayout.Label(saveMessage, hintStyle);
            GUI.color = old;
        }
    }

    /// <summary>列表布局/筛选设置的项目内JSON路径(随git共享, 全队同一套布局; 存ProjectSettings下不产生.meta)</summary>
    private const string LayoutPrefsFile = "ProjectSettings/TestTransformPotionLayout.json";
    /// <summary>布局设置是否已从JSON读取(每次域加载只读一次, 会话内改动走static)</summary>
    private static bool layoutPrefsLoaded;

    /// <summary>布局/筛选设置数据(JsonUtility序列化, 项目内共享)</summary>
    [Serializable]
    private class LayoutPrefsData
    {
        public int chessCols = 5, chessRows = 2;
        public int showCols = 2, showRows = 1;
        public int sceneCols = 6;
        public float chessCellX = 370, chessCellY = 500;
        public float showCellX = 920, showCellY = 980;
        public float sceneSpacing = 1.5f;
        public int modFilterIndex;
    }

    /// <summary>
    /// 从项目内JSON加载列表布局/筛选设置(面板Start时调用一次; 文件不存在时按当前默认值写一份, 方便提交进git共享; 打包无此逻辑)
    /// </summary>
    private static void LoadLayoutPrefs()
    {
        if (layoutPrefsLoaded) return;
        layoutPrefsLoaded = true;
        try
        {
            if (File.Exists(LayoutPrefsFile))
            {
                LayoutPrefsData data = JsonUtility.FromJson<LayoutPrefsData>(File.ReadAllText(LayoutPrefsFile));
                if (data != null)
                {
                    chessCols = Mathf.Clamp(data.chessCols, 1, 8);
                    chessRows = Mathf.Clamp(data.chessRows, 1, 4);
                    showCols = Mathf.Clamp(data.showCols, 1, 4);
                    showRows = Mathf.Clamp(data.showRows, 1, 2);
                    sceneCols = Mathf.Clamp(data.sceneCols, 1, 12);
                    chessCellX = Mathf.Max(10, data.chessCellX);
                    chessCellY = Mathf.Max(10, data.chessCellY);
                    showCellX = Mathf.Max(10, data.showCellX);
                    showCellY = Mathf.Max(10, data.showCellY);
                    sceneSpacing = Mathf.Max(0.1f, data.sceneSpacing);
                    modFilterIndex = Mathf.Max(0, data.modFilterIndex);
                    return;
                }
            }
            //文件不存在/损坏: 按当前默认值写一份(用户提交后全队共享)
            SaveLayoutPrefs();
        }
        catch (Exception e)
        {
            LogUtil.LogError($"[幻化药测试] 读取布局设置失败({LayoutPrefsFile}): {e.Message}");
        }
    }

    /// <summary>
    /// 把列表布局/筛选设置写入项目内JSON(布局步进/筛选变更后调用)
    /// </summary>
    private static void SaveLayoutPrefs()
    {
        try
        {
            LayoutPrefsData data = new LayoutPrefsData
            {
                chessCols = chessCols, chessRows = chessRows,
                showCols = showCols, showRows = showRows,
                sceneCols = sceneCols,
                chessCellX = chessCellX, chessCellY = chessCellY,
                showCellX = showCellX, showCellY = showCellY,
                sceneSpacing = sceneSpacing,
                modFilterIndex = modFilterIndex,
            };
            File.WriteAllText(LayoutPrefsFile, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            LogUtil.LogError($"[幻化药测试] 写入布局设置失败({LayoutPrefsFile}): {e.Message}");
        }
    }

    #endregion

    #region 悬停交互(滚轮缩放/拖拽位置)

    /// <summary>
    /// 收集当前页签可交互项(单个预览=小卡/大卡/场景幻化模型[场景项需当前药配 show_res, 否则 world_data 真实链不消费]; 列表页=当前页全部可见项)
    /// </summary>
    private void CollectInteractiveItems(List<ListItem> outItems)
    {
        outItems.Clear();
        if (currentTab == PanelTab.Single)
        {
            if (currentPotionItemId == 0 || currentCreature == null) return;
            if (cardItem != null)
                outItems.Add(new ListItem { potionId = currentPotionItemId, cardRoot = cardItem.transform as RectTransform, cardIcon = cardItem.ui_Icon });
            if (cardDetails != null && hasDetailAdjust)
                outItems.Add(new ListItem { potionId = currentPotionItemId, cardRoot = cardDetails.transform as RectTransform, cardIcon = cardDetails.ui_Icon });
            if (sceneSpineTransform != null && showSceneSpine && hasShowRes)
                outItems.Add(new ListItem
                {
                    potionId = currentPotionItemId,
                    sceneObj = sceneSpineTransform.transform.parent.gameObject,
                    sceneRenderer = sceneSpineTransform.transform,
                    sceneBaseScale = currentCreature.creatureModel.size_spine * currentCreature.GetBodySizeScale(),
                });
            return;
        }
        foreach (var item in listItems)
        {
            if (item.editable) outItems.Add(item);
        }
    }

    /// <summary>
    /// 取列表项的GUI交互矩形(卡片=卡根屏幕矩形; 场景=主相机投影, 以脚下为底向上约240px的模型区域)
    /// </summary>
    private Rect GetItemGuiRect(ListItem item)
    {
        if (item.cardRoot != null) return GetCardGuiRect(item.cardRoot);
        Camera cam = CameraHandler.Instance.manager.mainCamera;
        if (cam == null) return Rect.zero;
        Vector3 sp = cam.WorldToScreenPoint(item.sceneObj.transform.position);
        return new Rect(sp.x - 80, Screen.height - sp.y - 240, 160, 250);
    }

    /// <summary>
    /// 处理悬停交互：滚轮=改缩放(等比×1.05/格), 左键拖拽=改位置(卡片按Canvas单位/场景按世界单位), 左侧面板区不响应
    /// </summary>
    private void HandleInteraction()
    {
        Event e = Event.current;
        if (e.mousePosition.x <= PanelWidth + 20 && dragItem == null) return;
        List<ListItem> items = new List<ListItem>();
        CollectInteractiveItems(items);
        if (items.Count == 0 && dragItem == null) return;
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                foreach (var item in items)
                {
                    if (!GetItemGuiRect(item).Contains(e.mousePosition)) continue;
                    dragItem = item;
                    dragKind = currentTab == PanelTab.ChessList ? DataKind.Show
                        : currentTab == PanelTab.ShowList ? DataKind.UiShow
                        : currentTab == PanelTab.SceneList ? DataKind.World
                        : (item.cardIcon != null && cardItem != null && item.cardIcon == cardItem.ui_Icon ? DataKind.Show
                            : item.cardIcon != null ? DataKind.UiShow : DataKind.World);
                    dragActivated = false;
                    dragStartMouse = e.mousePosition;
                    GetCurrentData(item.potionId, dragKind, out _, out Vector2 startPos, item);
                    dragStartPos = startPos;
                    e.Use();
                    break;
                }
                break;
            case EventType.MouseDrag when dragItem != null:
            {
                //未过阈值不移动(防误触微小拖动)
                if (!dragActivated)
                {
                    if ((e.mousePosition - dragStartMouse).magnitude < DragThresholdPx) { e.Use(); break; }
                    dragActivated = true;
                }
                Vector2 delta = e.mousePosition - dragStartMouse;
                Vector2 newPos;
                if (dragKind == DataKind.World)
                {
                    //屏幕像素→世界单位: 按相机视场在模型深度处的每像素世界尺寸换算; GUI y向下/世界 y向上, y取反
                    Camera cam = CameraHandler.Instance.manager.mainCamera;
                    float dist = Mathf.Abs(cam.transform.position.z - dragItem.sceneObj.transform.position.z);
                    float worldPerPixel = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
                    newPos = dragStartPos + new Vector2(delta.x * worldPerPixel, -delta.y * worldPerPixel);
                    newPos = new Vector2(Mathf.Round(newPos.x * 100f) / 100f, Mathf.Round(newPos.y * 100f) / 100f);
                }
                else
                {
                    //屏幕像素→Canvas单位: 除以scaleFactor; GUI y向下/UI y向上, y取反; 取整(与配置惯例一致)
                    Vector2 uiDelta = new Vector2(delta.x, -delta.y) / canvas.scaleFactor;
                    newPos = dragStartPos + uiDelta;
                    newPos = new Vector2(Mathf.RoundToInt(newPos.x), Mathf.RoundToInt(newPos.y));
                }
                GetCurrentData(dragItem.potionId, dragKind, out float curScale, out Vector2 curPos, dragItem);
                if (newPos != curPos)
                {
                    SetOverrideData(dragItem.potionId, dragKind, curScale, newPos);
                    DirectApply(dragItem, dragKind, curScale, newPos);
                    SyncSingleBuffersFromOverride(dragItem.potionId, dragKind);
                }
                e.Use();
                break;
            }
            case EventType.MouseUp when dragItem != null && e.button == 0:
                dragItem = null;
                dragActivated = false;
                e.Use();
                break;
            case EventType.ScrollWheel:
                foreach (var item in items)
                {
                    if (!GetItemGuiRect(item).Contains(e.mousePosition)) continue;
                    //场景列表按住 Alt 滚轮=调亮度(show_brightness 场景亮度系数), 不按住=改大小(world_data)
                    if (currentTab == PanelTab.SceneList && e.alt)
                    {
                        float brightness = GetCurrentBrightness(item.potionId);
                        brightness = Mathf.Clamp(brightness + (e.delta.y > 0 ? BrightnessStep : -BrightnessStep), BrightnessMin, BrightnessMax);
                        ApplyBrightness(item, brightness);
                        e.Use();
                        break;
                    }
                    DataKind kind = currentTab == PanelTab.ChessList ? DataKind.Show
                        : currentTab == PanelTab.ShowList ? DataKind.UiShow
                        : currentTab == PanelTab.SceneList ? DataKind.World
                        : (item.cardIcon != null && cardItem != null && item.cardIcon == cardItem.ui_Icon ? DataKind.Show
                            : item.cardIcon != null ? DataKind.UiShow : DataKind.World);
                    GetCurrentData(item.potionId, kind, out float scale, out Vector2 pos, item);
                    scale = Mathf.Max(0.001f, scale * (e.delta.y > 0 ? 1.05f : 1f / 1.05f));
                    SetOverrideData(item.potionId, kind, scale, pos);
                    DirectApply(item, kind, scale, pos);
                    SyncSingleBuffersFromOverride(item.potionId, kind);
                    e.Use();
                    break;
                }
                break;
        }
    }

    /// <summary>
    /// 把调参结果直接应用到列表项显示(卡片=图标缩放/坐标, 场景=Renderer缩放/偏移), 不走完整 SetData 防打断动画
    /// </summary>
    private void DirectApply(ListItem item, DataKind kind, float scale, Vector2 pos)
    {
        if (kind == DataKind.World)
        {
            item.sceneRenderer.localScale = Vector3.one * (item.sceneBaseScale * scale);
            item.sceneRenderer.localPosition = new Vector3(pos.x, pos.y, 0);
            return;
        }
        if (item.cardIcon == null) return;
        item.cardIcon.transform.localScale = Vector3.one * scale;
        item.cardIcon.rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 取幻化药当前生效的场景亮度系数（覆盖层优先 → 配置 show_brightness 键 → 默认 1=原亮度；基础样板 potionId=0 恒 1）
    /// </summary>
    /// <param name="potionId">幻化药道具完整id</param>
    /// <returns>亮度系数（1=原亮度，&lt;1调暗，&gt;1调亮）</returns>
    private float GetCurrentBrightness(long potionId)
    {
        if (potionId == 0) return 1f;
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(potionId);
        if (itemInfo == null) return 1f;
        string raw = CreatureBean.ParseTransformOtherData(itemInfo.other_data).showBrightness;
#if UNITY_EDITOR
        if (TransformPotionUITestOverride.TryGetShowBrightness(potionId, out string ov)) raw = ov;
#endif
        if (string.IsNullOrEmpty(raw) || !float.TryParse(raw, out float k) || k <= 0f) return 1f;
        return Mathf.Min(k, BrightnessMax);
    }

    /// <summary>
    /// 设置幻化药亮度覆盖并实时应用到场景列表项（写覆盖层 + 直接重跑亮度覆盖/清除，不走完整 SetData 防打断动画；k=1=强制原亮度）
    /// </summary>
    /// <param name="item">场景列表项</param>
    /// <param name="brightness">目标亮度系数（clamp 到 [BrightnessMin,BrightnessMax]）</param>
    private void ApplyBrightness(ListItem item, float brightness)
    {
        brightness = Mathf.Clamp(brightness, BrightnessMin, BrightnessMax);
        TransformPotionUITestOverride.SetShowBrightness(item.potionId, brightness.ToString("0.00"));
        if (item.sceneRenderer == null) return;
        SkeletonAnimation sk = item.sceneRenderer.GetComponent<SkeletonAnimation>();
        if (sk == null) return;
        if (Mathf.Approximately(brightness, 1f))
            SpineHandler.Instance.ClearSceneDimOverride(sk);
        else
            SpineHandler.Instance.ApplySceneDimOverride(sk, brightness);
    }

    /// <summary>
    /// 列表/场景调参后同步单个预览页的文本框缓冲(同一幻化药时保持两边一致)
    /// </summary>
    private void SyncSingleBuffersFromOverride(long potionId, DataKind kind)
    {
        if (currentTab != PanelTab.Single || potionId != currentPotionItemId) return;
        GetCurrentData(potionId, kind, out float scale, out Vector2 pos);
        string composed = ComposeUIData(scale, pos);
        if (kind == DataKind.Show)
        {
            editShowScale = FmtNum(scale); editShowX = FmtNum(pos.x); editShowY = FmtNum(pos.y);
            appliedShowData = composed;
        }
        else if (kind == DataKind.UiShow)
        {
            editUiShowScale = FmtNum(scale); editUiShowX = FmtNum(pos.x); editUiShowY = FmtNum(pos.y);
            appliedUiShowData = composed;
        }
        else
        {
            editWorldScale = FmtNum(scale); editWorldX = FmtNum(pos.x); editWorldY = FmtNum(pos.y);
            appliedWorldData = composed;
        }
    }

    /// <summary>
    /// 绘制列表项名称标签(卡片下方/模型脚下, ●=有未保存修改; 大卡列表无Avator的标注提示但同样可调)与每项小按钮(还原/0,0)
    /// </summary>
    private void DrawListLabels()
    {
        if (currentTab == PanelTab.Single) return;
        DataKind kind = currentTab == PanelTab.ChessList ? DataKind.Show : currentTab == PanelTab.ShowList ? DataKind.UiShow : DataKind.World;
        ListItem pendingRestore = null; //「还原」延迟到枚举结束后执行(RestoreItem→BuildListView→Clear 会就地修改 listItems 使枚举器失效)
        foreach (var item in listItems)
        {
            Rect rect = GetItemGuiRect(item);
            bool dirty = TransformPotionUITestOverride.HasOverride(item.potionId);
            //资源缺失标注: 场景列表=无 show_res(无世界幻化, world_data 真实链不消费), 卡片列表=无 ui_show_res(无Avator)
            string resNote = currentTab == PanelTab.SceneList
                ? (item.hasShowRes ? "" : "(无世界幻化)")
                : (item.hasUiShowRes ? "" : "(无Avator)");
            string text = (dirty ? "●" : "") + item.label + resNote;
            //场景列表追加当前生效 world_data 值(覆盖层优先的实际值): 同骨架同参数必等大, 值不同即定位到未保存覆盖差异
            if (currentTab == PanelTab.SceneList && item.potionId != 0)
            {
                GetCurrentData(item.potionId, DataKind.World, out float effScale, out Vector2 effPos);
                text += $" ×{FmtNum(effScale)} ({FmtNum(effPos.x)},{FmtNum(effPos.y)}) 亮{Mathf.RoundToInt(GetCurrentBrightness(item.potionId) * 100)}%";
            }
            Color old = GUI.color;
            if (!item.editable) GUI.color = Color.gray;
            else if (dirty) GUI.color = new Color(1f, 0.8f, 0.3f);
            GUI.Label(new Rect(rect.x - 30, rect.yMax + 2, rect.width + 60, 20), TruncateLabel(text, labelCenterStyle, rect.width + 60), labelCenterStyle);
            //场景列表调试读数行(按钮行下方, 只放短值防相邻项重叠; 资产/骨骼/动画等完整信息用「📋输出场景诊断」dump 到控制台)
            if (currentTab == PanelTab.SceneList && item.sceneRenderer != null)
            {
                SkeletonAnimation sk = item.sceneRenderer.GetComponent<SkeletonAnimation>();
                if (sk != null && sk.Skeleton != null)
                {
                    string skinName = sk.Skeleton.Skin != null ? sk.Skeleton.Skin.Name : "默认";
                    GUI.Label(new Rect(rect.x - 30, rect.yMax + 42, rect.width + 60, 20),
                        TruncateLabel($"实×{FmtNum(item.sceneRenderer.localScale.x)} 皮肤:{skinName}", labelCenterStyle, rect.width + 60), labelCenterStyle);
                }
            }
            GUI.color = old;
            //详情按钮(仅卡片列表, 与可否编辑无关, 第二排居中): 打开游戏生物展示弹窗查看该幻化形象与骨架全部动画
            if (currentTab != PanelTab.SceneList && GUI.Button(new Rect(rect.center.x - 21, rect.yMax + 42, 42, 18), "详情", smallButtonStyle))
                OpenCreatureShowDialog(item);
            if (!item.editable) continue;
            //每项小按钮: 还原=清该药本段覆盖恢复配置值(防误拖动一键恢复); 0,0=位置快速归零(保留缩放); 复制/粘贴=参数快速套用
            if (GUI.Button(new Rect(rect.center.x - 92, rect.yMax + 22, 42, 18), "还原", smallButtonStyle))
                pendingRestore = item;
            if (GUI.Button(new Rect(rect.center.x - 46, rect.yMax + 22, 42, 18), "0,0", smallButtonStyle))
                ZeroItemPos(item, kind);
            if (GUI.Button(new Rect(rect.center.x + 4, rect.yMax + 22, 42, 18), "复制", smallButtonStyle))
                CopyItem(item, kind);
            if (GUI.Button(new Rect(rect.center.x + 50, rect.yMax + 22, 42, 18), "粘贴", smallButtonStyle))
                PasteItem(item, kind);
            //亮度调节行(场景列表): 滑动条+数值显示+「亮原」还原亮度专用按钮(还原=回配置键值)
            if (currentTab == PanelTab.SceneList)
            {
                float curBright = GetCurrentBrightness(item.potionId);
                Rect brightRect = new Rect(rect.x - 30, rect.yMax + 62, 116, 18);
                float newBright = GUI.HorizontalSlider(brightRect, curBright, BrightnessMin, BrightnessMax);
                GUI.Label(new Rect(brightRect.xMax + 2, brightRect.y, 40, 18), $"{Mathf.RoundToInt(curBright * 100)}%", smallButtonStyle);
                if (GUI.Button(new Rect(brightRect.xMax + 42, brightRect.y, 30, 18), "亮原", smallButtonStyle))
                    RestoreItemBrightness(item);
                if (!Mathf.Approximately(newBright, curBright))
                    ApplyBrightness(item, Mathf.Round(newBright / BrightnessStep) * BrightnessStep);
            }
        }
        if (pendingRestore != null) RestoreItem(pendingRestore);
    }

    /// <summary>
    /// 按显示宽度截断文本(超出末尾加…), 防单行标签文本过长换行溢出显示不全
    /// </summary>
    /// <param name="text">原文本</param>
    /// <param name="style">绘制样式(按其实际字号计算宽度)</param>
    /// <param name="maxWidth">可用最大宽度(px)</param>
    private static string TruncateLabel(string text, GUIStyle style, float maxWidth)
    {
        if (style.CalcSize(new GUIContent(text)).x <= maxWidth) return text;
        for (int len = text.Length - 1; len > 0; len--)
        {
            string candidate = text.Substring(0, len) + "…";
            if (style.CalcSize(new GUIContent(candidate)).x <= maxWidth) return candidate;
        }
        return "…";
    }

    /// <summary>
    /// 输出场景列表诊断到控制台(按 ` 键打开 UITestConsole 查看/Unity Console 均有): 逐项列出 数据值/sceneBaseScale/期望与实际Renderer缩放/
    /// 骨架资产名与实例ID/皮肤/骨骼ScaleX,Y/当前动画与轨道时间/坐标——"数据相同但渲染不同"时逐项对比, 不同的一列即差异源头
    /// </summary>
    private void LogSceneListDiagnostics()
    {
        Camera cam = CameraHandler.Instance.manager.mainCamera;
        System.Text.StringBuilder sb = new System.Text.StringBuilder($"[幻化药场景列表诊断] 共{listItems.Count}项 相机pos={(cam != null ? cam.transform.position.ToString() : "null")} fov={(cam != null ? cam.fieldOfView : 0)} ortho={(cam != null && cam.orthographic)}");
        foreach (var item in listItems)
        {
            if (item.sceneObj == null || item.sceneRenderer == null) continue;
            GetCurrentData(item.potionId, DataKind.World, out float effScale, out Vector2 effPos);
            sb.Append($"\n[{item.potionId}] {item.label} | 数据×{FmtNum(effScale)} ({FmtNum(effPos.x)},{FmtNum(effPos.y)}) | base×{FmtNum(item.sceneBaseScale)} 期望×{FmtNum(item.sceneBaseScale * effScale)} 实×{FmtNum(item.sceneRenderer.localScale.x)} | 根pos={item.sceneObj.transform.position} renderer局部pos={item.sceneRenderer.localPosition}");
            SkeletonAnimation sk = item.sceneRenderer.GetComponent<SkeletonAnimation>();
            if (sk == null || sk.Skeleton == null) { sb.Append(" | SkeletonAnimation缺失"); continue; }
            Spine.TrackEntry track = sk.AnimationState != null ? sk.AnimationState.GetTrack(0) : null;
            sb.Append($" | 资产={sk.skeletonDataAsset.name}#{sk.skeletonDataAsset.GetInstanceID()}(导入scale={sk.skeletonDataAsset.scale:0.#####}) | 皮肤={(sk.Skeleton.Skin != null ? sk.Skeleton.Skin.Name : "默认")} | 骨骼Scale=({sk.Skeleton.ScaleX:0.####},{sk.Skeleton.ScaleY:0.####}) | 动画={(track != null ? track.Animation.Name : "无")} t={(track != null ? track.TrackTime.ToString("0.00") : "-")} 速率={(track != null ? track.TimeScale.ToString("0.##") : "-")}");
        }
        LogUtil.Log(sb.ToString());
    }

    /// <summary>
    /// 还原列表项: 清该药当前数据段的覆盖并重建列表(走真实显示链按配置值重绘; 亮度有专用「亮原」按钮, 此处不含 show_brightness)
    /// </summary>
    private void RestoreItem(ListItem item)
    {
        if (currentTab == PanelTab.ChessList) TransformPotionUITestOverride.ClearShowData(item.potionId);
        else if (currentTab == PanelTab.ShowList) TransformPotionUITestOverride.ClearUiShowData(item.potionId);
        else TransformPotionUITestOverride.ClearWorldData(item.potionId);
        BuildListView();
    }

    /// <summary>
    /// 还原列表项亮度(「亮原」按钮): 清 show_brightness 覆盖回到配置键值, 并按配置重跑亮度覆盖(配置有键=应用配置系数, 无键=清除还原亮度)
    /// </summary>
    private void RestoreItemBrightness(ListItem item)
    {
        TransformPotionUITestOverride.ClearShowBrightness(item.potionId);
        if (item.sceneRenderer == null) return;
        SkeletonAnimation sk = item.sceneRenderer.GetComponent<SkeletonAnimation>();
        if (sk == null) return;
        //覆盖层已清, GetCurrentBrightness 此时返回配置键值(无键=1)
        float k = GetCurrentBrightness(item.potionId);
        if (Mathf.Approximately(k, 1f))
            SpineHandler.Instance.ClearSceneDimOverride(sk);
        else
            SpineHandler.Instance.ApplySceneDimOverride(sk, k);
    }

    /// <summary>
    /// 位置快速归零: 保留当前缩放, 位置设为0,0(直接应用不打断动画)
    /// </summary>
    private void ZeroItemPos(ListItem item, DataKind kind)
    {
        GetCurrentData(item.potionId, kind, out float scale, out _, item);
        SetOverrideData(item.potionId, kind, scale, Vector2.zero);
        DirectApply(item, kind, scale, Vector2.zero);
        SyncSingleBuffersFromOverride(item.potionId, kind);
    }

    /// <summary>
    /// 复制列表项当前参数到剪贴板(覆盖层优先的当前有效值, 记录数据段标签 clipKind)
    /// </summary>
    private void CopyItem(ListItem item, DataKind kind)
    {
        GetCurrentData(item.potionId, kind, out clipScale, out clipPos, item);
        clipKind = kind;
        clipHasValue = true;
    }

    /// <summary>
    /// 数据段显示名(剪贴板跨段提示用)
    /// </summary>
    private static string KindName(DataKind kind)
    {
        return kind == DataKind.Show ? "小卡(show_data)" : kind == DataKind.UiShow ? "大卡(ui_show_data)" : "场景(world_data)";
    }

    /// <summary>
    /// 把剪贴板参数粘贴到列表项(写覆盖层并直接应用, 同段项快速套用; 跨段拒绝——三段参数单位/语义不同, 串段粘贴必产生错误值)
    /// </summary>
    private void PasteItem(ListItem item, DataKind kind)
    {
        if (!clipHasValue) return;
        if (kind != clipKind)
        {
            SetSaveMessage($"⚠ 剪贴板是{KindName(clipKind)}参数，不能粘贴到{KindName(kind)}", true);
            return;
        }
        SetOverrideData(item.potionId, kind, clipScale, clipPos);
        DirectApply(item, kind, clipScale, clipPos);
        SyncSingleBuffersFromOverride(item.potionId, kind);
    }

    /// <summary>
    /// 一键把剪贴板参数粘贴到当前筛选(Mod筛选)下的全部场景项(同MOD骨架大小相近, 批量套用免逐项粘贴)：
    /// 只写覆盖层(仍走「保存全部修改」统一写回Excel), 无 show_res 项跳过(world_data 不被真实链消费, 贴了是假脏数据),
    /// 完成后重建列表按真实链重绘(同时重跑亮度覆盖), 跨段剪贴板拒绝(与 PasteItem 同规约)
    /// </summary>
    private void PasteClipboardToAllScenes()
    {
        if (!clipHasValue) { SetSaveMessage("⚠ 剪贴板为空，请先在某项上点[复制]", true); return; }
        if (clipKind != DataKind.World)
        {
            SetSaveMessage($"⚠ 剪贴板是{KindName(clipKind)}参数，不能粘贴到{KindName(DataKind.World)}", true);
            return;
        }
        List<SelectItem> potions = GetFilteredPotions();
        if (potions == null || potions.Count == 0) { SetSaveMessage("⚠ 当前筛选无幻化药", true); return; }
        int applied = 0, skipNoRes = 0;
        foreach (SelectItem potion in potions)
        {
            ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(potion.id);
            if (itemInfo == null) continue;
            if (CreatureBean.ParseTransformOtherData(itemInfo.other_data).showRes.IsNull()) { skipNoRes++; continue; }
            SetOverrideData(potion.id, DataKind.World, clipScale, clipPos);
            applied++;
        }
        BuildListView();
        string msg = $"✓ 已把 {ComposeUIData(clipScale, clipPos)} 套用到{applied}个场景项(world_data)，待「💾保存全部修改」写回Excel";
        if (skipNoRes > 0) msg += $"，跳过无世界幻化{skipNoRes}个";
        SetSaveMessage(msg, false);
    }

    /// <summary>
    /// 取卡片根的GUI坐标矩形(覆盖层Canvas世界坐标=屏幕像素左下原点, 转GUI左上原点)
    /// </summary>
    private Rect GetCardGuiRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, Screen.height - corners[1].y, corners[2].x, Screen.height - corners[0].y);
    }

    #endregion

    #region 生物展示弹窗(列表项详情按钮)

    /// <summary>
    /// 打开列表项的生物展示弹窗(详情按钮): 按真实链构建带幻化的生物数据(与卡片同源 BuildCreature, transformItemId=该项药),
    /// 复用游戏 UIDialogCreatureShow——展示生物形象与骨架全部动画列表(点动画名即播放);
    /// 小卡列表=显示小卡本身的 Chess 基础形象(show 骨架, isShowChessSpine=true), 大卡列表=显示 ui_show 高清形象(无Avator自动回落show形象);
    /// 打开期间进入详情模式=隐藏覆盖层Canvas + OnGUI 整体跳过(覆盖层 sortingOrder=5000 且 IMGUI 恒渲染在最上层, 不隐藏会遮挡弹窗),
    /// 弹窗销毁回调(点背景关闭, isDestroyBG=true)自动退出详情模式恢复面板
    /// </summary>
    /// <param name="item">列表项</param>
    private void OpenCreatureShowDialog(ListItem item)
    {
        if (creatureShowDialog != null) return;
        CreatureBean creatureData = BuildCreature(item.potionId);
        if (creatureData == null) return;
        DialogCreatureShowBean dialogData = new DialogCreatureShowBean
        {
            creatureData = creatureData,
            isShowChessSpine = currentTab == PanelTab.ChessList,
            actionDestoryAfter = (_) => CloseCreatureShowDialog(),
        };
        creatureShowDialog = UIHandler.Instance.ShowDialogCreatureShow(dialogData);
        SetDetailMode(true);
    }

    /// <summary>
    /// 详情弹窗销毁回调: 退出详情模式恢复测试面板显示
    /// </summary>
    private void CloseCreatureShowDialog()
    {
        creatureShowDialog = null;
        SetDetailMode(false);
    }

    /// <summary>
    /// 切换详情模式: 隐藏/恢复覆盖层Canvas(卡片列表网格); IMGUI面板由 OnGUI 入口按 creatureShowDialog 判空整体跳过
    /// </summary>
    /// <param name="isDetail">true=进入详情模式(隐藏测试面板)</param>
    private void SetDetailMode(bool isDetail)
    {
        if (canvas != null) canvas.enabled = !isDetail;
    }

    #endregion

    #region 测试场景加载(场景列表真实光照预览)

    /// <summary>懒加载测试场景候选（首项=基地特殊档，其后 FightSceneCfg 全量按 id 排序：森林×4/沙漠×2/皇宫/平原×4）</summary>
    private void InitTestSceneRows()
    {
        if (testSceneRows != null) return;
        testSceneRows = new List<TestSceneOption> { new TestSceneOption { label = "基地", fightScene = null } };
        var all = new List<FightSceneBean>(FightSceneCfg.GetAllData().Values);
        all.Sort((a, b) => a.id.CompareTo(b.id));
        foreach (FightSceneBean row in all)
            testSceneRows.Add(new TestSceneOption { label = row.remark, fightScene = row });
    }

    /// <summary>
    /// 绘制测试场景选择区（下拉列表：无场景/基地/各战斗场景变体），卸载还原按钮；
    /// 只还原天空盒/雾/全局环境光/Details 显隐——体积雾与景深不进测试预览（标注提示）
    /// </summary>
    private void DrawTestSceneSection()
    {
        InitTestSceneRows();
        GUILayout.Space(4);
        GUILayout.Label("── 测试场景（真实场景光照预览；体积雾/景深不还原）──", hintStyle);
        string curName = testSceneIndex >= 0 && testSceneIndex < testSceneRows.Count ? testSceneRows[testSceneIndex].label : "无场景";
        GUILayout.BeginHorizontal();
        GUILayout.Label("场景:", labelStyle, GUILayout.Width(36));
        if (GUILayout.Button(curName + (isSceneDropdownOpen ? " ▲" : " ▼"), buttonLeftStyle, GUILayout.Height(24), GUILayout.Width(170)))
            isSceneDropdownOpen = !isSceneDropdownOpen;
        if (testSceneIndex >= 0 && GUILayout.Button("卸载还原", GUILayout.Width(64)))
        {
            isSceneDropdownOpen = false;
            UnloadTestScene();
        }
        GUILayout.EndHorizontal();
        if (isSceneDropdownOpen)
        {
            scrollSceneDropdown = GUILayout.BeginScrollView(scrollSceneDropdown, GUI.skin.box, GUILayout.Height(180));
            //首项=无场景档(选中即卸载还原)
            Color oldColor = GUI.color;
            if (testSceneIndex < 0) GUI.color = Color.green;
            if (GUILayout.Button(testSceneIndex < 0 ? "✔ 无场景" : "无场景", buttonLeftStyle, GUILayout.Height(22)))
            {
                GUI.color = oldColor;
                isSceneDropdownOpen = false;
                UnloadTestScene();
            }
            else GUI.color = oldColor;
            for (int i = 0; i < testSceneRows.Count; i++)
            {
                bool isCurrent = i == testSceneIndex;
                oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                if (GUILayout.Button(isCurrent ? $"✔ {testSceneRows[i].label}" : testSceneRows[i].label, buttonLeftStyle, GUILayout.Height(22)))
                {
                    GUI.color = oldColor;
                    isSceneDropdownOpen = false;
                    if (i != testSceneIndex && !testSceneLoading)
                    {
                        UnloadTestScene();
                        testSceneIndex = i;
                        _ = LoadTestSceneAsync(testSceneRows[i]);
                    }
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
        if (testSceneLoading) GUILayout.Label("加载中…", hintStyle);
    }

    /// <summary>
    /// 异步加载测试场景：与游戏同路径同加载器实例化场景预制体到原点，随后应用光照还原；
    /// await 期间面板关闭/已卸载时销毁到手的实例防残留
    /// </summary>
    /// <param name="opt">测试场景候选项（fightScene=null=基地档）</param>
    private async Cysharp.Threading.Tasks.UniTaskVoid LoadTestSceneAsync(TestSceneOption opt)
    {
        testSceneLoading = true;
        CacheTestEnv();
        GameObject sceneObj;
        Material skyMat = null;
        if (opt.fightScene != null)
        {
            sceneObj = await WorldHandler.Instance.manager.GetFightScene($"{PathInfo.FightScenePrefabPath}/{opt.fightScene.name_res}");
            if (!opt.fightScene.skybox_mat.IsNull())
                skyMat = await WorldHandler.Instance.manager.GetSkybox(opt.fightScene.skybox_mat);
        }
        else
        {
            //基地档: 与游戏同加载器(WorldManager.GetBaseScene)
            sceneObj = await WorldHandler.Instance.manager.GetBaseScene();
        }
        if (this == null || testSceneIndex < 0)
        {
            //面板已关/已卸载：销毁到手的实例防残留
            if (sceneObj != null) Destroy(sceneObj);
            return;
        }
        if (sceneObj == null)
        {
            LogUtil.LogError($"[幻化药测试] 测试场景加载失败: {opt.label}");
            testSceneLoading = false;
            return;
        }
        testSceneObj = sceneObj;
        testSceneObj.transform.position = Vector3.zero;
        testSceneObj.transform.eulerAngles = Vector3.zero;
        //禁用面板原有灯光(如 TestScene 默认 Directional Light), 防与测试场景自带灯光叠加双份光照
        SetTestEnvLightsRestore(false);
        if (opt.fightScene != null)
        {
            testSceneObj.name = $"TransformPotionTestScene_{opt.fightScene.name_res}";
            ApplyTestSceneLighting(opt.fightScene, skyMat);
        }
        else
        {
            testSceneObj.name = "TransformPotionTestScene_Base";
            //销毁基地业务组件: 其 Update 依赖基地控制器(测试场景无), 不销毁会每帧空引用; 光照所需的 Light/渲染器不受影响
            var baseComp = testSceneObj.GetComponent<ScenePrefabForBase>();
            if (baseComp != null) Destroy(baseComp);
            //基地观感还原: 环境光白(游戏 GameScene Flat 白) + 纯色深底(游戏基地 RemoveSkybox+SolidColor #080613) + 无雾
            RenderSettings.ambientLight = Color.white;
            WorldHandler.Instance.manager.SetSkyboxColor(CameraClearFlags.SolidColor, new Color(0.031f, 0.024f, 0.075f));
            WorldHandler.Instance.manager.RemoveSkybox();
            VolumeHandler.Instance.SetFogActive(false);
        }
        testSceneLoading = false;
    }

    /// <summary>
    /// 应用测试场景光照（精简版 WorldHandler.InitData）：天空盒+旋转、雾（未配置=关闭）、
    /// 全局环境光（未配置=置白——与游戏 GameScene 默认 Flat 白一致，保证过曝场景还原度）、Details 显隐
    /// </summary>
    private void ApplyTestSceneLighting(FightSceneBean data, Material skyMat)
    {
        if (skyMat != null)
        {
            RenderSettings.skybox = skyMat;
            Vector3 skyboxRotate = data.GetSkyboxRotate();
            RenderSettings.skybox.SetFloat("_RotateX", skyboxRotate.x);
            RenderSettings.skybox.SetFloat("_RotateY", skyboxRotate.y);
            RenderSettings.skybox.SetFloat("_RotateZ", skyboxRotate.z);
        }
        if (data.HasFog && data.GetFogParams(out var fogColor, out var fogStart, out var fogEnd, out var fogMode))
            VolumeHandler.Instance.SetFog(fogColor, fogMode, fogStart, fogEnd, isActive: true);
        else
            VolumeHandler.Instance.SetFogActive(false);
        //未配置 ambient_light 的场景在游戏里沿用 GameScene 的 Flat 白, 这里对齐置白而非「不修改」(测试面板环境光未知)
        RenderSettings.ambientLight = data.HasAmbientLight ? data.GetAmbientLightColor() : Color.white;
        ApplyDetailsVisibility(data);
    }

    /// <summary>
    /// Details 显隐（与 WorldHandler.HandleFightSceneDetails 同语义）：配置了 details=只显示 Details 下同名子预制、其余隐藏；未配置=整个 Details 隐藏；无 Details 节点不处理
    /// </summary>
    private void ApplyDetailsVisibility(FightSceneBean data)
    {
        if (testSceneObj == null) return;
        Transform detailsRoot = testSceneObj.transform.Find("Details");
        if (detailsRoot == null) return;
        if (string.IsNullOrEmpty(data.details))
        {
            detailsRoot.gameObject.SetActive(false);
            return;
        }
        detailsRoot.gameObject.SetActive(true);
        bool isFind = false;
        foreach (Transform child in detailsRoot)
        {
            bool show = child.name == data.details;
            child.gameObject.SetActive(show);
            if (show) isFind = true;
        }
        if (!isFind)
            LogUtil.LogWarning($"[幻化药测试] 场景 {data.name_res} 配置了细节预制 {data.details}，但 Details 节点下没有找到同名子物体");
    }

    /// <summary>缓存面板进入时的环境（首个测试场景加载时一次；卸载时还原）</summary>
    private void CacheTestEnv()
    {
        if (hasCacheTestEnv) return;
        cacheEnvAmbient = RenderSettings.ambientLight;
        cacheEnvSkybox = RenderSettings.skybox;
        cacheEnvFog = RenderSettings.fog;
        cacheEnvFogColor = RenderSettings.fogColor;
        cacheEnvFogStart = RenderSettings.fogStartDistance;
        cacheEnvFogEnd = RenderSettings.fogEndDistance;
        cacheEnvFogMode = RenderSettings.fogMode;
        Camera cam = CameraHandler.Instance.manager.mainCamera;
        if (cam != null)
        {
            cacheEnvCamClearFlags = cam.clearFlags;
            cacheEnvCamBgColor = cam.backgroundColor;
        }
        //此刻测试场景实例尚未加载, 全场 Light 均为面板原有灯(如 TestScene 默认 Directional Light), 记录备用
        cacheEnvLights = FindObjectsOfType<Light>();
        cacheEnvLightsEnabled = new bool[cacheEnvLights.Length];
        for (int i = 0; i < cacheEnvLights.Length; i++)
            cacheEnvLightsEnabled[i] = cacheEnvLights[i].enabled;
        hasCacheTestEnv = true;
    }

    /// <summary>禁用/恢复面板原有灯光（加载测试场景后禁用防双份光照叠加；卸载时按各自原状恢复）</summary>
    /// <param name="restore">true=恢复原状，false=全部禁用</param>
    private void SetTestEnvLightsRestore(bool restore)
    {
        if (cacheEnvLights == null) return;
        for (int i = 0; i < cacheEnvLights.Length; i++)
        {
            if (cacheEnvLights[i] != null && i < cacheEnvLightsEnabled.Length)
                cacheEnvLights[i].enabled = restore ? cacheEnvLightsEnabled[i] : false;
        }
    }

    /// <summary>还原缓存的面板环境（卸载测试场景时）</summary>
    private void RestoreTestEnv()
    {
        if (!hasCacheTestEnv) return;
        VolumeHandler.Instance.SetFogActive(false);
        RenderSettings.ambientLight = cacheEnvAmbient;
        RenderSettings.skybox = cacheEnvSkybox;
        RenderSettings.fog = cacheEnvFog;
        RenderSettings.fogColor = cacheEnvFogColor;
        RenderSettings.fogStartDistance = cacheEnvFogStart;
        RenderSettings.fogEndDistance = cacheEnvFogEnd;
        RenderSettings.fogMode = cacheEnvFogMode;
        Camera cam = CameraHandler.Instance.manager.mainCamera;
        if (cam != null)
        {
            cam.clearFlags = cacheEnvCamClearFlags;
            cam.backgroundColor = cacheEnvCamBgColor;
        }
        //恢复面板原有灯光到各自原状
        SetTestEnvLightsRestore(true);
        hasCacheTestEnv = false;
    }

    /// <summary>卸载测试场景并还原环境（切换场景/选「无场景」档/关面板时调用，幂等）</summary>
    private void UnloadTestScene()
    {
        if (testSceneObj != null)
        {
            Destroy(testSceneObj);
            testSceneObj = null;
        }
        testSceneIndex = -1;
        //释放 manager 持有的天空盒加载句柄(GetSkybox 会覆写占用), 再还原面板环境
        WorldHandler.Instance.manager.RemoveSkybox();
        RestoreTestEnv();
    }

    #endregion

    #region 保存写回Mod项目

    /// <summary>
    /// 一键保存全部未保存修改：①Mod道具Excel(唯一真实源, 会话首次写入前备份到 Mod项目/ExcelBackup(滚动复用.bak.1~3只留最近3份), 批量一次写盘)
    /// ②Mod项目与主项目部署副本两处 ItemsInfo.txt ③当前会话内存配置；最后清对应覆盖层并刷新当前页签显示
    /// </summary>
    private void SaveAllOverrides()
    {
        List<long> dirtyIds = TransformPotionUITestOverride.GetAllDirtyIds();
        if (dirtyIds.Count == 0) { SetSaveMessage("没有未保存的修改", true); return; }
        //仅 Mod 幻化药可写回(完整id含modId前缀); 内置幻化药需改主项目自己的Excel, 批量保存跳过并计数
        List<long> modIds = new List<long>();
        int skipBuiltin = 0;
        foreach (long id in dirtyIds)
        {
            if (id >= ModIdDivisor) modIds.Add(id);
            else skipBuiltin++;
        }
        if (modIds.Count == 0) { SetSaveError($"⚠ 可保存的Mod幻化药为0（跳过内置幻化药{skipBuiltin}个，内置请改主项目Excel）"); return; }
        string modRoot = UnityEditor.EditorPrefs.GetString("ModBuildEditorWindow.ModProjectPath", "");
        if (modRoot.IsNull() || !Directory.Exists(Path.Combine(modRoot, "Assets")))
        {
            SetSaveError("⚠ 未配置有效的Mod项目路径（请先在「Mod构建工具」里设置Mod项目路径）");
            return;
        }
        //modId→modName 映射(保存按 Mod 分组路由: 各 Mod 有独立的道具Excel与JsonText)
        Dictionary<int, string> modNames = new Dictionary<int, string>();
        foreach (var (modId, modName, _) in ModHandler.Instance.manager.GetModJsonTextFileInfos("ItemsInfo"))
            modNames[modId] = modName;
        //逐药组新 other_data: 只换 ui_show_data/show_data/world_data 三键(覆盖层优先, 无覆盖保留原值——直接改写结构体字段), 其余键(ui_show_skin/idle_anim/ui_show_idle_anim等)原样保留; 按 modId 分组
        Dictionary<int, Dictionary<long, string>> saveDataByMod = new Dictionary<int, Dictionary<long, string>>();
        List<string> missingCfg = new List<string>();
        foreach (long id in modIds)
        {
            ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(id);
            if (itemInfo == null) { missingCfg.Add($"{id % ModIdDivisor}"); continue; }
            TransformOtherData data = CreatureBean.ParseTransformOtherData(itemInfo.other_data);
            if (TransformPotionUITestOverride.TryGetUiShowData(id, out string ovUiShow)) data.uiShowData = ovUiShow;
            if (TransformPotionUITestOverride.TryGetShowData(id, out string ovShow)) data.showData = ovShow;
            if (TransformPotionUITestOverride.TryGetWorldData(id, out string ovWorld)) data.worldData = ovWorld;
            if (TransformPotionUITestOverride.TryGetShowBrightness(id, out string ovBright)) data.showBrightness = ovBright;
            int modId = (int)(id / ModIdDivisor);
            if (!saveDataByMod.TryGetValue(modId, out Dictionary<long, string> group))
            {
                group = new Dictionary<long, string>();
                saveDataByMod[modId] = group;
            }
            group[id % ModIdDivisor] = BuildOtherData(data);
        }
        if (saveDataByMod.Count == 0) { SetSaveError($"⚠ 配置全部缺失: {string.Join(",", missingCfg)}"); return; }

        //①+② 按 Mod 分组写: Excel(唯一真实源,批量一次写盘) + 两处 JsonText 直补(Mod项目导出物 + 主项目部署副本)
        int savedTotal = 0, jsonPatchedTotal = 0, jsonExpectedTotal = 0;
        List<long> notFoundAll = new List<long>();
        List<string> unknownMod = new List<string>();
        HashSet<int> writtenModIds = new HashSet<int>();    //实际写盘成功的modId(③只同步这些组, 未落盘的保留覆盖层供重试)
        HashSet<long> notFoundFullIds = new HashSet<long>(); //Excel缺行的药完整id(③跳过不清覆盖层——Excel未落盘, JsonText直补会在下次export被盖回)
        foreach (var kvp in saveDataByMod)
        {
            if (!modNames.TryGetValue(kvp.Key, out string modName))
            {
                unknownMod.Add($"modId={kvp.Key}({kvp.Value.Count}个)");
                continue;
            }
            Dictionary<long, string> saveData = kvp.Value;
            //① Excel(唯一真实源, 批量一次写盘)
            string excelPath = Path.Combine(modRoot, GetModItemsExcelRelPath(modName));
            if (!File.Exists(excelPath)) { SetSaveError($"⚠ 找不到Mod道具Excel({modName}):\n{excelPath}"); return; }
            if (!excelBackupDonePaths.Contains(excelPath) && !TryBackupExcel(excelPath, modRoot, out string backupErr)) { SetSaveError(backupErr); return; }
            if (!TryWriteExcelOtherDataBatch(excelPath, saveData, out string excelErr, out List<long> notFound)) { SetSaveError(excelErr); return; }
            notFoundAll.AddRange(notFound);
            foreach (long selfId in notFound) notFoundFullIds.Add(kvp.Key * ModIdDivisor + selfId);
            //② 两处 JsonText 直补(均为gen脚本产物的单行紧凑JSON数组)
            string modJson = Path.Combine(modRoot, $"Mods/{modName}/JsonText/ItemsInfo.txt");
            string mainJson = Path.Combine(Application.dataPath, $"../Mods/{modName}/JsonText/ItemsInfo.txt");
            jsonExpectedTotal += 2;
            if (File.Exists(modJson) && TryPatchItemsInfoJsonBatch(modJson, saveData)) jsonPatchedTotal++;
            if (File.Exists(mainJson) && TryPatchItemsInfoJsonBatch(mainJson, saveData)) jsonPatchedTotal++;
            writtenModIds.Add(kvp.Key);
            savedTotal += saveData.Count;
        }

        //③ 当前会话内存同步 + 清覆盖层 + 刷新当前页签(仅实际落盘的Mod——未写盘成功的组/Excel缺行的药保留覆盖层, 供修复问题后重试保存)
        foreach (long id in modIds)
        {
            int modId = (int)(id / ModIdDivisor);
            if (!writtenModIds.Contains(modId)) continue;
            if (notFoundFullIds.Contains(id)) continue;
            if (!saveDataByMod.TryGetValue(modId, out Dictionary<long, string> group)) continue;
            ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(id);
            if (itemInfo != null) itemInfo.other_data = group[id % ModIdDivisor];
            TransformPotionUITestOverride.Clear(id);
        }
        if (currentTab == PanelTab.Single) { LoadEditBuffers(); RefreshCards(); }
        else BuildListView();
        //落盘完整性分级：0个落盘=整体失败(弹窗强提示, 防"已保存0个"绿勾被误读为成功)；有落盘但存在Excel缺行/未知Mod跳过/配置缺失=部分丢失(红字警告)；跳过内置药/JsonText副本缺失不算丢失(Excel才是唯一真实源, JsonText可由export再生)
        string msg = savedTotal > 0
            ? $"✓ 已保存{savedTotal}个幻化药（Excel✓ + JsonText×{jsonPatchedTotal}/{jsonExpectedTotal} + 当前会话✓）"
            : "✗ 已保存0个幻化药（Excel未写入，修改只留在本次Play会话内存）";
        if (skipBuiltin > 0) msg += $"，跳过内置{skipBuiltin}个";
        if (notFoundAll.Count > 0) msg += $"，Excel缺行:{string.Join(",", notFoundAll)}";
        if (unknownMod.Count > 0) msg += $"，未知Mod跳过:{string.Join(",", unknownMod)}";
        if (missingCfg.Count > 0) msg += $"，配置缺失:{string.Join(",", missingCfg)}";
        bool hasLoss = notFoundAll.Count > 0 || unknownMod.Count > 0 || missingCfg.Count > 0;
        if (savedTotal == 0) SetSaveError(msg);
        else SetSaveMessage(msg, hasLoss);
    }

    /// <summary>
    /// 设置保存结果提示(红=失败/绿=成功)
    /// </summary>
    private void SetSaveMessage(string msg, bool isError)
    {
        saveMessage = msg;
        saveMessageIsError = isError;
    }

    /// <summary>
    /// 设置保存失败提示：底部红字 + 弹窗强提示（防红字小字被忽略——失败时修改只活在本次Play会话内存覆盖层, 退出即丢失）
    /// </summary>
    private void SetSaveError(string msg)
    {
        SetSaveMessage(msg, true);
        UnityEditor.EditorUtility.DisplayDialog("幻化药保存失败", $"{msg}\n\n（修改目前只生效于本次Play会话内存，未写回Excel，退出即丢失）", "知道了");
    }

    /// <summary>
    /// 拼 other_data 键值串(&amp;拆项, :拆键值, 缺省键省略)——与 gen_aeonsecho_spine_mod.py / gen_arkre_spine_mod.py / gen_nikke_spine_mod.py 的 build_other_data 同规约；
    /// 键集合=TransformOtherData 结构体字段, 新增键只需加一行拼接
    /// </summary>
    private static string BuildOtherData(TransformOtherData data)
    {
        string result = "";
        if (!data.showRes.IsNull()) result = $"show_res:{data.showRes}";
        if (!data.uiShowRes.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}ui_show_res:{data.uiShowRes}";
        if (!data.uiShowData.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}ui_show_data:{data.uiShowData}";
        if (!data.showData.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}show_data:{data.showData}";
        if (!data.worldData.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}world_data:{data.worldData}";
        if (!data.showBrightness.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}show_brightness:{data.showBrightness}";
        if (!data.uiShowSkin.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}ui_show_skin:{data.uiShowSkin}";
        if (!data.idleAnim.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}idle_anim:{data.idleAnim}";
        if (!data.uiShowIdleAnim.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}ui_show_idle_anim:{data.uiShowIdleAnim}";
        if (!data.walkAnim.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}walk_anim:{data.walkAnim}";
        if (!data.attackAnim.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}attack_anim:{data.attackAnim}";
        if (!data.deadAnim.IsNull()) result += $"{(result.Length > 0 ? "&" : "")}dead_anim:{data.deadAnim}";
        return result;
    }

    /// <summary>
    /// 按 Mod 名取 Mod 道具 Excel 相对路径(唯一真实源)的约定：AeonsEchoSpine=历史文件名(无后缀)；
    /// 后续 Mod=excel_mod_items_info_{modName小写}[Mod道具信息-{modName}].xlsx(与 gen_{mod}_spine_mod.py 脚本常量一致)
    /// </summary>
    private static string GetModItemsExcelRelPath(string modName)
    {
        if (modName == "AeonsEchoSpine")
            return "Assets/Data/Excel/excel_mod_items_info[Mod道具信息].xlsx";
        return $"Assets/Data/Excel/excel_mod_items_info_{modName.ToLower()}[Mod道具信息-{modName}].xlsx";
    }

    /// <summary>
    /// 会话首次写入前备份Excel到 Mod项目/ExcelBackup/(Assets之外不会被导出工具扫描)；
    /// 滚动复用 .bak.1~.bak.3(1=最新)——移位覆盖旧文件不新增, 目录里永远只有最近3份, 并顺带清掉旧版时间戳命名备份
    /// </summary>
    private static bool TryBackupExcel(string excelPath, string modRoot, out string error)
    {
        error = null;
        try
        {
            string backupDir = Path.Combine(modRoot, "ExcelBackup");
            Directory.CreateDirectory(backupDir);
            string name = Path.GetFileName(excelPath);
            // 移位覆盖：bak.3←bak.2、bak.2←bak.1（复用旧文件，不产生新文件）
            for (int i = 3; i > 1; i--)
            {
                string newer = Path.Combine(backupDir, $"{name}.bak.{i - 1}");
                string older = Path.Combine(backupDir, $"{name}.bak.{i}");
                if (File.Exists(newer))
                {
                    if (File.Exists(older))
                        File.Delete(older);
                    File.Move(newer, older);
                }
            }
            string bakPath = Path.Combine(backupDir, $"{name}.bak.1");
            File.Copy(excelPath, bakPath, true);
            // 清理非滚动命名的旧备份（时间戳版等），保证永远都只有 3 份
            HashSet<string> keep = new HashSet<string> { $"{name}.bak.1", $"{name}.bak.2", $"{name}.bak.3" };
            foreach (string old in Directory.GetFiles(backupDir, $"{name}.bak.*"))
            {
                if (!keep.Contains(Path.GetFileName(old)))
                    File.Delete(old);
            }
            excelBackupDonePaths.Add(excelPath);
            LogUtil.Log($"[幻化药测试] 已备份Mod道具Excel → {bakPath}");
            return true;
        }
        catch (Exception e)
        {
            error = $"⚠ 备份Excel失败(请先在Excel/WPS中关闭该文件): {e.Message}";
            return false;
        }
    }

    /// <summary>
    /// EPPlus批量写Excel: 按自ID定位道具行, 只改 other_data 列(3行表头, 数据从第4行起, 与gen脚本布局一致), 全部行一次写盘；
    /// 写后回读校验防静默失败——2026-09-30 实证: 「new FileStream + new ExcelPackage(fs) + Save()」在本项目 EPPlus 版本下
    /// 无异常但文件完全未落盘(mtime/内容均不变), 导致手调值显示"保存成功"却被随后的 export 用旧Excel覆盖;
    /// 故改用项目标准的 new ExcelPackage(FileInfo) 写模式(ExcelUtil.SetExcelData 及全部编辑器工具同款)并加写后回读比对
    /// </summary>
    private static bool TryWriteExcelOtherDataBatch(string excelPath, Dictionary<long, string> saveData, out string error, out List<long> notFound)
    {
        error = null;
        notFound = new List<long>(saveData.Keys);
        try
        {
            int colId, colOther;
            using (ExcelPackage ep = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet ws = ep.Workbook.Worksheets["ItemsInfo"];
                if (ws == null) { error = "⚠ Mod道具Excel缺少 ItemsInfo 工作表"; return false; }
                colId = -1; colOther = -1;
                for (int c = 1; c <= ws.Dimension.End.Column; c++)
                {
                    string header = ws.Cells[1, c].Text.Trim();
                    if (header == "id") colId = c;
                    else if (header == "other_data") colOther = c;
                }
                if (colId < 0 || colOther < 0) { error = "⚠ Mod道具Excel缺少 id/other_data 列"; return false; }
                for (int r = 4; r <= ws.Dimension.End.Row; r++)
                {
                    if (!long.TryParse(ws.Cells[r, colId].Text.Trim(), out long rowId)) continue;
                    if (!saveData.TryGetValue(rowId, out string newOtherData)) continue;
                    ws.Cells[r, colOther].Value = newOtherData;
                    notFound.Remove(rowId);
                }
                ep.Save();
            }
            //写后回读校验：任何"Save()无异常但未落盘"的静默失败在此拦截(返回false→弹窗报错, 覆盖层保留可重试)
            List<long> verifyFailed = new List<long>();
            using (ExcelPackage epCheck = new ExcelPackage(new FileInfo(excelPath)))
            {
                ExcelWorksheet wsCheck = epCheck.Workbook.Worksheets["ItemsInfo"];
                if (wsCheck == null) { error = "⚠ 写后校验失败: 缺少 ItemsInfo 工作表"; return false; }
                for (int r = 4; r <= wsCheck.Dimension.End.Row; r++)
                {
                    if (!long.TryParse(wsCheck.Cells[r, colId].Text.Trim(), out long rowId)) continue;
                    if (!saveData.TryGetValue(rowId, out string expected)) continue;
                    if (wsCheck.Cells[r, colOther].Text.Trim() != expected)
                        verifyFailed.Add(rowId);
                }
            }
            if (verifyFailed.Count > 0)
            {
                error = $"⚠ Excel写后校验失败({verifyFailed.Count}行未落盘): {string.Join(",", verifyFailed)}";
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            error = $"⚠ 写入Excel失败(请先在Excel/WPS中关闭该文件): {e.Message}";
            return false;
        }
    }

    /// <summary>
    /// 直补 JsonText(批量): 机器生成的单行紧凑JSON数组, 逐 id 定位 "id":&lt;自ID&gt;, 锚点后首个 "other_data":" 到下个引号整体替换,
    /// 全部替换一次写盘(other_data值域=资源名+数字+;,:&amp;, 不含引号/反斜杠, 直接替换安全)
    /// </summary>
    private static bool TryPatchItemsInfoJsonBatch(string jsonPath, Dictionary<long, string> saveData)
    {
        try
        {
            string json = File.ReadAllText(jsonPath);
            bool changed = false;
            const string key = "\"other_data\":\"";
            foreach (var kv in saveData)
            {
                int idIdx = json.IndexOf($"\"id\":{kv.Key},", StringComparison.Ordinal);
                if (idIdx < 0) continue;
                int keyIdx = json.IndexOf(key, idIdx, StringComparison.Ordinal);
                if (keyIdx < 0) continue;
                int valStart = keyIdx + key.Length;
                int valEnd = json.IndexOf('"', valStart);
                if (valEnd < 0) continue;
                json = json.Substring(0, valStart) + kv.Value + json.Substring(valEnd);
                changed = true;
            }
            if (changed) File.WriteAllText(jsonPath, json, new UTF8Encoding(false));
            return changed;
        }
        catch (Exception e)
        {
            LogUtil.LogError($"[幻化药测试] 直补JsonText失败 {jsonPath}: {e.Message}");
            return false;
        }
    }

    #endregion

#endif
}
