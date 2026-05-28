using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForConquer : FightBean
{
    //征服模式数据
    public FightTypeConquerInfoBean fightTypeConquerInfo;
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
    /// 是否为最后一关（BOSS关）
    /// </summary>
    public bool IsBossFight()
    {
        return figthNumMax > 0 && fightNum >= figthNumMax;
    }

    /// <summary>
    /// 下一关是否为最后一关（BOSS关）
    /// </summary>
    public bool IsNextBossFight()
    {
        return figthNumMax > 0 && fightNum + 1 >= figthNumMax;
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
        FightCreatureBean fightCreatureDefenseCore = CreatureHandler.Instance.GetFightCreatureData(userData.selfCreature, CreatureFightTypeEnum.FightDefenseCore);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(1);
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i];
            if (itemLineupCreature.creatureState == CreatureStateEnum.Idle)
            {
                dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
            }
        }

        //设置战斗场景ID(根据是否为BOSS关选择不同场景)
        fightSceneId = fightTypeConquerInfo.GetRandomFightScene(IsBossFight());
        //初始化战斗数据
        InitFightAttackData();
    }

    /// <summary>
    /// 初始化战斗数据
    /// 根据当前关卡数(fightNum)计算敌人数量，并将所有敌人在 attack_show_time 内随机但相对均匀地排布
    /// </summary>
    public void InitFightAttackData()
    {
        //设置进攻生物数据
        fightAttackData = new FightAttackBean();

        //计算当前关卡的敌人数量
        int waveNum = CalcCurrentEnemyNum();
        if (waveNum <= 0) waveNum = 1;

        //总进攻时间(秒)，至少 1 秒，避免被 0 除
        float showTime = fightTypeConquerInfo.attack_show_time;
        if (showTime <= 0f) showTime = 1f;

        //BOSS关使用BOSS敌人池，否则使用普通敌人池
        bool isBoss = IsBossFight();

        //将 [0, showTime] 区间均分为 waveNum 段，在每段内随机一个出现时刻
        //保证整体随机但不至于过度聚集
        float bucket = showTime / waveNum;
        float prevTime = 0f;
        for (int i = 0; i < waveNum; i++)
        {
            float spawnTime = (i + UnityEngine.Random.value) * bucket;
            //确保单调递增
            if (spawnTime < prevTime) spawnTime = prevTime;
            float delay = spawnTime - prevTime;
            prevTime = spawnTime;

            long enemyId = fightTypeConquerInfo.GetRandomEmenyId(isBoss);
            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(delay, enemyId);
            fightAttackData.AddAttackQueue(fightAttackDetails);
        }
    }

    /// <summary>
    /// 根据当前关卡数(fightNum)计算应生成的敌人数量
    /// 递推公式：next = current * attack_num_addrate + attack_num_add
    /// 第一关(fightNum=1) 数量为 attack_start_num
    /// </summary>
    private int CalcCurrentEnemyNum()
    {
        int num = fightTypeConquerInfo.attack_start_num;
        for (int i = 1; i < fightNum; i++)
        {
            num = UnityEngine.Mathf.RoundToInt(num * fightTypeConquerInfo.attack_num_addrate) + fightTypeConquerInfo.attack_num_add;
        }
        return num;
    }
    
    /// <summary>
    /// 初始化下一关数据(用于场景重载，例如进入BOSS场景)
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
        //设置战斗场景ID(根据是否为BOSS关选择不同场景)
        fightSceneId = fightTypeConquerInfo.GetRandomFightScene(IsBossFight());
        //初始化战斗数据
        InitFightAttackData();
    }

    /// <summary>
    /// 初始化下一关数据(用于同场景内继续战斗，不重新加载战斗场景)
    /// </summary>
    public void InitNextDataForContinue()
    {
        //不需要保留防御生物数据，它们仍在场上
        listLastDefenseFightCreatureData = null;
        fightNum++;
        //不修改 fightSceneId（场景不重载）
        //重置进攻相关计时器，确保新一关的进攻节奏从头开始
        timeUpdateForAttackCreate = 0;
        timeUpdateTargetForAttackCreate = 0;
        //初始化战斗数据
        InitFightAttackData();
    }
}