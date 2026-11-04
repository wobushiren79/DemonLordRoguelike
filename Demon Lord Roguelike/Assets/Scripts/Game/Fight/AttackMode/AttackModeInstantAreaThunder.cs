using System;
using UnityEngine;

/// <summary>
/// 攻击模式-落雷（瞬时落点AOE + 按距离线性衰减 + 全局单例雷电粒子），深渊馈赠「闪电」用。
/// <para>中心伤害由 BUFF 注入，半径边缘为中心伤害的 falloff_min 配置倍率；各目标独立以 AOE 前的中心伤害快照计算，保底1点。</para>
/// <para>雷电粒子走基类 <see cref="AttackModeInstantArea.PlayHitEffect"/> → <see cref="BaseAttackMode.PlayEffectForHit"/> 通用路径：
/// 攻击模块表 300031~300035 已配 effect_hit=900003(Effect_Thunder_3)，全局单例通道 Stop(StopEmitting)+Play 重播，支持短间隔及同帧连发交叠。</para>
/// </summary>
public class AttackModeInstantAreaThunder : AttackModeInstantArea
{
    #region 命中处理
    /// <summary>
    /// 按目标距落点的距离结算伤害：中心为 baseDamage，检测半径边缘为 baseDamage × falloff_min，线性插值且保底1点；不按命中次序递减。
    /// </summary>
    /// <param name="targetCreature">本次命中的存活目标</param>
    /// <param name="centerPos">本次落雷的中心位置</param>
    /// <param name="baseDamage">整个 AOE 循环前快照的中心伤害</param>
    /// <param name="hitDecayRate">基类命中次序递减率（落雷不使用）</param>
    protected override void HandleForHitTarget(FightCreatureEntity targetCreature, Vector3 centerPos, int baseDamage, float hitDecayRate)
    {
        attackModeData.attackerDamage = baseDamage;
        float[] colliderAreaSize = attackModeInfo.GetColliderAreaSize();
        if (colliderAreaSize != null && colliderAreaSize.Length > 0 && colliderAreaSize[0] > 0 && targetCreature.creatureObj != null)
        {
            float radius = colliderAreaSize[0];
            float distanceRate = Mathf.Clamp01(Vector3.Distance(targetCreature.creatureObj.transform.position, centerPos) / radius);
            float falloffMinRate = attackModeInfo.GetFalloffMinRate();
            attackModeData.attackerDamage = Math.Max(1, (int)(baseDamage * Mathf.Lerp(1f, falloffMinRate, distanceRate)));
        }
        targetCreature.UnderAttack(this);
    }
    #endregion
}
