using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForDoomCouncil : FightBean
{
    public DoomCouncilBean doomCouncilData;

    public FightBeanForDoomCouncil(DoomCouncilBean doomCouncilData) : base()
    {
        this.doomCouncilData = doomCouncilData;
        gameFightType = GameFightTypeEnum.DoomCouncil;
        InitData();
    }

    /// <summary>
    /// 初始化征服模式
    /// </summary>
    public override void InitData()
    {
        base.InitData();

        var userData = GameDataHandler.Instance.manager.GetUserData();
        //设置道路数量
        sceneRoadNum = 1;
        //设置道路长度
        sceneRoadLength = 1;
        //设置最大关卡数量
        figthNumMax = 1;
        //设置当前关卡数量
        fightNum = 1;
        //初始化防御核心
        FightCreatureBean fightCreatureDefenseCore = CreatureHandler.Instance.GetFightCreatureData(userData.selfCreature, CreatureFightTypeEnum.FightDefenseCore);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(1);
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i];
            //收全部阵容生物：进阶中的魔物开始进阶时已移出阵容，此处无需再按状态过滤(旧的 == Idle 会误伤上一场残留 Fight/Rest 状态的阵容生物)
            dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
        }
        //设置进攻生物数据 一波进攻
        fightAttackData = new FightAttackBean();
        var councilorAllNpcId = doomCouncilData.GetCouncilorAllNpcId();
        var councilorPositionX = doomCouncilData.GetCouncilorAllPositionX();

        FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(0, councilorAllNpcId, councilorPositionX);
        fightAttackData.AddAttackQueue(fightAttackDetails);
    }
}