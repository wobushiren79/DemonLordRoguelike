using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
using UnityEngine.UI;

public class BuffManager : BaseManager
{
    //活跃的深渊馈赠BUFF
    public DictionaryList<string, List<BuffBaseEntity>> dicAbyssalBlessingBuffsActivie = new DictionaryList<string, List<BuffBaseEntity>>();
    //活跃的生物BUFF
    public DictionaryList<string, List<BuffBaseEntity>> dicCreatureBuffsActivie = new DictionaryList<string, List<BuffBaseEntity>>();


    //BUFF实例缓存池
    public Dictionary<string, Queue<BuffBaseEntity>> dicBuffEntityPool = new Dictionary<string, Queue<BuffBaseEntity>>();
    //BuffBean缓存池
    public Queue<BuffEntityBean> queueBuffEntityPool = new Queue<BuffEntityBean>();
    //buffpre的实例类
    public Dictionary<long, BuffBasePreEntity> dicBuffPreEntity = new Dictionary<long, BuffBasePreEntity>();

    #region 数据清理
    /// <summary>
    /// 清理深渊馈赠数据
    /// </summary>
    public void ClearAbyssalBlessing()
    {
        dicAbyssalBlessingBuffsActivie.Clear();
    }

    /// <summary>
    /// 清理Buff数据
    /// </summary>
    public void ClearBuff()
    {
        dicCreatureBuffsActivie.Clear();
        dicBuffEntityPool.Clear();
        dicBuffPreEntity.Clear();
        queueBuffEntityPool.Clear();
    }
    #endregion

    #region  基础
    /// <summary>
    /// 移除buffentitybean
    /// </summary>
    public void RemoveBuffEntityBean(BuffEntityBean targetEntityBean)
    {
        if (targetEntityBean == null)
            return;
        queueBuffEntityPool.Enqueue(targetEntityBean);
    }

    /// <summary>
    /// 移除buffentity
    /// </summary>
    /// <param name="itemBuffEntity"></param>
    public void RemoveBuffEntity(BuffBaseEntity itemBuffEntity)
    {
        itemBuffEntity.ClearData();
        Type actualType = itemBuffEntity.GetType();
        string className = actualType.Name; // 获取类名（不包含命名空间）
        //添加到缓存
        if (dicBuffEntityPool.TryGetValue(className, out var targetQueue))
        {
            targetQueue.Enqueue(itemBuffEntity);
        }
        else
        {
            Queue<BuffBaseEntity> newQueue = new Queue<BuffBaseEntity>();
            newQueue.Enqueue(itemBuffEntity);
            dicBuffEntityPool.Add(className, newQueue);
        }
    }

    /// <summary>
    /// 获取BUFFPre实例类
    /// </summary>
    public BuffBasePreEntity GetBuffPreEntity(BuffPreInfoBean buffPreInfo)
    {
        string className = $"{buffPreInfo.class_entity}";
        if (dicBuffPreEntity.TryGetValue(buffPreInfo.id, out var targetClass))
        {
            return targetClass;
        }
        else
        {
            targetClass = ReflexUtil.CreateInstance<BuffBasePreEntity>(className);
            if (targetClass == null)
            {
                return null;
            }
            else
            {
                dicBuffPreEntity.Add(buffPreInfo.id, targetClass);
                return targetClass;
            }
        }
    }

    /// <summary>
    /// 创建buffentitybean
    /// </summary>
    public BuffEntityBean GetBuffEntityBean(long buffId, string applierCreatureId, string targetCreatureId)
    {
        BuffEntityBean targetEntityBean = null;
        if (queueBuffEntityPool.Count > 0)
        {
            targetEntityBean = queueBuffEntityPool.Dequeue();
            targetEntityBean.SetData(buffId, applierCreatureId, targetCreatureId);
        }
        if (targetEntityBean == null)
        {
            targetEntityBean = new BuffEntityBean(buffId, applierCreatureId, targetCreatureId);
        }
        return targetEntityBean;
    }

    

    /// <summary>
    /// 创建buffentity
    /// </summary>
    public BuffBaseEntity GetBuffEntity(BuffEntityBean buffEntity)
    {
        string className = $"{buffEntity.buffInfo.class_entity}";
        BuffBaseEntity targetEntity = null;
        if (dicBuffEntityPool.TryGetValue(className, out var targetQueue))
        {
            if (targetQueue.Count > 0)
            {
                targetEntity = targetQueue.Dequeue();
            }
        }

        if (targetEntity == null)
        {
            targetEntity = ReflexUtil.CreateInstance<BuffBaseEntity>(className);
            if (targetEntity == null)
            {
                LogUtil.LogError($"CreateCreatureBuffs 失败 没有className:{className}");
                return null;
            }
        }
        targetEntity.SetData(buffEntity);
        return targetEntity;
    }
    #endregion

    #region 深渊馈赠BUFF
    /// <summary>
    /// 获取指定深渊馈赠的buff
    /// </summary>
    public List<BuffBaseEntity> GetAbyssalBlessingBuffsActivie(string abyssalBlessingUUID)
    {
        if (dicAbyssalBlessingBuffsActivie.TryGetValue(abyssalBlessingUUID, out var abyssalBlessingBuffsActivieList))
        {
            return abyssalBlessingBuffsActivieList;
        }
        return null;
    }

    /// <summary>
    /// 删除指定深渊馈赠的BUFF 并添加到缓存池pool
    /// </summary>
    public void RemoveAbyssalBlessingBuffsActivie(List<BuffBaseEntity> listBuffEntity)
    {
        for (int i = 0; i < listBuffEntity.Count; i++)
        {
            BuffBaseEntity itemBuffEntity = listBuffEntity[i];
            if (itemBuffEntity != null)
            {
                //移除数据到缓存
                RemoveBuffEntity(itemBuffEntity);
            }
        }
    }
    #endregion

    #region 生物BUFF
    /// <summary>
    /// 获取指定生物的buff
    /// </summary>
    public List<BuffBaseEntity> GetCreatureBuffsActivie(string creatureUUId)
    {
        if (dicCreatureBuffsActivie.TryGetValue(creatureUUId, out var creatureBuffsActivieList))
        {
            return creatureBuffsActivieList;
        }
        return null;
    }


    /// <summary>
    /// 删除指定生物的BUFF 并添加到缓存池pool
    /// </summary>
    public void RemoveCreatureBuffActivie(List<BuffBaseEntity> listBuffEntity, BuffBaseEntity itemBuffEntity)
    {
        listBuffEntity.Remove(itemBuffEntity);
        //移除数据到缓存
        RemoveBuffEntity(itemBuffEntity);
    }
    #endregion
}