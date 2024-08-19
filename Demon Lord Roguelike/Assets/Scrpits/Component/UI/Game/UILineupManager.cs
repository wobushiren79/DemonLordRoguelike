using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UILineupManager : BaseUIComponent
{
    public List<CreatureBean> listBackpackCreature = new List<CreatureBean>();

    public override void Awake()
    {
        base.Awake();
        ui_BackpackContent.AddCellListener(OnCellChangeForBackpack);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerEnter, EventForCardPointerEnter);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnPointerExit, EventForCardPointerExit);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForOnClickSelect);
        InitBackpackData();
        InitLineupData();
    }


    /// <summary>
    /// item�����仯
    /// </summary>
    /// <param name="itemCell"></param>
    public void OnCellChangeForBackpack(ScrollGridCell itemCell)
    {
        var itemData = listBackpackCreature[itemCell.index];
        var itemView = itemCell.GetComponent<UIViewCreatureCardItem>();
        itemView.SetData(itemData, CardUseState.LineupBackpack);
    }

    /// <summary>
    /// ��ʼ��������Ƭ����
    /// </summary>
    public void InitBackpackData()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        listBackpackCreature.Clear();
        listBackpackCreature.AddRange(userData.listBackpackCreature);
        //��������
        ui_BackpackContent.SetCellCount(userData.listBackpackCreature.Count);
    }

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    public void InitLineupData()
    {
       UserDataBean userData =  GameDataHandler.Instance.manager.GetUserData();
    }


    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback); 
        if(inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// �¼�-����ѡ�п�Ƭ
    /// </summary>
    public void EventForCardPointerEnter(UIViewCreatureCardItem targetView)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(true);
        ui_ViewCreatureCardDetails.SetData(targetView.cardData.creatureData);
    }

    /// <summary>
    /// �¼�-�����뿪
    /// </summary>
    public void EventForCardPointerExit(UIViewCreatureCardItem targetView)
    {
        ui_ViewCreatureCardDetails.gameObject.SetActive(false);
    }

    /// <summary>
    /// �¼�-���
    /// </summary>
    public void EventForOnClickSelect(UIViewCreatureCardItem targetView)
    {

    }
}
