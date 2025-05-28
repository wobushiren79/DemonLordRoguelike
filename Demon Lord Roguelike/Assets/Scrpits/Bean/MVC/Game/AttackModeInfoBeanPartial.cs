using System;
using System.Collections.Generic;
public partial class AttackModeInfoBean
{
    protected long[] buffIds;
    public long[] GetBuffIds()
    {
        if (buff.IsNull())
        {
            return null;
        }
        if (buffIds.IsNull())
        {
            buffIds = buff.SplitForArrayLong(',');
        }
        return buffIds;
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