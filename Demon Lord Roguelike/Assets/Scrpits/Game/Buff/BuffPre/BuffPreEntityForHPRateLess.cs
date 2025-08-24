using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using UnityEngine;

public class BuffPreEntityForHPRateLess : BuffBasePreEntity
{
    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    public override bool CheckIsPre(BuffEntityBean buffEntityData, float preValue)
    {
        GameFightCreatureEntity creatureEntity = GetTargetCreatureEntity(buffEntityData.getCreatureId);
        if (creatureEntity == null)
        {
            return false;
        }
        FightCreatureBean fightCreatureData = creatureEntity.fightCreatureData;
        if (fightCreatureData == null || fightCreatureData.HPMax == 0)
        {
            return false;
        }
        float currentHPRate = (float)creatureEntity.fightCreatureData.HPCurrent / creatureEntity.fightCreatureData.HPMax;
        //如果血量百分比小于值 则触发
        if (currentHPRate <= preValue)
        {
            return true;
        }
        return false;
    }
}
