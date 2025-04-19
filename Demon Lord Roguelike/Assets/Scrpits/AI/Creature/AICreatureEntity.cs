using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
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
    public GameFightCreatureEntity FindCreatureEntityForDis(Vector3 direction, CreatureTypeEnum searchCreatureType)
    {
        var fightCreatureData = selfCreatureEntity.fightCreatureData;
        //搜索范围
        float searchRange = fightCreatureData.creatureData.creatureInfo.attack_search_range;
        //搜索模式
        CreatureAttackSearchType searchType = fightCreatureData.creatureData.creatureInfo.GetCreatureAttackSearchType();
        //起始搜索点
        Vector3 startPosition = fightCreatureData.positionCreate + new Vector3(0, 0.5f, 0);

        int layoutInfo;
        if (searchCreatureType == CreatureTypeEnum.FightDefense)
        {
            layoutInfo = LayerInfo.CreatureDef;
        }
        else if (searchCreatureType == CreatureTypeEnum.FightAttack)
        {
            layoutInfo = LayerInfo.CreatureAtt;
        }
        else
        {
            return null;
        }

        switch (searchType)
        {
            case CreatureAttackSearchType.Ray:
                return FindCreatureEntityForDisMinByRay(startPosition, direction, searchRange, searchCreatureType,layoutInfo);
            case CreatureAttackSearchType.Area:
                return FindCreatureEntityForDisMinByArea(startPosition, searchRange, searchCreatureType, layoutInfo);
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

        if (RayUtil.RayToCast(startPosition, direction, maxDistance, 1 << layoutInfo, out RaycastHit hit))
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
    /// 查询范围内最近的敌人
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindCreatureEntityForDisMinByArea(Vector3 startPosition, float radius, CreatureTypeEnum searchCreatureType, int layoutInfo)
    {
        Collider[] colliders = RayUtil.OverlapToSphere(startPosition, radius, 1 << layoutInfo);
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
                return targetCreature;
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
