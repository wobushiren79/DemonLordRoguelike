/// <summary>
/// 攻击模式-落雷（瞬时落点AOE + 全局单例雷电粒子），深渊馈赠「闪电」用。
/// <para>雷电粒子走基类 <see cref="AttackModeInstantArea.PlayHitEffect"/> → <see cref="BaseAttackMode.PlayEffectForHit"/> 通用路径：
/// 攻击模块表 300031~300035 已配 effect_hit=900003(Effect_Thunder_3)，全局单例通道 Stop(StopEmitting)+Play 重播
/// 才支持 0.1 秒连发交叠；标准 ShowEffect 通道对持久型粒子不会重触发爆发。</para>
/// </summary>
public class AttackModeInstantAreaThunder : AttackModeInstantArea
{
}
