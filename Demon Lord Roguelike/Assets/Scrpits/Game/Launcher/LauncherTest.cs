using System.Collections.Generic;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("测试类型")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.Base;

    public override void Launch()
    {
        base.Launch();
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
            creatureItem.starLevel = Random.Range(0, 11);
            creatureItem.level = Random.Range(0, 101);
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
        userData.AddCrystal(999);
        userData.AddReputation(1000);
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
    /// 开始终焉议会测试
    /// </summary>
    public async void StartForDoomCouncil(long billId)
    {
        //打开终焉ui
        //var uiDoomCouncil = UIHandler.Instance.OpenUIAndCloseOther<UIDoomCouncilMain>();

        DoomCouncilInfoBean doomCouncilInfo = DoomCouncilInfoCfg.GetItemData(billId);
        //进入议会场景
        DoomCouncilBean doomCouncilData = new DoomCouncilBean();
        doomCouncilData.doomCouncilInfo = doomCouncilInfo;
        GameHandler.Instance.StartDoomCouncil(doomCouncilData);
    }

    /// <summary>
    /// 开始奖励选择
    /// </summary>
    public async void StartForRewardSelect()
    {
        //打开领奖界面
        var uiRewardSelect = UIHandler.Instance.OpenUIAndCloseOther<UIRewardSelect>();
        RewardSelectBean rewardSelectData = new RewardSelectBean();
        rewardSelectData.selectNum = 0;
        rewardSelectData.selectNumMax = 1;
        rewardSelectData.listReward = new List<ItemBean>();
        rewardSelectData.listReward.Add(new ItemBean(10100001));
        rewardSelectData.listReward.Add(new ItemBean(10100002));
        rewardSelectData.listReward.Add(new ItemBean(10100003));
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
        WorldHandler.Instance.EnterGameForBaseScene(userData, true);
    }
}
