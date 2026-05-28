using System;
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
    /// 设置数据-正式流程，从所有馈赠中随机抽取 SHOW_NUM 个
    /// </summary>
    public void SetData(Action<AbyssalBlessingInfoBean> actionForSelect = null, Action actionForSkip = null)
    {
        var allData = AbyssalBlessingInfoCfg.GetAllData();
        int visibleCount = Mathf.Min(SHOW_NUM, ui_AbyssalBlessingList.childCount);
        var pool = new System.Collections.Generic.List<AbyssalBlessingInfoBean>(allData.Values);
        var picked = pool.GetRandomDataForNumberNR(visibleCount);
        SetDataInternal(picked.ToArray(), actionForSelect, actionForSkip);
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
            var resolvedBuffInfo = ResolveBuffInfoForPreview(itemData);
            var targetView = itemView.GetComponent<UIViewFightAbyssalBlessingItem>();
            targetView.SetData(itemData, resolvedBuffInfo);
            targetView.AnimForShow(i * ITEM_SHOW_ANIM_STAGGER);
        }
    }

    /// <summary>
    /// 解析馈赠中第一个"有等级 BUFF"用于预览展示。
    /// 优先取玩家当前等级的下一级；等级链断裂时 fallback 到 1 级；
    /// 无等级 BUFF 时返回 null（由 ItemView 走默认展示）。
    /// </summary>
    private BuffInfoBean ResolveBuffInfoForPreview(AbyssalBlessingInfoBean itemData)
    {
        if (itemData.buff_ids.IsNull()) return null;

        long[] buffIds = itemData.buff_ids.SplitForArrayLong(',');
        for (int i = 0; i < buffIds.Length; i++)
        {
            BuffInfoBean buffInfo = BuffInfoCfg.GetItemData(buffIds[i]);
            if (buffInfo == null || buffInfo.buff_level <= 0) continue;

            var nextLevelBuffInfo = BuffHandler.Instance.ResolveAbyssalBlessingNextBuffInfo(buffIds[i]);
            return nextLevelBuffInfo ?? BuffInfoCfg.GetBuffByParentAndLevel(buffInfo.buff_parent_id, 1);
        }
        return null;
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
