using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
using UnityEngine.UI;

public class BuffManager : BaseManager
{
    //活跃的生物BUFF
    public DictionaryList<string, List<BuffBaseEntity>> dicCreatureBuffsActivie = new DictionaryList<string, List<BuffBaseEntity>>();
    //BUFF实例缓存池
    public Dictionary<string, Queue<BuffBaseEntity>> dicCreatureBuffPool = new Dictionary<string, Queue<BuffBaseEntity>>();
    //buffpre的实例类
    public Dictionary<long, BuffBasePreEntity> dicBuffPreEntity = new Dictionary<long, BuffBasePreEntity>();
    //BuffEntityBean的缓存池
    public Queue<BuffEntityBean> queueBuffEntityPool = new Queue<BuffEntityBean>();

    /// <summary>
    /// 清理数据
    /// </summary>
    public void Clear()
    {
        dicCreatureBuffsActivie.Clear();
        dicCreatureBuffPool.Clear();
        dicBuffPreEntity.Clear();
        queueBuffEntityPool.Clear();
    }

    /// <summary>
    /// 创建buffentitybean
    /// </summary>
    public BuffEntityBean CreateBuffEntityBean(long buffId, string applierCreatureId, string targetCreatureId)
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
    /// 移除buffentitybean
    /// </summary>
    public void RemoveBuffEntityBean(BuffEntityBean targetEntityBean)
    {
        if (targetEntityBean == null)
            return;
        queueBuffEntityPool.Enqueue(targetEntityBean);
    }

    /// <summary>
    /// 删除指定生物的BUFF 并添加到缓存池pool
    /// </summary>
    public void RemoveCreatureBuffsActivie(List<BuffBaseEntity> listBuffEntity, BuffBaseEntity itemBuffEntity)
    {
        listBuffEntity.Remove(itemBuffEntity);
        //移除数据到缓存
        RemoveBuffEntityBean(itemBuffEntity.buffEntityData);

        Type actualType = itemBuffEntity.GetType();
        string className = actualType.Name; // 获取类名（不包含命名空间）
        //添加到缓存
        if (dicCreatureBuffPool.TryGetValue(className, out var targetQueue))
        {
            targetQueue.Enqueue(itemBuffEntity);
        }
        else
        {
            Queue<BuffBaseEntity> newQueue = new Queue<BuffBaseEntity>();
            newQueue.Enqueue(itemBuffEntity);
            dicCreatureBuffPool.Add(className, newQueue);
        }
    }

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
    /// 获取BUFF实例类
    /// </summary>
    public BuffBaseEntity CreateCreatureBuffs(BuffEntityBean buffEntity)
    {
        string className = $"{buffEntity.buffInfo.class_entity}";
        BuffBaseEntity targetEntity = null;
        if (dicCreatureBuffPool.TryGetValue(className, out var targetQueue))
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
}