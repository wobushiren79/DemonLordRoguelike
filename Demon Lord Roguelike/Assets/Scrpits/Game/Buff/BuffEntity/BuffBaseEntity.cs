using System;
using UnityEditor;
using UnityEngine;

public class BuffBaseEntity
{
    public BuffEntityBean buffEntityData;

    #region 数据相关
    public virtual void SetData(BuffEntityBean buffEntityData)
    {
        this.buffEntityData = buffEntityData;
    }

    public virtual void ClearData()
    {
        if (buffEntityData != null)
            BuffHandler.Instance.manager.RemoveBuffEntityBean(buffEntityData);
    }
    #endregion

    #region  Update
    /// <summary>
    /// buff持续时间增加
    /// </summary>
    public virtual void UpdateBuffTime(float buffTime)
    {
        buffEntityData.timeUpdateTotal += buffTime;
        buffEntityData.timeUpdate += buffTime;
        float triggerTime = buffEntityData.GetTriggerTime();
        int triggerNum = buffEntityData.GetTriggerNum();
        //触发式BUFF（指定时间后触发 达到触发次数max之后结束）
        if (triggerNum > 0)
        {
            if (buffEntityData.timeUpdate >= triggerTime)
            {
                buffEntityData.timeUpdate = 0;
                //减少可触发次数
                buffEntityData.triggerNumLeft--;
                //周期性触发BUFF
                TriggerBuffPeriodic(buffEntityData);
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
            if (triggerTime <= 0)
            {

            }
            //如果不是永久存在
            else
            {
                if (buffEntityData.timeUpdate >= triggerTime)
                {
                    buffEntityData.timeUpdate = 0;
                    TriggerBuffPersistent(buffEntityData);
                    buffEntityData.isValid = false;
                }
            }
        }
    }
    #endregion

    #region 触发
    /// <summary>
    /// 条件性触发BUFF 不受时间影响
    /// </summary>
    public virtual bool TriggerBuffConditional(BuffEntityBean buffEntityData)
    {
        return TriggerBuff(buffEntityData);
    }  

    /// <summary>
    /// 初始化触发BUFF 
    /// </summary>
    /// <param name="buffEntityData"></param>
    /// <returns></returns>
    public virtual bool TriggerBuffInstant(BuffEntityBean buffEntityData)
    {
        return TriggerBuff(buffEntityData);
    }

    /// <summary>
    /// 周期性触发BUFF 触发次数
    /// </summary>
    public virtual bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
    {
        return TriggerBuff(buffEntityData);
    }  

    /// <summary>
    /// 持续型触发BUFF 时间触发
    /// </summary>
    public virtual bool TriggerBuffPersistent(BuffEntityBean buffEntityData)
    {
        return TriggerBuff(buffEntityData);
    }  

    /// <summary>
    /// 触发BUFF
    /// </summary>
    public virtual bool TriggerBuff(BuffEntityBean buffEntityData)
    {
        float triggerChance = buffEntityData.GetTriggerChance();
        var itemBuffChance = triggerChance;
        if (itemBuffChance > 0)
        {
            var randomOdds = UnityEngine.Random.Range(0f, 1f);
            if (randomOdds >= itemBuffChance)
            {
                return false;
            }
        }
        ShowBuffEffect();
        return true;
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 展示BUFF粒子
    /// </summary>
    public virtual void ShowBuffEffect()
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        if (buffInfo.trigger_effect == 0)
            return; 
        //获取指定生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureId, CreatureFightTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return;
        }
        EffectHandler.Instance.ShowEffect(buffInfo.trigger_effect, targetCreature.creatureObj.transform.position);
    }

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    /// <param name="buffEntityData"></param>
    /// <returns></returns>
    public bool CheckIsPre(BuffEntityBean buffEntityData)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        var preInfo = buffInfo.GetPreInfo();
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

    /// <summary>
    /// 获取BUFF改变身体颜色
    /// </summary>
    /// <param name="buffEntityData"></param>
    /// <returns></returns>
    public virtual Color GetChangeBodyColor(BuffEntityBean buffEntityData)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        return buffInfo.GetBodyColor();
    }
    #endregion
}
