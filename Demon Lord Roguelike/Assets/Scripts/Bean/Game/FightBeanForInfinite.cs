using System;
using System.Collections.Generic;

[Serializable]
public class FightBeanForInfinite : FightBean
{
    //征服模式配置行(无尽复用同难度征服行的怪物构成/数量/时长/场景/魔晶掉落)
    public FightTypeConquerInfoBean fightTypeConquerInfo;
    //无尽模式配置行(每轮强度倍率; null时轮次乘区按1降级, 不阻断战斗)
    public FightTypeInfiniteInfoBean fightTypeInfiniteInfo;
    //游戏随机数据
    public GameWorldInfoRandomBean gameWorldInfoRandomData;
    //当前轮次(从1开始; 每追加一轮进攻队列后+1; 无关卡语义, 仅用于强度递增与轮次展示)
    public int roundNum = 1;

    public FightBeanForInfinite(GameWorldInfoRandomBean gameWorldInfoRandomData) : base()
    {
        this.gameWorldInfoRandomData = gameWorldInfoRandomData;
        gameFightType = gameWorldInfoRandomData.gameFightType;
        InitData();
    }

    #region 初始化
    /// <summary>
    /// 初始化无尽模式: 取同难度征服行+无尽行配置, 建防御核心/收出战阵容, 选战斗场景, 填充第1轮进攻队列
    /// </summary>
    public override void InitData()
    {
        base.InitData();
        //获取同难度征服模式配置行(无尽复用其怪物构成/数量/时长/场景/魔晶掉落)
        fightTypeConquerInfo = FightTypeConquerInfoCfg.GetItemData(gameWorldInfoRandomData.worldId, gameWorldInfoRandomData.difficultyLevel);
        if (fightTypeConquerInfo == null)
        {
            LogUtil.LogError($"初始化无尽游戏模式失败 worldId:{gameWorldInfoRandomData.worldId} difficultyLevel:{gameWorldInfoRandomData.difficultyLevel}");
            return;
        }
        //获取无尽模式配置行(每轮强度倍率; 无配置时轮次乘区按1降级, 不阻断战斗)
        fightTypeInfiniteInfo = FightTypeInfiniteInfoCfg.GetItemData(gameWorldInfoRandomData.worldId, gameWorldInfoRandomData.difficultyLevel);
        if (fightTypeInfiniteInfo == null)
        {
            LogUtil.LogError($"无尽模式缺少配置行 worldId:{gameWorldInfoRandomData.worldId} difficultyLevel:{gameWorldInfoRandomData.difficultyLevel}, 轮次强度倍率按1处理");
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //设置道路数量
        sceneRoadNum = gameWorldInfoRandomData.roadNum;
        //设置道路长度
        sceneRoadLength = gameWorldInfoRandomData.roadLength;
        //无尽无关卡概念, 占位1保证核心创建/UI读取安全
        figthNumMax = 1;
        fightNum = 1;
        roundNum = 1;
        //初始化防御核心
        FightCreatureBean fightCreatureDefenseCore = CreatureHandler.Instance.GetFightCreatureData(userData.selfCreature, CreatureFightTypeEnum.FightDefenseCore);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物(读取当前出战阵容, 在传送门详情弹窗中选择)
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(userData.GetLineupFightIndex());
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i];
            //收全部阵容生物：进阶中的魔物开始进阶时已移出阵容，此处无需再按状态过滤(与征服一致)
            dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
        }

        //设置战斗场景ID(每轮都有BOSS, 取BOSS场景池)
        fightSceneId = fightTypeConquerInfo.GetRandomFightScene(true);
        //初始化战斗数据并填充第1轮进攻队列
        fightAttackData = new FightAttackBean();
        AppendNextRoundAttackData();
    }
    #endregion

    #region 轮次波次
    /// <summary>
    /// 追加下一轮进攻波次到进攻队列末尾(无尽模式核心: 队列永不空, 由 InitData(第1轮) 与 GameFightLogicInfinite.TryRefillNextAttackQueue(后续轮) 调用)
    /// 每轮构成与征服BOSS关一致: attack_start_num只普通怪在attack_show_time内分桶均匀随机 + attack_boss_num只BOSS在[50%,90%]时刻错开0.3s入场;
    /// 全部携带 GetRoundIntensityRate(roundNum) 强度; 每轮BOSS首波都携带 bossShowNpcIds 弹特写; 入队完成后 roundNum+1
    /// </summary>
    public void AppendNextRoundAttackData()
    {
        //每轮敌人数量固定=配置的第一关数量(无关卡递推), 至少1只
        int waveNum = fightTypeConquerInfo.attack_start_num;
        if (waveNum <= 0) waveNum = 1;
        //每轮进攻时间固定=attack_show_time, 至少1秒避免被0除
        float showTime = fightTypeConquerInfo.attack_show_time;
        if (showTime <= 0f) showTime = 1f;

        //先收集本轮所有出怪事件(绝对出现时间+npcId), 最后统一按时间排序再转换为带相对延迟的进攻队列
        List<SpawnEvent> spawnEvents = new List<SpawnEvent>();
        //本轮敌人(普通敌人与BOSS均适用)的强度倍率(HP/护甲/攻击力)
        float intensityRate = GetRoundIntensityRate(roundNum);

        //普通波次: 将 [0, showTime] 区间均分为 waveNum 段, 每段内随机一个出现时刻, 整体随机但不至于过度聚集
        float bucket = showTime / waveNum;
        for (int i = 0; i < waveNum; i++)
        {
            float spawnTime = (i + UnityEngine.Random.value) * bucket;
            long enemyId = fightTypeConquerInfo.GetRandomEmenyId(false);
            SpawnEvent normalEvent = new SpawnEvent(spawnTime, enemyId);
            //普通敌人按本轮强度倍率提升
            normalEvent.intensityRate = intensityRate;
            spawnEvents.Add(normalEvent);
        }

        //BOSS波次: 每轮都出BOSS(来自 enemy_boss_ids), 且每轮首只BOSS都携带特写数据(无尽模式每轮弹BOSS特写)
        AddBossSpawnEvents(spawnEvents, showTime, intensityRate);

        //按出现时间升序排序，保证队列按时间顺序出怪
        spawnEvents.Sort((a, b) => a.time.CompareTo(b.time));

        //转换为带相对延迟的进攻队列, 追加到现有队列末尾(不重建FightAttackBean)
        float prevTime = 0f;
        for (int i = 0; i < spawnEvents.Count; i++)
        {
            SpawnEvent evt = spawnEvents[i];
            float delay = evt.time - prevTime;
            if (delay < 0) delay = 0;
            prevTime = evt.time;

            FightAttackDetailsBean fightAttackDetails = new FightAttackDetailsBean(delay, evt.npcId);
            //携带BOSS特写展示数据(每轮BOSS首波非空)
            fightAttackDetails.bossShowNpcIds = evt.bossShowNpcIds;
            //携带强度倍率(普通敌人与BOSS均按轮次递增)
            fightAttackDetails.intensityRate = evt.intensityRate;
            fightAttackData.AddAttackQueue(fightAttackDetails);
        }
        roundNum++;
    }

    /// <summary>
    /// 获取指定轮次的敌人强度倍率(HP/护甲/攻击力乘区)
    /// 公式: 征服行 attack_intensity_baserate(每轮恒定基区) × 无尽行 round_intensity_addrate^(round-1)(逐轮乘区) × 终焉议会强度议案倍率
    /// </summary>
    /// <param name="round">轮次(从1开始)</param>
    /// <returns>该轮敌人的强度倍率</returns>
    public float GetRoundIntensityRate(int round)
    {
        //基区: 同难度征服行基础强度倍率(0或不配按1处理, 与征服一致)
        float intensityRate = fightTypeConquerInfo.attack_intensity_baserate;
        if (intensityRate <= 0f) intensityRate = 1f;
        //逐轮乘区: 无尽行每轮强度倍率(无配置按1降级)
        if (fightTypeInfiniteInfo != null)
            intensityRate *= fightTypeInfiniteInfo.GetRoundIntensityRate(round);
        //叠加终焉议会「挑战更强/更弱的敌人」议案的敌人强度倍率(run结束消耗)
        var userTempData = GameDataHandler.Instance.manager.GetUserData().GetUserTempData();
        intensityRate *= userTempData.GetEnemyIntensityRate();
        return intensityRate;
    }

    /// <summary>
    /// 生成一轮的BOSS出怪事件(同征服 AddBossSpawnEvents 规则)
    /// BOSS数量由 attack_boss_num 决定(支持单值"x"或区间"x-y"), 出现在进攻总时间的中后段[50%,90%]随机时刻,
    /// 多个BOSS在该时刻略微错开依次入场, 并由首个BOSS携带全部BOSS的npcId用于BOSS特写展示(无尽模式每轮都弹)
    /// </summary>
    /// <param name="spawnEvents">出怪事件列表(会被追加BOSS事件)</param>
    /// <param name="showTime">本轮进攻总时间</param>
    /// <param name="intensityRate">本轮强度倍率(同普通敌人, 作用到 HP/护甲/攻击力)</param>
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

        //收集本轮出现的所有BOSS的npcId, 用于BOSS特写展示
        List<long> bossNpcIds = new List<long>();
        List<SpawnEvent> bossEvents = new List<SpawnEvent>();
        for (int i = 0; i < bossNum; i++)
        {
            long bossId = fightTypeConquerInfo.GetRandomEmenyId(true);
            bossNpcIds.Add(bossId);
            float bossTime = bossAppearTime + i * bossStagger;
            SpawnEvent bossEvent = new SpawnEvent(bossTime, bossId);
            //BOSS 与普通敌人一致, 按本轮强度倍率提升 HP/护甲/攻击力
            bossEvent.intensityRate = intensityRate;
            bossEvents.Add(bossEvent);
        }
        //首个BOSS出现时弹出BOSS特写UI(展示所有BOSS)
        bossEvents[0].bossShowNpcIds = bossNpcIds;
        spawnEvents.AddRange(bossEvents);
    }

    /// <summary>
    /// 出怪事件(内部排程用): 与征服同构, 记录某个敌人的绝对出现时间, BOSS首波额外携带BOSS特写展示数据
    /// </summary>
    private class SpawnEvent
    {
        //绝对出现时间(从本轮开始计)
        public float time;
        //出现的敌人npcId
        public long npcId;
        //BOSS特写展示的npcId列表(仅BOSS首波非空)
        public List<long> bossShowNpcIds;
        //强度倍率(普通敌人与BOSS均按轮次递增; 默认1)
        public float intensityRate = 1f;

        public SpawnEvent(float time, long npcId)
        {
            this.time = time;
            this.npcId = npcId;
        }
    }
    #endregion
}
