using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//��Ϸʱ��
    public float gameProgress = 0;//��Ϸ����
    public float gameSpeed = 1;//��Ϸ�ٶ�
    public int gameStage = 0;//��Ϸ����

    public float timeUpdateForAttCreate = 0;//����ʱ��-��������
    public float timeUpdateTargetForAttCreate = 0;//����Ŀ��ʱ��-��������

    public int currentMagic;//��ǰħ��ֵ
    public FightAttCreateDetailsBean currentFightAttCreateDetails;//��ǰ��������

    public List<FightCreatureBean> listDefCreatureData = new List<FightCreatureBean>();//��ǰ���÷�����������
    public Dictionary<Vector2Int, FightPositionBean> dicFightPosition = new Dictionary<Vector2Int, FightPositionBean>();//�����ڳ��ϵ���������

    public FightAttCreateBean fightAttCreateData;//��������
    public List<GameFightCreatureEntity> listAttCreatureEntity = new List<GameFightCreatureEntity>();//����������������ʵ��

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    public void InitDataForAttCreateStage(int gameStage)
    {
        this.gameStage = gameStage;
        gameProgress = 0;
        timeUpdateForAttCreate = 0;
        timeUpdateTargetForAttCreate = 0;
        currentFightAttCreateDetails = fightAttCreateData.GetDetailData(gameStage);
        if (currentFightAttCreateDetails != null)
        {
            timeUpdateTargetForAttCreate = currentFightAttCreateDetails.createDelay;
        }
    }

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
    public bool CheckFightPositionHasCreature(Vector2Int targetPos)
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
    public void SetFightPosition(Vector2Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
            targetPositionData.creatureMain.fightCreatureData.positionZCurrent = Mathf.Abs(targetPos.y);
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            newPositionData.creatureMain.fightCreatureData.positionZCurrent = Mathf.Abs(targetPos.y);
            dicFightPosition.Add(targetPos, newPositionData);
        }
    }

    /// <summary>
    /// ��ȡս��λ������
    /// </summary>
    /// <returns></returns>
    public List<FightPositionBean> GetFightPosition(int roadIndex)
    {
        List<FightPositionBean> listData = new List<FightPositionBean>();
        for (int i = 1; i <= 10; i++)
        {
            Vector2Int targetPosition = new Vector2Int(i, -roadIndex);
            if (dicFightPosition.TryGetValue(targetPosition, out FightPositionBean targetPositionData))
            {
                if (targetPositionData != null)
                {
                    listData.Add(targetPositionData);
                }
            }
        }
        return listData;
    }
}
