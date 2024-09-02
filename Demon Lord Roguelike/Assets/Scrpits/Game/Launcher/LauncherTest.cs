using UnityEngine;

public class LauncherTest : BaseLauncher
{
    [Header("测试类型")]
    public TestSceneTypeEnum testSceneType = TestSceneTypeEnum.FightSceneTest;

    public override void Launch()
    {
        base.Launch();
        FightCreatureBean itemData = new FightCreatureBean(999998);
        itemData.creatureData.AddAllSkin();
        StartForBaseTest(itemData);
    }

    /// <summary>
    /// 开始战斗场景测试
    /// </summary>
    /// <param name="fightData"></param>
    public void StartForFightSceneTest(FightBean fightData)
    {
        WorldHandler.Instance.ClearWorldData(() =>
        {
            //打开加载UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData();
            //测试数据
            GameHandler.Instance.StartGameFight(fightData);
        });
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
    public void StartForBaseTest(FightCreatureBean fightCreature)
    {
        WorldHandler.Instance.ClearWorldData(() =>
        {
            //打开加载UI
            UIHandler.Instance.OpenUIAndCloseOther<UILoading>();
            //镜头初始化
            CameraHandler.Instance.InitData();
            //环境参数初始化
            VolumeHandler.Instance.InitData();
            //设置基地场景视角
            CameraHandler.Instance.SetBaseSceneCamera(() =>
            {
                //加载基地场景
                WorldHandler.Instance.LoadBaseScene((targetObj) =>
                {
                    //开启控制
                    GameControlHandler.Instance.SetBaseControl();
                    //关闭LoadingUI
                    var uiBaseMain = UIHandler.Instance.OpenUIAndCloseOther<UIBaseMain>();
                });
            }, fightCreature.creatureData);
        });
    }
}
