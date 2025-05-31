using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightCreatureBean
{
    public CreatureBean creatureData;    //生物数据
    public Vector3Int positionCreate;//生成位置（用于防守生物）
    public int roadIndex;//当前道路（用于进攻生物）

    public int HPCurrent;//当前生命值
    public int HPMax;//最大生命值

    public int DRCurrent;//当前护甲值
    public int DRMax;//最大护甲值

    public int ATKCurrent;//当前攻击力
    public float ASPDCurrent;//当前攻击间隔
    public float MSPDCurrent;//当前移动速度
    public float EVACurrent;//当前闪避率
    public float CRTCurrent;//暴击率

    public Color colorBodyCurrent;//当前身体颜色
    public List<BuffEntityBean> listBuffEntityData;//所有的buff实例
    public FightCreatureBean(int creatureId)
    {
        creatureData = new CreatureBean(creatureId);
        ResetData();
    }

    public FightCreatureBean(CreatureBean creatureData)
    {
        this.creatureData = creatureData;
        ResetData();
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        var creatureInfo = creatureData.creatureInfo;
        HPCurrent = creatureInfo.GetHP();
        HPMax = creatureInfo.GetHP();

        DRCurrent = creatureInfo.GetDR();
        DRMax = creatureInfo.GetDR();

        listBuffEntityData = new List<BuffEntityBean>();
        //刷新一下基础属性
        InitBaseAttribute();
    }

    /// <summary>
    /// 初始化基础属性
    /// </summary>
    public void InitBaseAttribute(Action actionForComplete = null)
    {
        //先还原基础数据
        MSPDCurrent = creatureData.GetMSPD();
        CRTCurrent = creatureData.GetCRT();
        EVACurrent = creatureData.GetEVA();
        ATKCurrent = creatureData.GetATK();
        ASPDCurrent = creatureData.GetASPD();

        colorBodyCurrent = Color.white;

        //buff相关加成
        if (!listBuffEntityData.IsNull())
        {
            for (int i = 0; i < listBuffEntityData.Count; i++)
            {
                BuffEntityBean itemBuff = listBuffEntityData[i];
                //获取buff执行实例
                var buffEntity = itemBuff.GetBuffEntity();
                //设置攻击力相关
                ATKCurrent += (int)buffEntity.GetChangeDataForATK(itemBuff);
                ATKCurrent = ATKCurrent + (int)(ATKCurrent * buffEntity.GetChangeRateDataForATK(itemBuff));
                //设置攻击速度相关
                ASPDCurrent += buffEntity.GetChangeDataForASPD(itemBuff);
                ASPDCurrent = ASPDCurrent + (ASPDCurrent * buffEntity.GetChangeRateDataForASPD(itemBuff));
                //设置移动速度相关
                MSPDCurrent += buffEntity.GetChangeDataForMSPD(itemBuff);
                MSPDCurrent *= 1 + buffEntity.GetChangeRateDataForMSPD(itemBuff);
                //设置暴击相关
                CRTCurrent += buffEntity.GetChangeRateDataForCRT(itemBuff);
                //设置闪避相关
                EVACurrent += buffEntity.GetChangeRateDataForEVA(itemBuff);

                //设置身体颜色
                if (!itemBuff.buffInfo.color_body.IsNull())
                {
                    colorBodyCurrent = buffEntity.GetChangeBodyColor(itemBuff);
                }
            }
        }
        if (ATKCurrent < 0)
            ATKCurrent = 0;
        if (ASPDCurrent < 0)
            ASPDCurrent = 0;
        if (MSPDCurrent < 0)
            MSPDCurrent = 0;
        if (CRTCurrent <= 0)
            CRTCurrent = 0;
        if (EVACurrent <= 0)
            EVACurrent = 0;

        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 获取攻击力
    /// </summary>
    /// <returns></returns>
    public int GetATK()
    {
        return ATKCurrent;
    }
    
    /// <summary>
    /// 获取攻击速度
    /// </summary>
    /// <returns></returns>
    public float GetASPD()
    {
        return ASPDCurrent;
    }

    /// <summary>
    /// 获取角色移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMSPD()
    {
        return MSPDCurrent;
    }

    /// <summary>
    /// 获取闪避概率
    /// </summary>
    /// <returns></returns>
    public float GetEVA()
    {
        return EVACurrent;
    }

    /// <summary>
    /// 获取暴击率
    /// </summary>
    public float GetCRT()
    {
        return CRTCurrent;
    }

    /// <summary>
    /// 获取身体颜色
    /// </summary>
    /// <returns></returns>
    public Color GetBodyColor()
    {
        return colorBodyCurrent;
    }


    public void ChangeDRAndHP(int changeValue,
        out int curDR, out int curHP,
        out int changeDRReal, out int changeHPReal)
    {
        changeDRReal = 0;
        changeHPReal = 0;

        //先改变护甲
        ChangeDR(changeValue, out int leftDR, out changeDRReal);
        //如果真实改变的护甲值==改变的值（没有冗余）
        if (changeValue == changeDRReal)
        {

        }
        //如果真实改变的护甲值!=改变的值有冗余（有冗余 需要再改变生命值）
        else
        {
            int changeValue2 = changeValue - changeDRReal;
            ChangeHP(changeValue2, out int leftHP, out changeHPReal);
        }
        curDR = DRCurrent;
        curHP = HPCurrent;
    }


    /// <summary>
    /// 改变护甲
    /// </summary>
    /// <param name="ChangeDR">改变值</param>
    /// <param name="leftDR">剩余值</param>
    /// <param name="changeDRReal">真实改变值</param>
    public void ChangeDR(int ChangeDR, out int leftDR, out int changeDRReal)
    {
        DRCurrent += ChangeDR;
        changeDRReal = ChangeDR;
        if (DRCurrent < 0)
        {
            changeDRReal = ChangeDR - DRCurrent;
            DRCurrent = 0;
        }
        if (DRCurrent > DRMax)
        {
            changeDRReal = ChangeDR - (DRCurrent - DRMax);
            DRCurrent = DRMax;
        }
        leftDR = DRCurrent;
        //刷新一下基础属性
        InitBaseAttribute();
    }


    /// <summary>
    /// 改变生命值
    /// </summary>
    /// <param name="ChangeHP">改变值</param>
    /// <param name="leftHP">剩余值</param>
    /// <param name="changeHPReal">真实改变值</param>
    public void ChangeHP(int ChangeHP, out int leftHP, out int changeHPReal)
    {
        HPCurrent += ChangeHP;
        changeHPReal = ChangeHP;
        if (HPCurrent < 0)
        {
            changeHPReal = ChangeHP - HPCurrent;
            HPCurrent = 0;
        }
        if (HPCurrent > HPMax)
        {
            changeHPReal = ChangeHP - (HPCurrent - HPMax);
            HPCurrent = HPMax;
        }
        leftHP = HPCurrent;
        //刷新一下基础属性
        InitBaseAttribute();
    }

    /// <summary>
    /// 添加BUFF
    /// </summary>
    public void AddBuff(List<BuffEntityBean> targetListBuffEntityData, Action<BuffEntityBean> actionForCombineNew = null, Action<BuffEntityBean> actionForCombineOld = null, Action actionForComplete = null)
    {
        for (int f = 0; f < targetListBuffEntityData.Count; f++)
        {
            var targetBuff = targetListBuffEntityData[f];
            bool hasOldBuff = false;
            for (int i = 0; i < listBuffEntityData.Count; i++)
            {
                var itemBuffEntityData = listBuffEntityData[i];
                if (itemBuffEntityData.buffInfo.id == targetBuff.buffInfo.id)
                {
                    hasOldBuff = true;
                    itemBuffEntityData.triggerNumLeft = targetBuff.triggerNumLeft;
                    actionForCombineOld?.Invoke(itemBuffEntityData);
                    break;
                }
            }
            if (!hasOldBuff)
            {
                listBuffEntityData.Add(targetBuff);
                actionForCombineNew?.Invoke(targetBuff);

            }
        }
        InitBaseAttribute(actionForComplete);
    }

    /// <summary>
    /// 移除BUFF
    /// </summary>
    public void RemoveBuff(BuffEntityBean buffEntityData, Action actionForComplete = null)
    {
        if (!listBuffEntityData.IsNull())
        {
            listBuffEntityData.Remove(buffEntityData);
        }
        InitBaseAttribute(actionForComplete);
    }

}
