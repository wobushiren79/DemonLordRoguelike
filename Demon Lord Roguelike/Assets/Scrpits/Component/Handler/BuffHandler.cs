using System;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public partial class BuffHandler : BaseHandler<BuffHandler, BuffManager>
{
    /// <summary>
    /// 更新数据
    /// </summary>
    public void UpdateData(float updateTime)
    {
        //战斗生物BUFF更新处理
        var fightCreatureBuffs = manager.dicFightCreatureBuffsActivie.List;
        if (fightCreatureBuffs.Count > 0)
        {
            for (int i = 0; i < fightCreatureBuffs.Count; i++)
            {
                List<BuffBaseEntity> listBuff = fightCreatureBuffs[i];
                //如果都删完了。再删除这个生物
                if (UpdateForActivieBuffs(updateTime, listBuff))
                {
                    manager.dicFightCreatureBuffsActivie.RemoveByValue(listBuff);
                }
            }
        }

        //深渊馈赠BUFF更新处理
        var abyssalBlessingBuffs = manager.dicAbyssalBlessingBuffsActivie.List;
        if (abyssalBlessingBuffs.Count > 0)
        {
            for (int i = 0; i < abyssalBlessingBuffs.Count; i++)
            {
                List<BuffBaseEntity> listBuff = abyssalBlessingBuffs[i];
                //如果都删完了。馈赠暂时不处理
                if (UpdateForActivieBuffs(updateTime, listBuff))
                {

                }
            }
        }
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    public bool UpdateForActivieBuffs(float updateTime, List<BuffBaseEntity> listBuff)
    {
        if (!listBuff.IsNull())
        {
            for (int f = listBuff.Count - 1; f >= 0; f--)
            {
                BuffBaseEntity itemBuffEntity = listBuff[f];
                //如果BUFF已经无效 则删除
                if (itemBuffEntity.buffEntityData.isValid == false)
                {
                    manager.RemoveBuffEntity(listBuff, itemBuffEntity);
                    continue;
                }
                itemBuffEntity.UpdateBuffTime(updateTime);
            }
            if (listBuff.Count == 0)
                return true;
            else
                return false;
        }
        else
            return true;
    }

    #region 深渊馈赠BUFF
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
            BuffBean buffData = new BuffBean(buffId);
            var buffEntity = manager.GetBuffEntity(buffData, abyssalBlessingEntityData.abyssalBlessingUUID, abyssalBlessingEntityData.abyssalBlessingUUID);
            listBuffEntity.Add(buffEntity);
        }
        manager.dicAbyssalBlessingBuffsActivie.Add(abyssalBlessingEntityData, listBuffEntity);
        //事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_AbyssalBlessingChange, abyssalBlessingEntityData);
    }
    #endregion


    #region  战斗生物BUFF

    /// <summary>
    /// 移除战斗生物BUFF
    /// </summary>
    public void RemoveFightCreatureBuffs(string creatureId)
    {
        List<BuffBaseEntity> listBuffBaseEntity = manager.GetFightCreatureBuffsActivie(creatureId);
        if (!listBuffBaseEntity.IsNull())
        {
            for (int i = 0; i < listBuffBaseEntity.Count; i++)
            {
                listBuffBaseEntity[i].buffEntityData.isValid = false;
            }
        }
    }

    /// <summary>
    /// 添加BUFF 带有触发概率的判定
    /// </summary>
    public bool AddFightCreatureBuff(List<BuffBean> listBuffData, string applierCreatureId, string targetCreatureId)
    {
        if (listBuffData.IsNull())
        {
            return false;
        }
        //获取触发的buff 计算触发概率
        var listBuffEntityBean = CheckBuffCreate(listBuffData, applierCreatureId, targetCreatureId);
        if (!listBuffEntityBean.IsNull())
        {
            AddFightCreatureBuff(listBuffEntityBean, applierCreatureId, targetCreatureId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 添加BUFF
    /// </summary>
    public void AddFightCreatureBuff(List<BuffEntityBean> addListBuffEntityData, string applierCreatureId, string targetCreatureId)
    {
        //获取当前生物的BUFF
        List<BuffBaseEntity> listBuffEntityActivie = manager.GetFightCreatureBuffsActivie(targetCreatureId);
        //遍历需要添加的BUFF
        for (int i = 0; i < addListBuffEntityData.Count; i++)
        {
            var itemAddBuffEntityBean = addListBuffEntityData[i];
            //如果当前生物没有BUFF 则直接添加----------------------------------
            if (listBuffEntityActivie == null)
            {
                var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                List<BuffBaseEntity> listBuffEntityNew = new List<BuffBaseEntity>() { buffEntity };
                manager.dicFightCreatureBuffsActivie.Add(targetCreatureId, listBuffEntityNew);
            }
            //如果当前生物有BUFF列表 但是是空----------------------------------
            else if (listBuffEntityActivie.Count == 0)
            {
                var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                listBuffEntityActivie.Add(buffEntity);
            }
            //如果当前生物有BUFF----------------------------------
            else
            {
                //判断一下是否有相同的buff
                bool hadBuff = false;
                for (int f = 0; f < listBuffEntityActivie.Count; f++)
                {
                    var buffEntityActivie = listBuffEntityActivie[f];
                    //如果有相同的BUFF 则只是刷新时间和次数 
                    if (buffEntityActivie.buffEntityData.buffId == itemAddBuffEntityBean.buffId)
                    {
                        hadBuff = true;
                        buffEntityActivie.buffEntityData.triggerNumLeft = itemAddBuffEntityBean.triggerNumLeft;
                        //如果不是次数触发型 则刷新时间
                        int triggerNum = buffEntityActivie.buffEntityData.GetTriggerNum();
                        if (triggerNum <= 0)
                        {
                            buffEntityActivie.buffEntityData.timeUpdate = itemAddBuffEntityBean.timeUpdate;
                        }
                        break;
                    }
                }
                //如果没有相同的buff 则添加一个新的
                if (!hadBuff)
                {
                    var buffEntity = manager.GetBuffEntity(itemAddBuffEntityBean);
                    listBuffEntityActivie.Add(buffEntity);
                }
                else
                {
                    //移除数据到缓存
                    manager.RemoveBuffEntityBean(itemAddBuffEntityBean);
                }
            }
        }
        //发送事件通知
        EventHandler.Instance.TriggerEvent(EventsInfo.Buff_FightCreatureChange, applierCreatureId, targetCreatureId);
    }
    #endregion

    #region 触发BUFF判定
    /// <summary>
    /// 尝试创建buff，根据创建概率看是否创建成功
    /// </summary>
    public List<BuffEntityBean> CheckBuffCreate(List<BuffBean> listBuffData, string applierCreatureId, string targetCreatureId)
    {
        List<BuffEntityBean> listData = new List<BuffEntityBean>();
        for (int i = 0; i < listBuffData.Count; i++)
        {
            var item = listBuffData[i];
            var targetEntityBean = CheckBuffCreate(item, applierCreatureId, targetCreatureId);
            listData.Add(targetEntityBean);
        }
        return listData;
    }

    /// <summary>
    /// 尝试创建buff，根据创建概率看是否创建成功
    /// </summary>
    public BuffEntityBean CheckBuffCreate(BuffBean buffData, string applierCreatureId, string targetCreatureId)
    {
        if (buffData.createRate > 0 && buffData.createRate < 1)
        {
            var randomRate = UnityEngine.Random.Range(0f, 1f);
            if (randomRate >= buffData.createRate)
            {
                return null;
            }
        }
        BuffEntityBean buffEntityData = manager.GetBuffEntityBean(buffData, applierCreatureId, targetCreatureId);
        return buffEntityData;
    }
    #endregion
}