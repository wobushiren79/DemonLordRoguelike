using Unity.VisualScripting;
using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("测试类型")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.Base;

    public override void Launch()
    {
        base.Launch();
        // CreatureBean itemData = new CreatureBean(999998);
        // itemData.AddAllSkin();
        // StartForBaseTest(itemData);
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
    /// 开始卡片测试
    /// </summary>
    /// <param name="fightCreature"></param>
    public void StartForCardTest(FightCreatureBean fightCreature)
    {
        WorldHandler.Instance.ClearWorldData(() =>
        {
            VolumeHandler.Instance.SetDepthOfField(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off, 0, 0, 0);
            //镜头初始化
            CameraHandler.Instance.InitData();
            //关闭额外的摄像头
            var ui = UIHandler.Instance.OpenUIAndCloseOther<UITestCard>();
            ui.SetData(fightCreature);
        });
    }

    /// <summary>
    /// 基地测试
    /// </summary>
    public void StartForBaseTest(CreatureBean creatureData)
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.selfCreature = creatureData;
        for (int i = 0; i < 50; i++)
        {
            CreatureBean creatureItem = new CreatureBean(2001);
            creatureItem.rarity = Random.Range(1, 7);
            creatureItem.starLevel = Random.Range(0, 11);
            creatureItem.level = Random.Range(0, 101);
            creatureItem.AddSkinForBase();
            userData.AddBackpackCreature(creatureItem);
        }
        WorldHandler.Instance.EnterGameForBaseScene(userData, true);
    }
}
