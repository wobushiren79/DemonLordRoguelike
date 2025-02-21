using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAttackMode
{
    //��ǰobj
    public GameObject gameObject;
    public SpriteRenderer spriteRenderer;
    //��Ϣ
    public AttackModeInfoBean attackModeInfo;
    //�����ߵĹ�����
    public int attackerDamage;
    //Ŀ��λ��
    public Vector3 targetPos;
    //��������
    public Vector3 attackDirection;
    //�������ߵĲ㼶
    public int attackedLayer;

    //������ID
    public string attackerId;
    //��������ID
    public string attackedId;
    /// <summary>
    /// ��ʼ����
    /// </summary>
    /// <param name="attacker">������</param>
    /// <param name="attacked">��������</param>
    public virtual void StartAttack(GameFightCreatureEntity attacker, GameFightCreatureEntity attacked, Action actionForAttackEnd)
    {
        attackerDamage = 0;
        if (attacker != null)
        {
            if (attacker.fightCreatureData != null)
            {
                if (attacker.fightCreatureData.creatureData != null)
                {
                    //���ù�����ID
                    attackerId = attacker.fightCreatureData.creatureData.creatureId;
                    //�����˺�
                    attackerDamage = attacker.fightCreatureData.creatureData.GetAttDamage();
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
                    //���ñ�������ID
                    attackedId = attacked.fightCreatureData.creatureData.creatureId;
                }
            }
            //LogUtil.Log($"attacker_{attacker.creatureObj.transform.position} attacked_{attacked.creatureObj.transform.position} attackDirection_{attackDirection}");
        }
    }

    /// <summary>
    /// ����
    /// </summary>
    public virtual void Update()
    {

    }

    /// <summary>
    /// �����Լ�
    /// </summary>
    public virtual void Destory()
    {
        attackerDamage = 0;
        FightHandler.Instance.RemoveAttackModePrefab(this);
    }
}
