using System.Collections.Generic;
using UnityEngine;

public static class BuffUtil
{
    
    //buff的实例类
    public static Dictionary<long, BuffBaseEntity> dicBuffEntity = new Dictionary<long, BuffBaseEntity>();

    //buffpre的实例类
    public static Dictionary<long, BuffBasePreEntity> dicBuffPreEntity = new Dictionary<long, BuffBasePreEntity>();

    /// <summary>
    /// 获取BUFFPre实例类
    /// </summary>
    public static BuffBasePreEntity GetBuffPreEntity(BuffPreInfoBean buffPreInfo)
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
    /// 获取BUFF实例类
    /// </summary>
    public static BuffBaseEntity GetBuffEntity(BuffInfoBean buffInfo)
    {
        string className = $"{buffInfo.class_entity}";
        if (dicBuffEntity.TryGetValue(buffInfo.id, out var targetClass))
        {
            return targetClass;
        }
        else
        {
            targetClass = ReflexUtil.CreateInstance<BuffBaseEntity>(className);
            if (targetClass == null)
            {
                return null;
            }
            else
            {
                dicBuffEntity.Add(buffInfo.id, targetClass);
                return targetClass;
            }
        }
    }

    /// <summary>
    /// 获取触发的BUFF
    /// </summary>
    /// <param name="buffIds"></param>
    /// <param name="creatureId"></param>
    /// <returns></returns>
    public static List<BuffEntityBean> GetTriggerBuff(long[] buffIds, string creatureId)
    {
        List<BuffEntityBean> listData = new List<BuffEntityBean>();
        for (int i = 0; i < buffIds.Length; i++)
        {
            var itemBuffId = buffIds[i];
            var buffInfo = BuffInfoCfg.GetItemData(itemBuffId);
            if (buffInfo.trigger_chance > 0)
            {
                var randomOdds = UnityEngine.Random.Range(0f, 1f);
                if (randomOdds >= buffInfo.trigger_chance)
                {
                    continue;
                }
            }

            BuffEntityBean buffData = new BuffEntityBean(creatureId,itemBuffId);
            listData.Add(buffData);
        }
        return listData;
    }
}
