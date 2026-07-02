using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
using UnityEngine.UI;

public class BuffManager : BaseManager
{
    //活跃的深渊馈赠BUFF
    public DictionaryList<AbyssalBlessingEntityBean, List<BuffBaseEntity>> dicAbyssalBlessingBuffsActivie = new DictionaryList<AbyssalBlessingEntityBean, List<BuffBaseEntity>>();
    //缓存：馈赠池内是否含"动态率属性馈赠"(BuffEntityAttributeDynamicRate)。仅在馈赠池变化时(增/替换/清空)更新，供死亡/新建魔物等高频事件守卫 O(1) 读取，避免每次遍历
    public bool hasDynamicRateAbyssalBlessing = false;
    //活跃的战斗生物BUFF
    public DictionaryList<string, List<BuffBaseEntity>> dicFightCreatureBuffsActivie = new DictionaryList<string, List<BuffBaseEntity>>();

    //BUFFEntity缓存池
    public Dictionary<string, Queue<BuffBaseEntity>> dicBuffEntityPool = new Dictionary<string, Queue<BuffBaseEntity>>();
    //BuffBean缓存池
    public Queue<BuffEntityBean> queueBuffEntityPool = new Queue<BuffEntityBean>();

    //buffPre的实例类 永久保存 不清除
    public Dictionary<long, BuffBasePreEntity> dicBuffPreEntity = new Dictionary<long, BuffBasePreEntity>();

    #region 数据清理
    /// <summary>
    /// 清理深渊馈赠数据
    /// </summary>
    public void ClearAbyssalBlessing()
    {
        ClearBuffCollection(dicAbyssalBlessingBuffsActivie.List);
        dicAbyssalBlessingBuffsActivie.Clear();
        //池清空，动态率馈赠缓存复位
        hasDynamicRateAbyssalBlessing = false;
    }

    /// <summary>
    /// 清理战斗生物Buff数据
    /// </summary>
    public void ClearFightCreatureBuff()
    {
        ClearBuffCollection(dicFightCreatureBuffsActivie.List);
        dicFightCreatureBuffsActivie.Clear();
    }
    
    /// <summary>
    /// 清理所有BUFF数据
    /// </summary>
    public void ClearAll()
    {
        ClearAbyssalBlessing();
        ClearFightCreatureBuff();

        dicBuffEntityPool.Clear();
        queueBuffEntityPool.Clear();
    }

    /// <summary>
    /// 清理BUFF集合
    /// </summary>
    /// <remarks>
    /// 只负责回收entity到对象池并清空内层list内容，不修改外层结构。
    /// 调用方在调用本方法后必须自行调用 dicXxx.Clear() 以保证DictionaryList反向映射一致。
    /// </remarks>
    protected void ClearBuffCollection(List<List<BuffBaseEntity>> listBuffCollection)
    {
        for (int i = 0; i < listBuffCollection.Count; i++)
        {
            var listBuff = listBuffCollection[i];
            if (listBuff == null) continue;
            for (int f = 0; f < listBuff.Count; f++)
            {
                var itemBuff = listBuff[f];
                if (itemBuff == null) continue;
                //先标记失效，让残留事件回调跳过
                if (itemBuff.buffEntityData != null)
                {
                    itemBuff.buffEntityData.isValid = false;
                }
                //仅回收到对象池，不动外层listBuff结构
                RemoveBuffEntity(itemBuff);
            }
            listBuff.Clear();
        }
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
        targetEntityBean.ClearData();
        queueBuffEntityPool.Enqueue(targetEntityBean);
    }

    /// <summary>
    /// 删除指定BUFF 并添加到缓存池pool
    /// </summary>
    public void RemoveBuffEntity(List<BuffBaseEntity> listBuffEntity, BuffBaseEntity targetBuffEntity)
    {
        listBuffEntity.Remove(targetBuffEntity);
        //移除数据到缓存
        RemoveBuffEntity(targetBuffEntity);
    }

    /// <summary>
    /// 移除buffentity
    /// </summary>
    public void RemoveBuffEntity(BuffBaseEntity itemBuffEntity)
    {
        //先获取bean引用（ClearData会将其置null）
        var beanToRecycle = itemBuffEntity.buffEntityData;
        //先清理entity数据：注销事件、置空buffEntityData，避免回收过程中残留回调访问已回收的bean
        itemBuffEntity.ClearData();
        //再回收entityBean
        RemoveBuffEntityBean(beanToRecycle);
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
    public BuffEntityBean GetBuffEntityBean(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        BuffEntityBean targetEntityBean = null;
        if (queueBuffEntityPool.Count > 0)
        {
            targetEntityBean = queueBuffEntityPool.Dequeue();
            targetEntityBean.SetData(buffData, applierCreatureId, targetCreatureId);
        }
        if (targetEntityBean == null)
        {
            targetEntityBean = new BuffEntityBean(buffData, applierCreatureId, targetCreatureId);
        }
        return targetEntityBean;
    }

    /// <summary>
    /// 创建buffentity
    /// </summary>
    public BuffBaseEntity GetBuffEntity(BuffEntityBean buffEntity)
    {
        BuffInfoBean buffInfo = buffEntity.GetBuffInfo();
        string className = $"{buffInfo.class_entity}";
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
    
    /// <summary>
    /// 创建buffentity
    /// </summary>
    public BuffBaseEntity GetBuffEntity(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        var buffEntityBean = GetBuffEntityBean(buffData, applierCreatureId, targetCreatureId);
        var buffEntity = GetBuffEntity(buffEntityBean);
        return buffEntity;
    }
    #endregion

    #region 深渊馈赠BUFF
    /// <summary>
    /// 获取指定深渊馈赠的BUFF
    /// </summary>
    public List<BuffBaseEntity> GetAbyssalBlessingBuffsActivie(AbyssalBlessingEntityBean abyssalBlessingEntityBean)
    {
        if (dicAbyssalBlessingBuffsActivie.TryGetValue(abyssalBlessingEntityBean, out var abyssalBlessingBuffsActivieList))
        {
            return abyssalBlessingBuffsActivieList;
        }
        return null;
    }
    #endregion

    #region 生物BUFF
    /// <summary>
    /// 获取指定生物的buff
    /// </summary>
    public List<BuffBaseEntity> GetFightCreatureBuffsActivie(string creatureUUId)
    {
        if (dicFightCreatureBuffsActivie.TryGetValue(creatureUUId, out var creatureBuffsActivieList))
        {
            return creatureBuffsActivieList;
        }
        return null;
    }
    #endregion
}