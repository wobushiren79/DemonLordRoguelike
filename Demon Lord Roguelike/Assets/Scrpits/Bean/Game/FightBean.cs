using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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

    public int fightSceneId;//ս������Id;

    public float timeUpdateForAttCreate = 0;//����ʱ��-��������
    public float timeUpdateTargetForAttCreate = 0;//����Ŀ��ʱ��-��������

    public int currentMagic;//��ǰħ��ֵ
    public FightAttCreateDetailsBean currentFightAttCreateDetails;//��ǰ��������

    public List<CreatureBean> listDefCreatureData = new List<CreatureBean>();//��ǰ���÷�����������

    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();//�����ڳ��ϵ���������

    public FightAttCreateBean fightAttCreateData;//��������
    public Dictionary<int, List<GameFightCreatureEntity>> dicAttCreatureEntity = new Dictionary<int, List<GameFightCreatureEntity>>();//����������������ʵ��

    public Dictionary<string, GameFightCreatureEntity> dicCreatureEntity = new Dictionary<string, GameFightCreatureEntity>();//��������ʵ��

    public FightCreatureBean fightDefCoreData;//���غ�������
    public GameFightCreatureEntity fightDefCoreCreature;//���ط���������ʵ��


    public float timeUpdateForFightBuff = 0;//����ʱ��-ս��buff
    public float timeUpdateMaxForFightBuff = 0.1f;//����ʱ��-ս��buff

    public List<FightBuffBean> listBuff = new List<FightBuffBean>();//�������е�buff
    /// <summary>
    /// ��������
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < listDefCreatureData.Count; i++)
        {
            var itemCreature = listDefCreatureData[i];
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
        dicCreatureEntity.Clear();
        dicFightPosition.Clear();
        dicAttCreatureEntity.Clear();

        if (fightDefCoreCreature != null && fightDefCoreCreature.creatureObj != null)
        {
            GameObject.DestroyImmediate(fightDefCoreCreature.creatureObj);
        }
        fightDefCoreCreature = null;
        fightDefCoreData = null;

        timeUpdateForFightBuff = 0;
        listBuff.Clear();
    }

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
    /// ��ȡ����buff
    /// </summary>
    /// <returns></returns>
    public List<FightBuffBean> GetAllBuff()
    {
        return listBuff;
    }

    /// <summary>
    /// ��ȡ�������� �������γ�ʼ������
    /// </summary>
    public void GetAttCreatureInitData(out int fightNum)
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
        if (dicFightPosition.TryGetValue(targetPos,out FightPositionBean fightPosition))
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
        if (dicAttCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
        {
            valueList.Add(targetEntity);
        }
        else
        {
            dicAttCreatureEntity.Add(road, new List<GameFightCreatureEntity>() { targetEntity });
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
        if (dicAttCreatureEntity.TryGetValue(targetEntity.fightCreatureData.positionCreate.z, out List<GameFightCreatureEntity> valueList))
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
        if (dicAttCreatureEntity.TryGetValue(road, out List<GameFightCreatureEntity> valueList))
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
        if (dicCreatureEntity.TryGetValue(creatureId,out GameFightCreatureEntity targetCreature))
        {
            return targetCreature;
        }
        return null;
    }

    /// <summary>
    /// ���һ��ս��BUFF
    /// </summary>
    /// <param name="fightBuffData"></param>
    public void AddFightBuff(FightBuffBean fightBuffData)
    {
        listBuff.Add(fightBuffData);
    }
     
    /// <summary>
    /// �Ƴ�һ��ս��BUFF
    /// </summary>
    public void RemoveFightBuff(FightBuffBean fightBuffData)
    {
        try
        {
            listBuff.Remove(fightBuffData);
            var targetCreature = GetFightCreatureById(fightBuffData.creatureId);
            if (targetCreature != null && targetCreature.fightCreatureData != null && !targetCreature.fightCreatureData.listBuff.IsNull())
            {
                targetCreature.fightCreatureData.listBuff.Remove(fightBuffData);
                targetCreature.fightCreatureData.InitBaseAttribute();
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"�Ƴ�ս��buffʧ��  {e.ToString()}");
        }
    }
}
