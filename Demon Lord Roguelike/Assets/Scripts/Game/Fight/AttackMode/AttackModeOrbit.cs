using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 环绕弹道（深渊馈赠「知识的力量」）
/// <para>常驻不自毁：围绕宿主生物（最前排己方魔物）在 XZ 水平面做圆周运动，半径 OrbitRadius，
/// 角速度 rotateSpeed（弧度/秒，由 BUFF 注入），帧推进走 GetFightDeltaTime() 兼容 2 倍速；
/// 环绕高度不用宿主高度，取注入的 orbitHeight（魔王位置+攻击起始偏移高度，与常规攻击发射高度对齐）。</para>
/// <para>触碰命中：attack_search_type=AreaSphere 走 live 球形检测（非射线，书本数量少开销可忽略），
/// 每只书对同一敌人有 HitCooldown 秒冷却；伤害=魔王实时攻击力×damageRate（命中瞬间取），不暴击。</para>
/// <para>宿主由 BuffEntityPeriodicAttackOrbit 选取并在变更时更新引用；宿主无效时原地待命（不移动不命中），
/// 是否销毁由 BUFF 决定（场上无己方魔物时 BUFF 负责销毁全部书本）。</para>
/// <para>纯数据发射路径（无 prefab，走 DSP visual_name 批量渲染），环绕在场景范围内，不做越界销毁。</para>
/// </summary>
public class AttackModeOrbit : BaseAttackMode
{
    #region 注入参数（由 BUFF 在 StartAttack 前写入、宿主变更时更新；Destroy 时清空防对象池残留）
    /// <summary>环绕宿主（最前排己方魔物，由 BUFF 负责选取与变更）</summary>
    public FightCreatureEntity orbitCenterEntity;
    /// <summary>当前环绕角（弧度；生成时按序号均分错开）</summary>
    public float orbitAngle;
    /// <summary>旋转角速度（弧度/秒）</summary>
    public float rotateSpeed;
    /// <summary>伤害倍率（相对魔王实时攻击力，命中瞬间结算）</summary>
    public float damageRate;
    /// <summary>环绕高度（世界 y：魔王位置+攻击起始偏移高度，由 BUFF 生成时注入）</summary>
    public float orbitHeight;
    #endregion

    #region 数值常量（策划调整入口）
    /// <summary>环绕半径（世界单位）</summary>
    public const float OrbitRadius = 0.75f;
    /// <summary>每只书对同一敌人的命中冷却（秒）</summary>
    public const float HitCooldown = 0.5f;
    #endregion

    #region 命中冷却状态
    /// <summary>各敌人下次可被本书命中的时刻（key=生物UUID，value=orbitTime+HitCooldown；战斗规模下条目有界，Destroy 清空）</summary>
    private readonly Dictionary<string, float> dicNextHitTime = new Dictionary<string, float>();
    /// <summary>本书的战斗内累计时间（秒，随游戏速度缩放，供命中冷却判定）</summary>
    private float orbitTime;
    #endregion

    #region 初始化外形
    /// <summary>
    /// 初始化攻击外形：环绕书走纯数据发射（无武器视觉、无 prefab），只需还原视觉参数 → 登记 DSP 桶。
    /// </summary>
    public override void InitAttackModeShow()
    {
        ResetVisualParams();
        FightHandler.Instance.manager.EnsureAttackModeVisual(this);
    }
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：重置环绕计时与命中冷却名单（对象池复用不残留）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        orbitTime = 0f;
        dicNextHitTime.Clear();
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：宿主无效则待命 → 命中检测（移动前位置，带冷却）→ 环绕移动
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        // 宿主无效（未选/死亡/实体回收）则原地待命，书本生死由 BUFF 统一管理
        if (orbitCenterEntity == null || orbitCenterEntity.creatureObj == null || orbitCenterEntity.IsDead())
            return;
        float deltaTime = GameFightLogic.GetFightDeltaTime();
        orbitTime += deltaTime;
        // 命中检测（移动前位置）：每只书对同一敌人有冷却，防止贴脸每帧掉血
        FightCreatureEntity hitTarget = CheckHitTargetForSingle();
        if (hitTarget != null && hitTarget.fightCreatureData?.creatureData != null)
        {
            string creatureId = hitTarget.fightCreatureData.creatureData.creatureUUId;
            if (!dicNextHitTime.TryGetValue(creatureId, out float nextHitTime) || orbitTime >= nextHitTime)
            {
                if (HandleForHitTarget(hitTarget))
                    dicNextHitTime[creatureId] = orbitTime + HitCooldown;
            }
        }
        // 环绕移动：XZ 水平面绕宿主圆周（高度取魔王攻击偏移高度），角度随时间推进并取模防长跑精度漂移
        orbitAngle += rotateSpeed * deltaTime;
        if (orbitAngle > Mathf.PI * 2f)
            orbitAngle -= Mathf.PI * 2f;
        Vector3 center = orbitCenterEntity.creatureObj.transform.position;
        center.y = orbitHeight;
        SetPosition(center + new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * OrbitRadius);
    }
    #endregion

    #region 命中处理
    /// <summary>
    /// 对触碰到的敌人结算伤害：实时取魔王攻击力×damageRate（不暴击）；伤害≤0 时不结算也不进冷却。
    /// </summary>
    /// <returns>是否成功结算（ false=伤害来源无效或伤害为0，不占用冷却）</returns>
    private bool HandleForHitTarget(FightCreatureEntity hitTarget)
    {
        // 实时取防守核心(魔王)实体：与 AddAbyssalBlessing 同款取法，跨关卡实体重建也能取到
        GameFightLogic gameFightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        var coreCreature = gameFightLogic.fightData.GetCreatureById("", CreatureFightTypeEnum.FightDefenseCore);
        if (coreCreature == null || coreCreature.fightCreatureData == null)
            return false;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = (int)(coreATK * damageRate);
        if (attackDamage <= 0)
            return false;
        attackModeData.attackerDamage = attackDamage;
        attackModeData.attackerCRT = 0;
        hitTarget.UnderAttack(this);
        PlayEffectForHit(hitTarget.creatureObj.transform.position);
        return true;
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清空注入参数与冷却名单，防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        orbitCenterEntity = null;
        orbitHeight = 0f;
        dicNextHitTime.Clear();
        base.Destroy(isPermanently);
    }
    #endregion
}
