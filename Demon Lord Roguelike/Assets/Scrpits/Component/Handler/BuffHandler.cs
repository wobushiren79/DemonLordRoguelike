using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffHandler : BaseHandler<BuffHandler, BuffManager>
{
    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(float updateTime)
    {
        //生物BUFF更新处理
        if (manager.dicCreatureBuffsActivie.List.Count > 0)
        {
            for (int i = 0; i < manager.dicCreatureBuffsActivie.List.Count; i++)
            {
                List<BuffBaseEntity> listBuff = manager.dicCreatureBuffsActivie.List[i];
                for (int f = 0; f < listBuff.Count; f++)
                {
                    BuffBaseEntity itemBuffEntity = listBuff[f];
                    //如果BUFF已经无效 则删除
                    if (itemBuffEntity.buffEntityData.isValid == false)
                    {
                        manager.RemoveCreatureBuffActivie(listBuff, itemBuffEntity);
                        f--;
                        continue;
                    }
                    itemBuffEntity.AddBuffTime(updateTime);
                }
                //如果都删完了。再删除这个生物
                if (listBuff.Count == 0)
                {
                    manager.dicCreatureBuffsActivie.RemoveByValue(listBuff);
                }
            }
        }
        //深渊馈赠BUFF处理
        if (manager.dicAbyssalBlessingBuffsActivie.List.Count > 0)
        {
            for (int i = 0; i < manager.dicAbyssalBlessingBuffsActivie.List.Count; i++)
            {
                List<BuffBaseEntity> listBuff = manager.dicAbyssalBlessingBuffsActivie.List[i];
                for (int f = 0; f < listBuff.Count; f++)
                {
                    BuffBaseEntity itemBuffEntity = listBuff[f];
                    if (itemBuffEntity.buffEntityData.isValid == false)
                    {
                        //即刻触发 触发之后移除
                        manager.RemoveBuffEntity(itemBuffEntity);
                        f--;
                        continue;
                    }
                    itemBuffEntity.AddBuffTime(updateTime);
                }
            }
        }
    }

    #region 深渊馈赠
    /// <summary>
    /// 增加深渊馈赠
    /// </summary>
    public void AddAbyssalBlessing(AbyssalBlessingEntityBean abyssalBlessingEntityData)
    {
        AbyssalBlessingInfoBean abyssalBlessingInfo = abyssalBlessingEntityData.abyssalBlessingInfo;
        List<BuffBaseEntity> listBuffEntity = new List<BuffBaseEntity>();
        //创建BUFFEntity列表
        long[] buffIds = abyssalBlessingInfo.buff_ids.SplitForArrayLong(',');
        for (int i = 0; i < buffIds.Length; i++)
        {
            long buffId = buffIds[i];
            var buffEntityBean = manager.GetBuffEntityBean(buffId, abyssalBlessingEntityData.abyssalBlessingUUID, abyssalBlessingEntityData.abyssalBlessingUUID);
            var buffEntity = manager.GetBuffEntity(buffEntityBean);
            listBuffEntity.Add(buffEntity);
        }
        manager.dicAbyssalBlessingBuffsActivie.Add(abyssalBlessingEntityData.abyssalBlessingUUID, listBuffEntity);
    }
    #endregion


    #region  BUFF
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
    /// 添加buff
    /// </summary>
    public bool AddBuff(Dictionary<long, float> buffIds, string applierCreatureId, string targetCreatureId)
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
    /// 添加BUFF 必定添加成功
    /// </summary>
    public bool AddBuff(long[] buffIds, string applierCreatureId, string targetCreatureId)
    {
        if (buffIds.IsNull())
        {
            return false;
        }
        Dictionary<long, float> dicBuffIds = new Dictionary<long, float>();
        for (int i = 0; i < buffIds.Length; i++)
        {
            var buffId = buffIds[i];
            dicBuffIds.Add(buffId, 1);
        }
        AddBuff(dicBuffIds, applierCreatureId, targetCreatureId);
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
                var buffEntity = manager.GetBuffEntity(itemBuffEntityData);
                List<BuffBaseEntity> listBuffEntityNew = new List<BuffBaseEntity>() { buffEntity };
                manager.dicCreatureBuffsActivie.Add(targetCreatureId, listBuffEntityNew);
            }
            else if (listBuffEntityActivie.Count == 0)
            {
                var buffEntity = manager.GetBuffEntity(itemBuffEntityData);
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
                    var buffEntity = manager.GetBuffEntity(itemBuffEntityData);
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
    public List<BuffEntityBean> CheckTriggerBuff(Dictionary<long, float> buffIds, string applierCreatureId, string targetCreatureId)
    {
        List<BuffEntityBean> listData = new List<BuffEntityBean>();
        foreach (var item in buffIds)
        {
            var itemBuffId = item.Key;
            var itemBuffChance = item.Value;
            if (itemBuffChance > 0)
            {
                var randomOdds = UnityEngine.Random.Range(0f, 1f);
                if (randomOdds >= itemBuffChance)
                {
                    continue;
                }
            }

            BuffEntityBean buffData = manager.GetBuffEntityBean(itemBuffId, applierCreatureId, targetCreatureId);
            listData.Add(buffData);
        }
        return listData;
    }
    #endregion
}