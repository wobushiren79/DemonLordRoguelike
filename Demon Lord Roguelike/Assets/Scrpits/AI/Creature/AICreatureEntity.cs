using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
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
    public GameFightCreatureEntity FindCreatureEntityForDis(Vector3 direction)
    {
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        var creatureInfo = fightCreatureData.creatureData.creatureInfo;
        //搜索范围
        float searchRange = creatureInfo.attack_search_range;
        CreatureTypeEnum searchCreatureType = creatureInfo.GetAttackSearchCreatureType();
        //搜索模式
        CreatureAttackSearchType searchType = fightCreatureData.creatureData.creatureInfo.GetCreatureAttackSearchType();
        //起始搜索点
        Vector3 startPosition = selfCreatureEntity.creatureObj.transform.position + new Vector3(0, 0.5f, 0);

        int layoutInfo;
        if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            layoutInfo = 1 << LayerInfo.CreatureDef;
        }
        else if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {
            layoutInfo = 1 << LayerInfo.CreatureAtt;
        }
        else
        {
            return null;
        }

        switch (searchType)
        {
            case CreatureAttackSearchType.Ray:
                //射线搜索
                return FindCreatureEntityForDisMinByRay(startPosition, direction, searchRange, searchCreatureType,layoutInfo);
            case CreatureAttackSearchType.Area:
            case CreatureAttackSearchType.AreaHPNoMax:
            case CreatureAttackSearchType.AreaDRNoMax:
                //范围搜索
                return FindCreatureEntityByArea(searchType, startPosition, searchRange, searchCreatureType, layoutInfo);
            case CreatureAttackSearchType.RoadForeach:
                //搜索路径
                int searchRoadIndex = fightCreatureData.roadIndex;
                return FindCreatureEntityForDisMinByRoadForeach(searchRoadIndex, searchCreatureType, direction.x > 0 ? DirectionEnum.Right : DirectionEnum.Left);
        }
        return null;
    }

    /// <summary>
    /// 找寻最近的生物-射线
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindCreatureEntityForDisMinByRay(Vector3 startPosition, Vector3 direction, float maxDistance, CreatureTypeEnum searchCreatureType,int layoutInfo)
    {
        if (RayUtil.RayToCast(startPosition, direction, maxDistance, layoutInfo, out RaycastHit hit))
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, searchCreatureType);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                return targetCreature;
            }
        }
        return null;
    }

    /// <summary>
    /// 查询范围敌人
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindCreatureEntityByArea(CreatureAttackSearchType creatureAttackSearchType, Vector3 startPosition, float radius, CreatureTypeEnum searchCreatureType, int layoutInfo)
    {
        Collider[] colliders = RayUtil.OverlapToSphere(startPosition, radius, layoutInfo);
        if (colliders.IsNull())
        {
            return null;
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            string creatureId = colliders[i].gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, searchCreatureType);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                var fightCreatureData = targetCreature.fightCreatureData;
                switch (creatureAttackSearchType)
                {
                    case CreatureAttackSearchType.AreaHPNoMax:
                        //不是满血
                        if (fightCreatureData.HPCurrent < fightCreatureData.HPMax)
                        {
                            return targetCreature;
                        }
                        break;
                    case CreatureAttackSearchType.AreaDRNoMax:
                        //不是满甲
                        if (fightCreatureData.DRCurrent < fightCreatureData.DRMax)
                        {
                            return targetCreature;
                        }
                        break;
                    default:
                        return targetCreature;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 找寻最近的生物-路径遍历
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindCreatureEntityForDisMinByRoadForeach(int roadIndex, CreatureTypeEnum searchCreatureType, DirectionEnum direction = DirectionEnum.Right)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listTargetCreature = null;
        if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {
            listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(roadIndex);
        }
        else if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            listTargetCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(roadIndex);
        }

        if (listTargetCreature.IsNull())
            return null;
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listTargetCreature.Count; i++)
        {
            var itemTargetEntity = listTargetCreature[i];
            if (itemTargetEntity != null && !itemTargetEntity.IsDead())
            {
                var creatureObj = itemTargetEntity.creatureObj;
                if (direction == DirectionEnum.Right && creatureObj.transform.position.x >= selfCreatureEntity.creatureObj.transform.position.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, selfCreatureEntity.creatureObj.transform.position);
                    if (dis < disMin)
                    {
                        disMin = dis;
                        targetEntity = itemTargetEntity;
                    }
                }
                if (direction == DirectionEnum.Left && creatureObj.transform.position.x <= selfCreatureEntity.creatureObj.transform.position.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, selfCreatureEntity.creatureObj.transform.position);
                    if (dis < disMin)
                    {
                        disMin = dis;
                        targetEntity = itemTargetEntity;
                    }
                }
            }
        }
        return targetEntity;
    }
}
