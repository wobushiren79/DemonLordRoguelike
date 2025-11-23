using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForConquer : FightBean
{
    //征服模式数据
    protected FightTypeConquerInfoBean fightTypeConquerInfo;
    //上一场还在场上的生物
    public List<FightCreatureBean> listLastDefenseFightCreatureData;
    //游戏随机数据
    public GameWorldInfoRandomBean gameWorldInfoRandomData;
    public FightBeanForConquer(GameWorldInfoRandomBean gameWorldInfoRandomData) : base()
    {
        this.gameWorldInfoRandomData = gameWorldInfoRandomData;
        gameFightType = gameWorldInfoRandomData.gameFightType;
        InitData();
    }

    /// <summary>
    /// 增加深渊馈赠
    /// </summary>
    /// <param name="abyssalBlessingInfo"></param>
    public void AddAbyssalBlessing(AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        AbyssalBlessingEntityBean abyssalBlessingEntityData = new AbyssalBlessingEntityBean(abyssalBlessingInfo);
        //添加BUFF
        BuffHandler.Instance.AddAbyssalBlessing(abyssalBlessingEntityData);
    }

    /// <summary>
    /// 初始化征服模式
    /// </summary>
    public override void InitData()
    {
        base.InitData();
        //获取征服模式游戏数据
        fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(gameWorldInfoRandomData.worldId, gameWorldInfoRandomData.difficultyLevel);
        if (fightTypeConquerInfo == null)
        {
            LogUtil.LogError($"初始化征服游戏模式失败 worldId:{gameWorldInfoRandomData.worldId} difficultyLevel:{gameWorldInfoRandomData.difficultyLevel}");
            return;
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //设置道路数量
        sceneRoadNum = gameWorldInfoRandomData.roadNum;
        //设置道路长度
        sceneRoadLength = gameWorldInfoRandomData.roadLength;
        //设置最大关卡数量
        figthNumMax = gameWorldInfoRandomData.fightNum;
        //设置当前关卡数量
        fightNum = 1;
        //初始化防御核心
        FightCreatureBean fightCreatureDefenseCore = new FightCreatureBean(userData.selfCreature, CreatureFightTypeEnum.FightDefenseCore);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(1);
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i]; ;
            dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
        }
        
        //设置战斗场景ID
        fightSceneId = fightTypeConquerInfo.GetRandomFightScene(false);
        //初始化战斗数据
        InitFightAttackData();
    }

    /// <summary>
    /// 初始化战斗数据
    /// </summary>
    public void InitFightAttackData()
    {
        //设置进攻生物数据
        fightAttackData = new FightAttackBean();
        int waveNum = UnityEngine.Random.Range(fightTypeConquerInfo.attack_wave_min, fightTypeConquerInfo.attack_wave_max + 1);
        float waveDelay = 5;
        for (int i = 0; i < waveNum; i++)
        {
            long enemyId = fightTypeConquerInfo.GetRandomEmenyId(false);
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(waveDelay - i * (waveDelay / waveNum), enemyId);
            fightAttackData.AddAttackQueue(fightAttackDetails);
        }
    }
    
    /// <summary>
    /// 初始化下一关数据
    /// </summary>
    public void InitNextData()
    {
        //保留还在场上的生物数据
        listLastDefenseFightCreatureData = new List<FightCreatureBean>();
        for (int i = 0; i < dlDefenseCreatureEntity.List.Count; i++)
        { 
            var creatureEntity = dlDefenseCreatureEntity.List[i];
            listLastDefenseFightCreatureData.Add(creatureEntity.fightCreatureData);
        }
        fightNum++;
        //设置战斗场景ID
        fightSceneId = fightTypeConquerInfo.GetRandomFightScene(false);
        //初始化战斗数据
        InitFightAttackData();
    }
}