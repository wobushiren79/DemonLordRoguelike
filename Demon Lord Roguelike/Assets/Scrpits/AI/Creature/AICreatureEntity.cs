using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class AICreatureEntity : AIBaseEntity
{
    //自己
    public GameFightCreatureEntity selfCreatureEntity;
    //目标
    public GameFightCreatureEntity targetCreatureEntity;

    /// <summary>
    /// 搜索离自己最近的目标
    /// </summary>
    public GameFightCreatureEntity FindCreatureEntity(DirectionEnum direction)
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

    public GameFightCreatureEntity FindCreatureEntity(Vector3 direction)
    {
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        var creatureInfo = fightCreatureData.creatureData.creatureInfo;
        //搜索范围
        float searchRange = creatureInfo.attack_search_range;
        CreatureTypeEnum searchCreatureType = creatureInfo.GetAttackSearchCreatureType();
        //搜索模式
        CreatureSearchType searchType = fightCreatureData.creatureData.creatureInfo.GetCreatureSearchType();
        //起始搜索点
        Vector3 startPosition = selfCreatureEntity.creatureObj.transform.position + new Vector3(0, 0.5f, 0);

        int searchRoadIndex = fightCreatureData.roadIndex;
        return FightCreatureSearchUtil.FindCreatureEntity(searchType, searchCreatureType, startPosition, direction, Vector3.zero, searchRange, searchRoadIndex);
    }
}
