using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class BuffPreEntityForHPRateLess : BuffBasePreEntity
{
    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        FightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.targetCreatureUUId);
        if (creatureEntity == null)
        {
            return false;
        }
        FightCreatureBean fightCreatureData = creatureEntity.fightCreatureData;
        if (fightCreatureData == null)
        {
            return false;
        }
        float HPMax = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.HP);
        if (HPMax == 0)
        {
            return false;
        }
        float currentHPRate = (float)fightCreatureData.HPCurrent / HPMax;
        //如果血量百分比小于值 则触发
        if (currentHPRate <= preValue)
        {
            return true;
        }
        return false;
    }
}
