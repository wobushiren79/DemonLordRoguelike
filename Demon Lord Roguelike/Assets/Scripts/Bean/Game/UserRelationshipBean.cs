using System;
using System.Collections.Generic;

/// <summary>
/// 用户好感度数据存档Bean
/// 用于按 npcId 持久化保存玩家对【议会固定NPC】的好感度(relationship)。
/// 默认好感为仇恨(Hatred, 数值0); 通过贿赂赠礼可提升, 会影响该固定NPC在议会中的初始投票态度。
/// 作为独立存档 UserRelationship_{slot} 存储, 不随主存档内嵌序列化。
/// </summary>
[Serializable]
public class UserRelationshipBean
{
    #region 数据字段

    /// <summary>
    /// 固定NPC好感度数据
    /// Key: 固定NPC的id (NpcInfo.id)
    /// Value: 当前好感度数值(经 NpcRelationshipInfo 区间映射为关系等级; 默认0=仇恨, 上限随配置最大区间)
    /// </summary>
    public Dictionary<long, int> dicRelationship = new Dictionary<long, int>();

    #endregion

    #region 好感度操作

    /// <summary>
    /// 获取指定固定NPC的好感度数值(无记录时返回默认值0, 即仇恨)
    /// </summary>
    /// <param name="npcId">固定NPC的id</param>
    /// <returns>好感度数值</returns>
    public int GetRelationship(long npcId)
    {
        if (dicRelationship.TryGetValue(npcId, out int relationship))
        {
            return relationship;
        }
        return 0;
    }

    /// <summary>
    /// 获取指定固定NPC的好感关系枚举(默认仇恨)
    /// </summary>
    /// <param name="npcId">固定NPC的id</param>
    /// <returns>关系枚举</returns>
    public NpcRelationshipEnum GetRelationshipEnum(long npcId)
    {
        return NpcRelationshipInfoCfg.GetNpcRelationshipEnum(GetRelationship(npcId));
    }

    /// <summary>
    /// 增加指定固定NPC的好感度(可为负, 最小钳制为0)
    /// </summary>
    /// <param name="npcId">固定NPC的id</param>
    /// <param name="addData">增加的好感度数值</param>
    /// <returns>增加后的好感度数值</returns>
    public int AddRelationship(long npcId, int addData)
    {
        int relationship = GetRelationship(npcId) + addData;
        if (relationship < 0)
        {
            relationship = 0;
        }
        dicRelationship[npcId] = relationship;
        return relationship;
    }

    #endregion
}
