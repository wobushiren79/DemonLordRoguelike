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

    //攻击者ID
    public string attackerId;
    //被攻击者ID
    public string attackedId;

    //发出攻击的生物ID
    public long creatureId; 
    //发出攻击的武器
    public long weaponItemId;

    /// <summary>
    /// 初始化攻击样式
    /// </summary>
    public virtual void InitAttackModeShow()
    {

    }

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
                if (attacker.fightCreatureData.creatureData != null)
                {
                    //设置攻击者ID
                    attackerId = attacker.fightCreatureData.creatureData.creatureId;
                    //设置伤害
                    attackerDamage = attacker.fightCreatureData.creatureData.GetATK();
                }
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
            if (attacked.fightCreatureData != null)
            {
                if (attacked.fightCreatureData.creatureData != null)
                {
                    //设置被攻击者ID
                    attackedId = attacked.fightCreatureData.creatureData.creatureId;
                }
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
        attackerId = null;
        attackedId = null;
        creatureId = 0;
        weaponItemId = 0;
        FightHandler.Instance.RemoveAttackModePrefab(this);
    }
}
