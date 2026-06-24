using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserAscendBean
{
    public Dictionary<int, UserAscendDetailsBean> dicAscendData = new Dictionary<int, UserAscendDetailsBean>();

    /// <summary>
    /// 检测是否有进阶数据
    /// </summary>
    public bool CheckHasAscend(int index)
    {
        return dicAscendData.ContainsKey(index);
    }

    /// <summary>
    /// 获取进阶数据
    /// </summary>
    public UserAscendDetailsBean GetAscendData(int index)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            return targetData;
        }
        return null;
    }

    /// <summary>
    /// 添加进阶数据(开始进阶时一次性写入:目标稀有度/耗时上限/预定BUFF 等临时进阶数据,随存档序列化)
    /// </summary>
    /// <param name="index">容器序号</param>
    /// <param name="creatureData">进阶目标生物</param>
    /// <param name="targetRarity">进阶后的目标稀有度</param>
    /// <param name="timeMax">完成所需总时长(秒,按源稀有度查表)</param>
    /// <param name="ascendBuff">开始时已确定的预定BUFF(完成时才落地到生物;无对应类型可为 null)</param>
    /// <returns>新建的进阶详情数据</returns>
    public UserAscendDetailsBean AddAscendData(int index, CreatureBean creatureData, int targetRarity, float timeMax, BuffBean ascendBuff)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            LogUtil.LogError($"添加进阶数据错误,已经存在index_{index}的数据 progress_{targetData.progress} creatureUUId_{targetData.creatureUUId}");
        }
        UserAscendDetailsBean newData = new UserAscendDetailsBean();
        newData.progress = 0;
        newData.index = index;
        newData.creatureUUId = creatureData.creatureUUId;
        newData.targetRarity = targetRarity;
        newData.timeMax = timeMax;
        newData.ascendBuff = ascendBuff;
        dicAscendData[index] = newData;
        return newData;
    }

    /// <summary>
    /// 移除进阶数据(同时把目标生物状态从 Vat 复位为 Idle)
    /// </summary>
    public void RemoveAscendData(int index)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            var userData = GameDataHandler.Instance.manager.GetUserData();
            var targetCreature = userData.GetBackpackCreature(targetData.creatureUUId);
            if (targetCreature != null)
            {
                targetCreature.creatureState = CreatureStateEnum.Idle;
            }
            dicAscendData.Remove(index);
        }
        else
        {
            LogUtil.LogError($"溢出进阶数据错误,没有数据index_{index}");
        }
    }
}

[Serializable]
public class UserAscendDetailsBean
{
    /// <summary>容器序号</summary>
    public int index;
    /// <summary>已累积进度(秒);≥timeMax 即可完成</summary>
    public float progress;
    /// <summary>进阶目标生物UUID</summary>
    public string creatureUUId;
    /// <summary>进阶后的目标稀有度(完成时赋值给生物)</summary>
    public int targetRarity;
    /// <summary>完成所需总时长(秒,按源稀有度查表)</summary>
    public float timeMax;
    /// <summary>开始时已确定的预定BUFF(完成时才落地到生物 dicRarityBuff;无对应类型为 null)</summary>
    public BuffBean ascendBuff;

    /// <summary>
    /// 增加进度(秒)。被动tick每秒+1、魔晶加速每颗+1;不在此触发存档。
    /// </summary>
    public void AddProgress(float value = 1f)
    {
        progress += value;
    }

    /// <summary>
    /// 是否已完成(进度达到总时长)。timeMax≤0 视为不可进阶/异常,直接返回 false。
    /// </summary>
    public bool IsComplete()
    {
        if (timeMax <= 0)
        {
            return false;
        }
        return progress >= timeMax;
    }

    /// <summary>
    /// 获取归一化进度(0~1),供进度条/场景水色使用。timeMax≤0 时返回 0。
    /// </summary>
    public float GetProgressNormalized()
    {
        if (timeMax <= 0)
        {
            return 0;
        }
        return Mathf.Clamp01(progress / timeMax);
    }
}
