using System;
using System.Collections.Generic;
using UnityEngine;
public partial class AbyssalBlessingInfoBean
{
    #region 升级链
    /// <summary>
    /// 是否为可升级馈赠（level &gt; 0）。level == 0 表示可重复选择、与等级无关。
    /// </summary>
    public bool IsLevelUp()
    {
        return level > 0;
    }

    #endregion
}
public partial class AbyssalBlessingInfoCfg
{
    #region 升级链

    /// <summary>
    /// 链表式 parent_id 缓存：每个馈赠ID -&gt; 其族根馈赠ID（level链顶端，parent_id==0 的那个）。
    /// </summary>
    private static Dictionary<long, long> dicFamilyRoot = null;

    /// <summary>
    /// 获取指定馈赠所属升级族的"根馈赠ID"。
    /// 链表式定义：lv1.parent_id==0 为根，lv2.parent_id==lv1.id，依次向上。
    /// 沿 parent_id 向上回溯到 parent_id==0 的节点即为族根；自身为根时返回自身ID。
    /// 防御循环引用：最多回溯 64 层。
    /// </summary>
    public static long GetFamilyRootId(long abyssalBlessingId)
    {
        if (dicFamilyRoot == null)
        {
            dicFamilyRoot = new Dictionary<long, long>();
        }
        if (dicFamilyRoot.TryGetValue(abyssalBlessingId, out long cached))
        {
            return cached;
        }
        long rootId = abyssalBlessingId;
        AbyssalBlessingInfoBean cur = GetItemData(abyssalBlessingId);
        int guard = 0;
        while (cur != null && cur.parent_id > 0 && guard < 64)
        {
            rootId = cur.parent_id;
            AbyssalBlessingInfoBean parent = GetItemData(cur.parent_id);
            if (parent == null) break;
            cur = parent;
            guard++;
        }
        dicFamilyRoot[abyssalBlessingId] = rootId;
        return rootId;
    }

    /// <summary>
    /// 族内最高等级缓存：族根馈赠ID -&gt; 该族出现过的最大 level（level==0 的常驻馈赠不计入）。
    /// </summary>
    private static Dictionary<long, int> dicFamilyMaxLevel = null;

    /// <summary>
    /// 获取指定升级族的最高等级（族内所有行 level 的最大值）。
    /// 用于区分"单级不可重复"（族内仅 1 级）与"多级升级链"（族内 &gt;1 级）。
    /// level==0 的常驻可重复馈赠不参与等级，不计入。
    /// </summary>
    public static int GetFamilyMaxLevel(long familyRootId)
    {
        if (dicFamilyMaxLevel == null)
        {
            dicFamilyMaxLevel = new Dictionary<long, int>();
            foreach (var info in GetAllData().Values)
            {
                if (info == null || info.level <= 0) continue;
                long root = GetFamilyRootId(info.id);
                if (!dicFamilyMaxLevel.TryGetValue(root, out int max) || info.level > max)
                    dicFamilyMaxLevel[root] = info.level;
            }
        }
        return dicFamilyMaxLevel.TryGetValue(familyRootId, out int v) ? v : 0;
    }

    /// <summary>
    /// 按"族根ID + 等级"获取该升级族指定等级的馈赠配置行；找不到（族不存在/无该等级）返回 null。
    /// 遍历全表(带 GetFamilyRootId 缓存)，仅供测试工具等低频场景使用。
    /// </summary>
    /// <param name="familyRootId">族根馈赠ID（parent_id==0 的那一行）</param>
    /// <param name="level">目标等级（level==0 的可重复馈赠请直接用族根行）</param>
    public static AbyssalBlessingInfoBean GetItemDataByFamilyLevel(long familyRootId, int level)
    {
        foreach (var info in GetAllData().Values)
        {
            if (info == null || info.level != level) continue;
            if (GetFamilyRootId(info.id) == familyRootId)
                return info;
        }
        return null;
    }

    /// <summary>
    /// 是否为"单级不可重复"馈赠：level==1 且所属族最高等级也是 1（族内仅此一行、不可升级、选 1 次后不再出现）。
    /// 与"单级可重复"(level==0) 和"多级升级链"(族内 &gt;1 级) 区分开。
    /// 主要用于 UI 判断是否隐藏等级角标。
    /// </summary>
    public static bool IsSingleLevelOnce(AbyssalBlessingInfoBean info)
    {
        if (info == null || info.level != 1) return false;
        return GetFamilyMaxLevel(GetFamilyRootId(info.id)) == 1;
    }

    #endregion

    #region 等级颜色

    /// <summary>
    /// 等级1-5对应的颜色（十六进制）。索引0=Lv1 ... 索引4=Lv5。
    /// </summary>
    private static readonly string[] LEVEL_COLORS = { "#FFFFFF", "#5BD15B", "#4FA8FF", "#C06BFF", "#FFB23E" };

    /// <summary>
    /// 按等级获取对应颜色（Lv1-5 共 5 种），等级超出范围时取边界值。
    /// 供深渊馈赠相关 UI（角标文本 / 背景 / 图标底 / 详情底 等）统一着色使用。
    /// </summary>
    public static Color GetLevelColor(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, LEVEL_COLORS.Length - 1);
        if (ColorUtility.TryParseHtmlString(LEVEL_COLORS[idx], out Color color))
            return color;
        return Color.white;
    }

    #endregion
}
