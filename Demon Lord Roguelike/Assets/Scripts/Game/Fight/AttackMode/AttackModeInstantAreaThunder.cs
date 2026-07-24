using UnityEngine;

/// <summary>
/// 攻击模式-落雷（瞬时落点AOE + 全局单例雷电粒子），深渊馈赠「闪电」用。
/// <para>雷电粒子必须走 <see cref="EffectHandler.ShowThunderEffect"/> 专用通道：Effect_Thunder_3 是全局单例持久型 PS，
/// 需 Stop(StopEmitting)+Play 重播才支持 0.1 秒连发交叠；标准 effect_hit/ShowEffect 通道对持久型粒子不会重触发爆发，
/// 直接配置会让第 2~N 道雷不闪。与血液/护盾走 EffectHandler 专用方法是同一先例。</para>
/// </summary>
public class AttackModeInstantAreaThunder : AttackModeInstantArea
{
    #region 攻击处理
    /// <summary>
    /// 播放落雷粒子：移动全局唯一实例到落雷点并重播（不走配置 effect_hit）
    /// </summary>
    protected override void PlayHitEffect(Vector3 centerPos)
    {
        EffectHandler.Instance.ShowThunderEffect(centerPos);
    }
    #endregion
}
