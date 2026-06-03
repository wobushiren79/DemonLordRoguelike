using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用户解锁数据存档Bean
/// 用于保存玩家已解锁的研究/功能/世界/生物等内容，以及对应的解锁等级
/// </summary>
[Serializable]
public class UserUnlockBean
{
    #region 数据字段

    /// <summary>
    /// 基础解锁数据
    /// Key：解锁ID（对应 UnlockEnum 或配置表中的 unlock_id）
    /// Value：解锁详情（包含解锁等级等信息）
    /// </summary>
    public Dictionary<long, UserUnlockInfoBean> unlockInfoData = new Dictionary<long, UserUnlockInfoBean>();

    #endregion

    #region 解锁操作

    /// <summary>
    /// 增加解锁
    /// 若已解锁则覆盖等级；若未解锁则新建条目并触发 User_AddUnlock 事件
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    /// <param name="unlockLevel">解锁等级，默认为 1（仅用于已存在条目时的等级覆盖）</param>
    public void AddUnlock(long unlockId,int unlockLevel = 1)
    {
        if (unlockInfoData.TryGetValue(unlockId, out var unlockData))
        {
            unlockData.unlockLevel = unlockLevel;
        }
        else
        {
            unlockInfoData.Add(unlockId, new UserUnlockInfoBean(unlockId));
            EventHandler.Instance.TriggerEvent(EventsInfo.User_AddUnlock, unlockId);
        }
    }

    #endregion

    #region 解锁检测

    /// <summary>
    /// 检测解锁了多少个
    /// 遍历传入的解锁ID数组，统计已解锁的数量
    /// </summary>
    /// <param name="unlockIds">需要检测的解锁ID数组</param>
    /// <returns>已解锁的数量</returns>
    public int CheckIsUnlockNum(long[] unlockIds)
    {
        int unlockNum = 0;
        for (int i = 0; i < unlockIds.Length; i++)
        {
            var unlockId = unlockIds[i];
            //只要有一个未解锁 那就都未解锁
            if (CheckIsUnlock(unlockId))
            {
                unlockNum++;
            }
        }
        return unlockNum;
    }

    /// <summary>
    /// 检测是否解锁（字符串表达式）
    /// 支持复合表达式：
    ///   - 逗号 ',' 表示与（AND），全部满足才视为解锁
    ///   - 竖线 '|' 表示或（OR），其中之一满足即视为解锁
    /// 例如："1,2|3,4" 表示 1 且 (2 或 3) 且 4
    /// </summary>
    /// <param name="unlockStr">解锁表达式字符串</param>
    /// <returns>是否解锁</returns>
    public bool CheckIsUnlock(string unlockStr)
    {
        if (unlockStr.IsNull())
        {
            LogUtil.LogError("检测解锁失败，unlockStr为null");
            return false;
        }
        string[] arrayDataStr = unlockStr.SplitForArrayStr(',');
        for (int i = 0; i < arrayDataStr.Length; i++)
        {
            var itemData = arrayDataStr[i];
            bool isUnlock = true;
            //如果包含或判定
            if (itemData.Contains("|"))
            {
                var unlockIdsOR = itemData.SplitForArrayLong('|');
                isUnlock = false;
                for (int f = 0; f < unlockIdsOR.Length; f++)
                {
                    //或判定 只要有一个解锁那这一组就都解锁了
                    var itemDataOR = unlockIdsOR[f];
                    if (CheckIsUnlock(itemDataOR))
                    {
                        isUnlock = true;
                        break;
                    }
                }
            }
            //其他情况直接转long
            else
            {
               isUnlock = CheckIsUnlock(long.Parse(itemData));
            }
            //只要有一个未解锁 那就都未解锁
            if(isUnlock == false)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检测是否解锁（ID数组，逻辑与）
    /// 数组中所有ID都必须解锁才返回 true
    /// </summary>
    /// <param name="unlockIds">需要检测的解锁ID数组</param>
    /// <returns>是否全部解锁</returns>
    public bool CheckIsUnlock(long[] unlockIds)
    {
        for (int i = 0; i < unlockIds.Length; i++)
        {
            var unlockId = unlockIds[i];
            //只要有一个未解锁 那就都未解锁
            if (!CheckIsUnlock(unlockId))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 检测是否解锁（枚举重载）
    /// </summary>
    /// <param name="unlockEnum">解锁枚举</param>
    /// <returns>是否解锁</returns>
    public bool CheckIsUnlock(UnlockEnum unlockEnum)
    {
        return CheckIsUnlock((long)unlockEnum);
    }

    /// <summary>
    /// 检测是否解锁（单个ID）
    /// 约定：unlockId 为 0 表示无需解锁，始终视为已解锁
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    /// <returns>是否解锁</returns>
    public bool CheckIsUnlock(long unlockId)
    {
        if (unlockId == 0)
        {
            return true;
        }
        if (unlockInfoData.ContainsKey(unlockId))
        {
            return true;
        }
        return false;
    }

    #endregion

    #region 研究等级获取

    /// <summary>
    /// 获取解锁的当前研究等级（通过解锁枚举）
    /// </summary>
    /// <param name="unlockEnum">解锁枚举</param>
    /// <returns>研究等级，未解锁则返回 0</returns>
    public int GetUnlockResearchLeveByUnlockEnum(UnlockEnum unlockEnum)
    {
        return GetUnlockResearchLeveByUnlockId((long)unlockEnum);
    }

    /// <summary>
    /// 获取解锁的当前研究等级（通过解锁ID）
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    /// <returns>研究等级，未解锁则返回 0</returns>
    public int GetUnlockResearchLeveByUnlockId(long unlockId)
    {
        if (unlockInfoData.TryGetValue(unlockId, out var unlockData))
        {
            return unlockData.unlockLevel;
        }
        return 0;
    }

    /// <summary>
    /// 获取解锁的当前研究等级（通过研究ID）
    /// </summary>
    /// <param name="researchId">研究ID</param>
    /// <returns>研究等级，未解锁或研究配置不存在则返回 0</returns>
    public int GetUnlockResearchLeveByResearchId(long researchId)
    {
        ResearchInfoBean researchInfo = ResearchInfoCfg.GetItemData(researchId);
        return GetUnlockResearchLevelByResearchInfo(researchInfo);
    }

    /// <summary>
    /// 获取解锁的当前研究等级（通过研究配置数据）
    /// </summary>
    /// <param name="researchInfo">研究配置数据</param>
    /// <returns>研究等级，未解锁则返回 0</returns>
    public int GetUnlockResearchLevelByResearchInfo(ResearchInfoBean researchInfo)
    {
        if (researchInfo == null)
        {
            return 0;
        }
        return GetUnlockResearchLeveByUnlockId(researchInfo.unlock_id);
    }
    #endregion

    #region 解锁数值获取

    /// <summary>
    /// 获取解锁传送门显示数量
    /// 基础数量取自 UserLimmitBean.portalShowMax + 对应研究等级
    /// </summary>
    /// <returns>传送门显示数量</returns>
    public int GetUnlockPortalShowCount()
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        return limmitData.portalShowMax + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.PortalShowNum);
    }

    /// <summary>
    /// 获取解锁阵容数量
    /// 基础数量取自 UserLimmitBean.lineupMax + 对应研究等级
    /// </summary>
    /// <returns>可使用的阵容数量</returns>
    public int GetUnlockLineupNum()
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        return limmitData.lineupMax + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.LineupNum);
    }

    /// <summary>
    /// 获取阵容生物上限
    /// 基础数量取自 UserLimmitBean.lineupCreatureMax + 对应研究等级
    /// </summary>
    /// <returns>单个阵容可容纳的生物数量上限</returns>
    public int GetUnlockLineupCreatureNum()
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        return limmitData.lineupCreatureMax + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.LineupCreatureAddNum);
    }

    /// <summary>
    /// 获取游戏世界-征服模式-难度等级
    /// 基础难度取自 UserLimmitBean.conquerDifficultyMax + 对应研究等级
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <returns>征服模式当前可挑战的最高难度等级</returns>
    public int GetUnlockGameWorldConquerDifficultyLevel(long worldId)
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        var gameWorldInfo = GameWorldInfoCfg.GetItemData(worldId);
        return limmitData.conquerDifficultyMax + GetUnlockResearchLeveByUnlockId(gameWorldInfo.unlock_id_conquer_difficulty_level);
    }

    /// <summary>
    /// 获取生物升阶容器数量
    /// 未解锁容器功能时返回 0；已解锁则返回 UserLimmitBean.creatureVatMax + 升阶研究等级
    /// </summary>
    /// <returns>当前可用的生物升阶容器数量</returns>
    public int GetUnlockCreatureVatNum()
    {
        bool isUnlockVat = CheckIsUnlock(UnlockEnum.CreatureVat);
        if (isUnlockVat)
        {
            var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
            int unlockLevel = GetUnlockResearchLeveByUnlockEnum(UnlockEnum.CreatureVatAdd);
            return limmitData.creatureVatMax + unlockLevel;
        }
        else
        {
            return 0;
        }
    }

    #endregion

    #region 解锁列表获取

    /// <summary>
    /// 获取解锁的游戏世界ID列表
    /// 默认包含第一个世界(ID=1)，再根据配置遍历所有世界并筛选出已解锁的部分
    /// </summary>
    /// <returns>玩家已解锁的世界ID列表</returns>
    public List<long> GetUnlockGameWorldIds()
    {
        List<long> listUnlockWorld = new List<long>()
        {
            //默认解锁第一个世界
            1,
        };
        var arrayWorld = GameWorldInfoCfg.GetAllArrayData();
        for (int i = 0; i < arrayWorld.Length; i++)
        {
            var itemWorldInfo = arrayWorld[i];
            if (CheckIsUnlock(itemWorldInfo.unlock_id))
            {
                listUnlockWorld.Add(itemWorldInfo.id);
            }
        }
        return listUnlockWorld;
    }

    /// <summary>
    /// 获取解锁的生物模组ID列表
    /// 遍历所有生物模组配置，筛选出对应解锁条件已满足的生物模组
    /// </summary>
    /// <returns>玩家已解锁的生物模组ID列表</returns>
    public List<long> GetUnlockCreatureModelIds()
    {
        List<long> listUnlock = new List<long>();
        var allCreatureModelInfo = CreatureModelCfg.GetAllArrayData();
        for (int i = 0; i < allCreatureModelInfo.Length; i++)
        {
            var itemCreatureModelInfo = allCreatureModelInfo[i];
            if (CheckIsUnlock(itemCreatureModelInfo.unlock_id))
            {
                listUnlock.Add(itemCreatureModelInfo.id);
            }
        }
        return listUnlock;
    }

    #endregion
}

/// <summary>
/// 单条解锁详情数据
/// 描述某个解锁ID的具体状态（解锁等级等）
/// </summary>
public class UserUnlockInfoBean
{
    #region 数据字段

    /// <summary>
    /// 解锁ID（对应 UnlockEnum 或配置表中的 unlock_id）
    /// </summary>
    public long unlockId;

    /// <summary>
    /// 解锁等级
    /// 对于具备研究等级概念的解锁项，表示当前已研究到的等级；普通解锁项默认为 1
    /// </summary>
    public int unlockLevel;

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造方法
    /// 创建一条解锁数据，默认解锁等级为 1
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    public UserUnlockInfoBean(long unlockId)
    {
        this.unlockId = unlockId;
        unlockLevel = 1;
    }

    #endregion
}
