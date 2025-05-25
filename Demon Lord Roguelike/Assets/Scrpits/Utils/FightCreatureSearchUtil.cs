using System.Collections.Generic;
using UnityEngine;

public static class FightCreatureSearchUtil
{
    /// <summary>
    /// 搜索离自己最近的目标
    /// </summary>
    public static GameFightCreatureEntity FindCreatureEntity(CreatureSearchType searchType, CreatureTypeEnum searchCreatureType, Vector3 startSearchPosition, Vector3 direction, Vector3 halfEx,
        float searchRange = 1, int searchRoadIndex = 1)
    {
        int searchLayoutInfo;
        //搜索范围
        if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            searchLayoutInfo = 1 << LayerInfo.CreatureDef;
        }
        else if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {
            searchLayoutInfo = 1 << LayerInfo.CreatureAtt;
        }
        else
        {
            return null;
        }

        switch (searchType)
        {
            case CreatureSearchType.Ray:
                //射线搜索
                return FindCreatureEntityForDisMinByRay(startSearchPosition, direction, searchRange, searchCreatureType, searchLayoutInfo);
            case CreatureSearchType.AreaSphere:
            case CreatureSearchType.AreaSphereHPNoMax:
            case CreatureSearchType.AreaSphereDRNoMax:

            case CreatureSearchType.AreaBox:
            case CreatureSearchType.AreaBoxHPNoMax:
            case CreatureSearchType.AreaBoxDRNoMax:
                //范围搜索
                return FindCreatureEntityByArea(searchType, searchCreatureType, direction, startSearchPosition, halfEx, searchRange, searchLayoutInfo);

            case CreatureSearchType.DisMinByAll:
                //所有生物距离最最近的
                return FindCreatureEntityForDisByAll(startSearchPosition, searchCreatureType, direction, 0);
            case CreatureSearchType.DisMinByRoad:
            case CreatureSearchType.DisMinByRoadAdjacentUpDown:
                //搜索路径
                return FindCreatureEntityForDisByRoad(searchRoadIndex, searchType, searchCreatureType, startSearchPosition, direction, 0);

            case CreatureSearchType.DisMaxByAll:
                //所有生物距离最最远的
                return FindCreatureEntityForDisByAll(startSearchPosition, searchCreatureType, direction, 1);
            case CreatureSearchType.DisMaxByRoad:
            case CreatureSearchType.DisMaxByRoadAdjacentUpDown:
                //搜索路径
                return FindCreatureEntityForDisByRoad(searchRoadIndex, searchType, searchCreatureType, startSearchPosition, direction, 1);
        }
        return null;
    }

    /// <summary>
    /// 找寻最近的生物-射线
    /// </summary>
    /// <returns></returns>
    public static GameFightCreatureEntity FindCreatureEntityForDisMinByRay(Vector3 startPosition, Vector3 direction, float maxDistance, CreatureTypeEnum searchCreatureType, int layoutInfo)
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
    public static GameFightCreatureEntity FindCreatureEntityByArea(CreatureSearchType creatureSearchType, CreatureTypeEnum searchCreatureType, Vector3 direction, Vector3 startPosition, Vector3 halfEx, float radius, int layoutInfo)
    {
        Collider[] colliders = null;
        Vector3 offsetPosition;
        switch (creatureSearchType)
        {
            case CreatureSearchType.AreaSphere:
            case CreatureSearchType.AreaBoxHPNoMax:
            case CreatureSearchType.AreaSphereHPNoMax:
                colliders = RayUtil.OverlapToSphere(startPosition, radius, layoutInfo);
                break;
            case CreatureSearchType.AreaSphereFront:
                if (direction.x > 0)
                {
                    offsetPosition = new Vector3(radius, 0, 0);
                }
                else
                {
                    offsetPosition = new Vector3(-radius, 0, 0);
                }
                colliders = RayUtil.OverlapToSphere(startPosition + offsetPosition, radius, layoutInfo);
                break;
            case CreatureSearchType.AreaBox:
            case CreatureSearchType.AreaSphereDRNoMax:
            case CreatureSearchType.AreaBoxDRNoMax:
                colliders = RayUtil.OverlapToBox(startPosition, halfEx, layoutInfo);
                break;
            case CreatureSearchType.AreaBoxFront:
                if (direction.x > 0)
                {
                    offsetPosition = new Vector3(halfEx.x, 0, 0);
                }
                else
                {
                    offsetPosition = new Vector3(-halfEx.x, 0, 0);
                }
                colliders = RayUtil.OverlapToBox(startPosition + offsetPosition, halfEx, layoutInfo);
                break;
            default:
                return null;
        }
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
                switch (creatureSearchType)
                {
                    case CreatureSearchType.AreaSphereHPNoMax:
                    case CreatureSearchType.AreaBoxHPNoMax:
                        //不是满血
                        if (fightCreatureData.HPCurrent < fightCreatureData.HPMax)
                        {
                            return targetCreature;
                        }
                        break;
                    case CreatureSearchType.AreaSphereDRNoMax:
                    case CreatureSearchType.AreaBoxDRNoMax:
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
    public static GameFightCreatureEntity FindCreatureEntityForDisByRoad(int roadIndex, CreatureSearchType creatureSearchType, CreatureTypeEnum searchCreatureType, Vector3 startSearchPosition, Vector3 direction, int disType)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listTargetCreature = null;
        if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {

            if (creatureSearchType == CreatureSearchType.DisMinByRoadAdjacentUpDown)
            {
                listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(new List<int> { roadIndex - 1, roadIndex + 1 });
            }
            else
            {
                listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(roadIndex);
            }
        }
        else if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            if (creatureSearchType == CreatureSearchType.DisMinByRoadAdjacentUpDown)
            {
                listTargetCreature = gameFightLogic.fightData.GetAttackCreatureByRoad(new List<int> { roadIndex - 1, roadIndex + 1 });
            }
            else
            {
                listTargetCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(roadIndex);
            }
        }
        return FindCreatureEntityForDis(listTargetCreature, startSearchPosition, direction, disType);
    }

    /// <summary>
    /// 找寻最近的生物-所有遍历
    /// </summary>
    /// <returns></returns>
    public static GameFightCreatureEntity FindCreatureEntityForDisByAll(Vector3 startSearchPosition, CreatureTypeEnum searchCreatureType, Vector3 direction, int disType)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        DictionaryList<string, GameFightCreatureEntity> dicTargetCreature = null;
        if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {
            dicTargetCreature = gameFightLogic.fightData.dlAttackCreatureEntity;
        }
        else if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            dicTargetCreature = gameFightLogic.fightData.dlDefenseCreatureEntity;
        }
        return FindCreatureEntityForDis(dicTargetCreature.List, startSearchPosition, direction, disType);
    }

    /// <summary>
    /// 查询距离最近的生物
    /// </summary>
    public static GameFightCreatureEntity FindCreatureEntityForDis(List<GameFightCreatureEntity> listCreature, Vector3 startSearchPosition, Vector3 direction, int disType)
    {
        if (listCreature.IsNull())
            return null;
        float disLimit;
        //距离最近
        if (disType == 0)
        {
            disLimit = float.MaxValue;
        }
        //距离最远
        else
        {
            disLimit = float.MinValue;
        }
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listCreature.Count; i++)
        {
            var itemTargetEntity = listCreature[i];
            if (itemTargetEntity != null && !itemTargetEntity.IsDead())
            {
                var creatureObj = itemTargetEntity.creatureObj;
                if (direction.x > 0 && creatureObj.transform.position.x >= startSearchPosition.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, startSearchPosition);
                    if ((disType == 0 && dis < disLimit) || (disType == 1 && dis > disLimit))
                    {
                        disLimit = dis;
                        targetEntity = itemTargetEntity;
                    }
                }
                if (direction.x <= 0 && creatureObj.transform.position.x <= startSearchPosition.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, startSearchPosition);
                    if ((disType == 0 && dis < disLimit) || (disType == 1 && dis > disLimit))
                    {
                        disLimit = dis;
                        targetEntity = itemTargetEntity;
                    }
                }
            }
        }
        return targetEntity;
    }

}
