using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    //��������
    public CreatureBean creatureData;

    public int liftCurrent;//��ǰ����ֵ
    public int liftMax;//�������ֵ

    public int armorCurrent;//��ǰ����ֵ
    public int armorMax;//��󻤼�ֵ

    /// <summary>
    /// ��ȡ������ħ��
    /// </summary>
    public int GetCreateMagic()
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(creatureData.id);
        return creatureInfo.create_magic;
    }
}
