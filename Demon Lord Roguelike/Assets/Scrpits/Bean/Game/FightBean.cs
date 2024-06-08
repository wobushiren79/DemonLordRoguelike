using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FightBean
{
    public float gameTime = 0;//游戏时间
    public float gameSpeed = 1;//游戏速度

    public int currentMagic;//当前魔力值、

    public List<FightCreatureBean> listDefCreatureData=new List<FightCreatureBean>();//当前可用防御生物数据
    public Dictionary<Vector3Int, FightPositionBean> dicFightPosition = new Dictionary<Vector3Int, FightPositionBean>();//放置在场上的生物数据

    public FightAttCreateBean fightAttCreateData;//进攻数据
    public List<GameFightCreatureEntity> listAttCreatureEntity = new List<GameFightCreatureEntity>();//进攻方的所有生物实例

    /// <summary>
    /// 获取进攻生物 进攻波次初始化数据
    /// </summary>
    public void GetAttCreateInitData(out int fightNum)
    {
        fightNum = 0;
        if (fightAttCreateData == null)
            return;
        if (!fightAttCreateData.dicDetailsData.IsNull())
        {
            fightNum = fightAttCreateData.dicDetailsData.Count;
        }
    }

    /// <summary>
    /// 改变魔力
    /// </summary>
    public void ChangeMagic(int changeData)
    {
        currentMagic += changeData;
        if (currentMagic < 0)
            currentMagic = 0;
    }

    /// <summary>
    /// 检测指定战斗位置上是否有生物
    /// </summary>
    public bool CheckFightPositionHasCreature(Vector3Int targetPos)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            if (targetPositionData.creatureMain != null)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 设置战斗位置数据
    /// </summary>
    public void SetFightPosition(Vector3Int targetPos, GameFightCreatureEntity fightCreature)
    {
        if (dicFightPosition.TryGetValue(targetPos, out FightPositionBean targetPositionData))
        {
            targetPositionData.creatureMain = fightCreature;
        }
        else
        {
            FightPositionBean newPositionData = new FightPositionBean();
            newPositionData.creatureMain = fightCreature;
            dicFightPosition.Add(targetPos, newPositionData);
        }
    }
}
