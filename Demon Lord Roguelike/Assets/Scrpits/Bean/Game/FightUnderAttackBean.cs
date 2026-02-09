
using UnityEngine;

/// <summary>
/// 受到攻击数据
/// </summary>
public class FightUnderAttackBean
{
    //攻击者ID
    public string attackerId;
    //被攻击者ID
    public string attackedId;
    //攻击者的造成的攻击
    public int attackerDamage;
    //攻击者暴击概率
    public float attackerCRT;

    //击中音效
    public int soundHitId;
    //未命中音效
    public int soundMissId;

    public FightUnderAttackBean()
    {

    }

    /// <summary>
    /// 攻击模块
    /// </summary>
    /// <param name="baseAttackMode"></param>
    /// <param name="attackedId">单独传一个被攻击者ID，因为被攻击者可能和attackMode锁定的目标不一样</param>
    public void SetData(BaseAttackMode baseAttackMode, string attackedId)
    {
        this.attackedId = attackedId;
        this.attackerId = baseAttackMode.attackModeData.attackerId;
        this.attackerDamage = baseAttackMode.attackModeData.attackerDamage;
        this.attackerCRT = baseAttackMode.attackModeData.attackerCRT;
        this.soundHitId = baseAttackMode.attackModeInfo.sound_hit;
        this.soundMissId = baseAttackMode.attackModeInfo.sound_miss;
    }

    /// <summary>
    /// BUFF系统
    /// </summary>
    public void SetData(BuffEntityBean buffEntityData, int attackerDamage)
    {
        this.attackedId = buffEntityData.applierCreatureUUId;
        this.attackerId = buffEntityData.targetCreatureUUId;
        this.attackerDamage = attackerDamage;
        this.attackerCRT = 0;
        this.soundHitId = 0;
        this.soundMissId = 0;
    }

    public void ClearData()
    {
        attackerId = null;
        attackedId = null;
        attackerDamage = 0;
        attackerCRT = 0;
        soundHitId = 0;
        soundMissId = 0;
    }
}