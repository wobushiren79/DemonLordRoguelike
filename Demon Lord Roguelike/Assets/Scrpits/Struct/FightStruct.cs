
/// <summary>
/// 受到攻击数据
/// </summary>
public struct FightUnderAttackStruct
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

    public FightUnderAttackStruct(BaseAttackMode baseAttackMode,string attackedId)
    {
        this.attackedId = attackedId;
        attackerId = baseAttackMode.attackerId;
        attackerDamage = baseAttackMode.attackerDamage;
        attackerCRT = baseAttackMode.attackerCRT;
        soundHitId = baseAttackMode.attackModeInfo.sound_hit;
        soundMissId = baseAttackMode.attackModeInfo.sound_miss;
    }

    public FightUnderAttackStruct(BuffEntityBean buffEntityData, int attackerDamage)
    {
        attackedId = buffEntityData.applierCreatureId;
        attackerId = buffEntityData.targetCreatureId;
        this.attackerDamage = attackerDamage;
        attackerCRT = 0;
        soundHitId = 0;
        soundMissId = 0;
    }
}
