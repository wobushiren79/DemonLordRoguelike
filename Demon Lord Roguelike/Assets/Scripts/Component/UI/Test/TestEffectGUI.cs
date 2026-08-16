using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 粒子特效测试（GUI版，纯代码UI）
/// 下拉选择(或手动输入)特效id后点播放，在10x10平面(顶面高度0)上方1格随机位置，按正式游戏里该特效对应的执行方法播放；
/// 播放次数可设，点击后按"每帧一次"分帧播放(单例粒子的Play一帧只能触发一次)。不依赖任何运行时UI预制。
/// 由 LauncherTest.StartForEffectTest 挂到空物体上启动。
/// </summary>
public class TestEffectGUI : MonoBehaviour
{
    #region 播放区域参数
    private const float PlaneHalfSize = 5f;//播放平面半边长(平面共10x10, 顶面高度0)
    private const float PlayHeight = 1f;   //播放高度(平面顶面上方1格)
    #endregion

    #region 测试专用参数(与生产调用同形)
    private const string TrailVisualKey = "EffectTestTrail"; //拖尾测试的视觉桶签名(生产为攻击弹道视觉桶)
    private const int TrailPointCount = 30;                  //拖尾测试模拟弹道的采样点数
    private const float TrailPointInterval = 0.3f;           //拖尾测试采样点间隔
    private const int AscendProgressNum = 10;                //进阶进度测试的光点数量
    private const float AscendProgressHeight = 2f;           //进阶进度测试终点相对起点的高度
    private const float ShockwaveTestRadius = 5f;            //冲击波测试半径(取平面半边长)
    private const float ShockwaveTestSpeed = 10f;            //冲击波测试扩张速度(生产为攻击模式配置速度)
    private const float ShockwaveVisualBaseRadius = 3f;      //冲击波视觉基准半径(与 AttackModeShockwaveRing 一致)
    private const float ShockwaveVisualBaseDuration = 0.5f;  //冲击波视觉基准时长(与 AttackModeShockwaveRing 一致)
    private const float BurningDuration = 5f;                //地面火焰测试燃烧时长(与 AttackModeRangedArcGround 一致)
    private const int PlayCountMax = 999;                    //播放次数输入上限(防误输超大数)
    #endregion

    #region 数据字段
    /// <summary>当前面板实例(供入口防重复创建, 面板销毁时置空)</summary>
    public static TestEffectGUI Instance;
    private GameObject planeObj;                 //10x10播放平面(随面板销毁清理)
    private List<SelectItem> listEffectOptions;  //特效候选列表(懒加载, 来自EffectInfoCfg)
    private long currentEffectId;                //当前选中的特效id
    private string effectDropdownLabel = "请选择特效";//特效下拉按钮当前显示(id+说明)
    private bool isEffectDropdownOpen;           //特效下拉列表是否展开
    private Vector2 scrollEffectDropdown;        //特效下拉列表滚动
    private string inputEffectId = "";           //手动输入的特效id(空=使用下拉选择)
    private string inputPlayCount = "1";         //播放次数(点击后每帧播一次, 共播N帧)
    private int playRemaining;                   //剩余待播放次数(每帧减1)
    #endregion

    #region GUI样式
    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, hintStyle, buttonLeftStyle;
    private Vector2 scrollMain;                  //主面板滚动(下拉展开后内容变高, 小窗口下可滚动查看)
    #endregion

    /// <summary>特效候选项(id + 显示名)</summary>
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

    #region 生命周期
    /// <summary>
    /// 初始化：创建10x10播放平面并摆放主相机俯视平面
    /// </summary>
    private void Start()
    {
        Instance = this;
        //创建10x10平面(Unity Plane原始体默认即10x10, 摆原点即顶面高度0)
        planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeObj.name = "EffectTestPlane";
        //隐藏Cinemachine虚拟相机、激活主相机并关闭切换动画(与卡片测试镜头同逻辑), 再摆到俯视平面的视角
        CameraManager cameraManager = CameraHandler.Instance.manager;
        if (cameraManager.mainCamera != null)
        {
            cameraManager.HideAllCM();
            cameraManager.mainCamera.gameObject.SetActive(true);
            cameraManager.SetMainCameraDefaultBlend(0);
            cameraManager.mainCamera.transform.position = new Vector3(0, 13, -11);
            cameraManager.mainCamera.transform.LookAt(Vector3.zero);
        }
    }

    /// <summary>
    /// 每帧执行一次播放(单例粒子一帧只能 Play 一次, 多次播放按帧分发)
    /// </summary>
    private void Update()
    {
        if (playRemaining > 0)
        {
            PlayOnce(GetCurrentEffectId());
            playRemaining--;
        }
    }

    /// <summary>
    /// 销毁时清理播放平面、注销拖尾测试桶并置空实例引用
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (planeObj != null) Destroy(planeObj);
        //注销拖尾测试的常驻VFX实例(生产为攻击弹道视觉桶, 测试结束一并清理)
        EffectHandler.Instance.ClearAttackModeTrailVfx(TrailVisualKey);
    }
    #endregion

    #region 特效候选列表
    /// <summary>
    /// 懒加载特效候选列表(EffectInfoCfg全量配置, id+说明作显示名), 首次加载时默认选中第一项
    /// </summary>
    private void EnsureEffectOptions()
    {
        if (listEffectOptions != null) return;
        listEffectOptions = new List<SelectItem>();
        foreach (var effectInfo in EffectInfoCfg.GetAllArrayData())
        {
            string remark = effectInfo.remark.IsNull() ? "" : $" {effectInfo.remark}";
            listEffectOptions.Add(new SelectItem(effectInfo.id, $"{effectInfo.id}{remark}"));
        }
        //默认选中第一项
        if (listEffectOptions.Count > 0)
        {
            SelectItem firstItem = listEffectOptions[0];
            currentEffectId = firstItem.id;
            effectDropdownLabel = firstItem.label;
        }
    }
    #endregion

    #region 播放(按id分发到正式游戏对应执行方法)
    /// <summary>
    /// 取当前要播放的特效id：手动输入优先(需存在于配置表)，空/非法输入回退下拉选择
    /// </summary>
    private long GetCurrentEffectId()
    {
        if (!inputEffectId.IsNull() && long.TryParse(inputEffectId, out long manualId))
        {
            //手动id必须在配置表中存在，否则回退下拉选择
            if (EffectInfoCfg.GetItemData(manualId) != null)
                return manualId;
        }
        return currentEffectId;
    }

    /// <summary>
    /// 解析播放次数输入(非法/超上限时取默认1)
    /// </summary>
    private int ParsePlayCount()
    {
        if (int.TryParse(inputPlayCount, out int count) && count > 0)
            return Mathf.Min(count, PlayCountMax);
        return 1;
    }

    /// <summary>
    /// 点击播放：按输入的次数开始分帧播放(每帧一次)
    /// </summary>
    private void OnClickForPlay()
    {
        playRemaining = ParsePlayCount();
    }

    /// <summary>
    /// 播放一次：在10x10平面(顶面高度0)上方1格随机位置，按该特效在正式游戏里的执行方法播放
    /// </summary>
    /// <param name="effectId">特效配置表 id</param>
    private void PlayOnce(long effectId)
    {
        if (EffectInfoCfg.GetItemData(effectId) == null) return;
        Vector3 randomPos = GetRandomPos();
        EffectManager effectManager = EffectHandler.Instance.manager;
        //受击溅血：生产为 ShowBloodEffect(生物位置+(0,0.5,0), 攻击来向), 方向随机左右
        if (effectId == effectManager.effectBloodId)
        {
            Vector3 attDirection = Random.Range(0, 2) == 0 ? Vector3.left : Vector3.right;
            EffectHandler.Instance.ShowBloodEffect(randomPos + new Vector3(0, 0.5f, 0), attDirection);
            return;
        }
        //护盾打击：生产为 ShowShieldHitEffect(生物位置+护盾偏移, 攻击来向), 偏移取默认(0,0.5,0), 方向随机左右
        if (effectId == effectManager.effectShieldHitId)
        {
            Vector3 attDirection = Random.Range(0, 2) == 0 ? Vector3.left : Vector3.right;
            EffectHandler.Instance.ShowShieldHitEffect(randomPos + new Vector3(0, 0.5f, 0), attDirection);
            return;
        }
        //进阶增加进度：生产为 ShowCreatureAscendAddProgressEffect(光点数, 起点, 终点), 测试从随机点向上飞2格
        if (effectId == effectManager.effectCreatureAscendAddProgressId)
        {
            EffectHandler.Instance.ShowCreatureAscendAddProgressEffect(AscendProgressNum, randomPos, randomPos + new Vector3(0, AscendProgressHeight, 0));
            return;
        }
        //进阶完成庆祝：生产为 ShowCreatureAscendCompleteEffect(容器位置+(0,1.2,0), 新稀有度主色), 测试随机稀有度上色
        if (effectId == effectManager.effectCreatureAscendCompleteId)
        {
            Color rarityColor = Color.white;
            RarityInfoBean rarityInfo = RarityInfoCfg.GetItemData((RarityEnum)Random.Range((int)RarityEnum.N, (int)RarityEnum.L + 1));
            if (rarityInfo != null)
                rarityColor = ColorUtil.ParseHtmlString(rarityInfo.ui_board_color);
            EffectHandler.Instance.ShowCreatureAscendCompleteEffect(randomPos + new Vector3(0, 1.2f, 0), rarityColor);
            return;
        }
        //放置魔物-魔王消耗魔力：生产为 ShowCreaturePlaceEffect(effectManaId, 魔王位置)
        if (effectId == effectManager.effectManaId)
        {
            EffectHandler.Instance.ShowCreaturePlaceEffect(effectManager.effectManaId, randomPos);
            return;
        }
        //放置魔物-生成登场：生产为 ShowCreaturePlaceEffect(effectCreatureShowId, 生成位置)
        if (effectId == effectManager.effectCreatureShowId)
        {
            EffectHandler.Instance.ShowCreaturePlaceEffect(effectManager.effectCreatureShowId, randomPos);
            return;
        }
        //攻击弹道拖尾(方案2 VFX)：生产为常驻实例+每帧喂位置缓冲(Register/Add/Flush), 非播放式；
        //测试按生产链路模拟一条直线弹道: 注册桶→清帧→沿随机水平方向铺采样点→Flush 一次性喷发
        if (effectId == effectManager.effectAttackModeTrailId)
        {
            EffectHandler.Instance.RegisterAttackModeTrailVfx(TrailVisualKey);
            EffectHandler.Instance.BeginAttackModeTrailVfxFrame();
            Vector3 trailDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            Vector3 trailColor = new Vector3(1f, 1f, 1f);
            for (int i = 0; i < TrailPointCount; i++)
            {
                EffectHandler.Instance.AddAttackModeTrailVfxPoint(TrailVisualKey, randomPos + trailDirection * (i * TrailPointInterval), trailColor);
            }
            EffectHandler.Instance.FlushAttackModeTrailVfxFrame();
            return;
        }
        //冲击波：生产为 ShowEnduringSingletonEffect(效果id, 按判定参数换算的尺寸/时长乘数, AttackModeShockwaveRing);
        //测试半径取平面半边长5、波速取10, 与生产同公式换算
        if (effectId == effectManager.effectShockwaveId)
        {
            float waveDuration = ShockwaveTestRadius / ShockwaveTestSpeed;
            EffectHandler.Instance.ShowEnduringSingletonEffect(effectId, new SingletonEffectParam()
            {
                targetPos = randomPos,
                startSizeMultiplier = ShockwaveTestRadius / ShockwaveVisualBaseRadius,
                startLifetimeMultiplier = Mathf.Max(waveDuration, 0.1f) / ShockwaveVisualBaseDuration,
            });
            return;
        }
        //地面火焰：生产为 ShowEnduringSingletonEffect(击中粒子id, 燃烧时长同步, AttackModeRangedArcGround)
        if (effectId == 1800001)
        {
            EffectHandler.Instance.ShowEnduringSingletonEffect(effectId, new SingletonEffectParam()
            {
                targetPos = randomPos,
                duration = BurningDuration,
            });
            return;
        }
        //攻击命中粒子(火球/冰球/爆炸/刀锋/加血/加甲/魅惑/地面打击/火锥/冰锥/雷电/落雷)：
        //生产统一走 BaseAttackMode.PlayEffectForHit → ShowEnduringSingletonEffect(effect_hit, {targetPos})
        if (setHitEffectIds.Contains(effectId))
        {
            EffectHandler.Instance.ShowEnduringSingletonEffect(effectId, new SingletonEffectParam() { targetPos = randomPos });
            return;
        }
        //兜底：配置新增且未归类到生产方法的特效走通用配置驱动通道
        EffectHandler.Instance.ShowEffect(effectId, randomPos);
    }

    /// <summary>
    /// 取10x10平面(顶面高度0)上方1格内的随机播放位置
    /// </summary>
    private Vector3 GetRandomPos()
    {
        return new Vector3(Random.Range(-PlaneHalfSize, PlaneHalfSize), PlayHeight, Random.Range(-PlaneHalfSize, PlaneHalfSize));
    }

    /// <summary>攻击命中粒子集合(excel_attackmode_info 的 effect_hit 引用, 生产统一走全局单例通道)</summary>
    private static readonly HashSet<long> setHitEffectIds = new HashSet<long>()
    {
        100001, 200001, 300001, 400001, 400002, 400003, 500001, 500002,
        600001, 700001, 800001, 800002, 900001, 900002, 900003,
    };

    /// <summary>
    /// 取当前特效在正式游戏里的执行方法名(信息行展示)
    /// </summary>
    /// <param name="effectId">特效配置表 id</param>
    private string GetProductionMethodName(long effectId)
    {
        EffectManager effectManager = EffectHandler.Instance.manager;
        if (effectId == effectManager.effectBloodId) return "ShowBloodEffect 受击溅血";
        if (effectId == effectManager.effectShieldHitId) return "ShowShieldHitEffect 护盾打击";
        if (effectId == effectManager.effectCreatureAscendAddProgressId) return "ShowCreatureAscendAddProgressEffect 进阶进度";
        if (effectId == effectManager.effectCreatureAscendCompleteId) return "ShowCreatureAscendCompleteEffect 进阶庆祝";
        if (effectId == effectManager.effectManaId) return "ShowCreaturePlaceEffect 放置魔物耗蓝(全局单例)";
        if (effectId == effectManager.effectCreatureShowId) return "ShowCreaturePlaceEffect 魔物登场(全局单例)";
        if (effectId == effectManager.effectAttackModeTrailId) return "拖尾系统 Register/Flush 弹道拖尾";
        if (effectId == effectManager.effectShockwaveId) return "ShowEnduringSingletonEffect 冲击波(半径/时长换算)";
        if (effectId == 1800001) return "ShowEnduringSingletonEffect 地面火焰(燃烧时长)";
        if (setHitEffectIds.Contains(effectId)) return "ShowEnduringSingletonEffect 攻击命中(PlayEffectForHit)";
        return "ShowEffect 通用配置驱动";
    }
    #endregion

    #region GUI绘制
    /// <summary>
    /// IMGUI入口，绘制纯代码创建的特效测试面板
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();

        //面板宽度自适应钳制(不超过窗口宽), 避免窄 Game 视图下右缘被裁掉
        float panelWidth = Mathf.Min(520, Screen.width - 20);
        GUILayout.BeginArea(new Rect(10, 10, panelWidth, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);
        GUILayout.Label("粒子特效测试（GUI版）", titleStyle);
        GUILayout.Space(4);

        //手动输入id(空=使用下拉选择)
        GUILayout.BeginHorizontal();
        GUILayout.Label("手动ID", labelStyle, GUILayout.Width(60));
        inputEffectId = GUILayout.TextField(inputEffectId, GUILayout.Height(26), GUILayout.Width(140));
        GUILayout.Label("(空=用下拉)", hintStyle);
        GUILayout.EndHorizontal();

        //特效下拉选择
        EnsureEffectOptions();
        GUILayout.BeginHorizontal();
        GUILayout.Label("特效", labelStyle, GUILayout.Width(60));
        if (GUILayout.Button(effectDropdownLabel, buttonLeftStyle, GUILayout.Height(26)))
            isEffectDropdownOpen = !isEffectDropdownOpen;
        GUILayout.EndHorizontal();

        //下拉展开时的候选列表(列出id+说明, 当前选中项高亮)
        if (isEffectDropdownOpen)
        {
            scrollEffectDropdown = GUILayout.BeginScrollView(scrollEffectDropdown, GUI.skin.box, GUILayout.Height(460));
            foreach (var option in listEffectOptions)
            {
                bool isCurrent = option.id == currentEffectId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                string optionLabel = isCurrent ? $"✔ {option.label}" : option.label;
                if (GUILayout.Button(optionLabel, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    currentEffectId = option.id;
                    effectDropdownLabel = option.label;
                    isEffectDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }

        //当前特效信息(手动id有效时优先展示手动id)
        long effectiveId = GetCurrentEffectId();
        EffectInfoBean effectiveInfo = EffectInfoCfg.GetItemData(effectiveId);
        if (effectiveInfo != null)
        {
            GUILayout.Space(4);
            string showTypeText = effectiveInfo.GetShowType() == EffectShowTypeEnum.Once ? "一次性" : "持久型";
            GUILayout.Label($"类型：{showTypeText}    资源：{effectiveInfo.res_name}", labelStyle);
            GUILayout.Label($"正式调用：{GetProductionMethodName(effectiveId)}", labelStyle);
        }
        //手动输入了非法/不存在的id时提示
        if (!inputEffectId.IsNull() && (!long.TryParse(inputEffectId, out long manualId) || EffectInfoCfg.GetItemData(manualId) == null))
        {
            GUILayout.Label("⚠ 手动ID无效或不在配置表，将使用下拉选择", hintStyle);
        }

        //播放次数输入
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label("播放次数", labelStyle, GUILayout.Width(60));
        inputPlayCount = GUILayout.TextField(inputPlayCount, GUILayout.Height(26), GUILayout.Width(60));
        GUILayout.Label("(每帧播1次)", hintStyle);
        GUILayout.EndHorizontal();

        //播放按钮
        GUILayout.Space(6);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶️ 随机位置播放", GUILayout.Height(34)))
            OnClickForPlay();
        GUI.backgroundColor = Color.white;

        //关闭按钮
        GUILayout.Space(6);
        if (GUILayout.Button("关闭", GUILayout.Height(26)))
            CloseSelf();

        //行为说明
        GUILayout.Space(6);
        GUILayout.Label("提示：按正式游戏对应执行方法播放。持久型粒子为全局单例，重复播放会移动原实例；血/护盾飞溅朝向随机。", hintStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>
    /// 关闭GUI并销毁自身
    /// </summary>
    private void CloseSelf()
    {
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
        //按钮文本左对齐(下拉按钮/选项按钮显示 id+说明 长文本用)
        buttonLeftStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 0, 0) };
    }
    #endregion
}
