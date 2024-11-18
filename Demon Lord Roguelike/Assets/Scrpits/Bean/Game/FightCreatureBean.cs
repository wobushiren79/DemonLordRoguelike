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

    public float moveSpeedCurrent;//��ǰ�ƶ��ٶ�
    public Color colorBodyCurrent;//��ǰ������ɫ
    public List<FightBuffBean> listBuff;//���е�buff

    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    public FightCreatureBean(CreatureBean creatureData)
    {
        this.creatureData = creatureData;
        ResetData();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = creatureData.creatureInfo;
        liftCurrent = creatureInfo.life;
        liftMax = creatureInfo.life;
        listBuff = new List<FightBuffBean>();

        InitBaseAttribute();
    }

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    public void InitBaseAttribute(Action actionForComplete = null)
    {
        moveSpeedCurrent = creatureData.GetMoveSpeed();
        colorBodyCurrent = Color.white;
        if (!listBuff.IsNull())
        {
            float changeMoveSpeed = 0;
            float changeRateMoveSpeed = 0;
            for (int i = 0; i < listBuff.Count; i++)
            {
                FightBuffBean itemBuff = listBuff[i];
                var buffEntity = itemBuff.GetBuffEntity();
                //�����ٶ����
                var targetChangeData = buffEntity.GetChangeDataForMoveSpeed(itemBuff);
                changeMoveSpeed += targetChangeData.change;
                changeRateMoveSpeed += targetChangeData.changeRate;
                //����������ɫ
                if (!itemBuff.fightBuffInfo.color_body.IsNull())
                {
                    colorBodyCurrent = itemBuff.fightBuffInfo.GetBodyColor();
                }
            }
            moveSpeedCurrent += changeMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
            moveSpeedCurrent *= 1 + changeRateMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
        }
        if (moveSpeedCurrent < 0)
            moveSpeedCurrent = 0;
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// ��ȡ��ɫ�ƶ��ٶ�
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        return moveSpeedCurrent;
    }

    /// <summary>
    /// ��ȡ������ɫ
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        return colorBodyCurrent;
    }

    /// <summary>
    /// �ı令��
    /// </summary>
    /// <param name="changeArmorData"></param>
    public int ChangeArmor(int changeArmorData, out int outArmorChangeData)
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
    /// ���BUFF
    /// </summary>
    public void AddBuff(List<FightBuffBean> targetBuffs, Action<FightBuffBean> actionForCombineNew = null, Action<FightBuffBean> actionForCombineOld = null, Action actionForComplete = null)
    {
        for (int f = 0; f < targetBuffs.Count; f++)
        {
            var targetBuff = targetBuffs[f];
            bool hasOldBuff = false;
            for (int i = 0; i < listBuff.Count; i++)
            {
                var itemBuff = listBuff[i];
                if (itemBuff.fightBuffStruct.id == targetBuff.fightBuffStruct.id)
                {
                    hasOldBuff = true;
                    itemBuff.triggerNumLeft = targetBuff.triggerNumLeft;
                    actionForCombineOld?.Invoke(itemBuff);
                    break;
                }
            }
            if (!hasOldBuff)
            {
                listBuff.Add(targetBuff);
                actionForCombineNew?.Invoke(targetBuff);

            }
        }
        InitBaseAttribute(actionForComplete);
    }

    /// <summary>
    /// �Ƴ�BUFF
    /// </summary>
    public void RemoveBuff(FightBuffBean targetBuff, Action actionForComplete = null)
    {
        if (!listBuff.IsNull())
        {
            listBuff.Remove(targetBuff);
        }
        InitBaseAttribute(actionForComplete);
    }

}
