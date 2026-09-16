using System;
using UnityEngine;

/// <summary>
/// 单体瞬时攻击（与 AttackModeInstantArea 范围瞬击对称；牧师普攻 102003）。
/// <para>当帧命中无弹道：对 AI 锁定的单体目标立即 UnderAttack 结算，命中特效播在目标位置+攻击者 attack_start_position 偏移
/// （劈在目标躯干，照 AttackModeFalluponChain.ExecuteAttack 取点先例），随后回收并回调。无连锁无扩散。</para>
/// </summary>
public class AttackModeInstant : BaseAttackMode
{
    #region 攻击流程
    /// <summary>
    /// 开始攻击-无目标：直接回收
    /// </summary>
    public override void StartAttack()
    {
        base.StartAttack();
        //攻击完了就回收这个攻击
        Destroy();
    }

    /// <summary>
    /// 开始攻击-生物：当帧命中单体目标 → 播击中特效 → 回收并回调
    /// </summary>
    public override void StartAttack(FightCreatureEntity attacker, FightCreatureEntity attacked, Action<BaseAttackMode> actionForAttackEnd)
    {
        base.StartAttack(attacker, attacked, actionForAttackEnd);
        if (attacker != null && attacked != null && !attacked.IsDead() && attacked.creatureObj != null)
        {
            //扣血
            attacked.UnderAttack(this);
            //播放击中粒子特效（目标位置+攻击者攻击起始位置偏移劈在目标躯干；攻击者已销毁时退化为目标脚底）
            Vector3 hitPos = attacked.creatureObj.transform.position;
            if (attacker.fightCreatureData?.creatureData?.creatureInfo != null)
            {
                hitPos += attacker.fightCreatureData.creatureData.creatureInfo.GetAttackStartPosition();
            }
            PlayEffectForHit(hitPos);
        }
        //攻击完了就回收这个攻击
        Destroy();
        //攻击结束回调
        actionForAttackEnd?.Invoke(this);
    }
    #endregion
}
