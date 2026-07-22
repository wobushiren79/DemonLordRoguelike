using System.Threading.Tasks;
using UnityEngine;

public class GameFightLogicTest : GameFightLogic
{
    /// <summary>
    /// 改变游戏状态
    /// </summary>
    public override void ChangeGameState(GameStateEnum gameState)
    {
        base.ChangeGameState(gameState);
        switch (gameState)
        {
            case GameStateEnum.Pre:
                break;
            case GameStateEnum.Gaming:
                break;
            case GameStateEnum.End:
                break;
            case GameStateEnum.Settlement:
                HandleForChangeGameStateSettlement();
                break;
        }
    }

    /// <summary>
    /// 准备游戏-防守核心创建之后：清理上一场遗留馈赠并添加测试深渊馈赠
    /// <para>测试馈赠必须在防守核心创建后才能添加(BuffHandler.AddAbyssalBlessing 以核心为BUFF目标)；
    /// 测试模式馈赠随每场战斗重建，先 ClearAbyssalBlessing 避免可重复馈赠跨场叠加。</para>
    /// </summary>
    public override async Task PreGameForAfterCreateDefenseCore()
    {
        //清理深渊馈赠数据(测试馈赠不跨场保留)
        BuffHandler.Instance.manager.ClearAbyssalBlessing();
        //添加测试深渊馈赠
        FightBeanForTest fightBeanForTest = fightData as FightBeanForTest;
        if (fightBeanForTest == null || fightBeanForTest.testAbyssalBlessingIds.IsNull())
            return;
        for (int i = 0; i < fightBeanForTest.testAbyssalBlessingIds.Count; i++)
        {
            AbyssalBlessingInfoBean abyssalBlessingInfo = AbyssalBlessingInfoCfg.GetItemData(fightBeanForTest.testAbyssalBlessingIds[i]);
            if (abyssalBlessingInfo == null)
            {
                LogUtil.LogWarning($"测试深渊馈赠添加失败，找不到配置 id:{fightBeanForTest.testAbyssalBlessingIds[i]}");
                continue;
            }
            BuffHandler.Instance.AddAbyssalBlessing(new AbyssalBlessingEntityBean(abyssalBlessingInfo));
        }
    }

    /// <summary>
    /// 处理结算
    /// </summary>
    public void HandleForChangeGameStateSettlement()
    {
        //清理
        ClearGameForSimple();
        //打开结算UI
        var uiFightSettlement = UIHandler.Instance.OpenUIAndCloseOther<UIFightSettlement>();
        uiFightSettlement.SetData(fightData, ActionForUIFightSettlementNext);
    }

    /// <summary>
    /// 回调-点击下一步 重启战斗
    /// </summary>
    public void ActionForUIFightSettlementNext()
    {
        FightBeanForTest fightBeanForTest = fightData as FightBeanForTest;
        fightData.fightAttackData = ClassUtil.DeepCopy(fightBeanForTest.fightAttackDataRemark);
        //重开走 PreGameForAfterCreateDefenseCore 统一清理并重新添加测试深渊馈赠
        WorldHandler.Instance.EnterGameForFightScene(fightData);
    }
}
