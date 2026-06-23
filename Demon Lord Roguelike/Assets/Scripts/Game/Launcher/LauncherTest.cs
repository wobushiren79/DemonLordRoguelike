using System.Collections.Generic;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("测试类型")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.Base;

    //献祭升级测试:基地场景加载完成后待执行的回调
    private System.Action actionForSacrificeTest;

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
    public async void StartForDoomCouncil(long billId)
    {
        //打开终焉ui
        //var uiDoomCouncil = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilBill>();
        //进入议会场景
        DoomCouncilBean doomCouncilData = new DoomCouncilBean(billId);
        GameHandler.Instance.StartDoomCouncil(doomCouncilData);
    }

    /// <summary>
    /// 开始奖励选择
    /// </summary>
    /// <param name="testData">测试数据，可配置装备品质、使用者类型、属性加成</param>
    public async void StartForRewardSelect(RewardSelectTestData testData = null)
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
        //以该存档数据替换运行时数据(测试模式不会落盘回真实存档)
        GameDataHandler.Instance.manager.SetUserData(userData);

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
            creatureSacrificeData.isTestMode = true;
            creatureSacrificeData.useManualSuccessRate = useManualSuccessRate;
            creatureSacrificeData.manualSuccessRate = manualSuccessRate;
            GameHandler.Instance.StartCreatureSacrifice(creatureSacrificeData);
        };
        EventHandler.Instance.RegisterEvent(EventsInfo.World_EnterGameForBaseScene, actionForSacrificeTest);

        //进入基地场景(使用该存档数据)
        WorldHandler.Instance.EnterGameForBaseScene(userData);
    }

    /// <summary>
    /// 正常游戏启动测试
    /// 走与 LauncherGame 一致的真实开始流程(清理运行时数据→加载基地场景→打开主菜单 UIMainStart)，
    /// 免去每次手动切换到 GameScene 再运行。
    /// </summary>
    public void StartForNormalGame()
    {
        WorldHandler.Instance.EnterMainForBaseScene();
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
