using System;

[Serializable]
public class FightBeanForDoomCouncil : FightBean
{
    public DoomCouncilBean doomCouncilData;

    public FightBeanForDoomCouncil(DoomCouncilBean doomCouncilData) : base()
    {
        this.doomCouncilData = doomCouncilData;
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
        FightCreatureBean fightCreatureDefenseCore = new FightCreatureBean(userData.selfCreature);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(1);
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i]; ;
            dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
        }
        //设置进攻生物数据 一波进攻
        fightAttackData = new FightAttackBean();
        var councilorAllNpcId = doomCouncilData.GetCouncilorAllNpcId();
        FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(0, councilorAllNpcId);
        fightAttackData.AddAttackQueue(fightAttackDetails);
    }
}