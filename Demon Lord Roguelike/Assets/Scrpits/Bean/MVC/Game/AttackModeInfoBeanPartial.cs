using System;
using System.Collections.Generic;
public partial class AttackModeInfoBean
{
    protected FightBuffStruct[] fightBuff;
    public FightBuffStruct[] GetBuff()
    {
        if (buff.IsNull())
        {
            return null;
        }
        if (fightBuff.IsNull())
        {
            fightBuff = FightBuffStruct.GetData(buff);
        }
        return fightBuff;
    }

    protected float[] colliderAreaSize;

    public float[] GetColliderAreaSize()
    {
        if (colliderAreaSize == null)
        {
            colliderAreaSize = collider_area_size.SplitForArrayFloat(',');
        }
        return colliderAreaSize;
    }

    /// <summary>
    /// 获取碰撞范围检测类型
    /// </summary>
    /// <returns></returns>
    public CreatureSearchType GetColliderAreaSerachType()
    {
        return (CreatureSearchType)collider_area_type;
    }

    public CreatureSearchType GetCreatureSerachType()
    {
        return (CreatureSearchType)attack_search_type;
    }
}
public partial class AttackModeInfoCfg
{

}