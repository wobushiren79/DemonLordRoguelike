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

    public float moveSpeedCurrent;//当前移动速度
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

        InitBaseAttribute();
    }

    /// <summary>
    /// 初始化基础属性
    /// </summary>
    public void InitBaseAttribute(Action actionForComplete = null)
    {
        moveSpeedCurrent = creatureData.GetMSPD();
        colorBodyCurrent = Color.white;
        if (!listBuffEntityData.IsNull())
        {
            float changeMoveSpeed = 0;
            float changeRateMoveSpeed = 0;
            for (int i = 0; i < listBuffEntityData.Count; i++)
            {
                BuffEntityBean itemBuff = listBuffEntityData[i];
                //获取buff执行实例
                var buffEntity = itemBuff.GetBuffEntity();
                //设置速度相关
                var targetChangeData = buffEntity.GetChangeDataForMoveSpeed(itemBuff);
                changeMoveSpeed += targetChangeData.change;
                changeRateMoveSpeed += targetChangeData.changeRate;
                //设置身体颜色
                if (!itemBuff.buffInfo.color_body.IsNull())
                {
                    colorBodyCurrent = itemBuff.buffInfo.GetBodyColor();
                }
            }
            moveSpeedCurrent += changeMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
            moveSpeedCurrent *= 1 + changeRateMoveSpeed;
            if (moveSpeedCurrent < 0)
                moveSpeedCurrent = 0;
        }
        if (moveSpeedCurrent < 0)
            moveSpeedCurrent = 0;
        actionForComplete?.Invoke();
    }

    /// <summary>
    /// 获取角色移动速度
    /// </summary>
    /// <returns></returns>
    public float GetMoveSpeed()
    {
        return moveSpeedCurrent;
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
