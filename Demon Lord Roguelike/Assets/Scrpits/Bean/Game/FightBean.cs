using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//��Ϸʱ��
    public float gameSpeed = 1;//��Ϸ�ٶ�

    public int fightSceneId;//ս������Id;

    public float timeUpdateForAttackCreate = 0;//����ʱ��-��������
    public float timeUpdateTargetForAttackCreate = 0;//����Ŀ��ʱ��-��������

    public float timeUpdateForFightCreature = 0;//����Ŀ��ʱ��-����
    public float timeUpdateTargetForFightCreature = 0.1f;//����Ŀ��ʱ��-����

    public int currentMagic;//��ǰħ��ֵ

    //��������
    public FightAttackBean fightAttackData;

    //���п�Ƭ������������
    public Dictionary<string, CreatureBean> dicDefCreatureData = new Dictionary<string, CreatureBean>();

    //��������λ������
    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();

    //���н���������ʵ��
    public Dictionary<int, List<GameFightCreatureEntity>> dicAttackCreatureEntity = new Dictionary<int, List<GameFightCreatureEntity>>();

    //����������������ͷ��أ�
    public Dictionary<string, GameFightCreatureEntity> dicCreatureEntity = new Dictionary<string, GameFightCreatureEntity>();

    //���غ�������
    public FightCreatureBean fightDefCoreData;
    //���ط���������ʵ��
    public GameFightCreatureEntity fightDefCoreCreature;

    //ս�����ݼ�¼
    public FightRecordsBean fightRecordsData = new FightRecordsBean();

    /// <summary>
    /// ����Ƿ�ӵ�н�������
    /// </summary>
    public bool CheckHasAttackCreature()
    {
        bool HasAttackCreature = false;
        foreach (var itemData in dicAttackCreatureEntity)
        {
            if (itemData.Value.Count > 0)
            {
                HasAttackCreature = true;
            }
        }
        return HasAttackCreature;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void Clear()
    {
        foreach (var itemData in dicDefCreatureData)
        {
            var itemCreature = itemData.Value;
            itemCreature.creatureState = CreatureStateEnum.Idle;
        }

        foreach (var item in dicCreatureEntity)
        {
            var itemValue = item.Value;
            if (itemValue != null && itemValue.creatureObj != null)
            {
                GameObject.DestroyImmediate(itemValue.creatureObj);
            }
        }
        dicDefCreatureData.Clear();
        dicCreatureEntity.Clear();
        dicFightPosition.Clear();
        dicAttackCreatureEntity.Clear();

        if (fightDefCoreCreature != null && fightDefCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefCoreCreature.creatureObj);
        }
        fightDefCoreCreature = null;
        fightDefCoreData = null;
    }

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    public void InitData()
    {
        timeUpdateForAttackCreate = 0;
        timeUpdateTargetForAttackCreate = 0;
        timeUpdateForFightCreature = 0;
    }

    /// <summary>
    /// �ı�ħ��
    /// </summary>
    public void ChangeMagic(int changeData)
    {
        currentMagic += changeData;
        if (currentMagic < 0)
            currentMagic = 0;
        EventHandler.Instance.TriggerEvent(EventsInfo.Magic_Change);
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
    /// �Ƴ�ս��λ������
    /// </summary>
    public void RemoveFightPosition(Vector3Int targetPos)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean fightPosition))
        {
            dicFightPosition.Remove(targetPos);

            if (fightPosition.creatureMain != null && dicCreatureEntity.ContainsKey(fightPosition.creatureMain.fightCreatureData.creatureData.creatureId))
            {
                dicCreatureEntity.Remove(fightPosition.creatureMain.fightCreatureData.creatureData.creatureId);
            }
            if (fightPosition.creatureAssist != null && dicCreatureEntity.ContainsKey(fightPosition.creatureAssist.fightCreatureData.creatureData.creatureId))
            {
                dicCreatureEntity.Remove(fightPosition.creatureAssist.fightCreatureData.creatureData.creatureId);
            }
        }
    }

    /// <summary>
    /// ����ս��λ������
    /// </summary>
    public void SetFightPosition(Vector3Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
            targetPositionData.creatureMain.fightCreatureData.positionCreate = targetPos;
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            newPositionData.creatureMain.fightCreatureData.positionCreate = targetPos;
            dicFightPosition.Add(targetPos, newPositionData);
        }

        if (!dicCreatureEntity.ContainsKey(fightCreature.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Add(fightCreature.fightCreatureData.creatureData.creatureId, fightCreature);
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
            Vector3Int targetPosition = new Vector3Int(i, 0, roadIndex);
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


    /// <summary>
    /// ����ս������
    /// </summary>
    public void AddFightAttCreature(int road, GameFightCreatureEntity targetEntity)
    {
        if (dicAttackCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Add(targetEntity);
        }
        else
        {
            dicAttackCreatureEntity.Add(road, new List<GameFightCreatureEntity>() { targetEntity });
        }

        if (!dicCreatureEntity.ContainsKey(targetEntity.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Add(targetEntity.fightCreatureData.creatureData.creatureId, targetEntity);
        }
    }

    /// <summary>
    /// �Ƴ�ս������
    /// </summary>
    public void RemoveFightAttCreature(GameFightCreatureEntity targetEntity)
    {
        if (dicAttackCreatureEntity.TryGetValue(targetEntity.fightCreatureData.positionCreate.z, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Remove(targetEntity);
        }
        if (dicCreatureEntity.ContainsKey(targetEntity.fightCreatureData.creatureData.creatureId))
        {
            dicCreatureEntity.Remove(targetEntity.fightCreatureData.creatureData.creatureId);
        }
    }

    /// <summary>
    /// ��ȡĳһ·���еĽ�������
    /// </summary>
    /// <param name="road"></param>
    /// <returns></returns>
    public List<GameFightCreatureEntity> GetFightAttCreatureByRoad(int road)
    {
        if (dicAttackCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            return valueList;
        }
        return null;
    }

    /// <summary>
    /// ͨ��ID��ȡĳһ����
    /// </summary>
    /// <param name="creatureId"></param>
    /// <returns></returns>
    public GameFightCreatureEntity GetFightCreatureById(string creatureId)
    {
        if (dicCreatureEntity.TryGetValue(creatureId, out GameFightCreatureEntity targetCreature))
        {
            return targetCreature;
        }
        return null;
    }
}
