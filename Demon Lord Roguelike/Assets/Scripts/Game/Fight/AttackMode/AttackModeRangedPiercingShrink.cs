using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 远程穿透弹道-逐击衰减收缩（兽人魔法师 火锥205002/冰锥205003）：
/// 子弹初始为原本 2 倍大小；每穿透一个敌人，伤害减半（向下取整、保底1，照 <see cref="AttackModeRangedPiercingRoad"/> 先例）、大小缩小当前的 1/3（变为上次的 2/3）；
/// 当次命中结算伤害 ≤1（即伤害降到 1 的那次命中）后子弹销毁。ATK=10 时伤害依次 10/5/2/1、大小依次 2→1.33→0.89→0.59 倍，共命中 4 个目标。
/// <para>与基类差异：①销毁条件由「穿满 numPierceMax 个」改为「伤害降至 1」（保底1后不再衰减，无此判定会无限穿透，命中数天然有界 ⌊log2(ATK)⌋+1）；</para>
/// <para>②每命中按 <see cref="ScaleShrinkRate"/> 收缩；③同帧多命中按距本弹近远升序结算（命中检测命中顺序不保证近远；不照矿车按 x 排——本弹可双向飞行，按距离方向无关）。</para>
/// <para>尺寸通道：visualScale 喂 DSP 桶 _InstanceScale 与实例矩阵（AttackModeInstanceRenderer 每帧现灌现画，当帧生效）；</para>
/// <para>spriteRenderer.localScale 为 prefab 渲染通道预留（205002/205003 prefab_name 空 → spriteRenderer 恒 null），</para>
/// <para>一律绝对赋值(Vector3.one × visualScale)而非乘性累积，防止将来配置 prefab 后对象池跨发残留。</para>
/// <para>规约：闪避/无敌目标不扣血不挂buff但仍消耗一次穿透与衰减（与基类计入 numPierceMax 同规约）；</para>
/// <para>纯数据发射路径(StartAttack() 无攻击者)attackerDamage=0，首击后即销毁——本类仅供生物武器攻击使用。</para>
/// </summary>
public class AttackModeRangedPiercingShrink : AttackModeRangedPiercing
{
    #region 数值常量（策划调整入口）
    /// <summary>初始大小倍率（相对武器 StartSize 配置值，未配置按 1 即"现在的 N 倍"）</summary>
    public const float InitialScaleRate = 2f;
    /// <summary>每穿透一个敌人后子弹大小缩小当前大小的比例（1/3=每次变为上次的 2/3；伤害衰减按 /2 写死，照 PiercingRoad 先例）</summary>
    public const float ScaleShrinkRate = 1f / 3f;
    #endregion

    #region 开始攻击
    /// <summary>
    /// 开始攻击基础：应用初始 2 倍大小（时机在 ResetVisualParams 归 -1 与武器 attack_mode_data 覆盖之后，
    /// 两条 StartAttack 路径都经由此处，对象池复用每发重算不残留）
    /// </summary>
    public override void StartAttackBase()
    {
        base.StartAttackBase();
        //未配置武器 StartSize 时 visualScale=-1，按 1 起算（即"现在的大小"）再乘初始倍率
        visualScale = (visualScale >= 0f ? visualScale : 1f) * InitialScaleRate;
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localScale = Vector3.one * visualScale;
        }
    }
    #endregion

    #region 逻辑处理
    /// <summary>
    /// 更新处理：同帧命中按距本弹近远升序逐个结算（已穿透过的跳过），伤害降至 1 的命中后销毁，否则继续飞行
    /// </summary>
    public override void Update()
    {
        List<FightCreatureEntity> listHitTarget = CheckHitTarget();
        if (!listHitTarget.IsNull())
        {
            //同帧命中多个时按距本弹从近到远依次结算（命中顺序本身不保证近远），保证伤害/尺寸递减顺序正确
            Vector3 selfPos = position;
            listHitTarget.Sort((a, b) =>
                (a.creatureObj.transform.position - selfPos).sqrMagnitude.CompareTo(
                (b.creatureObj.transform.position - selfPos).sqrMagnitude));
            for (int i = 0; i < listHitTarget.Count; i++)
            {
                var itemCreature = listHitTarget[i];
                string itemCreatureId = itemCreature.fightCreatureData.creatureData.creatureUUId;
                //已穿透过的目标跳过，避免重复扣血
                if (listPierceCreature.Contains(itemCreatureId))
                {
                    continue;
                }
                HandleForHitTarget(itemCreature);
                listPierceCreature.Add(itemCreatureId);
                //HandleForHitTarget 内伤害降至 1 即销毁（Destroy 立即翻 isValid），本帧停止结算后续命中并不再移动
                if (!isValid)
                {
                    return;
                }
            }
        }
        //移动处理
        HandleForMove();
        //边界处理
        HandleForBound();
    }

    /// <summary>
    /// 处理击中生物之后的逻辑：按当前伤害结算一次；当次伤害 ≤1 则销毁，否则伤害减半、大小缩小 1/3 继续飞
    /// </summary>
    public override void HandleForHitTarget(FightCreatureEntity fghtCreatureEntity)
    {
        //按当前伤害结算（第1个目标=满额伤害；BUFF 附加走 UnderAttack 存活回调，纯配置驱动）
        fghtCreatureEntity.UnderAttack(this);
        //伤害降到 1 的那次命中后子弹销毁（保底1后不再衰减，无此判定会无限穿透）
        if (attackModeData.attackerDamage <= 1)
        {
            Destroy();
            return;
        }
        //伤害逐目标减半，保底1点
        attackModeData.attackerDamage = Math.Max(1, attackModeData.attackerDamage / 2);
        //大小缩小当前的 1/3（变为上次的 2/3；DSP 桶每帧读 visualScale，当帧生效）
        visualScale *= 1f - ScaleShrinkRate;
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localScale = Vector3.one * visualScale;
        }
    }
    #endregion
}
