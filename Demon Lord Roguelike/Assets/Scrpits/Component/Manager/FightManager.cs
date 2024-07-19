using OfficeOpenXml.Packaging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class FightManager : BaseManager
{
    //攻击预制obj
    public Dictionary<string, GameObject> dicAttackModeObj = new Dictionary<string, GameObject>();
    //攻击预制的缓存池
    public Dictionary<long, Queue<BaseAttackMode>> dicPoolAttackModeObj = new Dictionary<long, Queue<BaseAttackMode>>();
    //攻击预制列表
    public List<BaseAttackMode> listAttackModePrefab = new List<BaseAttackMode>();


    public static string pathDropMagicPrefab = "Assets/LoadResources/Common/FightDropMagic.prefab";
    public static string pathDropCoinPrefab = "Assets/LoadResources/Common/FightDropCoin.prefab";
    //一些战斗杂项预制
    public Dictionary<string, GameObject> dicFightModeObj = new Dictionary<string, GameObject>();
    //战斗杂项缓存池
    public Dictionary<string, Queue<GameFightPrefabEntity>> dicPoolFightObj = new Dictionary<string, Queue<GameFightPrefabEntity>>();
    //战斗杂项列表
    public List<GameFightPrefabEntity> listFightPrefab = new List<GameFightPrefabEntity>();

    //倒计时
    public List<GameTimeCountDownBean> listTimeCountDown = new List<GameTimeCountDownBean>();
    public Queue<GameTimeCountDownBean> poolTimeCountDown = new Queue<GameTimeCountDownBean>();

    /// <summary>
    /// 清理所有数据
    /// </summary>
    public void Clear()
    {
        //战斗预制清理
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

        //战斗杂项清理
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

        //倒计时清理
        listTimeCountDown.Clear();
        poolTimeCountDown.Clear();
    }

    /// <summary>
    /// 获取一个新的倒计时
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
    /// 移除倒计时
    /// </summary>
    /// <param name="targetData"></param>
    public void RemoveTimeCountDown(GameTimeCountDownBean targetData)
    {
        listTimeCountDown.Remove(targetData);
        targetData.Clear();
        poolTimeCountDown.Enqueue(targetData);
    }

    /// <summary>
    /// 获取FightPrefab
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
        LogUtil.LogError($"GetFightPrefab 失败没有找到id_{id}");
        return null;
    }

    /// <summary>
    /// 获取掉落金币预制
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
    /// 获取掉落魔力预制
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
    /// 移除攻击模组
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
    /// 获取战斗预制-通用
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
    /// 获取攻击模组
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
    /// 移除攻击模组
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
