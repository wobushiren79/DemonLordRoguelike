using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameFightLogicTest : GameFightLogic
{
    /// <summary>
    /// 准备游戏：额外注册新建防守魔物回调（测试固定属性兜底）
    /// </summary>
    public override void PreGame()
    {
        base.PreGame();
        //克隆BUFF等战中深拷贝产出的生物数据会丢失固定属性字段(NonSerialized 不随 JsonUtility 深拷贝)，创建时兜底补设
        RegisterEvent<FightCreatureEntity>(EventsInfo.GameFightLogic_DefenseCreatureCreate, EventForDefenseCreatureCreateForFixedAttribute);
    }

    /// <summary>
    /// 事件-新建防守魔物实体：为战中生成(克隆BUFF深拷贝等)的生物兜底设置测试固定属性
    /// <para>卡片生物已在 GetTestData 预设 dicFixedAttribute(卡片显示/召唤消耗/复活CD 读 CreatureBean.GetAttribute 即时生效)，此处仅补设缺失者。</para>
    /// </summary>
    /// <param name="fightCreatureEntity">新建防守魔物实体</param>
    public void EventForDefenseCreatureCreateForFixedAttribute(FightCreatureEntity fightCreatureEntity)
    {
        FightBeanForTest fightBeanForTest = fightData as FightBeanForTest;
        if (fightBeanForTest == null || fightBeanForTest.dicTestDefenseFixedAttribute.IsNull())
            return;
        FightCreatureBean fightCreatureData = fightCreatureEntity?.fightCreatureData;
        CreatureBean creatureData = fightCreatureData?.creatureData;
        //已带固定属性(正常卡片生物)则跳过
        if (creatureData == null || !creatureData.dicFixedAttribute.IsNull())
            return;
        creatureData.dicFixedAttribute = new Dictionary<CreatureAttributeTypeEnum, float>(fightBeanForTest.dicTestDefenseFixedAttribute);
        //重算属性并把当前生命/护甲重置为固定后的满值(本就在生成当帧, 语义等同出生满状态)
        fightCreatureData.RefreshBaseAttribute();
        fightCreatureData.HPCurrent = (int)fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
        fightCreatureData.DRCurrent = (int)fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.DR);
    }

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
    /// 准备游戏-防守核心创建之后：清理上一场遗留馈赠并添加测试深渊馈赠、应用测试防守方固定属性(魔王核心)与测试魔王蓝量
    /// <para>测试馈赠必须在防守核心创建后才能添加(BuffHandler.AddAbyssalBlessing 以核心为BUFF目标)；
    /// 测试模式馈赠随每场战斗重建，先 ClearAbyssalBlessing 避免可重复馈赠跨场叠加。</para>
    /// </summary>
    public override Task PreGameForAfterCreateDefenseCore()
    {
        //清理深渊馈赠数据(测试馈赠不跨场保留)
        BuffHandler.Instance.manager.ClearAbyssalBlessing();
        FightBeanForTest fightBeanForTest = fightData as FightBeanForTest;
        if (fightBeanForTest == null)
            return Task.CompletedTask;
        //添加测试深渊馈赠
        if (!fightBeanForTest.testAbyssalBlessingIds.IsNull())
        {
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
        //应用测试防守方固定属性(魔王核心): 固定值作为基础值替换, 在馈赠添加后设置并重算——AddAbyssalBlessing 触发的属性刷新已按未固定基础值算过一轮
        FightCreatureBean defCoreData = fightData.fightDefenseCoreData;
        if (!fightBeanForTest.dicTestDefenseFixedAttribute.IsNull())
        {
            defCoreData.creatureData.dicFixedAttribute = new Dictionary<CreatureAttributeTypeEnum, float>(fightBeanForTest.dicTestDefenseFixedAttribute);
            //重算属性并把当前生命/护甲重置为固定后的满值(核心在 GetTestData 构建时按未固定值初始化)
            defCoreData.RefreshBaseAttribute();
            defCoreData.HPCurrent = (int)defCoreData.GetAttribute(CreatureAttributeTypeEnum.HP);
            defCoreData.DRCurrent = (int)defCoreData.GetAttribute(CreatureAttributeTypeEnum.DR);
        }
        //应用测试魔王蓝量(MP 被固定属性设置时蓝量设置让位: 固定值即上限, 当前蓝量取固定值; 在馈赠添加后设置,避免馈赠触发的属性刷新把上限提升冲掉)
        if (!fightBeanForTest.dicTestDefenseFixedAttribute.IsNull() && fightBeanForTest.dicTestDefenseFixedAttribute.ContainsKey(CreatureAttributeTypeEnum.MP))
        {
            defCoreData.MPCurrent = defCoreData.GetAttribute(CreatureAttributeTypeEnum.MP);
        }
        else
        {
            defCoreData.MPCurrent = fightBeanForTest.testDemonLordMP;
            //消耗蓝量时 ChangeMP 会按 MP 上限夹取,上限不足时同步提升避免一次消耗就被夹回配置上限
            float mpMax = defCoreData.GetAttribute(CreatureAttributeTypeEnum.MP);
            if (mpMax < fightBeanForTest.testDemonLordMP)
            {
                defCoreData.dicAttribute[CreatureAttributeTypeEnum.MP] = fightBeanForTest.testDemonLordMP;
            }
        }
        return Task.CompletedTask;
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
