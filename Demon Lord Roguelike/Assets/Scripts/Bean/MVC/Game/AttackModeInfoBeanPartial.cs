using System;
using System.Collections.Generic;
public partial class AttackModeInfoBean
{
    #region Buff
    protected List<BuffBean> listBuffData;
    /// <summary>
    /// 获取可能会触发的BUFF列表
    /// </summary>
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
    #endregion

    #region 碰撞检测
    protected float[] colliderAreaSize;

    /// <summary>
    /// 获取碰撞范围大小数组（逗号分隔，缓存解析结果）
    /// </summary>
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
    public CreatureSearchType GetColliderAreaSerachType()
    {
        return (CreatureSearchType)collider_area_type;
    }

    /// <summary>
    /// 获取攻击搜索检测类型
    /// </summary>
    public CreatureSearchType GetCreatureSerachType()
    {
        return (CreatureSearchType)attack_search_type;
    }
    #endregion

    #region 特效
    protected long[] effectHitIds;

    /// <summary>
    /// 获取击中特效ID（effect_hit 为 & 分隔的多个ID，缓存解析结果）
    /// </summary>
    /// <param name="index">特效索引，0=初始攻击，1=连锁攻击，以此类推</param>
    public long GetEffectHitId(int index = 0)
    {
        if (effect_hit.IsNull()) return 0;
        if (effectHitIds == null)
        {
            string[] parts = effect_hit.Split('&');
            effectHitIds = new long[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                long.TryParse(parts[i].Trim(), out effectHitIds[i]);
        }
        if (index < 0 || index >= effectHitIds.Length) return 0;
        return effectHitIds[index];
    }
    #endregion
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