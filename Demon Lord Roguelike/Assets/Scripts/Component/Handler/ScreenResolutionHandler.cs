using UnityEngine;

/// <summary>
/// 屏幕分辨率Handler：窗口模式下允许自由拖动窗口边缘调整大小，松手后按锚定宽高比等比吸附（宽*1.1则高也*1.1），并刷新UI
/// </summary>
public class ScreenResolutionHandler : BaseHandler<ScreenResolutionHandler, BaseManager>
{
    #region 常量
    //最小窗口宽度
    protected const int MinWidth = 640;
    //最小窗口高度
    protected const int MinHeight = 360;
    //非法分辨率的默认回退宽度
    protected const int DefaultWidth = 1920;
    //非法分辨率的默认回退高度
    protected const int DefaultHeight = 1080;
    //宽高比容差（兼容1366x768及自定义尺寸四舍五入产生的微小误差）
    protected const float AspectTolerance = 0.01f;
    //拖动结束判定时间（尺寸稳定该秒数后视为松手，执行等比吸附）
    protected const float ResizeSettleTime = 0.3f;
    #endregion

    #region 字段
    //锚定尺寸（最近一次确认的合法比例尺寸，等比吸附的比例基准）
    protected int anchorWidth;
    protected int anchorHeight;
    //上一帧看到的屏幕尺寸（用于识别尺寸是否还在变化）
    protected int lastSeenWidth = -1;
    protected int lastSeenHeight = -1;
    //SetResolution待生效的目标尺寸（用于识别"本次尺寸变化是自己发起的"）
    protected int pendingWidth = -1;
    protected int pendingHeight = -1;
    //是否有待生效的SetResolution
    protected bool hasPending;
    //拖动结束后待收尾标记（吸附/存档/刷新UI）
    protected bool resizeDirty;
    //最后一次尺寸变化时间
    protected float lastResizeTime;
    //是否已初始化
    protected bool isInit;
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化（以配置中的合法分辨率为锚点；非法配置回退默认尺寸，窗口模式下应用配置尺寸）
    /// </summary>
    public void InitData()
    {
        isInit = true;
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        lastSeenWidth = currentWidth;
        lastSeenHeight = currentHeight;
        hasPending = false;
        resizeDirty = false;
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        bool isValidConfig = TryParseResolution(gameConfig.screenResolution, out anchorWidth, out anchorHeight) && IsSupportedResolution(anchorWidth, anchorHeight);
        if (!isValidConfig)
        {
            string invalidResolution = gameConfig.screenResolution;
            anchorWidth = DefaultWidth;
            anchorHeight = DefaultHeight;
            gameConfig.screenResolution = $"{anchorWidth}x{anchorHeight}";
            GameDataHandler.Instance.manager.SaveGameConfig();
            LogUtil.LogWarning($"检测到非法窗口分辨率:{invalidResolution}，已恢复为:{gameConfig.screenResolution}");
        }
        //按配置的目标窗口模式判断，避免全屏切换尚未生效时误用Screen.fullScreen强制窗口化
        if (gameConfig.window != 1 && (currentWidth != anchorWidth || currentHeight != anchorHeight))
            SetResolutionByCode(anchorWidth, anchorHeight);
    }
    #endregion

    #region 公有方法
    /// <summary>
    /// 由代码主动设置分辨率（设置界面选择分辨率时调用）：更新锚定比例并标记来源，避免被吸附逻辑二次修正
    /// </summary>
    /// <param name="w">宽</param>
    /// <param name="h">高</param>
    public void SetResolutionByCode(int w, int h)
    {
        if (!isInit)
            InitData();
        if (!IsSupportedResolution(w, h))
        {
            w = DefaultWidth;
            h = DefaultHeight;
        }
        anchorWidth = w;
        anchorHeight = h;
        pendingWidth = w;
        pendingHeight = h;
        hasPending = true;
        resizeDirty = false;
        Screen.SetResolution(w, h, false);
    }
    #endregion

    #region 生命周期
    /// <summary>
    /// 每帧检测窗口尺寸：拖动中不干预（避免反复SetResolution导致闪烁），松手尺寸稳定后一次性等比吸附
    /// </summary>
    protected void Update()
    {
        //编辑器下Game视图本身可自由缩放且SetResolution无效，不处理
        if (Application.isEditor || !isInit)
            return;
        //全屏模式不允许拖动，仅同步实际尺寸并清理待处理状态，不用显示器比例覆盖窗口锚点
        if (Screen.fullScreen)
        {
            lastSeenWidth = Screen.width;
            lastSeenHeight = Screen.height;
            hasPending = false;
            resizeDirty = false;
            return;
        }
        int w = Screen.width;
        int h = Screen.height;
        //自己发起的SetResolution生效：确认为新锚点并收尾
        if (hasPending && w == pendingWidth && h == pendingHeight)
        {
            hasPending = false;
            anchorWidth = w;
            anchorHeight = h;
            lastSeenWidth = w;
            lastSeenHeight = h;
            FinishResize();
            return;
        }
        if (w == lastSeenWidth && h == lastSeenHeight)
        {
            //尺寸稳定，等待松手判定
            if (!resizeDirty || Time.unscaledTime - lastResizeTime < ResizeSettleTime)
                return;
            resizeDirty = false;
            //松手后尺寸与锚点不一致：一次性等比吸附（若系统钳制导致吸附未生效，锚点保持原比例，下次拖动会重新吸附）
            if (w != anchorWidth || h != anchorHeight)
                ApplyAspectSnap(w, h);
            return;
        }
        //尺寸变化中（用户正在拖动）：只记录不干预，用户输入同时作废未生效的pending
        hasPending = false;
        lastSeenWidth = w;
        lastSeenHeight = h;
        lastResizeTime = Time.unscaledTime;
        resizeDirty = true;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 尝试解析宽x高格式的分辨率字符串，格式损坏时返回false而不抛异常
    /// </summary>
    /// <param name="resolution">分辨率字符串</param>
    /// <param name="w">解析出的宽</param>
    /// <param name="h">解析出的高</param>
    /// <returns>是否成功解析出两个整数</returns>
    protected bool TryParseResolution(string resolution, out int w, out int h)
    {
        w = 0;
        h = 0;
        if (resolution.IsNull())
            return false;
        string[] sizeData = resolution.Split('x');
        if (sizeData.Length != 2)
            return false;
        return int.TryParse(sizeData[0], out w) && int.TryParse(sizeData[1], out h);
    }

    /// <summary>
    /// 校验分辨率：尺寸不得低于下限，宽高比需匹配任一预设比例（允许自定义像素尺寸）
    /// </summary>
    /// <param name="w">宽</param>
    /// <param name="h">高</param>
    /// <returns>尺寸与宽高比是否均受支持</returns>
    protected bool IsSupportedResolution(int w, int h)
    {
        if (w < MinWidth || h < MinHeight)
            return false;
        float aspect = (float)w / h;
        var listResolution = GameSystemInfo.ListScreenResolutionData;
        for (int i = 0; i < listResolution.Count; i++)
        {
            if (!TryParseResolution(listResolution[i], out int presetWidth, out int presetHeight) || presetHeight <= 0)
                continue;
            float presetAspect = (float)presetWidth / presetHeight;
            if (Mathf.Abs(aspect - presetAspect) <= AspectTolerance)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 松手后按锚定宽高比等比吸附（以松手时的实际尺寸为基准，宽*1.1则高也*1.1）
    /// </summary>
    /// <param name="w">松手时的屏幕宽</param>
    /// <param name="h">松手时的屏幕高</param>
    protected void ApplyAspectSnap(int w, int h)
    {
        float scaleW = (float)w / anchorWidth;
        float scaleH = (float)h / anchorHeight;
        //取变化幅度更大的一方作为缩放基准，保证拖单边与拖角落都按比例吸附
        float scale = Mathf.Abs(scaleW - 1f) >= Mathf.Abs(scaleH - 1f) ? scaleW : scaleH;
        int targetW = Mathf.Max(MinWidth, Mathf.RoundToInt(anchorWidth * scale));
        //以宽为基准精确还原锚定比例（消除四舍五入导致的比例漂移）
        int targetH = Mathf.Max(MinHeight, Mathf.RoundToInt(targetW * ((float)anchorHeight / anchorWidth)));
        //尺寸已符合比例时无需吸附，直接收尾
        if (targetW == w && targetH == h)
        {
            anchorWidth = w;
            anchorHeight = h;
            FinishResize();
            return;
        }
        pendingWidth = targetW;
        pendingHeight = targetH;
        hasPending = true;
        Screen.SetResolution(targetW, targetH, false);
    }

    /// <summary>
    /// 分辨率变更收尾：写回配置存档并刷新UI
    /// </summary>
    protected void FinishResize()
    {
        //落盘前再次校验，非法锚点走默认修正而不是写入配置
        if (!IsSupportedResolution(anchorWidth, anchorHeight))
        {
            SetResolutionByCode(DefaultWidth, DefaultHeight);
            return;
        }
        //写回实际分辨率并存档
        GameConfigBean gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
        gameConfig.screenResolution = $"{anchorWidth}x{anchorHeight}";
        GameDataHandler.Instance.manager.SaveGameConfig();
        //刷新UI（Canvas立即重建布局 + 项目级UI刷新）
        Canvas.ForceUpdateCanvases();
        UIHandler.Instance.RefreshAllUI();
    }
    #endregion
}
