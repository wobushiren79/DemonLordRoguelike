using System;
using UnityEditor;
using UnityEngine;

public class BuffBaseEntity
{
    public BuffEntityBean buffEntityData;

    public virtual void SetData(BuffEntityBean buffEntityData)
    {
        this.buffEntityData = buffEntityData;
    }

    public virtual void ClearData()
    {
        if(buffEntityData != null)
            BuffHandler.Instance.manager.RemoveBuffEntityBean(buffEntityData);
    }

    /// <summary>
    /// buff持续时间增加
    /// </summary>
    public virtual void AddBuffTime(float buffTime)
    {
        buffEntityData.timeUpdate += buffTime;
        //触发式BUFF（指定时间后触发 达到触发次数max之后结束）
        if (buffEntityData.buffInfo.trigger_num > 0)
        {
            if (buffEntityData.timeUpdate >= buffEntityData.buffInfo.trigger_time)
            {
                buffEntityData.timeUpdate = 0;
                //减少可触发次数
                buffEntityData.triggerNumLeft--;
                //触发BUFF
                TriggerBuff(buffEntityData);
                //如果剩余触发次数没了 则删除BUFF
                if (buffEntityData.triggerNumLeft <= 0)
                {
                    buffEntityData.isValid = false;
                }
            }
        }
        //持续型BUFF（持续指定时间后结束）
        else
        {
            //如果是永久存在
            if (buffEntityData.buffInfo.trigger_time < 0)
            {

            }
            //如果不是永久存在
            else
            {
                if (buffEntityData.timeUpdate >= buffEntityData.buffInfo.trigger_time)
                {
                    buffEntityData.timeUpdate = 0;
                    buffEntityData.isValid = false;
                }
            }
        }
    }

    /// <summary>
    /// 触发BUFF
    /// </summary>
    /// <param name="buffEntityData"></param>
    public virtual bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        var itemBuffChance = buffEntityData.buffInfo.trigger_chance;
        if (itemBuffChance > 0)
        {
            var randomOdds = UnityEngine.Random.Range(0f, 1f);
            if (randomOdds >= itemBuffChance)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    /// <param name="buffEntityData"></param>
    /// <returns></returns>
    public bool CheckIsPre(BuffEntityBean buffEntityData)
    {
        var preInfo = buffEntityData.buffInfo.GetPreInfo();
        if (preInfo.IsNull())
        {
            return true;
        }
        foreach (var itemData in preInfo)
        {
            var buffPreInfo = BuffPreInfoCfg.GetItemData(itemData.Key);
            var buffPreEntity = BuffHandler.Instance.manager.GetBuffPreEntity(buffPreInfo);
            if (!buffPreEntity.CheckIsPre(buffEntityData, itemData.Value))
            {
                return false;
            }
        }
        return true;
    }

    

    #region 身体颜色
    public virtual Color GetChangeBodyColor(BuffEntityBean buffEntityData)
    {
        return buffEntityData.buffInfo.GetBodyColor();
    }
    #endregion
}
