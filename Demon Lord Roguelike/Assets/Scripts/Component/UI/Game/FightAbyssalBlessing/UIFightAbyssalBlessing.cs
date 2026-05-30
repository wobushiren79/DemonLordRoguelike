using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIFightAbyssalBlessing : BaseUIComponent
{
    #region 常量

    /// <summary>
    /// 每次展示的馈赠选项数量
    /// </summary>
    private const int SHOW_NUM = 3;

    /// <summary>
    /// 卡片出现动画的每张错位延迟（秒）
    /// </summary>
    private const float ITEM_SHOW_ANIM_STAGGER = 0.08f;

    /// <summary>
    /// 优先生成"已选过馈赠的更高等级"的概率（0~1）。
    /// 当玩家已拥有可升级馈赠时，每个候选名额都按此概率独立判定是否优先填入升级项。
    /// </summary>
    private const float PRIORITY_OWNED_UPGRADE_CHANCE = 0.3f;

    #endregion

    #region 回调

    /// <summary>
    /// 选择回调
    /// </summary>
    public Action<AbyssalBlessingInfoBean> actionForSelect;

    /// <summary>
    /// 跳过回调
    /// </summary>
    public Action actionForSkip;

    #endregion

    #region 状态

    /// <summary>
    /// 动画播放中标记，防止多次点击重入
    /// </summary>
    private bool isAnimating;

    #endregion

    #region 数据设置

    /// <summary>
    /// 设置数据-正式流程：构建候选池 → 按规则抽取 SHOW_NUM 个 → 渲染
    /// </summary>
    public void SetData(Action<AbyssalBlessingInfoBean> actionForSelect = null, Action actionForSkip = null)
    {
        List<AbyssalBlessingInfoBean> pool = BuildCandidatePool();
        int visibleCount = Mathf.Min(SHOW_NUM, ui_AbyssalBlessingList.childCount);
        List<AbyssalBlessingInfoBean> picked = RollCandidates(pool, visibleCount);
        SetDataInternal(picked.ToArray(), actionForSelect, actionForSkip);
    }

    /// <summary>
    /// 构建本次可出现的候选池：遍历全部馈赠配置，按 <see cref="IsCandidateEligible"/> 过滤。
    /// （level==0 可重复；level&gt;0 仅保留"已拥有升级族的下一级"）
    /// </summary>
    private List<AbyssalBlessingInfoBean> BuildCandidatePool()
    {
        var pool = new List<AbyssalBlessingInfoBean>();
        foreach (var info in AbyssalBlessingInfoCfg.GetAllData().Values)
        {
            if (IsCandidateEligible(info))
                pool.Add(info);
        }
        return pool;
    }

    /// <summary>
    /// 从候选池中抽取最终展示的馈赠列表，叠加"优先升级"机制。
    /// 规则：逐个名额独立判定，每格按 <see cref="PRIORITY_OWNED_UPGRADE_CHANCE"/> 的概率
    /// 决定填入"已选过馈赠的更高等级"还是从剩余池随机补足；全程去重，最后打乱顺序。
    /// </summary>
    private List<AbyssalBlessingInfoBean> RollCandidates(List<AbyssalBlessingInfoBean> pool, int visibleCount)
    {
        if (visibleCount <= 0 || pool.IsNull())
            return new List<AbyssalBlessingInfoBean>();

        //关键点：筛出"已选过且仍可升级"的候选（已拥有该族 → 池中出现的必是其下一级）
        List<AbyssalBlessingInfoBean> upgrades = pool.FindAll(IsOwnedFamilyUpgrade);

        //快速路径：没有可升级项时纯随机抽取，省去后续列表拷贝
        if (upgrades.Count <= 0)
            return pool.GetRandomDataForNumberNR(visibleCount);

        //逐名额抽取：每格独立 roll 概率，决定"优先升级项 / 普通随机"，并实时去重
        var result = new List<AbyssalBlessingInfoBean>();
        var restPool = new List<AbyssalBlessingInfoBean>(pool);
        var restUpgrades = new List<AbyssalBlessingInfoBean>(upgrades);
        while (result.Count < visibleCount && restPool.Count > 0)
        {
            //关键点：仍有升级项且命中概率 → 优先填升级项；否则从剩余池随机
            bool pickUpgrade = restUpgrades.Count > 0 && UnityEngine.Random.value < PRIORITY_OWNED_UPGRADE_CHANCE;
            AbyssalBlessingInfoBean pick = pickUpgrade ? restUpgrades.GetRandomData() : restPool.GetRandomData();
            result.Add(pick);
            restPool.Remove(pick);
            restUpgrades.Remove(pick);   //pick 可能本身就是升级项，同步移除防重复
        }
        //关键点：打乱顺序，避免升级项因抽取顺序固定排在最前
        return result.GetRandomList();
    }

    /// <summary>
    /// 判断候选是否为"已选过馈赠的更高等级"：
    /// 属于升级族（level&gt;0）且玩家已拥有该族（已拥有等级≥1，因此本次可出现的必是其下一级）。
    /// </summary>
    private bool IsOwnedFamilyUpgrade(AbyssalBlessingInfoBean info)
    {
        if (info == null || info.level <= 0) return false;
        long familyRootId = AbyssalBlessingInfoCfg.GetFamilyRootId(info.id);
        return BuffHandler.Instance.GetAbyssalBlessingFamilyLevel(familyRootId) >= 1;
    }

    /// <summary>
    /// 判断馈赠是否可作为本次候选出现：
    /// - level == 0：可重复选择的馈赠，始终可出现，不考虑等级；
    /// - level &gt; 0：仅当它正好是"已拥有升级族的下一级"时出现
    ///   （未拥有该族 → 仅 lv1 可出现；已拥有 lv(N) → 仅 lv(N+1) 可出现；已满级则该族不再出现）。
    /// </summary>
    private bool IsCandidateEligible(AbyssalBlessingInfoBean info)
    {
        if (info == null) return false;
        if (info.level <= 0) return true;
        long familyRootId = AbyssalBlessingInfoCfg.GetFamilyRootId(info.id);
        int ownedLevel = BuffHandler.Instance.GetAbyssalBlessingFamilyLevel(familyRootId);
        return info.level == ownedLevel + 1;
    }

    /// <summary>
    /// 设置数据-测试流程，按指定 ID 列表展示（最多 SHOW_NUM 个），ID 无效时跳过
    /// </summary>
    public void SetDataForTest(long[] testIds, Action<AbyssalBlessingInfoBean> actionForSelect = null, Action actionForSkip = null)
    {
        int idLen = testIds == null ? 0 : testIds.Length;
        int visibleCount = Mathf.Min(SHOW_NUM, Mathf.Min(idLen, ui_AbyssalBlessingList.childCount));
        var showData = new AbyssalBlessingInfoBean[visibleCount];
        for (int i = 0; i < visibleCount; i++)
        {
            showData[i] = AbyssalBlessingInfoCfg.GetItemData(testIds[i]);
        }
        SetDataInternal(showData, actionForSelect, actionForSkip);
    }

    /// <summary>
    /// 数据渲染内部实现：按 showData 数组分别绑定子节点，多余子节点隐藏
    /// </summary>
    private void SetDataInternal(AbyssalBlessingInfoBean[] showData, Action<AbyssalBlessingInfoBean> actionForSelect, Action actionForSkip)
    {
        this.actionForSelect = actionForSelect;
        this.actionForSkip = actionForSkip;
        isAnimating = false;

        int childCount = ui_AbyssalBlessingList.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var itemView = ui_AbyssalBlessingList.GetChild(i);
            bool visible = i < showData.Length && showData[i] != null;
            itemView.gameObject.SetActive(visible);
            if (!visible) continue;

            var itemData = showData[i];
            var targetView = itemView.GetComponent<UIViewFightAbyssalBlessingItem>();
            targetView.SetData(itemData);
            targetView.AnimForShow(i * ITEM_SHOW_ANIM_STAGGER);
        }
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 关闭UI-主动清理所有子控件的悬停动画
    /// 避免鼠标停留在控件上时UI被关闭，未触发 OnPointerExit 导致动画残留
    /// </summary>
    public override void CloseUI()
    {
        base.CloseUI();
        int childCount = ui_AbyssalBlessingList.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var view = ui_AbyssalBlessingList.GetChild(i).GetComponent<UIViewFightAbyssalBlessingItem>();
            if (view != null)
                view.KillHoverAnim();
        }
    }

    #endregion

    #region 点击

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_SkipBtn)
        {
            OnClickForSkip();
        }
    }

    /// <summary>
    /// 点击选择-播放选中动画（被选卡放大强调→收缩，其余卡缩小消失），动画结束后再触发回调
    /// </summary>
    public void OnClickForSelect(UIViewFightAbyssalBlessingItem selectedView, AbyssalBlessingInfoBean abyssalBlessingInfo)
    {
        if (isAnimating) return;
        isAnimating = true;
        UIHandler.Instance.ShowScreenLock();

        int childCount = ui_AbyssalBlessingList.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = ui_AbyssalBlessingList.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var view = child.GetComponent<UIViewFightAbyssalBlessingItem>();
            if (view == selectedView) continue;
            view.AnimForHide();
        }

        selectedView.AnimForSelect(() =>
        {
            UIHandler.Instance.HideScreenLock();
            actionForSelect?.Invoke(abyssalBlessingInfo);
            actionForSelect = null;
        });
    }

    /// <summary>
    /// 点击跳过-所有可见卡片同时收缩消失，动画结束后再触发回调
    /// </summary>
    public void OnClickForSkip()
    {
        if (isAnimating) return;
        isAnimating = true;
        UIHandler.Instance.ShowScreenLock();

        bool callbackHooked = false;
        int childCount = ui_AbyssalBlessingList.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = ui_AbyssalBlessingList.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var view = child.GetComponent<UIViewFightAbyssalBlessingItem>();
            if (!callbackHooked)
            {
                view.AnimForHide(InvokeSkipCallback);
                callbackHooked = true;
            }
            else
            {
                view.AnimForHide();
            }
        }
        //没有可见卡片时兜底直接回调
        if (!callbackHooked) InvokeSkipCallback();

        void InvokeSkipCallback()
        {
            UIHandler.Instance.HideScreenLock();
            actionForSkip?.Invoke();
            actionForSkip = null;
        }
    }

    #endregion
}
