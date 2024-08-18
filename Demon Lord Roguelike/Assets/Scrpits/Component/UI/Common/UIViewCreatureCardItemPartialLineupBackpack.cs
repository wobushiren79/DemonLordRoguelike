using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//战斗卡片特殊设置
public partial class UIViewCreatureCardItem
{

    /// <summary>
    /// 点击选择卡片
    /// </summary>
    public void OnClickSelectForLineup()
    {
        LogUtil.Log($"OnClickSelectForLineup");
    }
    public void OnClickSelectForLineupBackpack()
    {
        LogUtil.Log($"OnClickSelectForLineupBackpack");
    }

    #region 触摸相关事件

    /// <summary>
    /// 触摸-进入
    /// </summary>
    void OnPointerEnterForLineup(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerEnter_{originalSibling}");
        timeUpdateForShowDetails = 0;
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //设置层级最上
        transform.SetAsLastSibling();
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, true);
    }

    void OnPointerEnterForLineupBackpack(PointerEventData eventData)
    {
        LogUtil.Log($"OnPointerEnter");
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, creatureData);
    }

    /// <summary>
    /// 触摸-退出
    /// </summary>
    void OnPointerExitForLineup(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{originalSibling}");
        timeUpdateForShowDetails = -1;
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //还原层级
        transform.SetSiblingIndex(originalSibling);
        //触发避让事件
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, false);
        //隐藏卡片详情
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_HideDetails, fightCreatureData);
    }

    void OnPointerExitForLineupBackpack(PointerEventData eventData)
    {
        LogUtil.Log($"OnPointerExit");
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerExit, creatureData);
    }
    #endregion

}
