using UnityEditor;
using UnityEngine;

public class BuffBaseEntity
{
    /// <summary>
    /// 触发BUFF
    /// </summary>
    /// <param name="buffEntityData"></param>
    public virtual void TriggerBuff(BuffEntityBean buffEntityData)
    {

    }

    /// <summary>
    /// 检测是否满足前置条件
    /// </summary>
    /// <param name="buffEntityData"></param>
    /// <returns></returns>
    public bool CheckIsPre(BuffEntityBean buffEntityData)
    {
        var preInfo = buffEntityData.buffInfo.GetPreInfo();
        if (preInfo.IsNull())
        {
            return true;
        }
        foreach (var itemData in preInfo)
        {
            var buffPreInfo = BuffPreInfoCfg.GetItemData(itemData.Key);
            var buffPreEntity = BuffUtil.GetBuffPreEntity(buffPreInfo);
            if (!buffPreEntity.CheckIsPre(buffEntityData, itemData.Value))
            {
                return false;
            }
        }
        return true;
    }

    #region 攻击力
    public virtual float GetChangeDataForATK(BuffEntityBean buffEntityData)
    {
        return 0;
    }

    public virtual float GetChangeRateDataForATK(BuffEntityBean buffEntityData)
    {
        return 0;
    }
    #endregion

    #region 攻击速度
    public virtual float GetChangeDataForASPD(BuffEntityBean buffEntityData)
    {
        return 0;
    }

    public virtual float GetChangeRateDataForASPD(BuffEntityBean buffEntityData)
    {
        return 0;
    }
    #endregion

    #region  移动速度
    public virtual float GetChangeDataForMSPD(BuffEntityBean buffEntityData)
    {
        return 0;
    }

    public virtual float GetChangeRateDataForMSPD(BuffEntityBean buffEntityData)
    {
        return 0;
    }
    #endregion

    #region  闪避
    public virtual float GetChangeRateDataForEVA(BuffEntityBean buffEntityData)
    {
        return 0;
    }
    #endregion

    #region  暴击
    public virtual float GetChangeRateDataForCRT(BuffEntityBean buffEntityData)
    {
        return 0;
    }
    #endregion

    #region 身体颜色
    public virtual Color GetChangeBodyColor(BuffEntityBean buffEntityData)
    {
        return buffEntityData.buffInfo.GetBodyColor();
    }
    #endregion
}
