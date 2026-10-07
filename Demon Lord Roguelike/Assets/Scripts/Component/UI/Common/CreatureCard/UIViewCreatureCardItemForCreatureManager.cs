using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//魔物管理卡片特殊设置
public partial class UIViewCreatureCardItemForCreatureManager : UIViewCreatureCardItem
{
    //缓存池里的阵容标记Item
    protected Queue<TextMeshProUGUI> queuePoolMarkItem = new Queue<TextMeshProUGUI>();
    //展示中的阵容标记Item
    protected List<TextMeshProUGUI> listShowMarkItem = new List<TextMeshProUGUI>();

    #region 重写
    /// <summary>
    /// 初始化:阵容标记模板Item仅作实例化模板(隐藏不直接展示);标记整体为纯展示不响应射线(与卡片其它遮罩元素一致,避免挡住卡片点击)
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        foreach (var graphic in ui_CreatureLineUpMark.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
        ui_CreatureLineUpMarkItem.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public override void SetData(CreatureBean creatureData, CardUseStateEnum cardUseState)
    {
        base.SetData(creatureData, cardUseState);
        SetLineupMark(creatureData);
    }

    /// <summary>
    /// 刷新状态
    /// </summary>
    public override void RefreshCardState(CardStateEnum cardState)
    {
        base.RefreshCardState(cardState);
        switch (cardState)
        {
            case CardStateEnum.CreatureManagerNoSelect:
                break;
            case CardStateEnum.CreatureManagerSelect:
                ui_SelectBg.gameObject.SetActive(true);
                break;
        }
    }
    #endregion

    #region 阵容标记
    /// <summary>
    /// 设置阵容标记:逐行展示生物所在的全部阵容名字(一个生物可同时属于多套阵容);不在任何阵容时隐藏整个标记
    /// </summary>
    public void SetLineupMark(CreatureBean creatureData)
    {
        if (creatureData == null)
        {
            RefreshLineupMarkItemCount(0);
            ui_CreatureLineUpMark.gameObject.SetActive(false);
            return;
        }
        var userData = GameDataHandler.Instance.manager.GetUserData();
        List<int> listLineupIndex = userData.GetLineupIndexes(creatureData.creatureUUId);
        if (listLineupIndex.Count <= 0)
        {
            //不在任何阵容:回收所有展示Item并隐藏标记
            RefreshLineupMarkItemCount(0);
            ui_CreatureLineUpMark.gameObject.SetActive(false);
            return;
        }
        ui_CreatureLineUpMark.gameObject.SetActive(true);
        //按阵容数量增删展示Item,再逐行设置阵容显示名(自定义名优先,否则默认「阵容 {序号}」)
        RefreshLineupMarkItemCount(listLineupIndex.Count);
        for (int i = 0; i < listLineupIndex.Count; i++)
        {
            listShowMarkItem[i].text = userData.GetLineupShowName(listLineupIndex[i]);
        }
    }

    /// <summary>
    /// 调整展示中的阵容标记Item数量:不足时从对象池取/按模板实例化补充,多余的隐藏回收进池(卡片被列表复用时池随卡片实例保留)
    /// </summary>
    /// <param name="targetCount">目标展示数量</param>
    protected void RefreshLineupMarkItemCount(int targetCount)
    {
        //补充不足的Item
        while (listShowMarkItem.Count < targetCount)
        {
            TextMeshProUGUI itemMark;
            if (queuePoolMarkItem.Count > 0)
            {
                itemMark = queuePoolMarkItem.Dequeue();
            }
            else
            {
                itemMark = ui_CreatureLineUpMarkItem.Instantiate(ui_CreatureLineUpMark);
            }
            itemMark.gameObject.SetActive(true);
            listShowMarkItem.Add(itemMark);
        }
        //回收多余的Item
        while (listShowMarkItem.Count > targetCount)
        {
            int lastIndex = listShowMarkItem.Count - 1;
            TextMeshProUGUI itemMark = listShowMarkItem[lastIndex];
            itemMark.gameObject.SetActive(false);
            queuePoolMarkItem.Enqueue(itemMark);
            listShowMarkItem.RemoveAt(lastIndex);
        }
    }
    #endregion
}
