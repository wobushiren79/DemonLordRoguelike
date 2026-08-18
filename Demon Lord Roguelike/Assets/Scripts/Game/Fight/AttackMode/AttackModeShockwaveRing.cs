using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 圆环冲击波弹道（深渊馈赠「第六次冲击」）
/// <para>以魔王为圆心的扩张圆环：半径从 0 起每帧按 speed_move 扩张，XZ 距离落在「上一帧半径~当前半径」环带内的存活敌人被命中，
/// 每只敌人每波只命中一次（命中名单去重）；伤害由发射方 BUFF 注入（魔王实时攻击力×倍率、不暴击）。</para>
/// <para>命中时击退敌人（交 AIAttackCreatureEntity.StartKnockback 击退意图：方向固定 +x 沿道路向后推、不带 z 分量防推离路径，
/// 固定时长推移、落点 x 钳制道路范围、攻击循环被打断、结束回闲置重新索敌；距离配在 collider_area_size 第 2 项）。</para>
/// <para>最大半径 = 道路右缘(0.5+路长) + 余量(collider_area_size 第 1 项) − 圆心 x：保证波扫到路尽头、覆盖整条道路，达到即销毁；
/// 扩张时长 = 最大半径 / 扩张速度（GetMoveSpeed，纯数据发射 attackerSpeedRate=1 即配置速度）。</para>
/// <para>纯数据发射路径：由 BuffEntityPeriodicAttackShockwave 创建，伤害/圆心由 BUFF 侧注入；无 prefab/visual_name，
/// 冲击波视觉走 EffectHandler.ShowEnduringSingletonEffect（全局单例通道，StartAttackBase 时播放一次，
/// 用 startSizeMultiplier/startLifetimeMultiplier 按本类判定参数换算，视觉波前与判定环带严格同步）。</para>
/// </summary>
public class AttackModeShockwaveRing : BaseAttackMode
{
    #region 视觉基准常量
    /// <summary>冲击波视觉基准半径（世界单位）：Effect_Shockwave_1 主粒子在 startSizeMultiplier=1 时的视觉半径，
    /// 代码按 判定半径/基准半径 换算 multiplier；若视觉波前与判定环带不重合，校准此常量（或改 prefab 主粒子 startSize）</summary>
    private const float ShockwaveVisualBaseRadius = 3f;
    /// <summary>冲击波视觉基准时长（秒）：prefab 主粒子 startLifetime，代码按 判定扩张时长/基准时长 换算 multiplier
    /// （Size over Lifetime 曲线按寿命归一化，拉长寿命即等比放慢扩张动画）</summary>
    private const float ShockwaveVisualBaseDuration = 0.5f;
    #endregion

    #region 字段
    /// <summary>当前半径（每帧按 speed_move 扩张）</summary>
    private float radiusCurrent;
    /// <summary>最大半径（StartAttackBase 按当场道路长度计算，达到即销毁）</summary>
    private float radiusMax;
    /// <summary>击退距离（发射时从 collider_area_size 第 2 项读取，默认 0.5）</summary>
    private float knockbackDistance = 0.5f;
    /// <summary>圆心（魔王位置+攻击起始偏移，由 BUFF 注入 startPos；距离判定只算 XZ 平面）</summary>
    private Vector3 centerPos;
    /// <summary>本波已命中名单（每只敌人只命中一次，对象池复用前清空）</summary>
    private readonly HashSet<string> hitCreatureIds = new HashSet<string>();
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：缓存圆心与命中名单，按道路长度算最大半径与击退参数，并播放与判定同步的冲击波视觉
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        hitCreatureIds.Clear();
        radiusCurrent = 0;
        centerPos = attackModeData.startPos;
        // 配置 collider_area_size = "余量,击退距离"（余量默认 0.5，击退默认 0.5）
        float[] arrAreaSize = attackModeInfo.GetColliderAreaSize();
        float radiusMargin = (arrAreaSize != null && arrAreaSize.Length > 0 && arrAreaSize[0] > 0) ? arrAreaSize[0] : 0.5f;
        knockbackDistance = (arrAreaSize != null && arrAreaSize.Length > 1 && arrAreaSize[1] > 0) ? arrAreaSize[1] : 0.5f;
        // 道路右缘 x（矿车同款：左缘 0.5，右缘 0.5+路长）
        float roadMaxX;
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (fightLogic?.fightData != null)
        {
            roadMaxX = 0.5f + fightLogic.fightData.sceneRoadLength;
        }
        else
        {
            roadMaxX = 15f;
        }
        // 最大半径 = 道路右缘 + 余量 − 圆心x（保证覆盖整条道路；下限 1 防圆心在路右缘之外时算出负值）
        radiusMax = Mathf.Max(roadMaxX + radiusMargin - centerPos.x, 1f);
        // 播放冲击波视觉（全局单例通道，特效id取攻击模式配置的 effect_hit）：按判定参数换算 multiplier，视觉波前与判定环带严格同步
        float waveDuration = radiusMax / GetMoveSpeed();
        EffectHandler.Instance.ShowEnduringSingletonEffect(attackModeInfo.GetEffectHitId(), new SingletonEffectParam()
        {
            targetPos = centerPos,
            startSizeMultiplier = radiusMax / ShockwaveVisualBaseRadius,
            startLifetimeMultiplier = Mathf.Max(waveDuration, 0.1f) / ShockwaveVisualBaseDuration,
        });
    }
    #endregion

    #region Update
    /// <summary>
    /// 每帧：扩张半径 → 环带命中检测（上一帧半径~当前半径）→ 达到最大半径销毁
    /// </summary>
    public override void Update()
    {
        if (!isValid)
            return;
        float radiusPrev = radiusCurrent;
        radiusCurrent += GameFightLogic.GetFightDeltaTime() * GetMoveSpeed();
        if (radiusCurrent >= radiusMax)
            radiusCurrent = radiusMax;
        CheckHitRing(radiusPrev, radiusCurrent);
        if (radiusCurrent >= radiusMax)
            Destroy();
    }

    /// <summary>
    /// 环带命中检测：XZ 距离落在 [radiusPrev, radiusNow] 内的存活敌人，命中+击退；每只敌人每波只中一次
    /// </summary>
    private void CheckHitRing(float radiusPrev, float radiusNow)
    {
        var fightLogic = FightHandler.Instance.manager.GetCachedFightLogic();
        if (fightLogic?.fightData == null)
            return;
        var listEnemy = fightLogic.fightData.dlAttackCreatureEntity.List;
        if (listEnemy == null)
            return;
        float sqrPrev = radiusPrev * radiusPrev;
        float sqrNow = radiusNow * radiusNow;
        for (int i = 0; i < listEnemy.Count; i++)
        {
            var enemy = listEnemy[i];
            if (enemy == null || enemy.IsDead() || enemy.creatureObj == null)
                continue;
            string creatureId = enemy.fightCreatureData.creatureData.creatureUUId;
            if (hitCreatureIds.Contains(creatureId))
                continue;
            Vector3 enemyPos = enemy.creatureObj.transform.position;
            // XZ 平面距离（忽略 y 高度差），落在环带内才命中
            float dirX = enemyPos.x - centerPos.x;
            float dirZ = enemyPos.z - centerPos.z;
            float sqrDis = dirX * dirX + dirZ * dirZ;
            if (sqrDis < sqrPrev || sqrDis > sqrNow)
                continue;
            hitCreatureIds.Add(creatureId);
            //受击特效（血液）朝向沿波的外扩方向
            attackModeData.attackDirection = new Vector3(dirX, 0, dirZ).normalized;
            enemy.UnderAttack(this);
            //打死了就不击退（尸体留在原地）
            if (!enemy.IsDead())
                Knockback(enemy);
        }
    }

    /// <summary>
    /// 击退：交给敌人 AI 的击退意图处理（AIAttackCreatureEntity.StartKnockback）——
    /// 方向固定 +x（沿道路向后推，不带 z 分量，敌人不会被推离自己的路径），按固定时长推移一个击退距离，
    /// 推移中攻击循环被打断、结束回闲置重新索敌；打死了就不击退（尸体留在原地，由调用方在命中时先行判断）。
    /// </summary>
    private void Knockback(FightCreatureEntity enemy)
    {
        if (enemy.aiEntity is AIAttackCreatureEntity aiAttack)
        {
            aiAttack.StartKnockback(Vector3.right, knockbackDistance);
        }
    }
    #endregion

    #region 回收
    /// <summary>
    /// 回收：清空命中名单（防对象池复用残留）
    /// </summary>
    public override void Destroy(bool isPermanently = false)
    {
        hitCreatureIds.Clear();
        base.Destroy(isPermanently);
    }
    #endregion
}
