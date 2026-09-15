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
        Vector3 directionV = Vector3.right;
        switch (direction)
        {
            case DirectionEnum.Left:
                directionV = Vector3.left;
                break;
            case DirectionEnum.Right:
                directionV = Vector3.right;
                break;
        }
        return FindCreatureEntity(directionV);
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
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        var creatureInfo = fightCreatureData.creatureData.creatureInfo;
        //如果有NPC数据 优先使用NPC数据里的属性
        var npcInfo = fightCreatureData.creatureData.creatureNpcData?.npcInfo;
        //搜索范围
        float searchRange = (npcInfo != null && npcInfo.attack_search_range != 0)
            ? npcInfo.attack_search_range
            : creatureInfo.attack_search_range;
        CreatureFightTypeEnum searchCreatureFightType = creatureInfo.GetAttackSearchCreatureFightType();
        //搜索模式
        CreatureSearchType searchType = fightCreatureData.creatureData.creatureInfo.GetCreatureSearchType();
        //起始搜索点
        Vector3 startPosition = selfCreatureEntity.creatureObj.transform.position + new Vector3(0, 0.5f, 0);

        int searchRoadIndex = fightCreatureData.roadIndex;
        var listSearchResult = FightCreatureSearchUtil.FindCreatureEntity(searchType, searchCreatureFightType, startPosition, direction, Vector3.zero, searchRange, searchRoadIndex);
        return listSearchResult;
    }
}
