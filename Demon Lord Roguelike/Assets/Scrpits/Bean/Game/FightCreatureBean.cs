using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //��������
    public int positionZCurrent;//��ǰλ��

    public int liftCurrent;//��ǰ����ֵ
    public int liftMax;//�������ֵ

    public int armorCurrent;//��ǰ����ֵ
    public int armorMax;//��󻤼�ֵ

    protected CreatureInfoBean creatureInfo;//������Ϣ

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        var creatureInfo = GetCreatureInfo();
        liftCurrent = creatureInfo.life;
        liftMax = creatureInfo.life;
    }

    /// <summary>
    /// ��ȡ�ƶ��ٶ�
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.speed_move;
    }

    /// <summary>
    /// ��ȡ����CD
    /// </summary>
    /// <returns></returns>
    public float GetAttCD()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_cd;
    }

    /// <summary>
    /// ��ȡ������Ϣ
    /// </summary>
    /// <returns></returns>
    public CreatureInfoBean GetCreatureInfo()
    {
        if (creatureInfo == null || creatureInfo.id != creatureData.id)
        {
            creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        }
        return creatureInfo;
    }

    /// <summary>
    /// ��ȡ������ħ��
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        return creatureInfo.create_magic;
    }
}
