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
    /// 未解锁则按传入等级新建条目；已解锁且等级发生变化则覆盖等级。
    /// 只要「解锁状态或等级」真正发生改变都会触发 User_AddUnlock 事件——
    /// 这样可升级解锁的后续升级(如生物进阶容器+1 CreatureVatAdd)也能驱动场景刷新/出现动画，
    /// 而非仅在首次解锁时刷新(否则升级出的新容器要重进游戏才显示)
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    /// <param name="unlockLevel">解锁等级，默认为 1</param>
    public void AddUnlock(long unlockId,int unlockLevel = 1)
    {
        if (unlockInfoData.TryGetValue(unlockId, out var unlockData))
        {
            //已解锁：仅当等级真正变化时才覆盖并通知，避免无意义重复触发
            if (unlockData.unlockLevel != unlockLevel)
            {
                unlockData.unlockLevel = unlockLevel;
                EventHandler.Instance.TriggerEvent(EventsInfo.User_AddUnlock, unlockId);
            }
        }
        else
        {
            //未解锁：按传入等级新建条目并通知
            unlockInfoData.Add(unlockId, new UserUnlockInfoBean(unlockId, unlockLevel));
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
    /// 获取传送门刷新次数上限
    /// 上限 = 传送门刷新研究(PortalRefreshNum)等级; 未解锁(0级)返回 0; 满级(10级)上限 10
    /// </summary>
    /// <returns>每次回满时的刷新次数上限</returns>
    public int GetUnlockPortalRefreshMax()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.PortalRefreshNum);
    }

    /// <summary>
    /// 是否已解锁传送门刷新功能(刷新研究等级>0)
    /// 未解锁时传送门界面整个刷新按钮隐藏(默认不开启)
    /// </summary>
    /// <returns>true=已解锁,显示刷新按钮</returns>
    public bool CheckIsUnlockPortalRefresh()
    {
        return CheckIsUnlock(UnlockEnum.PortalRefreshNum);
    }

    /// <summary>
    /// 获取深渊馈赠刷新次数上限
    /// 上限 = 深渊馈赠刷新研究(AbyssalBlessingRefreshNum)等级; 未解锁(0级)返回 0; 满级(5级)上限 5
    /// </summary>
    /// <returns>单次征服run内的刷新次数上限</returns>
    public int GetUnlockAbyssalBlessingRefreshMax()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.AbyssalBlessingRefreshNum);
    }

    /// <summary>
    /// 是否已解锁深渊馈赠刷新功能(刷新研究等级>0)
    /// 未解锁时馈赠选择界面整个刷新按钮隐藏(默认不开启)
    /// </summary>
    /// <returns>true=已解锁,显示刷新按钮</returns>
    public bool CheckIsUnlockAbyssalBlessingRefresh()
    {
        return CheckIsUnlock(UnlockEnum.AbyssalBlessingRefreshNum);
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
    /// 获取魔晶掉落物的额外存在时长(秒)
    /// 未解锁(0级)返回 0；每级研究 +5 秒（UnlockEnum.DropCrystalLifeTime，level_max=6，满级 +30 秒）
    /// </summary>
    /// <returns>叠加到掉落水晶基础存在时长上的额外秒数</returns>
    public float GetUnlockDropCrystalAddLifeTime()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.DropCrystalLifeTime) * 5f;
    }

    /// <summary>
    /// 获取魔王魔力上限(MP)的研究加成
    /// 未解锁(0级)返回 0；每级研究 +10（UnlockEnum.DemonLordMPMax，level_max=5，满级 +50）
    /// </summary>
    /// <returns>叠加到魔王魔力上限上的额外点数</returns>
    public float GetUnlockDemonLordMPMaxAddValue()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.DemonLordMPMax) * 10f;
    }

    /// <summary>
    /// 获取魔王魔力恢复速度(MPF)的研究加成(每秒)
    /// 未解锁(0级)返回 0；每级研究 +1/秒（UnlockEnum.DemonLordMPF，level_max=3，满级 +3/秒）
    /// </summary>
    /// <returns>叠加到魔王每秒魔力恢复速度上的额外点数</returns>
    public float GetUnlockDemonLordMPFAddValue()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.DemonLordMPF) * 1f;
    }

    /// <summary>
    /// 获取魔王自动拾取魔晶的间隔(秒)
    /// 未解锁(0级)返回 -1 表示禁用；等级 L(1~10) → 间隔 = 11-L（1级10秒…满级1秒）（UnlockEnum.DemonLordAutoPickCrystal，level_max=10）
    /// </summary>
    /// <returns>两次自动拾取之间的秒数；-1 表示未解锁不拾取</returns>
    public float GetUnlockDemonLordAutoPickCrystalInterval()
    {
        int level = GetUnlockResearchLeveByUnlockEnum(UnlockEnum.DemonLordAutoPickCrystal);
        if (level <= 0)
            return -1f;
        return 11f - level;
    }

    /// <summary>
    /// 获取魔王每次自动拾取的魔晶数量
    /// 基础 1 颗 + DemonLordAutoPickCrystalNum 研究等级（level_max=5，满级每次 6 颗）；仅在自动拾取本身已解锁时才有意义
    /// </summary>
    /// <returns>单次自动拾取的魔晶颗数</returns>
    public int GetUnlockDemonLordAutoPickCrystalCount()
    {
        return 1 + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.DemonLordAutoPickCrystalNum);
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
    /// 世界解锁 id 块基址：每个世界的专属解锁 id 都落在 1003_10_W_nn 块内（W=世界id, nn=块内偏移）。
    /// 现有块内偏移：01=世界解锁, 02=无尽解锁, 12~20=征服难度(等级2~10, level_max=9 占满该段), 30=加快进攻节奏(Quick)。集中此处便于统一维护。
    /// </summary>
    public const long WORLD_UNLOCK_BLOCK_BASE = 100310000;

    /// <summary>
    /// 世界「加快进攻节奏(Quick)」研究在世界解锁 id 块内的偏移(nn=30，避开征服难度占用的 12~20 段)。
    /// </summary>
    public const int WORLD_QUICK_ATTACK_UNLOCK_OFFSET = 30;

    /// <summary>
    /// 获取指定世界「加快进攻节奏(Quick)」研究的解锁ID
    /// 按世界解锁 id 块约定推导：WORLD_UNLOCK_BLOCK_BASE + worldId*100 + WORLD_QUICK_ATTACK_UNLOCK_OFFSET
    /// （如 world1 → 100310130）。每新增一个世界，其 Quick 研究 unlock_id 依此规则推导，无需世界配置表新增列。
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <returns>该世界 Quick 研究对应的解锁ID</returns>
    public long GetWorldQuickAttackUnlockId(long worldId)
    {
        return WORLD_UNLOCK_BLOCK_BASE + worldId * 100 + WORLD_QUICK_ATTACK_UNLOCK_OFFSET;
    }

    /// <summary>
    /// 是否已解锁指定世界的「加快进攻节奏(Quick)」研究
    /// Quick 按钮与世界绑定：仅当玩该世界且已解锁该世界的 Quick 研究时，战斗界面才显示 Quick 按钮。
    /// </summary>
    /// <param name="worldId">游戏世界ID</param>
    /// <returns>true=已解锁该世界的加速进攻研究</returns>
    public bool CheckIsUnlockWorldQuickAttack(long worldId)
    {
        return CheckIsUnlock(GetWorldQuickAttackUnlockId(worldId));
    }

    /// <summary>
    /// 获取献祭祭品选择上限
    /// 基础数量取自 UserLimmitBean.sacrificeMax + 对应研究等级（UnlockEnum.SacrificeNum）
    /// </summary>
    /// <returns>献祭时可选择的最大祭品数量</returns>
    public int GetUnlockSacrificeMax()
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        return limmitData.sacrificeMax + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.SacrificeNum);
    }

    /// <summary>
    /// 获取献祭失败时累积的保底成功率增量
    /// 未解锁(0级)返回 0；每级研究 +5%（UnlockEnum.SacrificePityRate，level_max=10，满级 +50%）
    /// </summary>
    /// <returns>本次失败应累加到 sacrificePityRate 上的保底增量(0~0.5)</returns>
    public float GetUnlockSacrificeFailPityAddRate()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.SacrificePityRate) * 0.05f;
    }

    /// <summary>
    /// 获取「不同生物id」祭品的单个献祭成功率
    /// 未解锁(0级)返回 0（默认不同id祭品成功率为0）；每级研究 +5%（UnlockEnum.SacrificeDifferentIdRate，level_max=10，满级 +50%）
    /// </summary>
    /// <returns>单个不同id祭品的基础成功率(0~0.5，稀有度惩罚在公式中另行叠加)</returns>
    public float GetUnlockSacrificeDifferentIdRate()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.SacrificeDifferentIdRate) * 0.05f;
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

    /// <summary>
    /// 获取生物进阶「魔晶加速」研究等级
    /// 该等级同时决定：单次加速消耗魔晶数 = 进度增加倍率；0级(未研究)表示加速功能未解锁(加速按钮隐藏)
    /// </summary>
    /// <returns>魔晶加速研究等级(0~5)，0=未解锁</returns>
    public int GetUnlockCreatureVatAddProgressLevel()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.CreatureVatAddProgress);
    }

    /// <summary>
    /// 获取生物进阶素材魔物可选上限
    /// 基础数量取自 UserLimmitBean.creatureVatMaterialMax + 对应研究等级（UnlockEnum.CreatureVatMaterialNum，满级+5）
    /// </summary>
    /// <returns>进阶时可选择的最大素材魔物数量</returns>
    public int GetUnlockCreatureVatMaterialMax()
    {
        var limmitData = GameDataHandler.Instance.manager.GetUserData().GetUserLimmitData();
        return limmitData.creatureVatMaterialMax + GetUnlockResearchLeveByUnlockEnum(UnlockEnum.CreatureVatMaterialNum);
    }

    /// <summary>
    /// 空格突进默认冷却时间(秒)：未研究「突进CD」(SpaceDashCD=0级)时的基础冷却。集中此处便于统一调整。
    /// </summary>
    public const float SPACE_DASH_CD_BASE = 3f;

    /// <summary>
    /// 空格突进每级「突进CD」研究减少的冷却(秒)：每研究 1 级在基础冷却上减少此值。集中此处便于统一调整。
    /// </summary>
    public const float SPACE_DASH_CD_PER_LEVEL = 0.5f;

    /// <summary>
    /// 空格突进保底最低冷却时间(秒)：「突进CD」研究满级后的冷却下限，冷却不会低于此值。集中此处便于统一调整。
    /// </summary>
    public const float SPACE_DASH_CD_MIN = 1f;

    /// <summary>
    /// 获取空格突进研究等级（UnlockEnum.SpaceDash，level_max=3）
    /// 0=未解锁(不可突进)；1/2/3 级分别向朝向突进 1/2/3 个距离单位（每单位的世界距离由控制系统 dashDistancePerLevel 决定）
    /// </summary>
    /// <returns>空格突进研究等级(0~3)，0=未解锁</returns>
    public int GetUnlockSpaceDashLevel()
    {
        return GetUnlockResearchLeveByUnlockEnum(UnlockEnum.SpaceDash);
    }

    /// <summary>
    /// 获取空格突进冷却时间(秒)
    /// 默认 SPACE_DASH_CD_BASE(3秒)；每级「突进CD」研究(UnlockEnum.SpaceDashCD，level_max=4)减 SPACE_DASH_CD_PER_LEVEL(0.5秒)，
    /// 满级(4级)取保底下限 SPACE_DASH_CD_MIN(1秒)。即 3/2.5/2/1.5/1 秒。改基础/每级/保底值只需改这三个常量。
    /// </summary>
    /// <returns>当前突进冷却时间(秒，范围 SPACE_DASH_CD_MIN ~ SPACE_DASH_CD_BASE)</returns>
    public float GetUnlockSpaceDashCD()
    {
        float cd = SPACE_DASH_CD_BASE - GetUnlockResearchLeveByUnlockEnum(UnlockEnum.SpaceDashCD) * SPACE_DASH_CD_PER_LEVEL;
        return Mathf.Max(cd, SPACE_DASH_CD_MIN);
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
    /// 创建一条解锁数据，解锁等级默认为 1
    /// </summary>
    /// <param name="unlockId">解锁ID</param>
    /// <param name="unlockLevel">解锁等级，默认为 1</param>
    public UserUnlockInfoBean(long unlockId, int unlockLevel = 1)
    {
        this.unlockId = unlockId;
        this.unlockLevel = unlockLevel;
    }

    #endregion
}
