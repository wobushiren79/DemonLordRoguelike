using OfficeOpenXml.Packaging;
using System;
using System.Collections;
using System.Collections.Generic;
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
