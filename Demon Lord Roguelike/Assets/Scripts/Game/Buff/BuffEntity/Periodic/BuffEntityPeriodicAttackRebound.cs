using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 周期型BUFF-回弹菱块维持（深渊馈赠「回弹菱块」）
/// <para>每 trigger_time 秒自检一次：场上由本BUFF发射且仍存活的菱块不足 class_entity_data[0] 颗时立即补足。
/// 菱块为永久弹（不随时间销毁、不随轮次累积），总数恒定=弹数；关卡切换存量弹被 ClearAttackModePrefab 清掉后靠本机制自动补回，
/// 馈赠升级替换/清空时 ClearData 主动销毁全部存量弹。</para>
/// <para>发射：从防守核心（魔王）位置 + CreatureInfo 攻击起始位置 + 攻击模块偏移射出，初始角度为以 +x 为 0° 的前向锥
/// [-LaunchAngleHalfRange, +LaunchAngleHalfRange] 内随机（保证射入道路范围）；场上无敌人也照常发射（子弹在场内反弹待命）。</para>
/// <para>伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每颗发射时实时取，保底1点），不暴击；
/// 四壁反弹（±5°随机偏转）与同目标 1 秒命中冷却由 AttackModeRangedRebound 自身处理。</para>
/// <para>class_entity_data 格式："弹数,攻击模块ID"（如 "3,300073"）；弹速按等级配在攻击模块行 speed_move。</para>
/// </summary>
public class BuffEntityPeriodicAttackRebound : BuffEntityPeriodic
{
    #region 常量
    /// <summary>初始发射角前向锥半幅（度）：以 +x 为 0° 在 ±此值内随机，保证射入道路范围且不接近垂直</summary>
    protected const float LaunchAngleHalfRange = 75f;
    #endregion

    #region 字段
    /// <summary>本轮补弹的攻击模块ID（触发时从 class_entity_data 解析缓存）</summary>
    protected long attackModeId;
    /// <summary>常驻弹数上限（=馈赠等级，触发时从 class_entity_data 解析缓存）</summary>
    protected int bulletCount;
    /// <summary>本BUFF发射且仍存活的菱块列表（补弹计数与统一销毁用；失效项在触发时剔除）</summary>
    protected readonly List<BaseAttackMode> listBullet = new List<BaseAttackMode>();
    #endregion

    #region 数据相关
    /// <summary>
    /// 清理数据（馈赠升级替换/清空时调用）：主动销毁全部存量弹并清空追踪列表与解析缓存，防对象池复用残留
    /// </summary>
    public override void ClearData()
    {
        base.ClearData();
        DestroyAllBullets();
        attackModeId = 0;
        bulletCount = 0;
    }
    #endregion

    #region 触发
    /// <summary>
    /// 周期性自检：剔除已失效弹 → 存活数不足 bulletCount 时补足（满编则本轮无事）；无敌人也照常补弹
    /// </summary>
    public override bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        //解析参数 "弹数,攻击模块ID"
        var buffInfo = buffEntityData.GetBuffInfo();
        string[] arrEntityData = buffInfo.class_entity_data.Split(',');
        if (arrEntityData.Length < 2)
        {
            LogUtil.LogError($"回弹菱块BUFF[{buffInfo.id}]的 class_entity_data 格式错误，应为 \"弹数,攻击模块ID\"：{buffInfo.class_entity_data}");
            return false;
        }
        bulletCount = int.Parse(arrEntityData[0]);
        attackModeId = long.Parse(arrEntityData[1]);

        //剔除已失效弹（关卡切换被 ClearAttackModePrefab 清掉的、或被其他逻辑销毁的）
        PruneDeadBullets();
        int deficit = bulletCount - listBullet.Count;
        if (deficit <= 0)
            return false;

        bool isTriggerSuccess = base.TriggerBuffPeriodic(buffEntityData);
        if (!isTriggerSuccess) return false;

        for (int i = 0; i < deficit; i++)
        {
            LaunchBullet();
        }
        return true;
    }
    #endregion

    #region 菱块发射
    /// <summary>
    /// 从魔王处发射1颗菱块（纯数据发射路径）：注入攻击者快照、伤害（保底1点）、起点与前向锥随机方向，并登记到追踪列表
    /// </summary>
    private void LaunchBullet()
    {
        if (attackModeId == 0)
        {
            LogUtil.LogError($"回弹菱块BUFF[{buffEntityData.buffId}]未解析到攻击模块ID，无法发射");
            return;
        }
        //伤害 = BUFF目标(魔王)实时攻击力 × trigger_value 倍率（每颗发射时实时取，保底1点）
        var coreCreature = GetFightCreatureEntityForTarget();
        if (coreCreature == null || coreCreature.fightCreatureData == null || coreCreature.creatureObj == null)
            return;
        float coreATK = coreCreature.fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.ATK);
        int attackDamage = Math.Max(1, (int)(coreATK * buffEntityData.GetTriggerValue()));

        //起点：魔王生物位置 + CreatureInfo 攻击起始位置 + 攻击模块偏移（与回旋镖/斧头对齐）
        Vector3 startPosOffset = coreCreature.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (attackModeInfo != null)
            startPosOffset += attackModeInfo.GetStartPosOffset();
        Vector3 startPos = coreCreature.creatureObj.transform.position + startPosOffset;
        //初始方向：以 +x 为 0° 的前向锥 ±LaunchAngleHalfRange 内随机（保证射入道路范围）
        float launchAngle = UnityEngine.Random.Range(-LaunchAngleHalfRange, LaunchAngleHalfRange);
        Vector3 direction = Quaternion.AngleAxis(launchAngle, Vector3.up) * Vector3.right;

        //纯数据发射路径（照回旋镖/斧头先例）：注入攻击者快照与起点方向
        var fightManager = FightHandler.Instance.manager;
        AttackModeBean attackModeData = fightManager.GetAttackModeData(attackModeId);
        attackModeData.attackerId = coreCreature.fightCreatureData.creatureData.creatureUUId;
        attackModeData.attackerDamage = attackDamage;
        //不暴击（保持闪电/矿车/回旋镖/斧头的设计）
        attackModeData.attackerCRT = 0;
        attackModeData.startPos = startPos;
        //直线弹道不读 targetPos，仅给个前向点保持数据完整
        attackModeData.targetPos = startPos + direction;
        attackModeData.attackedId = "";
        attackModeData.attackedLayerTarget = LayerInfo.CreatureAtt;
        attackModeData.attackDirection = direction;

        fightManager.GetAttackModePrefab(attackModeId, (attackMode) =>
        {
            attackMode.StartAttackInit(attackModeData);
            attackMode.StartAttack();
            //登记追踪（补弹计数与 ClearData 统一销毁用）
            listBullet.Add(attackMode);
        });
    }

    /// <summary>
    /// 剔除追踪列表中已失效的弹（null 或已 Destroy），使补弹计数只算存活弹
    /// </summary>
    private void PruneDeadBullets()
    {
        for (int i = listBullet.Count - 1; i >= 0; i--)
        {
            var bullet = listBullet[i];
            if (bullet == null || !bullet.isValid)
                listBullet.RemoveAt(i);
        }
    }

    /// <summary>
    /// 销毁全部存量弹并清空追踪列表（馈赠升级替换/清空时调用，防旧等级弹残留场上）
    /// </summary>
    private void DestroyAllBullets()
    {
        for (int i = 0; i < listBullet.Count; i++)
        {
            var bullet = listBullet[i];
            if (bullet != null && bullet.isValid)
                bullet.Destroy();
        }
        listBullet.Clear();
    }
    #endregion
}
