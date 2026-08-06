using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回弹菱块弹道（深渊馈赠「回弹菱块」）
/// <para>直线飞行 + 四壁反弹：在道路矩形范围（x∈[0.5, 0.5+路长]，z∈[0.5, 路数+0.5]）内永久反弹，
/// 越墙即钳位回墙内并镜像反射方向，再叠加 ±5° 随机偏转，避免 90° 等特殊角陷入左右/上下死循环。</para>
/// <para>左墙特例：子弹从魔王（x=0）出生、尚未进入场地（x≥0.5）前不做左墙反弹（hasEnteredField 标记），防止出生即被弹回魔王侧。</para>
/// <para>命中规则：命中敌人不销毁、穿透继续飞；同一目标 1 秒冷却后可再次命中（按目标记录最近命中时刻）；
/// 伤害恒定为发射时注入值（魔王攻击力×BUFF trigger_value 倍率，当前配置100%），不逐目标递减；冷却名单超阈值时清理已过期条目防无限增长。</para>
/// <para>音效：发射音 sound_start（配置 sound_fight_3）由 GetAttackModePrefab 每颗发射时播放；回弹音 sound_hit_6 在四壁反弹成功时播放；
/// 命中敌人音走通用 sound_hit 配置（记入受击数据播放）。发射/回弹音均受 PlaySound 0.1s 同音去重，同帧多颗只播一声。</para>
/// <para>生命周期：永不自动销毁（CheckIsMoveBound 恒 false）；由发射方 BuffEntityPeriodicAttackRebound 追踪存活，
/// 关卡切换被 ClearAttackModePrefab 清掉后由 BUFF 补弹，馈赠升级/清空时由 BUFF 主动销毁。无 prefab（走 DSP visual_name 批量渲染）。</para>
/// </summary>
public class AttackModeRangedRebound : AttackModeRangedPiercing
{
    #region 常量
    /// <summary>同一目标命中冷却（秒）：冷却内重复接触不结算</summary>
    private const float HitCooldown = 1f;
    /// <summary>反弹随机偏转角上限（度）：每次反弹绕 Y 轴 ±此值随机偏转，避免特殊角死循环</summary>
    private const float BounceJitterAngle = 5f;
    /// <summary>冷却名单清理阈值：超过即清掉已过冷却的条目，防永久弹长期战斗名单无限增长</summary>
    private const int CooldownPruneThreshold = 100;
    #endregion

    #region 字段
    /// <summary>存活时长（秒，按 GetFightDeltaTime 累积，跟随暂停/倍速）：命中冷却判定的时间基准</summary>
    private float lifeTime;
    /// <summary>是否已进入过场地（x≥左墙）：未进入前不做左墙反弹，防出生即被弹回魔王侧</summary>
    private bool hasEnteredField;
    /// <summary>反弹边界（StartAttackBase 时按当场战斗路数/路长缓存）</summary>
    private float boundXMin, boundXMax, boundZMin, boundZMax;
    /// <summary>各目标最近命中时刻（key=creatureUUId，value=命中时的 lifeTime；冷却判定用）</summary>
    private readonly Dictionary<string, float> dictLastHitTime = new Dictionary<string, float>();
    /// <summary>清理过期条目时的待删键缓冲（复用，避免每次 new List 产生 GC）</summary>
    private readonly List<string> listPruneCache = new List<string>();
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：清空本发状态（存活时长/入场标记/冷却名单），并按当场战斗路数路长缓存反弹边界（对象池复用不残留）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        lifeTime = 0f;
        hasEnteredField = false;
        dictLastHitTime.Clear();
        var gameLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (gameLogic != null && gameLogic.fightData != null)
        {
            //道路矩形：x∈[0.5, 0.5+路长]，z∈[0.5, 路数+0.5]（与矿车路尽头算法一致）
            boundXMin = 0.5f;
            boundXMax = 0.5f + gameLogic.fightData.sceneRoadLength;
            boundZMin = 0.5f;
            boundZMax = gameLogic.fightData.sceneRoadNum + 0.5f;
        }
        else
        {
            boundXMin = 0.5f; boundXMax = 11f;
            boundZMin = 0.5f; boundZMax = 6.5f;
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// 更新处理：累积存活时长 → 命中结算（冷却内目标跳过）→ 移动+反弹 → 边界（永不销毁）
    /// </summary>
    public override void Update()
    {
        lifeTime += GameFightLogic.GetFightDeltaTime();
        List<FightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
            for (int i = 0; i < listHitTarget.Count; i++)
            {
                var itemCreature = listHitTarget[i];
                string itemCreatureId = itemCreature.fightCreatureData.creatureData.creatureUUId;
                //冷却中的目标跳过，穿透飞过不结算
                if (dictLastHitTime.TryGetValue(itemCreatureId, out float lastHitTime) && lifeTime - lastHitTime < HitCooldown)
                    continue;
                HandleForHitTarget(itemCreature);
                dictLastHitTime[itemCreatureId] = lifeTime;
            }
            //永久弹的冷却名单只增不减，超阈值清掉已过冷却的条目
            if (dictLastHitTime.Count > CooldownPruneThreshold)
                PruneCooldownDict();
        }
        //移动处理（含四壁反弹）
        HandleForMove();
        //边界处理（本类永不因越界销毁）
        HandleForBound();
    }
    #endregion

    #region 移动与反弹
    /// <summary>
    /// 移动处理：先按攻击方向直线飞行，再检测四壁反弹
    /// </summary>
    public override void HandleForMove()
    {
        base.HandleForMove();
        HandleWallBounce();
    }

    /// <summary>
    /// 四壁反弹：越墙即钳位回墙内、方向对应分量取反，并叠加 ±5° 随机偏转（防 90° 死角死循环）；
    /// 左墙（x=0.5）在子弹进入过场地后才生效，防止出生即被弹回魔王侧
    /// </summary>
    private void HandleWallBounce()
    {
        Vector3 pos = position;
        Vector3 dir = attackModeData.attackDirection;
        bool isBounced = false;
        //左右墙（右墙始终生效；左墙仅入场后生效）
        if (pos.x > boundXMax && dir.x > 0)
        {
            pos.x = boundXMax;
            dir.x = -dir.x;
            isBounced = true;
        }
        else if (hasEnteredField && pos.x < boundXMin && dir.x < 0)
        {
            pos.x = boundXMin;
            dir.x = -dir.x;
            isBounced = true;
        }
        //上下墙（z 向始终生效，出生点 z 本就在界内不会出生即弹）
        if (pos.z < boundZMin && dir.z < 0)
        {
            pos.z = boundZMin;
            dir.z = -dir.z;
            isBounced = true;
        }
        else if (pos.z > boundZMax && dir.z > 0)
        {
            pos.z = boundZMax;
            dir.z = -dir.z;
            isBounced = true;
        }
        //首次进入场地后左墙才开始生效
        if (!hasEnteredField && pos.x >= boundXMin)
            hasEnteredField = true;
        if (!isBounced)
            return;
        //回弹音效（框架 PlaySound 有 0.1s 同音去重，同帧多颗/极速连弹只播一声，无需自行节流）
        AudioHandler.Instance.PlaySound(AudioEnum.sound_hit_6);
        //归一化后叠加 ±5° 随机偏转，避免恰好 90°/0° 等特殊角陷入左右/上下死循环
        dir.Normalize();
        float jitterAngle = UnityEngine.Random.Range(-BounceJitterAngle, BounceJitterAngle);
        attackModeData.attackDirection = Quaternion.AngleAxis(jitterAngle, Vector3.up) * dir;
        SetPosition(pos);
    }

    /// <summary>
    /// 边界检测：回弹菱块永不因越界销毁（出界一律由反弹钳回界内）
    /// </summary>
    public override bool CheckIsMoveBound()
    {
        return false;
    }
    #endregion

    #region 冷却名单
    /// <summary>
    /// 清理冷却名单中已过冷却的条目（永久弹长期战斗名单只增不减，超阈值时调用）
    /// </summary>
    private void PruneCooldownDict()
    {
        listPruneCache.Clear();
        foreach (var kv in dictLastHitTime)
        {
            if (lifeTime - kv.Value >= HitCooldown)
                listPruneCache.Add(kv.Key);
        }
        for (int i = 0; i < listPruneCache.Count; i++)
        {
            dictLastHitTime.Remove(listPruneCache[i]);
        }
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清空本发状态（存活时长/入场标记/冷却名单），防对象池复用残留
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        lifeTime = 0f;
        hasEnteredField = false;
        dictLastHitTime.Clear();
        base.Destroy(isPermanently);
    }
    #endregion
}
