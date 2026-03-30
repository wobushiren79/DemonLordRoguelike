
using UnityEngine;

/// <summary>
/// 攻击模块数据
/// </summary>
public class AttackModeBean
{
    //攻击模块ID
    public long attackModeId;
    //攻击者的攻击力
    public int attackerDamage;
    //攻击者暴击概率
    public float attackerCRT;
    //起始位置
    public Vector3 startPos;
    //目标位置
    public Vector3 targetPos;
    //攻击方向
    public Vector3 attackDirection;
    //攻击者ID
    public string attackerId;
    //目标被攻击者（不一定击中，只是一开始锁定的目标）
    public string attackedId;
    //目标被攻击者的层级（不一定击中，只是一开始锁定的目标）
    public int attackedLayerTarget;
    //攻击者的生物ID（要在初始化攻击样式之前设置）
    public long attackerCreatureId;
    //攻击者的武器道具ID（要在初始化攻击样式之前设置）
    public long attackerWeaponItemId;

    public AttackModeBean()
    {

    }

    public AttackModeBean(long attackModeId)
    {
        InitData(attackModeId);
    }

    public void InitData(long attackModeId)
    {
        this.attackModeId = attackModeId;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearData()
    {
        attackModeId = 0;
        attackerDamage = 0;
        attackerCRT = 0;
        startPos = Vector3.zero;
        targetPos = Vector3.zero;
        attackDirection = Vector3.zero;
        attackerId = null;
        attackedId = null;
        attackedLayerTarget = 0;
        attackerCreatureId = 0;
        attackerWeaponItemId = 0;
    }
}