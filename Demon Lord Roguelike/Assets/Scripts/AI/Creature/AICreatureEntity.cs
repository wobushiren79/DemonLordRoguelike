using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 生物 AI 基类（partial：通用 Update 事件系统在 AICreatureEntityForUpdateEvent.cs）
/// </summary>
public abstract partial class AICreatureEntity : AIBaseEntity
{
    //自己
    public FightCreatureEntity selfCreatureEntity;
    //目标
    public FightCreatureEntity targetCreatureEntity;

    /// <summary>
    /// 搜索目标
    /// </summary>
    public FightCreatureEntity FindCreatureEntityForSinge(DirectionEnum direction)
    {
        var listData = FindCreatureEntity(direction);
        if (listData.IsNull())
        {
            return null;
        }
        return listData[0];
    }

    #region 出土冒出
    /// <summary>
    /// 发起出土冒出（召唤物出场动画的统一入口，攻守通用，照 AIAttackCreatureEntity.StartKnockback 强制切换先例）：
    /// 切换到通用出土意图 CreatureEmerge 从地底冒出，冒出完成按阵营回各自闲置意图；
    /// 当前唯一调用方=AttackModeSummon 召唤生成当帧（召唤物 isSummoned 生物），后续防守方召唤系同样走本入口。
    /// </summary>
    public void StartEmerge()
    {
        ChangeIntent(AIIntentEnum.CreatureEmerge);
    }
    #endregion

    /// <summary>
    /// 搜索目标
    /// </summary>
    public List<FightCreatureEntity> FindCreatureEntity(DirectionEnum direction)
    {
        return FindCreatureEntity(GetSearchDirection(direction));
    }

    /// <summary>
    /// 搜索目标（正面优先，正面无目标且允许背后搜索时反向补搜一次）
    /// </summary>
    /// <param name="frontDirection">正面朝向（防守生物为 Right）</param>
    /// <param name="searchBack">是否允许搜索身后（由配置 IsAttackSearchBack 门控，false 时行为与单向搜索完全一致）</param>
    public FightCreatureEntity FindCreatureEntityForSingeFrontThenBack(DirectionEnum frontDirection, bool searchBack)
    {
        //正面优先：命中即返回，短路掉背后搜索，正面有敌人时不产生额外开销
        var targetCreature = FindCreatureEntityForSinge(frontDirection);
        if (targetCreature == null && searchBack)
        {
            //仅正面无目标时才向身后补搜一次（复用同一 searchType/searchRange，背后范围与正面一致）
            DirectionEnum backDirection = frontDirection == DirectionEnum.Right ? DirectionEnum.Left : DirectionEnum.Right;
            targetCreature = FindCreatureEntityForSinge(backDirection);
        }
        return targetCreature;
    }

    /// <summary>
    /// 搜索目标
    /// </summary>
    public FightCreatureEntity FindCreatureEntityForSinge(Vector3 direction)
    {
        var listData = FindCreatureEntity(direction);
        if (listData.IsNull())
        {
            return null;
        }
        return listData[0];
    }

    /// <summary>
    /// 搜索目标
    /// </summary>
    public List<FightCreatureEntity> FindCreatureEntity(Vector3 direction)
    {
        //自身已被回收（死亡回池/清场后 selfCreatureEntity 置空，在途弹道回调仍会触发索敌）→ 返回空（照 AIIntentCreatureDead 先例，兜底所有索敌入口）
        if (selfCreatureEntity == null)
            return null;
        GetSearchParameters(out CreatureSearchType searchType, out CreatureFightTypeEnum searchCreatureFightType, out float searchRange, out Vector3 startPosition, out int searchRoadIndex);
        return FightCreatureSearchUtil.FindCreatureEntity(searchType, searchCreatureFightType, startPosition, direction, Vector3.zero, searchRange, searchRoadIndex, selfCreatureEntity);
    }

    #region 指定目标搜索与索敌参数共用
    /// <summary>
    /// 检查指定目标是否仍命中当前生物的索敌结果，身份与候选规则统一由 FightCreatureSearchUtil 校验。
    /// </summary>
    /// <param name="direction">搜索方向</param>
    /// <param name="targetCreature">需要确认的锁定目标</param>
    /// <param name="snapshotUUId">锁定目标时的 creatureUUId 快照</param>
    public bool CheckCreatureEntityInSearchResult(DirectionEnum direction, FightCreatureEntity targetCreature, string snapshotUUId)
    {
        if (selfCreatureEntity == null || selfCreatureEntity.creatureObj == null || targetCreature == null)
            return false;
        GetSearchParameters(out CreatureSearchType searchType, out CreatureFightTypeEnum searchCreatureFightType, out float searchRange, out Vector3 startPosition, out int searchRoadIndex);
        Vector3 directionV = GetSearchDirection(direction);
        return FightCreatureSearchUtil.CheckCreatureEntityInSearchResult(searchType, searchCreatureFightType, targetCreature, snapshotUUId, startPosition, directionV, Vector3.zero, searchRange, searchRoadIndex, selfCreatureEntity);
    }

    /// <summary>
    /// 将 UI/AI 方向枚举转换为索敌向量，未识别方向沿用原默认向右。
    /// </summary>
    private static Vector3 GetSearchDirection(DirectionEnum direction)
    {
        return direction == DirectionEnum.Left ? Vector3.left : Vector3.right;
    }

    /// <summary>
    /// 准备普通搜索与指定目标查询共用的索敌参数，范围和目标阵营优先使用 NPC 的非零配置。
    /// </summary>
    private void GetSearchParameters(out CreatureSearchType searchType, out CreatureFightTypeEnum searchCreatureFightType, out float searchRange, out Vector3 startPosition, out int searchRoadIndex)
    {
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        var creatureInfo = fightCreatureData.creatureData.creatureInfo;
        var npcInfo = fightCreatureData.creatureData.creatureNpcData?.npcInfo;
        searchRange = (npcInfo != null && npcInfo.attack_search_range != 0)
            ? npcInfo.attack_search_range
            : creatureInfo.attack_search_range;
        searchType = creatureInfo.GetCreatureSearchType();
        searchCreatureFightType = (npcInfo != null && npcInfo.attack_search_creature_fight_type != 0)
            ? (CreatureFightTypeEnum)npcInfo.attack_search_creature_fight_type
            : creatureInfo.GetAttackSearchCreatureFightType();
        startPosition = selfCreatureEntity.creatureObj.transform.position + new Vector3(0, 0.5f, 0);
        searchRoadIndex = fightCreatureData.roadIndex;
    }
    #endregion
}
