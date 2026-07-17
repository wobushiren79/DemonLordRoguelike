
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
    //攻击者弹道速度倍率（由攻速ASPD换算，StartAttack时快照，1为无加成）
    public float attackerSpeedRate = 1f;
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
    /// 复制攻击者快照数据（不含 attackModeId：各弹道持有自己的配置ID）
    /// <para>供「一次攻击发射多发子弹道」的发射器（如分裂弹 AttackModeRangedSplit）把父弹道快照好的攻击者数据原样传给每一发子弹，
    /// 使子弹的伤害/暴击/速度倍率/起点/朝向/目标层级与父弹道完全一致。</para>
    /// </summary>
    public void CopyAttackerDataFrom(AttackModeBean targetData)
    {
        if (targetData == null)
        {
            return;
        }
        attackerDamage = targetData.attackerDamage;
        attackerCRT = targetData.attackerCRT;
        attackerSpeedRate = targetData.attackerSpeedRate;
        startPos = targetData.startPos;
        targetPos = targetData.targetPos;
        attackDirection = targetData.attackDirection;
        attackerId = targetData.attackerId;
        attackedId = targetData.attackedId;
        attackedLayerTarget = targetData.attackedLayerTarget;
        attackerCreatureId = targetData.attackerCreatureId;
        attackerWeaponItemId = targetData.attackerWeaponItemId;
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public void ClearData()
    {
        attackModeId = 0;
        attackerDamage = 0;
        attackerCRT = 0;
        attackerSpeedRate = 1f;
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