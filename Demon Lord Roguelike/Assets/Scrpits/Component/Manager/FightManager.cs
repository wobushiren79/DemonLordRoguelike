using OfficeOpenXml.Packaging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class FightManager : BaseManager
{
    //����Ԥ��obj
    public Dictionary<string, GameObject> dicAttackModeObj = new Dictionary<string, GameObject>();
    //����Ԥ�ƵĻ����
    public Dictionary<long, Queue<BaseAttackMode>> dicPoolAttackModeObj = new Dictionary<long, Queue<BaseAttackMode>>();
    //����Ԥ���б�
    public List<BaseAttackMode> listAttackModePrefab = new List<BaseAttackMode>();


    public static string pathDropMagicPrefab = "Assets/LoadResources/Common/FightDropMagic.prefab";
    public static string pathDropCoinPrefab = "Assets/LoadResources/Common/FightDropCoin.prefab";
    //һЩս������Ԥ��
    public Dictionary<string, GameObject> dicFightModeObj = new Dictionary<string, GameObject>();
    //ս��������
    public Dictionary<string, Queue<GameFightPrefabEntity>> dicPoolFightObj = new Dictionary<string, Queue<GameFightPrefabEntity>>();
    //ս�������б�
    public List<GameFightPrefabEntity> listFightPrefab = new List<GameFightPrefabEntity>();

    //����ʱ
    public List<GameTimeCountDownBean> listTimeCountDown = new List<GameTimeCountDownBean>();
    public Queue<GameTimeCountDownBean> poolTimeCountDown = new Queue<GameTimeCountDownBean>();

    /// <summary>
    /// ������������
    /// </summary>
    public void Clear()
    {
        //ս��Ԥ������
        for (int i = 0; i < listAttackModePrefab.Count; i++)
        {
            var item = listAttackModePrefab[i];
            Destroy(item.gameObject);
        }
        listAttackModePrefab.Clear();
        foreach (var itemData in dicPoolAttackModeObj)
        {
            var queue = itemData.Value;
            for (int i = 0; i < queue.Count; i++)
            {
                var targetData = queue.Dequeue();
                Destroy(targetData.gameObject);
            }
        }
        dicPoolAttackModeObj.Clear();

        //ս����������
        for (int i = 0; i < listFightPrefab.Count; i++)
        {
            var item = listFightPrefab[i];
            Destroy(item.gameObject);
        }
        listFightPrefab.Clear();
        foreach (var itemData in dicPoolFightObj)
        {
            var queue = itemData.Value;
            for (int i = 0; i < queue.Count; i++)
            {
                var targetData = queue.Dequeue();
                Destroy(targetData.gameObject);
            }
        }
        dicPoolFightObj.Clear();

        //����ʱ����
        listTimeCountDown.Clear();
        poolTimeCountDown.Clear();
    }

    /// <summary>
    /// ��ȡһ���µĵ���ʱ
    /// </summary>
    public GameTimeCountDownBean GetNewTimeCountDown()
    {
        if (poolTimeCountDown.Count > 0)
        {
            var targetData = poolTimeCountDown.Dequeue();
            targetData.Clear();
            listTimeCountDown.Add(targetData);
            return targetData;
        }
        GameTimeCountDownBean newData = new GameTimeCountDownBean();
        listTimeCountDown.Add(newData);
        return newData;
    }

    /// <summary>
    /// �Ƴ�����ʱ
    /// </summary>
    /// <param name="targetData"></param>
    public void RemoveTimeCountDown(GameTimeCountDownBean targetData)
    {
        listTimeCountDown.Remove(targetData);
        targetData.Clear();
        poolTimeCountDown.Enqueue(targetData);
    }

    /// <summary>
    /// ��ȡFightPrefab
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public GameFightPrefabEntity GetFightPrefab(string id)
    {
        for (int i = 0; i < listFightPrefab.Count; i++)
        {
            var itemFightPrefab = listFightPrefab[i];
            if (itemFightPrefab.id.Equals(id))
            {
                return itemFightPrefab;
            }
        }
        LogUtil.LogError($"GetFightPrefab ʧ��û���ҵ�id_{id}");
        return null;
    }

    /// <summary>
    /// ��ȡ������Ԥ��
    /// </summary>
    public void GetDropCoinPrefab(Action<GameFightPrefabEntity> actionForComplete)
    {
        GetFightPrefabCommon(pathDropCoinPrefab,(targetPrefab)=> 
        {
            targetPrefab.pathAsstes = pathDropCoinPrefab;
            targetPrefab.SetState(GameFightPrefabStateEnum.None);
            targetPrefab.valueInt = 10;
            targetPrefab.lifeTime = 30;
            actionForComplete?.Invoke(targetPrefab);
        });
    }
    
    /// <summary>
    /// ��ȡ����ħ��Ԥ��
    /// </summary>
    public void GetDropMagicPrefab(Action<GameFightPrefabEntity> actionForComplete)
    {
        GetFightPrefabCommon(pathDropMagicPrefab, (targetPrefab) =>
        {
            targetPrefab.pathAsstes = pathDropMagicPrefab;
            targetPrefab.SetState(GameFightPrefabStateEnum.None);
            targetPrefab.valueInt = 100;
            targetPrefab.lifeTime = 30;
            actionForComplete?.Invoke(targetPrefab);
        });
    }

    /// <summary>
    /// �Ƴ�����ģ��
    /// </summary>
    /// <param name="targetMode"></param>
    public void RemoveFightPrefabCommon(GameFightPrefabEntity targetEntity)
    {
        listFightPrefab.Remove(targetEntity);
        if (dicPoolFightObj.TryGetValue(targetEntity.pathAsstes, out Queue<GameFightPrefabEntity> pool))
        {
            pool.Enqueue(targetEntity);
        }
        else
        {
            Queue<GameFightPrefabEntity> poolNew = new Queue<GameFightPrefabEntity>();
            poolNew.Enqueue(targetEntity);
            dicPoolFightObj.Add(targetEntity.pathAsstes, poolNew);
        }
    }


    /// <summary>
    /// ��ȡս��Ԥ��-ͨ��
    /// </summary>
    /// <param name="assetsPath"></param>
    /// <param name="actionForComplete"></param>
    public void GetFightPrefabCommon(string assetsPath,Action<GameFightPrefabEntity> actionForComplete)
    {
        if (dicPoolFightObj.TryGetValue(assetsPath, out Queue<GameFightPrefabEntity> pool))
        {
            if (pool.Count > 0)
            {
                GameFightPrefabEntity targetPrefab = pool.Dequeue();
                listFightPrefab.Add(targetPrefab);

                targetPrefab.id = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
                targetPrefab.gameObject.name = targetPrefab.id;
                targetPrefab.gameObject.SetActive(true);
                actionForComplete?.Invoke(targetPrefab);
                return;
            }
        }
        GameObject objModel = GetModelForAddressablesSync(dicFightModeObj, assetsPath);
        GameObject objTarget = Instantiate(gameObject, objModel);

        GameFightPrefabEntity fightPrefab = new GameFightPrefabEntity();
        fightPrefab.id = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        objTarget.gameObject.name = fightPrefab.id;
        objTarget.gameObject.SetActive(true);
        fightPrefab.gameObject = objTarget;
        listFightPrefab.Add(fightPrefab);
        actionForComplete?.Invoke(fightPrefab);
    }

    /// <summary>
    /// ��ȡ����ģ��
    /// </summary>
    public void GetAttackModePrefab(int attackModeId, Action<BaseAttackMode> actionForComplete)
    {
        var attackModeInfo = AttackModeInfoCfg.GetItemData(attackModeId);
        if (dicPoolAttackModeObj.TryGetValue(attackModeInfo.id, out Queue<BaseAttackMode> pool))
        {
            if (pool.Count > 0)
            {
                BaseAttackMode targetMode = pool.Dequeue();
                listAttackModePrefab.Add(targetMode);
                actionForComplete?.Invoke(targetMode);
                return;
            }
        }
        GameObject objModel = GetModelForAddressablesSync(dicAttackModeObj, $"{PathInfo.AttackModePrefabPath}/{attackModeInfo.prefab_name}.prefab");
        GameObject objTarget = Instantiate(gameObject, objModel);
        BaseAttackMode targetModeNew = ReflexUtil.CreateInstance<BaseAttackMode>(attackModeInfo.class_name);
        targetModeNew.gameObject = objTarget;
        targetModeNew.spriteRenderer = objTarget.GetComponentInChildren<SpriteRenderer>();
        targetModeNew.attackModeInfo = attackModeInfo;

        listAttackModePrefab.Add(targetModeNew);
        actionForComplete?.Invoke(targetModeNew);
    }

    /// <summary>
    /// �Ƴ�����ģ��
    /// </summary>
    /// <param name="targetMode"></param>
    public void RemoveAttackModePrefab(BaseAttackMode targetMode)
    {
        listAttackModePrefab.Remove(targetMode);
        if (dicPoolAttackModeObj.TryGetValue(targetMode.attackModeInfo.id, out Queue<BaseAttackMode> pool))
        {
            pool.Enqueue(targetMode);
        }
        else
        {
            Queue<BaseAttackMode> poolNew = new Queue<BaseAttackMode>();
            poolNew.Enqueue(targetMode);
            dicPoolAttackModeObj.Add(targetMode.attackModeInfo.id, poolNew);
        }
    }


}
