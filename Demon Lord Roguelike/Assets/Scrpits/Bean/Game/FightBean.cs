using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//��Ϸʱ��
    public float gameSpeed = 1;//��Ϸ�ٶ�

    public int currentMagic;//��ǰħ��ֵ��

    public List<FightCreatureBean> listDefCreatureData=new List<FightCreatureBean>();//��ǰ���÷�����������
    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();//�����ڳ��ϵ���������

    public FightAttCreateBean fightAttCreateData;//��������
    public List<GameFightCreatureEntity> listAttCreatureEntity = new List<GameFightCreatureEntity>();//����������������ʵ��

    /// <summary>
    /// ��ȡ�������� �������γ�ʼ������
    /// </summary>
    public void GetAttCreateInitData(out int fightNum)
    {
        fightNum = 0;
        if (fightAttCreateData == null)
            return;
        if (!fightAttCreateData.dicDetailsData.IsNull())
        {
            fightNum = fightAttCreateData.dicDetailsData.Count;
        }
    }

    /// <summary>
    /// �ı�ħ��
    /// </summary>
    public void ChangeMagic(int changeData)
    {
        currentMagic += changeData;
        if (currentMagic < 0)
            currentMagic = 0;
    }

    /// <summary>
    /// ���ָ��ս��λ�����Ƿ�������
    /// </summary>
    public bool CheckFightPositionHasCreature(Vector3Int targetPos)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            if (targetPositionData.creatureMain != null)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// ����ս��λ������
    /// </summary>
    public void SetFightPosition(Vector3Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            dicFightPosition.Add(targetPos, newPositionData);
        }
    }
}
