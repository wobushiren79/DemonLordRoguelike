using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPopupPortalDetails : PopupShowCommonView
{
    #region 数据
    //奖励道具显示的缓存池(模板 ui_UIViewItem 为池首项, 不足时克隆复用, 多余项隐藏)
    protected List<UIViewItem> listRewardItemPool;
    #endregion

    #region 数据设置
    /// <summary>
    /// 设置数据(气泡展示传送门名字, 以及受研究门控的线路数/关卡数/路径长度/奖励; 未解锁的项整行隐藏)
    /// </summary>
    /// <param name="data">(世界配置, 世界随机数据, 要展示的难度)三元组</param>
    public override void SetData(object data)
    {
        var targetData = ((GameWorldInfoBean, GameWorldInfoRandomBean, int))data;
        GameWorldInfoBean gameWorldInfo = targetData.Item1;
        GameWorldInfoRandomBean gameWorldInfoRandom = targetData.Item2;
        //气泡要展示的难度(由调用方传入: 难度详情每个item展示各自难度, 地图传送门item展示当前难度)
        int difficultyLevel = targetData.Item3;

        //征服模式: 按指定难度取该难度预生成的道路/关卡/路径数据(各难度在创建时已全部随出);
        //无尽模式无难度概念, 直接用当前字段值
        int roadNum = gameWorldInfoRandom.roadNum;
        int fightNum = gameWorldInfoRandom.fightNum;
        int roadLength = gameWorldInfoRandom.roadLength;
        if (gameWorldInfoRandom.gameFightType == GameFightTypeEnum.Conquer)
        {
            GameWorldDifficultyRandomBean difficultyRandom = gameWorldInfoRandom.GetDifficultyRandom(difficultyLevel);
            if (difficultyRandom != null)
            {
                roadNum = difficultyRandom.roadNum;
                fightNum = difficultyRandom.fightNum;
                roadLength = difficultyRandom.roadLength;
            }
        }

        var userUnlock = GameDataHandler.Instance.manager.GetUserData().GetUserUnlockData();
        //无尽模式无关卡数/路径长度/通关奖励(均为征服模式数据)
        bool isShowFightNum = gameWorldInfoRandom.gameFightType != GameFightTypeEnum.Infinite;

        //名字: 始终显示(不受研究门控)
        SetDetailsItem(ui_UIViewPopupProtalDetailsItem_Name, TextHandler.Instance.GetTextById(411), $"{gameWorldInfo.name_language}", true);
        //线路数量: 需解锁「线路数预览」研究, 未解锁整行隐藏
        SetDetailsItem(ui_UIViewPopupProtalDetailsItem_RoadNum, TextHandler.Instance.GetTextById(412), $"{roadNum}",
            userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadNum));
        //关卡数量: 需解锁「关卡数预览」研究 + 非无尽模式
        SetDetailsItem(ui_UIViewPopupProtalDetailsItem_FightNum, TextHandler.Instance.GetTextById(413), $"{fightNum}",
            isShowFightNum && userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewFightNum));
        //路径长度: 需解锁「路径长度预览」研究 + 非无尽模式
        SetDetailsItem(ui_UIViewPopupProtalDetailsItem_RoadLength, TextHandler.Instance.GetTextById(414), $"{roadLength}",
            isShowFightNum && userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewRoadLength));

        //奖励道具显示: 需解锁「奖励预览」研究 + 非无尽模式(无尽模式无通关奖励); 未解锁则不展示奖励
        bool isShowReward = isShowFightNum && userUnlock.CheckIsUnlock(UnlockEnum.PortalPreviewReward);
        List<ItemBean> listReward = isShowReward ? gameWorldInfoRandom.GetDifficultyReward(difficultyLevel) : null;
        RefreshRewardItems(listReward);

        //内容变化后立即重建布局(先重建奖励容器再重建整体), 保证气泡尺寸与跟随定位正确
        if (ui_Items != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(ui_Items);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    /// <summary>
    /// 设置单个详情项(标题+内容, 委托给绑定的 UIViewPopupPortalDetailsItem; 未解锁则整行隐藏)
    /// </summary>
    /// <param name="itemView">详情项视图</param>
    /// <param name="title">标题文本</param>
    /// <param name="content">内容文本</param>
    /// <param name="isShow">是否显示(false 时整行隐藏)</param>
    protected void SetDetailsItem(UIViewPopupPortalDetailsItem itemView, string title, string content, bool isShow = true)
    {
        if (itemView == null)
            return;
        itemView.SetData(title, content, isShow);
    }
    #endregion

    #region 奖励道具缓存池
    /// <summary>
    /// 按奖励列表刷新道具显示: 以模板 ui_UIViewItem 为池首项, 不足时克隆复用, 多余项隐藏
    /// </summary>
    /// <param name="listReward">该传送门世界预生成的奖励(无奖励/未解锁时全部隐藏)</param>
    protected void RefreshRewardItems(List<ItemBean> listReward)
    {
        if (ui_UIViewItem == null)
            return;
        //初始化缓存池(模板item作为池中第一个)
        if (listRewardItemPool == null)
        {
            listRewardItemPool = new List<UIViewItem>();
            listRewardItemPool.Add(ui_UIViewItem);
        }
        int rewardNum = listReward == null ? 0 : listReward.Count;
        //按需克隆扩容(以模板item为蓝本, 克隆到同一容器下)
        Transform itemParent = ui_UIViewItem.transform.parent;
        for (int i = listRewardItemPool.Count; i < rewardNum; i++)
        {
            GameObject objItem = Instantiate(ui_UIViewItem.gameObject, itemParent);
            objItem.transform.localScale = Vector3.one;
            UIViewItem itemView = objItem.GetComponent<UIViewItem>();
            listRewardItemPool.Add(itemView);
        }
        //填充奖励数据并隐藏多余项
        for (int i = 0; i < listRewardItemPool.Count; i++)
        {
            UIViewItem itemView = listRewardItemPool[i];
            if (itemView == null)
                continue;
            bool isShow = i < rewardNum;
            itemView.gameObject.SetActive(isShow);
            if (isShow)
                itemView.SetData(listReward[i]);
        }
    }
    #endregion
}
