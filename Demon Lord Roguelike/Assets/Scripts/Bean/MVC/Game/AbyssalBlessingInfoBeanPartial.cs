using System;
using System.Collections.Generic;
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

    #endregion
}
