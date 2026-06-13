using TMPro;
using UnityEngine;

/// <summary>
/// 战斗生物实体-魔王（防守核心）专属逻辑
/// <para>魔王：被防守的核心生物（CreatureFightTypeEnum.FightDefenseCore），魔王死亡则战斗失败。</para>
/// <para>魔力(MP)显示：魔王预制下的 MPShow 进度条（与LifeShow同款进度材质）+ MPText 文本（当前/上限格式）。</para>
/// </summary>
public partial class FightCreatureEntity
{
    #region 魔王-魔力显示
    /// <summary>
    /// 魔力条显示（仅魔王核心预制下有MPShow节点 与LifeShow同款进度条材质）
    /// </summary>
    public SpriteRenderer creatureMPShow;
    /// <summary>
    /// 魔力值文本（MPShow/MPText 以 当前/上限 格式显示）
    /// </summary>
    public TextMeshPro creatureMPText;
    /// <summary>
    /// 上一次显示的魔力值（避免每帧重设文本产生开销）
    /// </summary>
    int lastMPShowCurrent = -1;
    /// <summary>
    /// 上一次显示的魔力上限（避免每帧重设文本产生开销）
    /// </summary>
    int lastMPShowMax = -1;
    /// <summary>
    /// 魔力文本的渲染排序值（设到足够高 保证魔力文本渲染在最上层 不被其它生物/特效遮挡）
    /// </summary>
    const int MPTextSortingOrder = 9999;

    /// <summary>
    /// 数据初始化-魔王（由 SetData 统一调用）
    /// <para>挂接魔力显示节点并重置显示缓存（非核心生物预制下无MPShow节点 查找为空自动跳过显示）。</para>
    /// </summary>
    public void SetDataForDefenseCore()
    {
        //获取魔力值显示（仅魔王核心有 创建魔物消耗魔力）
        creatureMPShow = creatureObj.transform.Find("MPShow")?.GetComponent<SpriteRenderer>();
        creatureMPText = creatureObj.transform.Find("MPShow/MPText")?.GetComponent<TextMeshPro>();
        //将魔力文本的渲染排序设到最上层（TextMeshPro 为世界空间网格 通过 MeshRenderer.sortingOrder 控制渲染顺序）
        if (creatureMPText != null)
        {
            var mpTextRenderer = creatureMPText.GetComponent<MeshRenderer>();
            if (mpTextRenderer != null)
                mpTextRenderer.sortingOrder = MPTextSortingOrder;
        }
        lastMPShowCurrent = -1;
        lastMPShowMax = -1;
        RefreshMPShow();
    }

    /// <summary>
    /// 刷新魔力显示（魔王核心专用 与防守生物的LifeShow一样在数值变化时通知刷新）
    /// <para>魔力条进度实时刷新；MPText 文本仅在整数值变化时重设，格式为 当前/上限（如 100/100）。</para>
    /// <para>非魔王核心生物（预制下无MPShow节点）调用时直接跳过。</para>
    /// </summary>
    public void RefreshMPShow()
    {
        if (creatureMPShow == null)
            return;
        float MPMax = fightCreatureData.GetAttribute(CreatureAttributeTypeEnum.MP);
        //设置魔力条进度
        if (MPMax > 0)
        {
            creatureMPShow.material.SetFloat("_Progress_1", fightCreatureData.MPCurrent / MPMax);
        }
        else
        {
            creatureMPShow.material.SetFloat("_Progress_1", 0);
        }
        //魔力条不使用护盾层（材质与LifeShow共用 默认值非0 需要归零）
        creatureMPShow.material.SetFloat("_Progress_2", 0);
        //设置魔力文本（仅在显示的整数值变化时重设 避免每帧产生文本开销）
        if (creatureMPText != null)
        {
            int MPCurrentInt = (int)fightCreatureData.MPCurrent;
            int MPMaxInt = (int)MPMax;
            if (MPCurrentInt != lastMPShowCurrent || MPMaxInt != lastMPShowMax)
            {
                lastMPShowCurrent = MPCurrentInt;
                lastMPShowMax = MPMaxInt;
                creatureMPText.text = $"{MPCurrentInt}/{MPMaxInt}";
            }
        }
    }
    #endregion

    #region 魔王-死亡相关
    /// <summary>
    /// 死亡意图切换-魔王（由 SetCreatureDead 统一分发 非魔王核心自动跳过）
    /// </summary>
    public void SetCreatureDeadForDefenseCore()
    {
        if (aiEntity is AIDefenseCoreCreatureEntity)
        {
            aiEntity.ChangeIntent(AIIntentEnum.DefenseCoreCreatureDead);
        }
    }
    #endregion
}
