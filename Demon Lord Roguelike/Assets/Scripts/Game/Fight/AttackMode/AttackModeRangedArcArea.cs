using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

/// <summary>
/// 抛物线落点范围攻击弹道（范围杀版）：抛射物飞向起飞时锁定目标脚底的固定落点（不追踪，目标中途死亡/移动照飞原落点），
/// 途中不命中任何目标（抛射越过途中敌人），到达落点按 collider_area_type/collider_area_size 做范围检测，
/// 范围内所有存活敌人全部结算伤害（AOE，hit_max>0 按距落点近者优先截断+同生物去重；0=不限）。
/// <para>与基类差异：①途中不命中（基类后半程射线检测单体命中）；②命中只发生在落点范围结算（基类单体）。</para>
/// <para>【配置】class_name + collider_area_type(11=AreaSphere) + collider_area_size(落点AOE半径) + 可选 hit_max，视觉/音效/拖尾配置同普通远程。</para>
/// </summary>
public class AttackModeRangedArcArea : AttackModeRangedArc
{
    #region 逻辑处理
    /// <summary>
    /// 收集本帧射线检测请求：全程不入队（命中只发生在落点范围检测，不用弹道射线）
    /// </summary>
    public override void PrepareRaycast(FightRaycastBatch batch)
    {
        batchRayStart = -1;
    }

    /// <summary>
    /// 检测碰撞：途中不命中任何目标（抛射越过途中敌人，命中只发生在落点范围结算）
    /// </summary>
    public override FightCreatureEntity CheckHitTargetForSingle()
    {
        return null;
    }

    /// <summary>
    /// 到达落点：范围检测结算范围内所有存活敌人伤害（hit_max>0 近者优先截断），随后回收弹道
    /// </summary>
    protected override void HandleForReachEnd()
    {
        //播放击中粒子特效
        PlayEffectForHit(position);
        //落点范围检测，范围内敌人全部命中（复用基类AOE：hit_max>0 时近者优先截断+同生物去重，0=不限）
        CheckHitTargetArea(position, (targetCreature) =>
        {
            targetCreature.UnderAttack(this);
        });
        Destroy();
    }
    #endregion
}
