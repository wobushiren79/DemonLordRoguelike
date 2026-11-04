using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-多次瞬时攻击（深渊馈赠「闪电」落雷）
/// <para>每 trigger_time 秒触发一轮：触发瞬间一次性检测全场存活敌人（此后不再检测，间隔期新刷敌人不补入也不受波及），
/// 不放回随机抽取 class_entity_data[0] 个主目标（一轮内多道雷的主目标互不重复；场上敌人少于雷数时只发同等数量的雷）；</para>
/// <para>限制只落在主目标层面：溅射不受限——同一目标可被多道雷重复溅射、被溅射过的目标仍可作后续雷的主目标、
/// 当过主目标的目标也可被后续雷溅射（同一道雷内同一生物只命中一次由攻击模块局部去重保证）。</para>
/// <para>每道雷在可配置释放窗口内独立随机一个释放时刻，按时间排序后由 UpdateBuffTime 发射，同帧到期则全部发射；
/// 待发目标只保存 UUID，到点重新解析，目标死亡或实体池复用导致 UUID 失效则跳过该道。</para>
/// <para>每次瞬时攻击 = 发射一个 class_entity_data[1] 指定的攻击模块（AttackModeInstantAreaThunder 落雷），
/// 粒子/AOE/距离衰减由攻击模式框架处理；本类注入「BUFF目标(魔王)实时攻击力 × trigger_value」的中心伤害快照（至少1）
/// 与触发瞬间的敌人快照名单，CRT=0 不暴击。</para>
/// <para>class_entity_data 格式："攻击次数,攻击模块ID[,释放窗口秒数]"（如 "3,300033,0.5"；第三项省略时默认0.5秒）。</para>
/// </summary>
public class BuffEntityPeriodicAttackMultiInstant : BuffEntityPeriodic
{
    //默认随机释放窗口（秒）
    protected const float DefaultMultiAttackWindow = 0.5f;
    //随机释放窗口（秒），从 class_entity_data 第3项读取
    protected float multiAttackWindow = DefaultMultiAttackWindow;
    //待发射目标与随机释放时刻列表（按释放时刻升序）
    protected readonly List<PendingAttack> listPendingAttack = new List<PendingAttack>();
    //触发瞬间快照的敌人名单(AOE只作用于名单内，间隔期新刷敌人不受波及)
    protected HashSet<string> setSnapshotCreatureId = new HashSet<string>();
    //本轮随机释放窗口的已累计时间
    protected float timeElapsedCurrent = 0;
    //本轮发射的攻击模块ID(触发时从 class_entity_data 解析缓存)
    protected long attackModeId = 0;

    /// <summary>
    /// 待发射攻击数据（只存主目标 UUID，避免跨帧持有实体引用后被对象池复用误导）。
    /// </summary>
    protected struct PendingAttack
    {
        /// <summary>主目标生物 UUID</summary>
        public string targetUUId;
        /// <summary>相对本轮触发时刻的随机释放时间（秒）</summary>
        public float releaseTime;

        /// <summary>
        /// 创建一条待发射攻击数据。
        /// </summary>
        public PendingAttack(string targetUUId, float releaseTime)
        {
            this.targetUUId = targetUUId;
            this.releaseTime = releaseTime;
        }
    }

    #region 数据相关
    /// <summary>
    /// 清理数据（对象池复用前清空待发射列表/快照/计时与配置缓存，防残留）
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        listPendingAttack.Clear();
        setSnapshotCreatureId.Clear();
        timeElapsedCurrent = 0;
        multiAttackWindow = DefaultMultiAttackWindow;
        attackModeId = 0;
    }
    #endregion

    #region Update
    /// <summary>
    /// buff持续时间增加（base 维持周期触发；此处按随机释放时刻发射所有已到期攻击）
    /// </summary>
    public override void UpdateBuffTime(float buffTime)
    {
        base.UpdateBuffTime(buffTime);
        if (listPendingAttack.Count == 0)
            return;
        timeElapsedCurrent += buffTime;
        //同帧跨过多个释放时刻时全部补齐，避免固定每帧一道拖出配置窗口
        while (listPendingAttack.Count > 0 && timeElapsedCurrent >= listPendingAttack[0].releaseTime)
        {
            PendingAttack pendingAttack = listPendingAttack[0];
            listPendingAttack.RemoveAt(0);
            LaunchStrike(pendingAttack.targetUUId);
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性触发，无次数限制（抽取主目标并为每道雷生成窗口内随机释放时刻）
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (isTriggerSuccess == false) return false;

        //触发瞬间一次性检测全场存活敌人(后续多次攻击不再检测)
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var listEnemy = gameFightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy.IsNull()) return false;
        List<FightCreatureEntity> listAliveEnemy = new List<FightCreatureEntity>(listEnemy.Count);
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var itemEnemy = listEnemy[i];
            if (itemEnemy != null && !itemEnemy.IsDead())
                listAliveEnemy.Add(itemEnemy);
        }
        //场上无敌人则本轮不触发
        if (listAliveEnemy.Count == 0) return false;

        //解析参数 "攻击次数,攻击模块ID[,释放窗口秒数]"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"多次瞬时攻击BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"攻击次数,攻击模块ID[,释放窗口秒数]\"：{buffInfo.class_entity_data}");
            return false;
        }
        int attackCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);
        multiAttackWindow = DefaultMultiAttackWindow;
        if (arrEntityData.Length >= 3)
        {
            if (!float.TryParse(arrEntityData[2], out multiAttackWindow) || multiAttackWindow < 0)
            {
                LogUtil.LogError($"多次瞬时攻击BUFF[{buffInfo.id}]的释放窗口无效：{arrEntityData[2]}");
                multiAttackWindow = DefaultMultiAttackWindow;
                return false;
            }
        }

        //快照本轮敌人名单(供攻击模块AOE过滤间隔期新刷敌人)
        setSnapshotCreatureId.Clear();
        for (int i = 0; i < listAliveEnemy.Count; i++)
        {
            setSnapshotCreatureId.Add(listAliveEnemy[i].fightCreatureData.creatureData.creatureUUId);
        }

        //不放回随机抽取主目标，并为每道雷独立生成窗口内随机释放时刻
        listPendingAttack.Clear();
        timeElapsedCurrent = 0;
        List<FightCreatureEntity> listCandidate = new List<FightCreatureEntity>(listAliveEnemy);
        int strikeNum = Mathf.Min(attackCount, listCandidate.Count);
        for (int i = 0; i < strikeNum; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, listCandidate.Count);
            var targetEnemy = listCandidate[randomIndex];
            listCandidate.RemoveAt(randomIndex);
            string targetUUId = targetEnemy.fightCreatureData.creatureData.creatureUUId;
            float releaseTime = UnityEngine.Random.Range(0f, multiAttackWindow);
            listPendingAttack.Add(new PendingAttack(targetUUId, releaseTime));
        }
        listPendingAttack.Sort((a, b) => a.releaseTime.CompareTo(b.releaseTime));
        return true;
    }
    #endregion

    #region 瞬时攻击发射
    /// <summary>
    /// 对指定 UUID 的主目标发射一次瞬时攻击（到点重新解析目标，死亡或 UUID 已失效则跳过）
    /// </summary>
    protected void LaunchStrike(string targetUUId)
    {
        if (string.IsNullOrEmpty(targetUUId))
            return;
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        FightCreatureEntity targetEnemy = gameFightLogic?.fightData?.GetCreatureById(targetUUId, CreatureFightTypeEnum.FightAttack);
        //目标在间隔期内死亡或实体池复用后 UUID 不再匹配则跳过本次
        if (targetEnemy == null || targetEnemy.fightCreatureData == null || targetEnemy.IsDead() || targetEnemy.creatureObj == null)
            return;
        if (attackModeId == 0)
        {
            LogUtil.LogError($"多次瞬时攻击BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }
        //中心伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每发落地时实时取，至少1防低攻击力取整为0）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = Mathf.Max(1, (int)(coreATK * buffEntityData.GetTriggerValue()));
        Vector3 strikePosition = targetEnemy.creatureObj.transform.position;

        //纯数据发射路径（照分裂弹发射器先例）：注入攻击者快照与落点，攻击者快照不随弹道存活期变化
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        //不暴击（保持闪电不暴击的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = strikePosition;
        attackModeData.targetPos = strikePosition;
        attackModeData.attackedId = targetUUId;
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        //血液等受击特效朝向（核心→落点）
        attackModeData.attackDirection = (strikePosition - coreCreature.creatureObj.transform.position).normalized;
        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            //把触发瞬间的敌人快照名单注入攻击模块（当帧同步发射即用完Destroy，引用共享安全）
            if (attackMode is AttackModeInstantArea instantArea)
            {
                instantArea.filterCreatureIds = setSnapshotCreatureId;
            }
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
        });
    }
    #endregion
}
