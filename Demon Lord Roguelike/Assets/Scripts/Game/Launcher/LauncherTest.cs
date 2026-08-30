using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("测试类型")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.Base;

    //献祭升级测试:基地场景加载完成后待执行的回调
    private System.Action actionForSacrificeTest;
    //魔物进阶测试:基地场景加载完成后待执行的回调
    private System.Action actionForCreatureVatTest;
    //魔汁机测试:基地场景加载完成后待执行的回调
    private System.Action actionForJuicerTest;
    //故事演出测试:场景就绪后待执行的回调(基地/战斗共用,一次性)
    private System.Action actionForStoryTest;

    public override void Launch()
    {
        base.Launch();     
        ModHandler.Instance.InitializeAllModsSync();
        InitTestData();
        // CreatureBean itemData = new CreatureBean(999998);
        // itemData.AddAllSkin();
        // StartForBaseTest(itemData);
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    public void InitTestData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        var npcInfo = NpcInfoCfg.GetItemData(1010010001);
        userData.selfCreature = new CreatureBean(npcInfo);
        for (int i = 0; i < 50; i++)
        {
            CreatureBean creatureItem = new CreatureBean(2002);
            creatureItem.rarity = Random.Range(1, 7);
            creatureItem.level = Random.Range(0, 11);
            creatureItem.AddSkinForBase();
            //史莱姆加一个身体皮肤
            if (creatureItem.creatureId > 3000 && creatureItem.creatureId < 4000)
            {
                creatureItem.AddSkin(3040001);
            }
            userData.AddBackpackCreature(creatureItem);

            //添加到阵容1
            userData.AddLineupCreature(1, creatureItem.creatureUUId);
        }
        userData.AddCrystal(99999);
        userData.AddReputation(1000);
        //添加道具
        userData.AddBackpackItem(new ItemBean(10100001));
        userData.AddBackpackItem(new ItemBean(10100002));
        userData.AddBackpackItem(new ItemBean(10100003));
        userData.AddBackpackItem(new ItemBean(10100004));
        //解锁所有unlock
        var userUnlockData = userData.GetUserUnlockData();
        var allUnlockInfo = UnlockInfoCfg.GetAllArrayData();
        allUnlockInfo.ForEach((index, value) =>
        {
            var researchInfo = ResearchInfoCfg.GetItemDataByUnlockId(value.id);
            if (researchInfo == null)
            {
                userUnlockData.AddUnlock(value.id);
            }
            else
            {
                userUnlockData.AddUnlock(value.id, researchInfo.level_max);
            }
        });
    }

    /// <summary>
    /// 开始战斗场景测试
    /// </summary>
    /// <param name="fightData"></param>
    public void StartForFightSceneTest(FightBean fightData)
    {
        WorldHandler.Instance.EnterGameForFightScene(fightData);
    }

    /// <summary>
    /// 开始征服模式BOSS关测试
    /// 指定世界与难度，将关卡总数设为1使首关即为BOSS关，直接进入征服模式BOSS关
    /// </summary>
    /// <param name="worldId">世界ID</param>
    /// <param name="difficultyLevel">难度等级</param>
    public void StartForConquerBossTest(long worldId, int difficultyLevel)
    {
        //校验征服模式配置是否存在
        FightTypeConquerInfoBean conquerInfo = FightTypeConquerInfoCfg.GetItemData(worldId, difficultyLevel);
        if (conquerInfo == null)
        {
            LogUtil.LogError($"征服模式BOSS关测试失败，找不到配置 worldId:{worldId} difficultyLevel:{difficultyLevel}");
            return;
        }
        //构建征服模式随机数据
        GameWorldInfoRandomBean gameWorldInfoRandomData = new GameWorldInfoRandomBean();
        gameWorldInfoRandomData.worldId = worldId;
        gameWorldInfoRandomData.gameFightType = GameFightTypeEnum.Conquer;
        gameWorldInfoRandomData.difficultyLevel = difficultyLevel;
        //随机道路数据(沿用征服配置)
        gameWorldInfoRandomData.roadNum = conquerInfo.GetRandomRoadNum();
        gameWorldInfoRandomData.roadLength = conquerInfo.GetRandomRoadLength();
        //关卡总数设为1，使首关(fightNum=1)即满足 IsBossFight，直接进入BOSS关
        gameWorldInfoRandomData.fightNum = 1;
        //进入征服模式战斗
        FightBeanForConquer fightData = new FightBeanForConquer(gameWorldInfoRandomData);
        WorldHandler.Instance.EnterGameForFightScene(fightData);
    }

    /// <summary>
    /// 开始终焉议会测试
    /// </summary>
    public void StartForDoomCouncil(long billId)
    {
        //打开终焉ui
        //var uiDoomCouncil = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilBill>();
        //进入议会场景
        DoomCouncilBean doomCouncilData = new DoomCouncilBean(billId);
        GameHandler.Instance.StartDoomCouncil(doomCouncilData);
    }

    /// <summary>
    /// 开始终焉议会测试(直接载入所有固定议员, 用于测试固定议员的显示/参数)
    /// </summary>
    /// <param name="billId">议案 ID(仍需有效, 用于议员态度生成)</param>
    public void StartForDoomCouncilAllFixed(long billId)
    {
        //进入议会场景, 标记为载入所有固定议员
        DoomCouncilBean doomCouncilData = new DoomCouncilBean(billId);
        doomCouncilData.isTestAllFixedCouncilor = true;
        GameHandler.Instance.StartDoomCouncil(doomCouncilData);
    }

    /// <summary>
    /// 开始奖励选择
    /// </summary>
    /// <param name="testData">测试数据，可配置装备品质、使用者类型、属性加成</param>
    public void StartForRewardSelect(RewardSelectTestData testData = null)
    {
        //打开领奖界面
        var uiRewardSelect = UIHandler.Instance.OpenUIAndCloseOther<UIRewardSelect>();
        RewardSelectBean rewardSelectData = new RewardSelectBean();
        rewardSelectData.InitData(null, testData);
        uiRewardSelect.SetData(rewardSelectData, null);
    }

    /// <summary>
    /// 开始卡片测试
    /// </summary>
    /// <param name="fightCreature"></param>
    public async void StartForCardTest(FightCreatureBean fightCreature)
    {
        await WorldHandler.Instance.ClearWorldData();
        //设置焦距
        VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
        //镜头初始化
        CameraHandler.Instance.InitData();
        //关闭额外的摄像头
        var ui = UIHandler.Instance.OpenUIAndCloseOther<UITestCard>();
        ui.SetData(fightCreature);
    }

    /// <summary>
    /// 开始NPC创建
    /// </summary>
    public async void StartNpcCreate()
    {
        await WorldHandler.Instance.ClearWorldData();
        //设置焦距
        VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
        //镜头初始化
        CameraHandler.Instance.InitData();
        //关闭额外的摄像头
        var ui = UIHandler.Instance.OpenUIAndCloseOther<UITestNpcCreate>();
    }

    /// <summary>
    /// 开始NPC创建（GUI版，纯代码UI，不依赖预制）
    /// </summary>
    public async void StartNpcCreateGUI()
    {
        await WorldHandler.Instance.ClearWorldData();
        //设置焦距
        VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
        //镜头初始化
        CameraHandler.Instance.InitData();
        //关闭其它UI，避免预制版NPC创建界面叠加
        UIHandler.Instance.CloseAllUI();
        //挂载纯GUI代码的NPC创建组件到空物体
        new GameObject("NpcCreateGUI").AddComponent<TestNpcCreateGUI>();
    }

    /// <summary>
    /// 开始粒子特效测试（GUI版，纯代码UI，不依赖预制）
    /// </summary>
    public async void StartForEffectTest()
    {
        await WorldHandler.Instance.ClearWorldData();
        //设置焦距
        VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
        //镜头初始化
        CameraHandler.Instance.InitData();
        //关闭其它UI
        UIHandler.Instance.CloseAllUI();
        //清理上一次可能残留的特效测试面板，避免重复开始导致面板叠加
        if (TestEffectGUI.Instance != null) Destroy(TestEffectGUI.Instance.gameObject);
        //挂载纯GUI代码的特效测试组件到空物体
        new GameObject("EffectTestGUI").AddComponent<TestEffectGUI>();
    }

    /// <summary>
    /// 开始对话系统测试（自由输入文本 + 指定NPC，在测试场景直接打开对话UI）
    /// 清理世界数据后打开 UIGameConversation：显示所选NPC的名字/头像，逐字展示输入文本（带说话音效）。
    /// </summary>
    /// <param name="npcId">说话NPC的ID（NpcInfo.id，为 long）</param>
    /// <param name="content">要展示的对话文本（自由输入，不走多语言）</param>
    public async void StartForConversationTest(long npcId, string content)
    {
        //校验NPC配置与对话文本
        NpcInfoBean npcInfo = NpcInfoCfg.GetItemData(npcId);
        if (npcInfo == null)
        {
            LogUtil.LogError($"对话系统测试失败，找不到NPC配置 id:{npcId}");
            return;
        }
        if (content.IsNull())
        {
            LogUtil.LogError("对话系统测试失败，对话文本为空");
            return;
        }
        await WorldHandler.Instance.ClearWorldData();
        //设置焦距
        VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
        //镜头初始化
        CameraHandler.Instance.InitData();
        //开启测试模拟：对话UI带贿赂入口（贿赂固定议员会 SaveUserData），防止误写真实存档
        GameDataHandler.Instance.manager.isTestSimulation = true;
        //构建说话生物数据（NPC构造入口初始化皮肤/装备/名字，与议会交谈同一数据口径）
        CreatureBean creatureData = new CreatureBean(npcInfo);
        //creatureObj 在对话UI中仅用于贿赂特效定位，测试场景无实体，用空物体兜底防空引用
        GameObject creatureObj = new GameObject($"ConversationTest_{npcId}");
        var uiConversation = UIHandler.Instance.OpenUIAndCloseOther<UIGameConversation>();
        //结束回调：测试无后续界面，对话结束后直接关闭对话UI
        uiConversation.SetData(creatureObj, creatureData, content, () => uiConversation.CloseUI());
    }

    /// <summary>
    /// 基地测试
    /// </summary>
    public void StartForBaseTest(CreatureBean creatureData)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.selfCreature = creatureData;
        WorldHandler.Instance.EnterGameForBaseScene(userData);
    }

    /// <summary>
    /// 研究UI测试
    /// </summary>
    public void StartForResearchUI()
    {
        UIBaseResearch uiBaseResearch = UIHandler.Instance.OpenUIAndCloseOther<UIBaseResearch>();
        uiBaseResearch.SetDataForTest();
    }

    /// <summary>
    /// 开始生物献祭升级测试
    /// 加载指定存档槽位的数据作为运行时数据，进入基地场景后直接对选中生物发起献祭(测试模式，结算不落盘到真实存档)
    /// </summary>
    /// <param name="saveSlot">存档槽位(1~3，与游戏一致：UserData_1/2/3)</param>
    /// <param name="targetCreatureUUId">目标生物 UUId(从该存档背包中选取)</param>
    /// <param name="useManualSuccessRate">是否使用手动成功率(false 则使用该存档真实数据按公式计算)</param>
    /// <param name="manualSuccessRate">手动成功率(0~1)</param>
    public void StartForCreatureSacrificeTest(int saveSlot, string targetCreatureUUId, bool useManualSuccessRate, float manualSuccessRate)
    {
        //加载指定槽位存档数据
        UserDataService dataService = new UserDataService();
        dataService.ChangeSlot(saveSlot);
        UserDataBean userData = dataService.Load(false);
        if (userData == null)
        {
            LogUtil.LogError($"献祭升级测试失败，存档 {saveSlot} 不存在或为空");
            return;
        }
        //定位目标生物(必须是存档背包中的同一引用，献祭逻辑按引用排除目标)
        CreatureBean targetCreature = null;
        foreach (var creatureData in userData.GetUserBackpackCreatureData().listBackpackCreature)
        {
            if (creatureData.creatureUUId == targetCreatureUUId)
            {
                targetCreature = creatureData;
                break;
            }
        }
        if (targetCreature == null)
        {
            LogUtil.LogError($"献祭升级测试失败，存档 {saveSlot} 中找不到目标生物 UUId:{targetCreatureUUId}");
            return;
        }
        //以该存档数据替换运行时数据，并开启测试模拟(全程不落盘回真实存档，由 GameDataManager 统一拦截)
        GameDataHandler.Instance.manager.SetUserData(userData);
        GameDataHandler.Instance.manager.isTestSimulation = true;

        //清理上一次未触发的待执行回调，避免重复注册
        if (actionForSacrificeTest != null)
        {
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForSacrificeTest);
            actionForSacrificeTest = null;
        }

        //基地场景加载完成后，直接进入献祭流程
        actionForSacrificeTest = () =>
        {
            //一次性回调，触发后立即注销
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForSacrificeTest);
            CreatureSacrificeBean creatureSacrificeData = new CreatureSacrificeBean();
            creatureSacrificeData.targetCreature = targetCreature;
            creatureSacrificeData.useManualSuccessRate = useManualSuccessRate;
            creatureSacrificeData.manualSuccessRate = manualSuccessRate;
            GameHandler.Instance.StartCreatureSacrifice(creatureSacrificeData);
        };
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForSacrificeTest);

        //进入基地场景(使用该存档数据)
        WorldHandler.Instance.EnterGameForBaseScene(userData);
    }

    /// <summary>
    /// 开始魔物进阶(生物升阶容器)测试
    /// 加载指定存档槽位数据作为运行时数据，覆盖解锁的VAT数量/加速等级后进入基地场景，直接打开魔物进阶UI(测试模拟，全程内存模拟不落盘)。
    /// </summary>
    /// <param name="saveSlot">存档槽位(1~3，与游戏一致：UserData_1/2/3)</param>
    /// <param name="vatNum">解锁的VAT总数量(≥1，运行时按配置钳制到 基础creatureVatMax+CreatureVatAdd研究满级)</param>
    /// <param name="addProgressLevel">解锁的魔晶加速等级(0=加速锁定/隐藏加速按钮，运行时按配置钳制到 CreatureVatAddProgress研究满级)</param>
    public void StartForCreatureVatTest(int saveSlot, int vatNum, int addProgressLevel)
    {
        //加载指定槽位存档数据
        UserDataService dataService = new UserDataService();
        dataService.ChangeSlot(saveSlot);
        UserDataBean userData = dataService.Load(false);
        if (userData == null)
        {
            LogUtil.LogError($"魔物进阶测试失败，存档 {saveSlot} 不存在或为空");
            return;
        }
        //以该存档数据替换运行时数据，并开启测试模拟(全程不落盘回真实存档，由 GameDataManager 统一拦截)
        GameDataHandler.Instance.manager.SetUserData(userData);
        GameDataHandler.Instance.manager.isTestSimulation = true;

        //覆盖VAT相关解锁:数量=基础creatureVatMax+CreatureVatAdd研究等级；传入值按配置上限钳制(容错编辑器与配置的漂移)
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        int baseVatMax = userData.GetUserLimmitData().creatureVatMax;
        int vatAddLevelMax = ResearchInfoCfg.GetItemDataByUnlockId((long)UnlockEnum.CreatureVatAdd)?.level_max ?? 0;
        int progressLevelMax = ResearchInfoCfg.GetItemDataByUnlockId((long)UnlockEnum.CreatureVatAddProgress)?.level_max ?? 0;
        int targetVatNum = Mathf.Clamp(vatNum, baseVatMax, baseVatMax + vatAddLevelMax);
        int targetProgressLevel = Mathf.Clamp(addProgressLevel, 0, progressLevelMax);
        //确保VAT功能已解锁，并把附加数量研究等级覆盖为目标值(总数=基础+附加等级)
        userUnlock.AddUnlock((long)UnlockEnum.CreatureVat);
        userUnlock.AddUnlock((long)UnlockEnum.CreatureVatAdd, targetVatNum - baseVatMax);
        //覆盖魔晶加速研究等级(0=锁定，隐藏加速按钮)
        userUnlock.AddUnlock((long)UnlockEnum.CreatureVatAddProgress, targetProgressLevel);

        //清理上一次未触发的待执行回调，避免重复注册
        if (actionForCreatureVatTest != null)
        {
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForCreatureVatTest);
            actionForCreatureVatTest = null;
        }

        //基地场景加载完成后，直接打开魔物进阶UI(不落盘由全局测试模拟标记统一拦截)
        actionForCreatureVatTest = () =>
        {
            //一次性回调，触发后立即注销
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForCreatureVatTest);
            //测试入口打开: 退出时返回 UIBaseCore(与核心入口一致)
            UIHandler.Instance.OpenUIAndCloseOther<UICreatureVat>((ui) =>
            {
                ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
            });
        };
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForCreatureVatTest);

        //进入基地场景(使用该存档数据)
        WorldHandler.Instance.EnterGameForBaseScene(userData);
    }

    /// <summary>
    /// 开始魔汁机(魔物回收)测试
    /// 加载指定存档槽位数据作为运行时数据，覆盖魔汁机解锁/投入数量上限后进入基地场景，直接打开魔汁机UI(测试模拟，全程内存模拟不落盘)。
    /// </summary>
    /// <param name="saveSlot">存档槽位(1~3，与游戏一致：UserData_1/2/3)</param>
    /// <param name="juicerCreatureMax">投入魔物可选上限(运行时按配置钳制到 基础juicerCreatureMax+JuicerNum研究满级)</param>
    public void StartForCreatureJuicerTest(int saveSlot, int juicerCreatureMax)
    {
        //加载指定槽位存档数据
        UserDataService dataService = new UserDataService();
        dataService.ChangeSlot(saveSlot);
        UserDataBean userData = dataService.Load(false);
        if (userData == null)
        {
            LogUtil.LogError($"魔汁机测试失败，存档 {saveSlot} 不存在或为空");
            return;
        }
        //以该存档数据替换运行时数据，并开启测试模拟(全程不落盘回真实存档，由 GameDataManager 统一拦截)
        GameDataHandler.Instance.manager.SetUserData(userData);
        GameDataHandler.Instance.manager.isTestSimulation = true;

        //覆盖魔汁机相关解锁:投入上限=基础juicerCreatureMax+JuicerNum研究等级；传入值按配置上限钳制(容错编辑器与配置的漂移)
        UserUnlockBean userUnlock = userData.GetUserUnlockData();
        int baseJuicerMax = userData.GetUserLimmitData().juicerCreatureMax;
        int juicerNumLevelMax = ResearchInfoCfg.GetItemDataByUnlockId((long)UnlockEnum.JuicerNum)?.level_max ?? 0;
        int targetJuicerMax = Mathf.Clamp(juicerCreatureMax, baseJuicerMax, baseJuicerMax + juicerNumLevelMax);
        //确保魔汁机功能已解锁，并把投入上限研究等级覆盖为目标值(上限=基础+研究等级)
        userUnlock.AddUnlock((long)UnlockEnum.Juicer);
        userUnlock.AddUnlock((long)UnlockEnum.JuicerNum, targetJuicerMax - baseJuicerMax);

        //清理上一次未触发的待执行回调，避免重复注册
        if (actionForJuicerTest != null)
        {
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForJuicerTest);
            actionForJuicerTest = null;
        }

        //基地场景加载完成后，直接打开魔汁机UI(不落盘由全局测试模拟标记统一拦截)
        actionForJuicerTest = () =>
        {
            //一次性回调，触发后立即注销
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForJuicerTest);
            //测试入口打开: 退出时返回 UIBaseMain(与场景E键交互入口一致)
            UIHandler.Instance.OpenUIAndCloseOther<UICreatureJuicer>((ui) =>
            {
                ui.actionForExit = () => UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
            });
        };
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForJuicerTest);

        //进入基地场景(使用该存档数据)
        WorldHandler.Instance.EnterGameForBaseScene(userData);
    }
    /// 走与 LauncherGame 一致的真实开始流程(清理运行时数据→加载基地场景→打开主菜单 UIMainStart)，
    /// 免去每次手动切换到 GameScene 再运行。
    /// </summary>
    public void StartForNormalGame()
    {
        //与 LauncherGame 对齐:注册故事演出自动触发事件——此入口等价正式游戏流程,不注册则进存档后引导演出(进基地/进战斗/掉晶)全部无人监听永不触发;
        //StoryTest 测试场景(StartForStoryTest)仍不注册,自动触发天然关闭,测试走 PlayStory 强制播放
        StoryHandler.Instance.InitData();
        //测试模拟标记的复位统一在 WorldHandler.EnterMainForBaseScene 内(真实回主菜单收口点)完成，此处无需再复位
        WorldHandler.Instance.EnterMainForBaseScene();
    }

    /// <summary>
    /// 开始故事演出测试
    /// 按故事配置的演出场景先进对应场景(基地/战斗/终焉议会),就绪后强制播放演出。
    /// 测试场景不注册故事自动触发(InitData 仅 LauncherGame 调用),这里直接调 StoryHandler.PlayStory;
    /// 全程测试模拟(isTestSimulation),播放记录不落盘到真实存档。
    /// </summary>
    /// <param name="storyId">故事ID(StoryInfo.id)</param>
    /// <param name="saveSlot">存档槽位(0=使用当前测试数据 InitTestData 伪造数据;1~3=读取对应存档槽位 UserData_1/2/3 作为运行时数据,与献祭测试同范式)</param>
    public void StartForStoryTest(long storyId, int saveSlot = 0)
    {
        var storyInfo = StoryInfoCfg.GetItemData(storyId);
        if (storyInfo == null)
        {
            LogUtil.LogError($"故事演出测试失败,找不到故事配置 id:{storyId}");
            return;
        }
        //选择存档槽位(1~3)时,读取该存档数据替换为运行时数据(全程内存模拟,不写回真实存档)
        if (saveSlot > 0)
        {
            UserDataService dataService = new UserDataService();
            dataService.ChangeSlot(saveSlot);
            UserDataBean userData = dataService.Load(false);
            if (userData == null)
            {
                LogUtil.LogError($"故事演出测试失败,存档 {saveSlot} 不存在或为空");
                return;
            }
            GameDataHandler.Instance.manager.SetUserData(userData);
        }
        //开启测试模拟:播完记录已播故事(UserStoryBean)时 SaveUserData 被 GameDataManager 统一拦截
        GameDataHandler.Instance.manager.isTestSimulation = true;
        switch (storyInfo.GetSceneType())
        {
            case StorySceneTypeEnum.Base:
                //基地场景加载完成后强制播放(一次性回调,范式同献祭测试)
                RegisterStoryTestPlayCallback(EventsInfo.World_EnterGameForBaseScene, storyId);
                WorldHandler.Instance.EnterGameForBaseScene(GameDataHandler.Instance.manager.GetUserData());
                break;
            case StorySceneTypeEnum.Fight:
                //战斗卡片出现动画播完后强制播放(与真实触发同钩点,保证高亮手卡等目标已落位);用内置默认测试战斗数据进入
                RegisterStoryTestPlayCallback(EventsInfo.UIFightMain_CardCreateAnimEnd, storyId);
                WorldHandler.Instance.EnterGameForFightScene(BuildStoryTestFightData());
                break;
            case StorySceneTypeEnum.DoomCouncil:
                //议会场景无就绪事件(自动触发钩子为第二期预留),进场景后轮询就绪再强制播放
                GameHandler.Instance.StartDoomCouncil(new DoomCouncilBean(1000000001));
                _ = WaitForDoomCouncilThenPlayStory(storyId);
                break;
        }
    }

    /// <summary>
    /// 注册故事演出测试的一次性场景就绪回调(就绪后强制播放;重复调用先清旧回调防重复注册)
    /// </summary>
    /// <param name="eventName">就绪事件名(World_EnterGameForBaseScene / UIFightMain_CardCreateAnimEnd)</param>
    /// <param name="storyId">待播放的故事ID</param>
    private void RegisterStoryTestPlayCallback(string eventName, long storyId)
    {
        if (actionForStoryTest != null)
        {
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForStoryTest);
            EventHandler.Instance.UnRegisterEvent(EventsInfo.UIFightMain_CardCreateAnimEnd, actionForStoryTest);
            actionForStoryTest = null;
        }
        actionForStoryTest = () =>
        {
            //一次性回调,触发后立即注销
            EventHandler.Instance.UnRegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForStoryTest);
            EventHandler.Instance.UnRegisterEvent(EventsInfo.UIFightMain_CardCreateAnimEnd, actionForStoryTest);
            actionForStoryTest = null;
            StoryHandler.Instance.PlayStory(storyId);
        };
        EventHandler.Instance.RegisterEvent(eventName, actionForStoryTest);
    }

    /// <summary>
    /// 等终焉议会场景就绪后强制播放故事(async UniTaskVoid 发射即忘;议会无就绪事件,场景出现后再给固定缓冲等实体初始化)
    /// </summary>
    private async UniTaskVoid WaitForDoomCouncilThenPlayStory(long storyId)
    {
        await GTask.WaitUntil(() => WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.DoomCouncil) != null, null);
        await GTask.WaitReal(1f, null);
        StoryHandler.Instance.PlayStory(storyId);
    }

    /// <summary>
    /// 构建故事演出测试用的默认测试战斗数据(1路x10长/2波进攻/5张2002防守卡/核心2001,保持测试入口自包含)
    /// </summary>
    private FightBean BuildStoryTestFightData()
    {
        FightBeanForTest fightData = new FightBeanForTest();
        fightData.sceneRoadNum = 1;
        fightData.sceneRoadLength = 10;
        fightData.gameFightType = GameFightTypeEnum.Test;
        //进攻数据:2波,默认敌人
        fightData.fightAttackData = new FightAttackBean();
        var enemyIds = new List<long> { 1010010001 };
        for (int i = 0; i < 2; i++)
        {
            fightData.fightAttackData.AddAttackQueue(new FightAttackDetailsBean(1, enemyIds));
        }
        fightData.fightAttackDataRemark = ClassUtil.DeepCopy(fightData.fightAttackData);
        //防守卡片:5张默认魔物
        fightData.dlDefenseCreatureData.Clear();
        for (int i = 0; i < 5; i++)
        {
            CreatureBean itemData = new CreatureBean(2002);
            itemData.AddSkinForBase();
            itemData.order = i;
            fightData.dlDefenseCreatureData.Add(itemData.creatureUUId, itemData);
        }
        //防守核心(魔王)
        FightCreatureBean fightDefCoreData = CreatureHandler.Instance.GetFightCreatureData(2001, CreatureFightTypeEnum.FightDefenseCore);
        fightDefCoreData.creatureData.AddSkinForBase();
        fightData.fightDefenseCoreData = fightDefCoreData;
        fightData.testDemonLordMP = 9999;
        fightData.InitData();
        fightData.fightSceneId = 10001;
        return fightData;
    }

    /// <summary>
    /// 深渊馈赠 UI 测试-按指定 IDs 展示 UIFightAbyssalBlessing
    /// </summary>
    /// <param name="ids">深渊馈赠 ID 列表，null 或空时不展示任何卡片</param>
    public void StartForAbyssalBlessingUI(List<long> ids)
    {
        long[] arrayIds = ids == null ? new long[0] : ids.ToArray();
        var uiBlessing = UIHandler.Instance.OpenUIAndCloseOther<UIFightAbyssalBlessing>();
        uiBlessing.SetDataForTest(
            arrayIds,
            info => LogUtil.Log($"[Test] 选择深渊馈赠: id={info.id}"),
            () => LogUtil.Log("[Test] 跳过深渊馈赠")
        );
    }

}
