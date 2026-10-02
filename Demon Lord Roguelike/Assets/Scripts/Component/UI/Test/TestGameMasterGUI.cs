using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GM 面板（GUI版，纯代码 IMGUI，不依赖预制体）：在基地主页面按 F11 打开，再次按 F11 关闭。
/// 覆盖 UITestBase 预制面板的全部功能（魔晶/声望/道具/生物/测试生物/解锁/世界难度），
/// 并新增 Mod 道具区：下拉选择具体某个 Mod，一键添加该 Mod 的全部道具（装备每种稀有度各一，非装备仅1件）。
/// 由 UIBaseMain 的 F11 输入触发 TestGameMasterGUI.Toggle() 创建/销毁（面板专用文本直接写死，不走多语言）。
/// </summary>
public class TestGameMasterGUI : MonoBehaviour
{
    #region 常量

    private const float PanelWidth = 440;               //面板宽度
    private const long ModIdDivisor = 100000000000000L; //ModID除数(10^14，与 BaseBean.CombineModId 的 D5+D14 拼接规则一致)
    private const float StatusShowSeconds = 8f;         //操作结果提示的展示时长(秒)

    /// <summary>稀有度下拉选项(0=随机，1~6=N~L)</summary>
    private static readonly string[] RarityLabels = { "随机", "1 N", "2 R", "3 SR", "4 SSR", "5 UR", "6 L" };

    #endregion

    #region 数据字段

    /// <summary>当前面板实例(供 F11 开关判定与 LauncherTest 清理，面板销毁时置空)</summary>
    public static TestGameMasterGUI Instance;

    //资源区
    private string inputCrystal = "";                   //魔晶数量输入(空=999999)
    private string inputReputation = "";                //声望数量输入(空=999999)

    //道具区
    private string inputItemId = "";                    //道具ID输入(空=全部道具)

    //生物区
    private long currentCreatureId = 2001;              //测试生物-下拉选中的生物id
    private string inputManualCreatureId = "";          //测试生物-手动输入id(非空且合法时优先于下拉)
    private string creatureDropdownLabel = "请选择生物";//测试生物-下拉按钮显示文本
    private List<SelectItem> listCreatureOptions;       //测试生物-下拉候选(懒加载)
    private int raritySelectIndex;                      //测试生物-稀有度下拉索引(0=随机)
    private bool isRandomLevel = true;                  //测试生物-等级随机开关
    private int level;                                  //测试生物-指定等级(0~10)

    //解锁区
    private string inputUnlockId = "";                  //解锁ID输入(空=全部解锁)

    //Mod区
    private List<ModOption> listModOptions;             //Mod候选(懒加载，仅含带道具配置的Mod)
    private int modSelectIndex;                         //Mod下拉选中索引
    private string modDropdownLabel = "请选择Mod";      //Mod下拉按钮显示文本

    //下拉展开状态(互斥，开一个关其他)
    private bool isCreatureDropdownOpen, isRarityDropdownOpen, isModDropdownOpen;
    private Vector2 scrollCreatureDropdown, scrollModDropdown;

    //操作结果提示
    private string statusText = "";
    private bool statusIsError;
    private float statusExpireTime;

    #endregion

    #region GUI样式

    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, hintStyle, buttonLeftStyle, sectionHeaderStyle, statusStyle;
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

    /// <summary>Mod候选项(modId + 显示名)</summary>
    private struct ModOption
    {
        public int modId;
        public string label;
        public ModOption(int modId, string label)
        {
            this.modId = modId;
            this.label = label;
        }
    }

    #region 生命周期

    /// <summary>
    /// 初始化：记录实例并关闭所有游戏控制（防点透到场景，与 UITestBase.OpenUI 同款处理）
    /// </summary>
    private void Start()
    {
        Instance = this;
        GameControlHandler.Instance.manager.EnableAllControl(false);
    }

    /// <summary>
    /// 销毁时置空实例引用并恢复基地控制（与 UITestBase 退出回 UIBaseMain 的效果一致）
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameControlHandler.Instance != null && GameControlHandler.Instance.manager != null)
        {
            GameControlHandler.Instance.SetBaseControl();
        }
    }

    #endregion

    #region 开关入口

    /// <summary>
    /// F11 开关入口：已打开则关闭，未打开则创建（由 UIBaseMain 的 F11 输入调用）
    /// </summary>
    public static void Toggle()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        else
        {
            new GameObject("GameMasterGUI").AddComponent<TestGameMasterGUI>();
        }
    }

    #endregion

    #region GM功能-资源

    /// <summary>
    /// 增加魔晶：输入数量，空=999999
    /// </summary>
    private void AddCrystal()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (inputCrystal.IsNull())
        {
            userData.AddCrystal(999999);
        }
        else if (long.TryParse(inputCrystal, out var addCoin))
        {
            userData.AddCrystal(addCoin);
        }
        else
        {
            SetStatus("魔晶数量必须是数字", true);
            return;
        }
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus("魔晶添加成功！");
    }

    /// <summary>
    /// 增加声望：输入数量，空=999999
    /// </summary>
    private void AddReputation()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (inputReputation.IsNull())
        {
            userData.AddReputation(999999);
        }
        else if (long.TryParse(inputReputation, out var addReputation))
        {
            userData.AddReputation(addReputation);
        }
        else
        {
            SetStatus("声望数量必须是数字", true);
            return;
        }
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus("声望添加成功！");
    }

    #endregion

    #region GM功能-道具

    /// <summary>
    /// 添加道具：输入道具ID=仅该道具；空=全部道具。按道具类型分流生成(见 AddItemByType)
    /// </summary>
    private void AddItem()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        if (inputItemId.IsNull())
        {
            var allData = ItemsInfoCfg.GetAllData();
            int totalCount = 0;
            foreach (var itemData in allData)
            {
                totalCount += AddItemByType(userData, itemData.Value.id);
            }
            SetStatus($"全部道具添加成功（共{allData.Count}种 {totalCount}件；装备6稀有度/非装备1件）！");
        }
        else if (long.TryParse(inputItemId, out var itemId))
        {
            if (ItemsInfoCfg.GetItemData(itemId) == null)
            {
                SetStatus($"道具ID {itemId} 不存在", true);
                return;
            }
            int addCount = AddItemByType(userData, itemId);
            SetStatus(addCount > 1 ? $"道具 {itemId} 添加成功（6稀有度各一）！" : $"道具 {itemId} 添加成功（非装备，1件）！");
        }
        else
        {
            SetStatus("道具ID必须是数字", true);
            return;
        }
        GameDataHandler.Instance.manager.SaveUserData();
    }

    /// <summary>
    /// 按道具类型生成测试道具并入背包：装备每种稀有度(N~L)各一(走统一装备生成逻辑 EquipUtil.CreateEquipItemForTest 随机属性)；
    /// 非装备(幻化药/幻原药/魔汁/魔晶/肖像等)只生成1件(稀有度1、无随机属性，与征服奖励投放 new ItemBean(id,1) 一致)
    /// </summary>
    /// <param name="userData">用户数据</param>
    /// <param name="itemId">道具ID</param>
    /// <returns>实际生成的件数</returns>
    private int AddItemByType(UserDataBean userData, long itemId)
    {
        ItemsInfoBean itemInfo = ItemsInfoCfg.GetItemData(itemId);
        //非装备(含配置缺失兜底): 固定稀有度1, 不添加随机属性
        if (itemInfo == null || !itemInfo.IsEquipType())
        {
            userData.AddBackpackItem(new ItemBean(itemId, 1));
            return 1;
        }
        //装备: 每种稀有度(N~L)各生成一个
        for (int rarity = (int)RarityEnum.N; rarity <= (int)RarityEnum.L; rarity++)
        {
            ItemBean rewardItem = EquipUtil.CreateEquipItemForTest(itemId, rarity);
            userData.AddBackpackItem(rewardItem);
        }
        return (int)RarityEnum.L - (int)RarityEnum.N + 1;
    }

    #endregion

    #region GM功能-生物

    /// <summary>
    /// 添加所有生物：遍历生物配置表，稀有度随机1~6、等级0
    /// </summary>
    private void AddAllCreature()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var allCreature = CreatureInfoCfg.GetAllData();
        foreach (var itemData in allCreature)
        {
            var itemCreatureInfo = itemData.Value;
            CreatureBean creatureData = new CreatureBean(itemCreatureInfo.id);
            creatureData.rarity = Random.Range(1, 7);
            creatureData.level = 0;
            creatureData.AddSkinForBase();
            userData.AddBackpackCreature(creatureData);
        }
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus($"所有生物添加成功（共{allCreature.Count}只）！");
    }

    /// <summary>
    /// 添加测试生物：下拉/手动选定生物ID + 稀有度(可随机) + 等级(可随机0-10)，生成后走孕育同款随机稀有度BUFF逻辑
    /// </summary>
    private void AddTestCreature()
    {
        long targetId = GetCurrentCreatureId();
        if (CreatureInfoCfg.GetItemData(targetId) == null)
        {
            SetStatus($"生物ID {targetId} 不存在", true);
            return;
        }
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //稀有度: 下拉0=随机, 否则取索引即稀有度值(1~6)
        int rarity = raritySelectIndex == 0
            ? Random.Range((int)RarityEnum.N, (int)RarityEnum.L + 1)
            : raritySelectIndex;
        //等级: 勾选随机则 0~10, 否则取滑条值
        int targetLevel = isRandomLevel ? Random.Range(0, 11) : level;

        CreatureBean creatureData = new CreatureBean(targetId);
        creatureData.rarity = rarity;
        creatureData.level = targetLevel;
        creatureData.AddSkinForBase();
        //走孕育同款随机稀有度BUFF逻辑(按稀有度逐级授予)
        creatureData.RandomRarityBuffForCreate();
        userData.AddBackpackCreature(creatureData);
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus($"测试生物添加成功：{targetId} 稀有度{rarity} 等级{targetLevel}");
    }

    /// <summary>
    /// 取当前生效的生物id：手动输入优先(需存在于生物配置表)，空/非法输入回退下拉选择
    /// </summary>
    private long GetCurrentCreatureId()
    {
        if (!inputManualCreatureId.IsNull() && long.TryParse(inputManualCreatureId, out long manualId))
        {
            if (CreatureInfoCfg.GetItemData(manualId) != null)
            {
                return manualId;
            }
        }
        return currentCreatureId;
    }

    #endregion

    #region GM功能-解锁

    /// <summary>
    /// 添加解锁：输入解锁ID=仅该项；空=全部解锁。有研究配置的行按研究满级解锁
    /// </summary>
    private void AddUnlock()
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlockData = userData.GetUserUnlockData();
        if (inputUnlockId.IsNull())
        {
            var all = UnlockInfoCfg.GetAllData();
            foreach (var item in all)
            {
                AddUnlockWithMaxLevel(userUnlockData, item.Key);
            }
            GameDataHandler.Instance.manager.SaveUserData();
            SetStatus($"全部解锁成功（共{all.Count}项）！");
            return;
        }
        if (long.TryParse(inputUnlockId, out long outValue))
        {
            AddUnlockWithMaxLevel(userUnlockData, outValue);
            GameDataHandler.Instance.manager.SaveUserData();
            SetStatus($"解锁 {outValue} 添加成功！");
        }
        else
        {
            SetStatus("解锁ID必须是数字", true);
        }
    }

    /// <summary>
    /// 按研究配置决定解锁等级后添加解锁（无研究配置=1级开关型，有=研究满级）
    /// </summary>
    /// <param name="userUnlockData">解锁存档数据</param>
    /// <param name="unlockId">解锁ID</param>
    private void AddUnlockWithMaxLevel(UserUnlockBean userUnlockData, long unlockId)
    {
        var researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(unlockId);
        if (researchInfo == null)
        {
            userUnlockData.AddUnlock(unlockId);
        }
        else
        {
            userUnlockData.AddUnlock(unlockId, researchInfo.level_max);
        }
    }

    /// <summary>
    /// 解锁所有世界的征服难度
    /// </summary>
    /// <param name="isHalf">true=解锁到一半难度(向上取整), false=解锁到该世界配置的最高难度</param>
    private void UnlockWorldDifficulty(bool isHalf)
    {
        var userData = GameDataHandler.Instance.manager.GetUserData();
        var userUnlockData = userData.GetUserUnlockData();
        //征服难度基础值(GetUnlockGameWorldConquerDifficultyLevel = conquerDifficultyMax + 已解锁难度研究个数)
        int conquerDifficultyBase = userData.GetUserLimmitData().conquerDifficultyMax;

        var allWorld = GameWorldInfoCfg.GetAllData();
        foreach (var itemData in allWorld)
        {
            GameWorldInfoBean gameWorldInfo = itemData.Value;
            //该世界征服难度的起始解锁ID(为0表示无可解锁难度, 难度恒为基础值)
            long unlockId = gameWorldInfo.unlock_id_conquer_difficulty_level;
            if (unlockId == 0)
                continue;
            //该世界配置存在的最高难度(无配置则跳过)
            int configDifficultyMax = FightTypeConquerInfoCfg.GetMaxLevel(itemData.Key);
            if (configDifficultyMax <= 0)
                continue;
            //目标难度: 一半(向上取整) 或 最高
            int targetDifficulty = isHalf ? Mathf.Max(1, Mathf.CeilToInt(configDifficultyMax / 2f)) : configDifficultyMax;
            //需要解锁的难度研究个数 = 目标难度 - 基础难度(≤0说明基础值已覆盖, 无需解锁)
            int needUnlockLevel = targetDifficulty - conquerDifficultyBase;
            if (needUnlockLevel <= 0)
                continue;
            //难度研究已拆分为每难度独立节点(起始id起连续), 逐个解锁
            for (int i = 0; i < needUnlockLevel; i++)
            {
                userUnlockData.AddUnlock(unlockId + i);
            }
        }
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus(isHalf ? "已解锁所有世界一半难度！" : "已解锁所有世界全部难度！");
    }

    #endregion

    #region GM功能-Mod道具

    /// <summary>
    /// 添加选中 Mod 的全部道具：按 modId 过滤 ItemsInfoCfg（id 号段 = modId×10^14 起），按道具类型分流生成(见 AddItemByType)
    /// </summary>
    private void AddModItems()
    {
        if (listModOptions == null || listModOptions.Count == 0)
        {
            SetStatus("没有包含道具配置的 Mod", true);
            return;
        }
        ModOption modOption = listModOptions[modSelectIndex];
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        int itemKindCount = 0;
        int totalCount = 0;
        foreach (var itemData in ItemsInfoCfg.GetAllData())
        {
            if (itemData.Key / ModIdDivisor != modOption.modId)
                continue;
            totalCount += AddItemByType(userData, itemData.Key);
            itemKindCount++;
        }
        if (itemKindCount == 0)
        {
            SetStatus("该 Mod 没有已合并的道具（请确认 Mod 已加载且 JsonText 已合并）", true);
            return;
        }
        GameDataHandler.Instance.manager.SaveUserData();
        SetStatus($"Mod 道具添加成功：{itemKindCount}种，共{totalCount}件（装备6稀有度/非装备1件）！");
    }

    #endregion

    #region 下拉候选列表

    /// <summary>
    /// 懒加载生物下拉候选，并按当前选中项初始化下拉按钮显示文本
    /// </summary>
    private void EnsureCreatureOptions()
    {
        if (listCreatureOptions != null) return;
        listCreatureOptions = new List<SelectItem>();
        foreach (var creatureInfo in CreatureInfoCfg.GetAllArrayData())
        {
            string name = creatureInfo.name_language;
            listCreatureOptions.Add(new SelectItem(creatureInfo.id, name.IsNull() ? $"{creatureInfo.id}" : $"{creatureInfo.id} {name}"));
        }
        foreach (var option in listCreatureOptions)
        {
            if (option.id == currentCreatureId)
            {
                creatureDropdownLabel = option.label;
                break;
            }
        }
    }

    /// <summary>
    /// 懒加载 Mod 下拉候选：仅列出带 ItemsInfo JsonText 且已分配 modId 的 Mod，并统计各 Mod 已合并的道具种数
    /// </summary>
    private void EnsureModOptions()
    {
        if (listModOptions != null) return;
        listModOptions = new List<ModOption>();
        var modFileInfos = ModHandler.Instance.manager.GetModJsonTextFileInfos("ItemsInfo");
        foreach (var (modId, modName, _) in modFileInfos)
        {
            int itemKindCount = 0;
            foreach (var itemData in ItemsInfoCfg.GetAllData())
            {
                if (itemData.Key / ModIdDivisor == modId)
                {
                    itemKindCount++;
                }
            }
            listModOptions.Add(new ModOption(modId, $"{modName}（{itemKindCount}种道具）"));
        }
        if (listModOptions.Count > 0)
        {
            modDropdownLabel = listModOptions[0].label;
        }
    }

    #endregion

    #region 状态提示

    /// <summary>
    /// 设置操作结果提示（面板顶部状态栏展示数秒）
    /// </summary>
    /// <param name="text">提示文本</param>
    /// <param name="isError">是否错误（红色显示）</param>
    private void SetStatus(string text, bool isError = false)
    {
        statusText = text;
        statusIsError = isError;
        statusExpireTime = Time.unscaledTime + StatusShowSeconds;
        if (isError)
        {
            LogUtil.LogError($"[GM] {text}");
        }
        else
        {
            LogUtil.Log($"[GM] {text}");
        }
    }

    #endregion

    #region GUI绘制

    /// <summary>
    /// IMGUI入口，绘制 GM 面板（左侧全高滚动面板）
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();
        EnsureCreatureOptions();
        EnsureModOptions();

        float panelWidth = Mathf.Min(PanelWidth, Screen.width - 20);
        GUILayout.BeginArea(new Rect(10, 10, panelWidth, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);

        DrawTitleBar();
        DrawStatusBar();
        DrawResourceSection();
        DrawItemSection();
        DrawCreatureSection();
        DrawUnlockSection();
        DrawModSection();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭面板（F11）", GUILayout.Height(30)))
        {
            Destroy(gameObject);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>
    /// 绘制标题栏（标题 + 关闭按钮）
    /// </summary>
    private void DrawTitleBar()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("GM 模式", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", GUILayout.Width(30), GUILayout.Height(26)))
        {
            Destroy(gameObject);
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("F11 开关本面板；所有修改即时写入存档", hintStyle);
        GUILayout.Space(4);
    }

    /// <summary>
    /// 绘制操作结果状态栏（成功绿色/失败红色，超时后隐藏）
    /// </summary>
    private void DrawStatusBar()
    {
        if (statusText.IsNull() || Time.unscaledTime >= statusExpireTime) return;
        Color oldColor = GUI.color;
        GUI.color = statusIsError ? new Color(1f, 0.45f, 0.45f) : new Color(0.45f, 1f, 0.55f);
        GUILayout.Label(statusText, statusStyle);
        GUI.color = oldColor;
        GUILayout.Space(4);
    }

    /// <summary>
    /// 绘制资源区（魔晶/声望）
    /// </summary>
    private void DrawResourceSection()
    {
        DrawSectionHeader("资源");
        //魔晶
        GUILayout.BeginHorizontal();
        GUILayout.Label("魔晶", labelStyle, GUILayout.Width(50));
        inputCrystal = GUILayout.TextField(inputCrystal, GUILayout.Height(26), GUILayout.Width(150));
        if (GUILayout.Button("添加", GUILayout.Height(26)))
        {
            AddCrystal();
        }
        GUILayout.Label("(空=999999)", hintStyle);
        GUILayout.EndHorizontal();
        //声望
        GUILayout.BeginHorizontal();
        GUILayout.Label("声望", labelStyle, GUILayout.Width(50));
        inputReputation = GUILayout.TextField(inputReputation, GUILayout.Height(26), GUILayout.Width(150));
        if (GUILayout.Button("添加", GUILayout.Height(26)))
        {
            AddReputation();
        }
        GUILayout.Label("(空=999999)", hintStyle);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制道具区（按ID/全部添加，每稀有度各一）
    /// </summary>
    private void DrawItemSection()
    {
        DrawSectionHeader("道具");
        GUILayout.BeginHorizontal();
        GUILayout.Label("道具ID", labelStyle, GUILayout.Width(50));
        inputItemId = GUILayout.TextField(inputItemId, GUILayout.Height(26), GUILayout.Width(150));
        if (GUILayout.Button("添加", GUILayout.Height(26)))
        {
            AddItem();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("空=全部道具；装备每种稀有度(N~L)各一，非装备(幻化药等)仅1件无属性", hintStyle);
    }

    /// <summary>
    /// 绘制生物区（添加所有生物 + 定制测试生物）
    /// </summary>
    private void DrawCreatureSection()
    {
        DrawSectionHeader("生物");
        if (GUILayout.Button("添加所有生物（稀有度随机/等级0）", GUILayout.Height(26)))
        {
            AddAllCreature();
        }
        GUILayout.Space(2);
        //生物下拉
        GUILayout.BeginHorizontal();
        GUILayout.Label("生物", labelStyle, GUILayout.Width(50));
        if (GUILayout.Button(creatureDropdownLabel, buttonLeftStyle, GUILayout.Height(26)))
        {
            isCreatureDropdownOpen = !isCreatureDropdownOpen;
            isRarityDropdownOpen = false;
            isModDropdownOpen = false;
        }
        GUILayout.EndHorizontal();
        if (isCreatureDropdownOpen)
        {
            scrollCreatureDropdown = GUILayout.BeginScrollView(scrollCreatureDropdown, GUI.skin.box, GUILayout.Height(240));
            foreach (var option in listCreatureOptions)
            {
                bool isCurrent = option.id == currentCreatureId;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                if (GUILayout.Button(isCurrent ? $"✔ {option.label}" : option.label, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    currentCreatureId = option.id;
                    creatureDropdownLabel = option.label;
                    isCreatureDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
        //手动ID(空=用下拉)
        GUILayout.BeginHorizontal();
        GUILayout.Label("手动ID", labelStyle, GUILayout.Width(50));
        inputManualCreatureId = GUILayout.TextField(inputManualCreatureId, GUILayout.Height(26), GUILayout.Width(150));
        GUILayout.Label("(空=用下拉)", hintStyle);
        GUILayout.EndHorizontal();
        if (!inputManualCreatureId.IsNull())
        {
            bool isValidManual = long.TryParse(inputManualCreatureId, out long manualId) && CreatureInfoCfg.GetItemData(manualId) != null;
            if (!isValidManual)
            {
                GUILayout.Label("⚠ 手动ID无效或不在配置表，将使用下拉选择", hintStyle);
            }
        }
        //稀有度下拉(0=随机)
        GUILayout.BeginHorizontal();
        GUILayout.Label("稀有度", labelStyle, GUILayout.Width(50));
        if (GUILayout.Button(RarityLabels[raritySelectIndex], buttonLeftStyle, GUILayout.Height(26), GUILayout.Width(100)))
        {
            isRarityDropdownOpen = !isRarityDropdownOpen;
            isCreatureDropdownOpen = false;
            isModDropdownOpen = false;
        }
        //等级: 随机开关 + 滑条
        GUILayout.Label("等级", labelStyle, GUILayout.Width(36));
        isRandomLevel = GUILayout.Toggle(isRandomLevel, "随机", labelStyle, GUILayout.Width(50));
        GUI.enabled = !isRandomLevel;
        level = (int)GUILayout.HorizontalSlider(level, 0, 10, GUILayout.Height(26));
        GUILayout.Label(isRandomLevel ? "0~10" : $"{level}", labelStyle, GUILayout.Width(34));
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        if (isRarityDropdownOpen)
        {
            for (int i = 0; i < RarityLabels.Length; i++)
            {
                bool isCurrent = i == raritySelectIndex;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                if (GUILayout.Button(isCurrent ? $"✔ {RarityLabels[i]}" : RarityLabels[i], buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    raritySelectIndex = i;
                    isRarityDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
        }
        if (GUILayout.Button("添加测试生物（含随机稀有度BUFF）", GUILayout.Height(26)))
        {
            AddTestCreature();
        }
    }

    /// <summary>
    /// 绘制解锁区（按ID/全部解锁 + 世界难度）
    /// </summary>
    private void DrawUnlockSection()
    {
        DrawSectionHeader("解锁");
        GUILayout.BeginHorizontal();
        GUILayout.Label("解锁ID", labelStyle, GUILayout.Width(50));
        inputUnlockId = GUILayout.TextField(inputUnlockId, GUILayout.Height(26), GUILayout.Width(150));
        if (GUILayout.Button("添加", GUILayout.Height(26)))
        {
            AddUnlock();
        }
        GUILayout.Label("(空=全部解锁)", hintStyle);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("世界难度解锁一半", GUILayout.Height(26)))
        {
            UnlockWorldDifficulty(true);
        }
        if (GUILayout.Button("世界难度解锁全部", GUILayout.Height(26)))
        {
            UnlockWorldDifficulty(false);
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制 Mod 道具区（下拉选择 Mod，添加该 Mod 全部道具）
    /// </summary>
    private void DrawModSection()
    {
        DrawSectionHeader("Mod 道具");
        if (listModOptions.Count == 0)
        {
            GUILayout.Label("没有包含道具配置(ItemsInfo)的 Mod", hintStyle);
            return;
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Mod", labelStyle, GUILayout.Width(50));
        if (GUILayout.Button(modDropdownLabel, buttonLeftStyle, GUILayout.Height(26)))
        {
            isModDropdownOpen = !isModDropdownOpen;
            isCreatureDropdownOpen = false;
            isRarityDropdownOpen = false;
        }
        GUILayout.EndHorizontal();
        if (isModDropdownOpen)
        {
            scrollModDropdown = GUILayout.BeginScrollView(scrollModDropdown, GUI.skin.box, GUILayout.Height(160));
            for (int i = 0; i < listModOptions.Count; i++)
            {
                bool isCurrent = i == modSelectIndex;
                Color oldColor = GUI.color;
                if (isCurrent) GUI.color = Color.green;
                if (GUILayout.Button(isCurrent ? $"✔ {listModOptions[i].label}" : listModOptions[i].label, buttonLeftStyle, GUILayout.Height(24)))
                {
                    GUI.color = oldColor;
                    modSelectIndex = i;
                    modDropdownLabel = listModOptions[i].label;
                    isModDropdownOpen = false;
                    break;
                }
                GUI.color = oldColor;
            }
            GUILayout.EndScrollView();
        }
        if (GUILayout.Button("添加该 Mod 全部道具（装备每稀有度各一）", GUILayout.Height(26)))
        {
            AddModItems();
        }
    }

    /// <summary>
    /// 绘制分区标题（加粗带底色分隔）
    /// </summary>
    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(6);
        Color oldBgColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.35f, 0.55f, 0.85f);
        GUILayout.Label($" {title}", sectionHeaderStyle);
        GUI.backgroundColor = oldBgColor;
        GUILayout.Space(2);
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
        //分区标题样式(白字加粗, 配合底色块)
        sectionHeaderStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        sectionHeaderStyle.normal.textColor = Color.white;
        //状态栏样式(颜色由 GUI.color 控制)
        statusStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft, wordWrap = true };
        statusStyle.normal.textColor = Color.white;
    }

    #endregion
}
