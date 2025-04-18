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
    /// 找寻最近的生物
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindCreatureEntityForDisMinByRay(Vector3 startPosition, Vector3 direction, float maxDistance, CreatureTypeEnum creatureType)
    {
        int layoutInfo;
        if (creatureType == CreatureTypeEnum.FightDefense)
        {
            layoutInfo = LayerInfo.CreatureDef;
        }
        else if (creatureType == CreatureTypeEnum.FightAttack)
        {
            layoutInfo = LayerInfo.CreatureAtt;
        }
        else
        {
            return null;
        }
        if (RayUtil.RayToCast(startPosition, direction, maxDistance, 1 << layoutInfo, out RaycastHit hit))
        {
            string creatureId = hit.collider.gameObject.name;
            GameFightLogic gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
            var targetCreature = gameFightLogic.fightData.GetCreatureById(creatureId, creatureType);
            if (targetCreature != null && !targetCreature.IsDead())
            {
                return targetCreature;
            }
        }
        return null;
    }

    /// <summary>
    /// 查询要攻击的防御生物(距离最近)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindAttCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Right)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listTargetData = gameFightLogic.fightData.GetAttackCreatureByRoad(roadIndex);
        if (listTargetData.IsNull())
            return null;
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listTargetData.Count; i++)
        {
            //获取距离最近的防守生物
            var itemTargetEntity = listTargetData[i];
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
            }
        }
        return targetEntity;
    }



    /// <summary>
    /// 查询要攻击的防御生物(距离最近)
    /// </summary>
    /// <returns></returns>
    public GameFightCreatureEntity FindDefCreatureDisMinEntity(int roadIndex, DirectionEnum direction = DirectionEnum.Left)
    {
        //首先查询同一路的防守生物
        var gameFightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        List<GameFightCreatureEntity> listDefenseCreature = gameFightLogic.fightData.GetDefenseCreatureByRoad(roadIndex);
        float disMin = float.MaxValue;
        GameFightCreatureEntity targetEntity = null;
        for (int i = 0; i < listDefenseCreature.Count; i++)
        {
            //获取距离最近的防守生物
            var itemCreature = listDefenseCreature[i];
            if (itemCreature != null && !itemCreature.IsDead())
            {
                var creatureObj = itemCreature.creatureObj;
                if (direction == DirectionEnum.Left && creatureObj.transform.position.x <= selfCreatureEntity.creatureObj.transform.position.x)
                {
                    float dis = Vector3.Distance(creatureObj.transform.position, selfCreatureEntity.creatureObj.transform.position);
                    if (dis < disMin)
                    {
                        disMin = dis;
                        targetEntity = itemCreature;
                    }
                }
            }
        }
        return targetEntity;
    }
}
