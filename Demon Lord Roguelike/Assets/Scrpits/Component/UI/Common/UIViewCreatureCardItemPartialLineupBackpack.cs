using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

//ս����Ƭ��������
public partial class UIViewCreatureCardItem
{

    /// <summary>
    /// ���ѡ��Ƭ
    /// </summary>
    public void OnClickSelectForLineup()
    {
        LogUtil.Log($"OnClickSelectForLineup");
    }
    public void OnClickSelectForLineupBackpack()
    {
        LogUtil.Log($"OnClickSelectForLineupBackpack");
    }

    #region ��������¼�

    /// <summary>
    /// ����-����
    /// </summary>
    void OnPointerEnterForLineup(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerEnter_{originalSibling}");
        timeUpdateForShowDetails = 0;
        KillAnimForSelect();
        animForSelectStart = rectTransform
                .DOScale(new Vector3(animCardSelectStartScale, animCardSelectStartScale, animCardSelectStartScale), animCardSelectStartTime)
                .SetEase(animCardSelectStart);
        //���ò㼶����
        transform.SetAsLastSibling();
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, true);
    }

    void OnPointerEnterForLineupBackpack(PointerEventData eventData)
    {
        LogUtil.Log($"OnPointerEnter");
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, creatureData);
    }

    /// <summary>
    /// ����-�˳�
    /// </summary>
    void OnPointerExitForLineup(PointerEventData eventData)
    {
        //LogUtil.Log($"OnPointerExit_{originalSibling}");
        timeUpdateForShowDetails = -1;
        KillAnimForSelect();
        animForSelectEnd = rectTransform
                .DOScale(Vector3.one, animCardSelectEndTime)
                .SetEase(animCardSelectEnd);
        //��ԭ�㼶
        transform.SetSiblingIndex(originalSibling);
        //���������¼�
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_SelectKeep, originalSibling, originalCardPos, false);
        //���ؿ�Ƭ����
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_HideDetails, fightCreatureData);
    }

    void OnPointerExitForLineupBackpack(PointerEventData eventData)
    {
        LogUtil.Log($"OnPointerExit");
        TriggerEvent(EventsInfo.UIViewCreatureCardItem_OnPointerExit, creatureData);
    }
    #endregion

}
