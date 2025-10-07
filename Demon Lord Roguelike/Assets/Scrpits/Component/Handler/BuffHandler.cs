using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffHandler : BaseHandler<BuffHandler, BuffManager>
{

    /// <summary>
    /// 设置生物BUFF是否有效 用于删除BUFF
    /// </summary>
    /// <param name="creatureId"></param>
    /// <param name="isValid"></param>
    public void SetCreatureBuffsActivieIsValid(string creatureId, bool isValid)
    {
        List<BuffBaseEntity> listBuffBaseEntity = manager.GetCreatureBuffsActivie(creatureId);
        if (!listBuffBaseEntity.IsNull())
        {
            for (int i = 0; i < listBuffBaseEntity.Count; i++)
            {
                listBuffBaseEntity[i].buffEntityData.isValid = isValid;
            }
        }
    }

    /// <summary>
    /// 更新BUFF
    /// </summary>
    /// <param name="updateTime"></param>
    public void UpdateData(float updateTime)
    {
        if (manager.dicCreatureBuffsActivie.List.Count > 0)
        {
            for (int i = 0; i < manager.dicCreatureBuffsActivie.List.Count; i++)
            {
                List<BuffBaseEntity> listCreatureBuff = manager.dicCreatureBuffsActivie.List[i];
                for (int f = 0; f < listCreatureBuff.Count; f++)
                {
                    BuffBaseEntity itemBuffEntity = listCreatureBuff[f];
                    //如果BUFF已经无效 则删除
                    if (itemBuffEntity.buffEntityData.isValid == false)
                    {
                        manager.RemoveCreatureBuffsActivie(listCreatureBuff, itemBuffEntity);
                        f--;
                        continue;
                    }
                    itemBuffEntity.AddBuffTime(updateTime);
                }
                //如果都删完了。再删除这个生物
                if (listCreatureBuff.Count == 0)
                {
                    manager.dicCreatureBuffsActivie.RemoveByValue(listCreatureBuff);
                }
            }
        }
    }

    /// <summary>
    /// 添加buff
    /// </summary>
    /// <param name="buffEntity"></param>
    public bool AddBuff(long[] buffIds, string applierCreatureId, string targetCreatureId)
    {
        if (buffIds.IsNull())
        {
            return false;
        }
        //获取触发的buff 计算触发概率
        var buffsTrigger = CheckTriggerBuff(buffIds, applierCreatureId, targetCreatureId);
        if (!buffsTrigger.IsNull())
        {
            AddBuff(applierCreatureId, targetCreatureId, buffsTrigger);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 添加BUFF
    /// </summary>
    public void AddBuff(string applierCreatureId, string targetCreatureId, List<BuffEntityBean> targetListBuffEntityData)
    {
        //如果以后的数据
        List<BuffBaseEntity> listBuffEntityActivie = manager.GetCreatureBuffsActivie(targetCreatureId);
        for (int i = 0; i < targetListBuffEntityData.Count; i++)
        {
            var itemBuffEntityData = targetListBuffEntityData[i];
            if (listBuffEntityActivie == null)
            {
                var buffEntity = manager.CreateCreatureBuffs(itemBuffEntityData);
                List<BuffBaseEntity> listBuffEntityNew = new List<BuffBaseEntity>() { buffEntity };
                manager.dicCreatureBuffsActivie.Add(targetCreatureId, listBuffEntityNew);
            }
            else if (listBuffEntityActivie.Count == 0)
            {
                var buffEntity = manager.CreateCreatureBuffs(itemBuffEntityData);
                listBuffEntityActivie.Add(buffEntity);
            }
            else
            {
                //判断一下是否有相同的buff
                bool hasOldBuff = false;
                for (int f = 0; f < listBuffEntityActivie.Count; f++)
                {
                    var buffEntityActivie = listBuffEntityActivie[f];
                    //如果有相同的BUFF 则只是刷新时间和次数 
                    if (buffEntityActivie.buffEntityData.buffInfo.id == itemBuffEntityData.buffInfo.id)
                    {
                        hasOldBuff = true;
                        buffEntityActivie.buffEntityData.triggerNumLeft = itemBuffEntityData.triggerNumLeft;
                        //如果不是次数触发型 则刷新时间
                        if (buffEntityActivie.buffEntityData.buffInfo.trigger_num <= 0)
                        {
                            buffEntityActivie.buffEntityData.timeUpdate = itemBuffEntityData.timeUpdate;
                        }
                        break;
                    }
                }
                //如果没有相同的buff 则添加一个新的
                if (!hasOldBuff)
                {
                    var buffEntity = manager.CreateCreatureBuffs(itemBuffEntityData);
                    listBuffEntityActivie.Add(buffEntity);
                }
                else
                {
                    //移除数据到缓存
                    manager.RemoveBuffEntityBean(itemBuffEntityData);
                }
            }
        }
    
        //刷新一下生物属性
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        GameFightCreatureEntity gameFightCreatureEntity = gameFightLogic.fightData.GetCreatureById(targetCreatureId);
        gameFightCreatureEntity.fightCreatureData.RefreshBaseAttribute();
    }



    /// <summary>
    /// 尝试触发buff，根据触发概率看是否触发成功
    /// </summary>
    public List<BuffEntityBean> CheckTriggerBuff(long[] buffIds, string applierCreatureId, string targetCreatureId)
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

            BuffEntityBean buffData = manager.CreateBuffEntityBean(itemBuffId, applierCreatureId, targetCreatureId);
            listData.Add(buffData);
        }
        return listData;
    }
}