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
        //周期性触发，有次数限制
        if (triggerNum > 0)
        {
            if (buffEntityData.timeUpdate >= triggerTime)
            {
                buffEntityData.timeUpdate = 0;
                //减少可触发次数
                buffEntityData.triggerNumLeft--;
                //周期性触发，有次数限制
                TriggerBuffPecurrent(buffEntityData);
                //如果剩余触发次数没了 则删除BUFF
                if (buffEntityData.triggerNumLeft <= 0)
                {
                    buffEntityData.isValid = false;
                }
            }
        }
        //倒计时结束后触发一次
        else if (triggerNum == 0)
        {
            if (buffEntityData.timeUpdate >= triggerTime)
            {
                buffEntityData.timeUpdate = 0;
                TriggerBuffExpire(buffEntityData);
                buffEntityData.isValid = false;
            }
        }
        //周期性触发，无次数限制
        else
        {
            if (buffEntityData.timeUpdate >= triggerTime)
            {
                buffEntityData.timeUpdate = 0;
                TriggerBuffPeriodic(buffEntityData);
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
    ///  倒计时结束后触发一次
    /// </summary>
    public virtual bool TriggerBuffExpire(BuffEntityBean buffEntityData)
    {
         return TriggerBuff(buffEntityData);
    }

    /// <summary>
    /// 周期性触发，有次数限制
    /// </summary>
    public virtual bool TriggerBuffPecurrent(BuffEntityBean buffEntityData)
    {
        return TriggerBuff(buffEntityData);
    }  

    /// <summary>
    /// 周期性触发，无次数限制
    /// </summary>
    public virtual bool TriggerBuffPeriodic(BuffEntityBean buffEntityData)
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
    /// 获取BUFF的目标生物
    /// </summary>
    /// <returns></returns>
    public virtual FightCreatureEntity GetFightCreatureEntityForTarget()
    {
        //获取指定生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightCreatureEntity = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureUUId, CreatureFightTypeEnum.None);
        return fightCreatureEntity;
    }
    
    /// <summary>
    /// 获取BUFF的施加生物
    /// </summary>
    /// <returns></returns>
    public virtual FightCreatureEntity GetFightCreatureEntityForApplier()
    {
        //获取指定生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var fightCreatureEntity = gameFightLogic.fightData.GetCreatureById(buffEntityData.applierCreatureUUId, CreatureFightTypeEnum.None);
        return fightCreatureEntity;
    }

    /// <summary>
    /// 展示BUFF粒子
    /// </summary>
    public virtual void ShowBuffEffect()
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        if(buffInfo == null)
            return;
        if (buffInfo.trigger_effect == 0)
            return; 
        //获取指定生物
        GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        var targetCreature = gameFightLogic.fightData.GetCreatureById(buffEntityData.targetCreatureUUId, CreatureFightTypeEnum.None);
        if (targetCreature == null || targetCreature.fightCreatureData == null)
        {
            return;
        }
        EffectHandler.Instance.ShowEffect(buffInfo.trigger_effect, targetCreature.creatureObj.transform.position);
    }

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
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
    public virtual Color GetChangeBodyColor(BuffEntityBean buffEntityData)
    {
        var buffInfo = buffEntityData.GetBuffInfo();
        return buffInfo.GetBodyColor();
    }
    #endregion
}
