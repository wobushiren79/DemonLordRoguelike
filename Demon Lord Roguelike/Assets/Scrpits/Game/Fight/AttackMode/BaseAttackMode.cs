using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAttackMode
{
    //当前obj
    public GameObject gameObject;
    public SpriteRenderer spriteRenderer;
    //信息
    public AttackModeInfoBean attackModeInfo;
    //攻击者的攻击力
    public int attackerDamage;
    //目标位置
    public Vector3 targetPos;
    //攻击方向
    public Vector3 attackDirection;
    //被攻击者的层级
    public int attackedLayer;

    /// <summary>
    /// 开始攻击
    /// </summary>
    /// <param name="attacker">攻击方</param>
    /// <param name="attacked">被攻击方</param>
    public virtual void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        attackerDamage = 0;
        if (attacker != null)
        {
            if (attacker.fightCreatureData != null)
            {
                //设置伤害
                attackerDamage = attacker.fightCreatureData.GetAttDamage();
            }
        }
        if (attacked != null)
        {
            if (attacked.creatureObj != null)
            {
                targetPos = attacked.creatureObj.transform.position;
                attackDirection = Vector3.Normalize(attacked.creatureObj.transform.position - attacker.creatureObj.transform.position);
                attackedLayer = attacked.creatureObj.layer;
            }
            //LogUtil.Log($"attacker_{attacker.creatureObj.transform.position} attacked_{attacked.creatureObj.transform.position} attackDirection_{attackDirection}");
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    public virtual void Update()
    {

    }

    /// <summary>
    /// 清理自己
    /// </summary>
    public virtual void Destory()
    {
        attackerDamage = 0;
        FightHandler.Instance.RemoveAttackModePrefab(this);
    }
}
