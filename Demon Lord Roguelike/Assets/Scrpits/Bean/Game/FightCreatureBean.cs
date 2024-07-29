using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //��������
    public Vector3Int positionCreate;//����λ��

    public int liftCurrent;//��ǰ����ֵ
    public int liftMax;//�������ֵ

    public int armorCurrent;//��ǰ����ֵ
    public int armorMax;//��󻤼�ֵ

    public CardStateEnum stateForCard = CardStateEnum.None;//��Ƭ״̬(����UIչʾ)
    protected CreatureInfoBean creatureInfo;//������Ϣ

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    /// <summary>
    /// �������Ƥ�� ���ڲ���
    /// </summary>
    public void AddAllSkin()
    {
        var allData = CreatureModelInfoCfg.GetAllData();
        var creatureInfo = GetCreatureInfo();
        foreach (var itemData in allData)
        {
            var itemCreatureModelInfo = itemData.Value;
            if (itemCreatureModelInfo.model_id == creatureInfo.model_id)
            {
                creatureData.AddSkin(itemCreatureModelInfo.id);
            }
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = GetCreatureInfo();
        liftCurrent = creatureInfo.life;
        liftMax = creatureInfo.life;
    }

    /// <summary>
    /// �ı令��
    /// </summary>
    /// <param name="changeArmorData"></param>
    public int ChangeArmor(int changeArmorData,out int outArmorChangeData)
    {
        outArmorChangeData = 0;
        armorCurrent += changeArmorData;
        if (armorCurrent < 0)
        {
            outArmorChangeData = armorCurrent;
            armorCurrent = 0;
        }
        if (armorCurrent > armorMax)
        {
            armorCurrent = armorMax;
        }
        return armorCurrent;
    }

    /// <summary>
    /// �ı�����ֵ
    /// </summary>
    /// <param name="changeLifeData"></param>
    public int ChangeLife(int changeLifeData)
    {
        liftCurrent += changeLifeData;
        if (liftCurrent < 0)
        {
            liftCurrent = 0;
        }
        if (liftCurrent > liftMax)
        {
            liftCurrent = liftMax;
        }
        return liftCurrent;
    }

    /// <summary>
    /// ��ȡ����
    /// </summary>
    /// <returns></returns>
    public int GetAttDamage()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_damage;
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
    /// ��ȡ������������ʱ��
    /// </summary>
    /// <returns></returns>
    public float GetAttAnimCastTime()
    {
        var creatureInfo = GetCreatureInfo();
        return creatureInfo.att_anim_cast_time;
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
