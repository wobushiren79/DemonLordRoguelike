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

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = creatureData.GetCreatureInfo();
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

}
