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
    //深渊馈赠「奖励多多」累计加成的领奖奖励物品数量(每选取一次+1，BOSS通关领奖时生效)
    public int rewardAddItemNum = 0;
    //深渊馈赠「再来一瓶」累计加成的领奖可选择次数(每选取一次+1，BOSS通关领奖时生效)
    public int rewardAddSelectNum = 0;
    //深渊馈赠刷新已用次数(剩余 = 刷新研究上限 - 已用; 每刷新一次+1; 整个征服run共享,新run随本bean重建自动归0回满)
    public int abyssalBlessingRefreshUsedNum = 0;
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
    /// 获取深渊馈赠刷新剩余次数
    /// 剩余 = 刷新研究上限(UserUnlockBean.GetUnlockAbyssalBlessingRefreshMax) - 已用次数(abyssalBlessingRefreshUsedNum), 下限 0
    /// 未解锁(上限0)恒为0; 整个征服run共享该池
    /// </summary>
    /// <returns>本次征服run内当前还能刷新的次数</returns>
    public int GetAbyssalBlessingRefreshRemainNum()
    {
        int max = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData().GetUnlockAbyssalBlessingRefreshMax();
        int remain = max - abyssalBlessingRefreshUsedNum;
        return remain < 0 ? 0 : remain;
    }

    /// <summary>
    /// 消耗一次深渊馈赠刷新次数(已用次数+1)
    /// </summary>
    public void ReduceAbyssalBlessingRefreshNum()
    {
        abyssalBlessingRefreshUsedNum++;
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
    /// 普通波次始终使用 enemy_ids 敌人池(BOSS关与非BOSS关逻辑一致)，在 attack_show_time 内随机但相对均匀地排布；
    /// BOSS关额外从 enemy_boss_ids 生成BOSS敌人，出现在进攻总时间的中后段并触发BOSS特写UI
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

        bool isBoss = IsBossFight();

        //先收集所有出怪事件(绝对出现时间 + npcId)，最后统一按时间排序再转换为带相对延迟的进攻队列
        List<SpawnEvent> spawnEvents = new List<SpawnEvent>();

        //本关敌人(普通敌人与BOSS均适用)的累计强度倍率(HP/护甲/攻击力)，第1关为1，之后按 attack_intensity_addrate 逐关相乘
        float intensityRate = fightTypeConquerInfo.GetCurrentIntensityRate(fightNum);
        //叠加终焉议会「挑战更强/更弱的敌人」议案的敌人强度倍率(下一整场征服run所有关卡+BOSS均生效，run结束消耗)
        var userTempData = GameDataHandler.Instance.manager.GetUserData().GetUserTempData();
        intensityRate *= userTempData.GetEnemyIntensityRate();

        //普通波次：将 [0, showTime] 区间均分为 waveNum 段，在每段内随机一个出现时刻，整体随机但不至于过度聚集
        //BOSS关也照常出 enemy_ids 的普通敌人，逻辑与非BOSS关一致
        float bucket = showTime / waveNum;
        for (int i = 0; i < waveNum; i++)
        {
            float spawnTime = (i + UnityEngine.Random.value) * bucket;
            long enemyId = fightTypeConquerInfo.GetRandomEmenyId(false);
            SpawnEvent normalEvent = new SpawnEvent(spawnTime, enemyId);
            //普通敌人按关卡强度倍率提升
            normalEvent.intensityRate = intensityRate;
            spawnEvents.Add(normalEvent);
        }

        //BOSS关：额外生成BOSS敌人(来自 enemy_boss_ids)，BOSS 同样享受本关强度倍率
        if (isBoss)
        {
            AddBossSpawnEvents(spawnEvents, showTime, intensityRate);
        }

        //按出现时间升序排序，保证队列按时间顺序出怪
        spawnEvents.Sort((a, b) => a.time.CompareTo(b.time));

        //转换为带相对延迟的进攻队列
        float prevTime = 0f;
        for (int i = 0; i < spawnEvents.Count; i++)
        {
            SpawnEvent evt = spawnEvents[i];
            float delay = evt.time - prevTime;
            if (delay < 0) delay = 0;
            prevTime = evt.time;

            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(delay, evt.npcId);
            //携带BOSS特写展示数据(仅BOSS首波非空)
            fightAttackDetails.bossShowNpcIds = evt.bossShowNpcIds;
            //携带强度倍率(普通敌人与BOSS均按关卡递增)
            fightAttackDetails.intensityRate = evt.intensityRate;
            fightAttackData.AddAttackQueue(fightAttackDetails);
        }
    }

    /// <summary>
    /// 生成BOSS出怪事件
    /// BOSS数量由 attack_boss_num 决定(支持单值"x"或区间"x-y")，出现在进攻总时间的中后段[50%,90%]随机时刻，
    /// 多个BOSS在该时刻略微错开依次入场，并由首个BOSS携带全部BOSS的npcId用于一次性BOSS特写展示；
    /// BOSS 与普通敌人一样按本关强度倍率(intensityRate)提升 HP/护甲/攻击力
    /// </summary>
    /// <param name="spawnEvents">出怪事件列表(会被追加BOSS事件)</param>
    /// <param name="showTime">本关进攻总时间</param>
    /// <param name="intensityRate">本关强度倍率(同普通敌人，作用到 HP/护甲/攻击力)</param>
    private void AddBossSpawnEvents(List<SpawnEvent> spawnEvents, float showTime, float intensityRate)
    {
        //BOSS数量
        int bossNum = fightTypeConquerInfo.GetRandomBossNum();
        if (bossNum <= 0)
            return;

        //BOSS出现在进攻总时间的中后段[50%,90%]随机一个时刻
        float bossAppearTime = UnityEngine.Random.Range(showTime * 0.5f, showTime * 0.9f);
        //多个BOSS在同一时刻略微错开依次入场
        float bossStagger = 0.3f;

        //收集本次出现的所有BOSS的npcId，用于一次性BOSS特写展示
        List<long> bossNpcIds = new List<long>();
        List<SpawnEvent> bossEvents = new List<SpawnEvent>();
        for (int i = 0; i < bossNum; i++)
        {
            long bossId = fightTypeConquerInfo.GetRandomEmenyId(true);
            bossNpcIds.Add(bossId);
            float bossTime = bossAppearTime + i * bossStagger;
            SpawnEvent bossEvent = new SpawnEvent(bossTime, bossId);
            //BOSS 与普通敌人一致，按本关强度倍率提升 HP/护甲/攻击力
            bossEvent.intensityRate = intensityRate;
            bossEvents.Add(bossEvent);
        }
        //首个BOSS出现时弹出BOSS特写UI(展示所有BOSS)
        bossEvents[0].bossShowNpcIds = bossNpcIds;
        spawnEvents.AddRange(bossEvents);
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

    /// <summary>
    /// 出怪事件(内部排程用)：记录某个敌人的绝对出现时间，BOSS首波额外携带BOSS特写展示数据
    /// </summary>
    private class SpawnEvent
    {
        //绝对出现时间(从本关开始计)
        public float time;
        //出现的敌人npcId
        public long npcId;
        //BOSS特写展示的npcId列表(仅BOSS首波非空)
        public List<long> bossShowNpcIds;
        //强度倍率(普通敌人与BOSS均按关卡递增; 默认1)
        public float intensityRate = 1f;

        public SpawnEvent(float time, long npcId)
        {
            this.time = time;
            this.npcId = npcId;
        }
    }
}