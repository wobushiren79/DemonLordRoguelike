using System;
using System.Collections.Generic;
public partial class AttackModeInfoBean
{
    protected List<BuffBean> listBuffData;
    /// <summary>
    /// 获取可能会触发的BUFF
    /// </summary>
    /// <returns></returns>
    public List<BuffBean> GetListBuff()
    {
        if (buff.IsNull())
        {
            return null;
        }
        if (listBuffData.IsNull())
        {
            listBuffData = new List<BuffBean>();
            var dicBuffData = buff.SplitForDictionaryLongFloat();
            foreach (var item in dicBuffData)
            {
                BuffBean buffData = new BuffBean(item.Key, createRate: item.Value);
                listBuffData.Add(buffData);
            }
        }
        return listBuffData;
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

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    public static void InitTestData(string buffTestData)
    {
        var allData = GetAllData();
        allData.ForEach((key, value) =>
        {
            value.buff = buffTestData;
            value.GetListBuff();
        });
    }
    
}