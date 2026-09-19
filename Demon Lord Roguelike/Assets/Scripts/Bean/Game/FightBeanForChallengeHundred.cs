using System;
using System.Collections.Generic;

/// <summary>
/// 挑战100勇士战斗数据（单关100只怪：怪物构成/强度/奖励稀有度/刷怪时长来自冻结的配置行，道路/奖励在传送门生成时已冻结）
/// </summary>
[Serializable]
public class FightBeanForChallengeHundred : FightBean
{
    //挑战100勇士配置行（传送门生成时冻结抽中）
    public FightTypeChallengeHundredInfoBean fightTypeChallengeHundredInfo;
    //游戏随机数据
    public GameWorldInfoRandomBean gameWorldInfoRandomData;

    //进攻怪物总量（固定100）
    public const int AttackCreatureNum = 100;

    public FightBeanForChallengeHundred(GameWorldInfoRandomBean gameWorldInfoRandomData) : base()
    {
        this.gameWorldInfoRandomData = gameWorldInfoRandomData;
        gameFightType = gameWorldInfoRandomData.gameFightType;
        InitData();
    }

    /// <summary>
    /// 初始化挑战100勇士模式
    /// </summary>
    public override void InitData()
    {
        base.InitData();
        //获取冻结的挑战100勇士配置行
        fightTypeChallengeHundredInfo = FightTypeChallengeHundredInfoCfg.GetItemData(gameWorldInfoRandomData.challengeHundredRowId);
        if (fightTypeChallengeHundredInfo == null)
        {
            LogUtil.LogError($"初始化挑战100勇士模式失败 worldId:{gameWorldInfoRandomData.worldId} challengeHundredRowId:{gameWorldInfoRandomData.challengeHundredRowId}");
            return;
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //设置道路数量
        sceneRoadNum = gameWorldInfoRandomData.roadNum;
        //设置道路长度
        sceneRoadLength = gameWorldInfoRandomData.roadLength;
        //固定单关
        figthNumMax = 1;
        fightNum = 1;
        //初始化防御核心
        FightCreatureBean fightCreatureDefenseCore = CreatureHandler.Instance.GetFightCreatureData(userData.selfCreature, CreatureFightTypeEnum.FightDefenseCore);
        fightDefenseCoreData = fightCreatureDefenseCore;
        //设置防御生物(读取当前出战阵容, 在传送门详情弹窗中选择)
        dlDefenseCreatureData.Clear();
        var lineupCreature = userData.GetLineupCreature(userData.GetLineupFightIndex());
        for (int i = 0; i < lineupCreature.Count; i++)
        {
            var itemLineupCreature = lineupCreature[i];
            //收全部阵容生物：进阶中的魔物开始进阶时已移出阵容，此处无需再按状态过滤
            dlDefenseCreatureData.Add(itemLineupCreature.creatureUUId, itemLineupCreature);
        }

        //设置战斗场景ID(配置行场景池随机其一)
        fightSceneId = fightTypeChallengeHundredInfo.GetRandomFightScene();
        //初始化战斗数据
        InitFightAttackData();
    }

    /// <summary>
    /// 初始化战斗数据
    /// 100只怪在 attack_show_time 内分桶均匀随机排布（每桶随机一个时刻），每只在配置行 enemy_ids 中独立随机抽取（数量随机、总量100）；
    /// 强度倍率=配置行 attack_intensity_baserate（本模式强度自配，不叠加终焉议会强度议案）
    /// </summary>
    public void InitFightAttackData()
    {
        //设置进攻生物数据
        fightAttackData = new FightAttackBean();

        //总进攻时间(秒)，至少 1 秒，避免被 0 除
        float showTime = fightTypeChallengeHundredInfo.attack_show_time;
        if (showTime <= 0f) showTime = 1f;

        //强度倍率（敌人HP/护甲/攻击力×该值；按冻结难度从配置行逐难度对齐值取档）
        float intensityRate = fightTypeChallengeHundredInfo.GetIntensityRate(gameWorldInfoRandomData.difficultyLevel);

        //将 [0, showTime] 区间均分为 100 段，在每段内随机一个出现时刻，整体随机但不至于过度聚集
        float bucket = showTime / AttackCreatureNum;
        List<SpawnEvent> spawnEvents = new List<SpawnEvent>(AttackCreatureNum);
        for (int i = 0; i < AttackCreatureNum; i++)
        {
            float spawnTime = (i + UnityEngine.Random.value) * bucket;
            //每只怪独立从敌人列表随机抽取（配置多个敌人时数量随机、总量保持100）
            long enemyId = fightTypeChallengeHundredInfo.GetRandomEnemyId();
            spawnEvents.Add(new SpawnEvent(spawnTime, enemyId));
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
            //携带强度倍率
            fightAttackDetails.intensityRate = intensityRate;
            fightAttackData.AddAttackQueue(fightAttackDetails);
        }
    }

    /// <summary>
    /// 出怪事件(内部排程用)：记录某个敌人的绝对出现时间
    /// </summary>
    private class SpawnEvent
    {
        //绝对出现时间(从本关开始计)
        public float time;
        //出现的敌人npcId
        public long npcId;

        public SpawnEvent(float time, long npcId)
        {
            this.time = time;
            this.npcId = npcId;
        }
    }
}
