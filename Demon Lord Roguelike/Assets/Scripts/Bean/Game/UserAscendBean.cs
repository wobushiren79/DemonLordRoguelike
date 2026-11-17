using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserAscendBean
{
    public Dictionary<int, UserAscendDetailsBean> dicAscendData = new Dictionary<int, UserAscendDetailsBean>();

    #region 查询
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
    #endregion

    #region 增删
    /// <summary>
    /// 添加进阶数据(开始进阶时一次性写入:目标稀有度/耗时上限/开始时间戳/预定BUFF 等临时进阶数据,随存档序列化)。
    /// <para>目标生物本体(含装备)在此一并托管:置 Vat 状态、移出所有阵容、从背包物理移除——进阶期间任何背包列表类UI天然不可见不可操作,完成/取消时由 RemoveAscendData 归还背包。</para>
    /// </summary>
    /// <param name="index">容器序号</param>
    /// <param name="creatureData">进阶目标生物(本体随进阶数据托管)</param>
    /// <param name="targetRarity">进阶后的目标稀有度</param>
    /// <param name="timeMax">开始时按基础耗时和素材数量确定的总时长(秒,真实时间)</param>
    /// <param name="ascendBuff">开始时已确定的预定BUFF(完成时写入生物;没有可继承词条时为 null)</param>
    /// <returns>新建的进阶详情数据</returns>
    public UserAscendDetailsBean AddAscendData(int index, CreatureBean creatureData, int targetRarity, float timeMax, BuffBean ascendBuff)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            LogUtil.LogError($"添加进阶数据错误,已经存在index_{index}的数据 progress_{targetData.GetProgress()} creatureUUId_{targetData.creatureData?.creatureUUId}");
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        //托管目标生物:置进阶状态、从背包物理移除(RemoveBackpackCreature 内部连带移出所有阵容;装备随生物一并托管)
        creatureData.creatureState = CreatureStateEnum.Vat;
        userData.RemoveBackpackCreature(creatureData);
        UserAscendDetailsBean newData = new UserAscendDetailsBean();
        newData.index = index;
        newData.creatureData = creatureData;
        newData.targetRarity = targetRarity;
        newData.timeMax = timeMax;
        //开始进阶的本地时间戳:进度按真实时间差实时计算(暂停/关游戏不影响),开始进阶后调用方会 SaveUserData 落盘
        newData.startTimeTicks = DateTime.Now.Ticks;
        newData.ascendBuff = ascendBuff;
        dicAscendData[index] = newData;
        return newData;
    }

    /// <summary>
    /// 移除进阶数据(完成/取消进阶时调用;托管生物复位 Idle 并归还背包)
    /// </summary>
    public void RemoveAscendData(int index)
    {
        if (dicAscendData.TryGetValue(index, out var targetData))
        {
            if (targetData.creatureData != null)
            {
                //托管生物复位状态并归还背包(回到背包列表末尾)
                targetData.creatureData.creatureState = CreatureStateEnum.Idle;
                GameDataHandler.Instance.manager.GetUserData().AddBackpackCreature(targetData.creatureData);
            }
            dicAscendData.Remove(index);
        }
        else
        {
            LogUtil.LogError($"移除进阶数据错误,没有数据index_{index}");
        }
    }
    #endregion
}

[Serializable]
public class UserAscendDetailsBean
{
    /// <summary>容器序号</summary>
    public int index;
    /// <summary>进阶目标生物本体(含装备;开始进阶时从背包移入托管,完成/取消时归还背包)</summary>
    public CreatureBean creatureData;
    /// <summary>进阶后的目标稀有度(完成时赋值给生物)</summary>
    public int targetRarity;
    /// <summary>开始时按基础耗时和素材数量确定的总时长(秒,真实时间)</summary>
    public float timeMax;
    /// <summary>开始进阶的本地时间戳(DateTime.Now.Ticks);进度按真实时间差计算,暂停/关游戏不影响</summary>
    public long startTimeTicks;
    /// <summary>魔晶加速奖励的额外进度(秒,真实时间差之外;时钟回拨修正时保留不回扣)</summary>
    public float extraProgress;
    /// <summary>开始时已确定的预定BUFF(完成时写入生物 dicRarityBuff;没有可继承词条时为 null)</summary>
    public BuffBean ascendBuff;

    #region 进阶进度
    /// <summary>
    /// 获取当前进度(秒,实时计算):真实耗时(now-startTime,回拨负值钳0) + 魔晶奖励秒数。
    /// <para>开始时间戳无效时返回 0，等待定时检查修正起点。</para>
    /// </summary>
    public float GetProgress()
    {
        if (startTimeTicks <= 0 || startTimeTicks > DateTime.MaxValue.Ticks)
        {
            return 0f;
        }
        float realElapsed = (float)(DateTime.Now - new DateTime(startTimeTicks)).TotalSeconds;
        //时钟回拨兜底:真实耗时不为负(tick 的 CheckAndFixTimeRollback 会重置起点并落盘,这里只做读取保护)
        if (realElapsed < 0)
        {
            realElapsed = 0;
        }
        return realElapsed + extraProgress;
    }

    /// <summary>
    /// 开始时间戳无效或晚于当前时间时，将起点重置为当前时间并保留魔晶加速进度。
    /// </summary>
    /// <returns>是否发生了修正(调用方据此落盘一次)</returns>
    public bool CheckAndFixTimeRollback()
    {
        long nowTicks = DateTime.Now.Ticks;
        if (startTimeTicks <= 0 || startTimeTicks > nowTicks)
        {
            startTimeTicks = nowTicks;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 增加魔晶加速奖励进度(秒)。不在此触发存档。
    /// </summary>
    public void AddProgress(float value = 1f)
    {
        extraProgress += value;
    }

    /// <summary>
    /// 托管生物存在、总时长有效且进度达标时返回 true。
    /// </summary>
    public bool IsComplete()
    {
        if (creatureData == null || timeMax <= 0)
        {
            return false;
        }
        return GetProgress() >= timeMax;
    }

    /// <summary>
    /// 获取归一化进度(0~1),供进度条和场景水色使用。缺少托管生物或总时长无效时返回 0。
    /// </summary>
    public float GetProgressNormalized()
    {
        if (creatureData == null || timeMax <= 0)
        {
            return 0;
        }
        return Mathf.Clamp01(GetProgress() / timeMax);
    }
    #endregion
}
