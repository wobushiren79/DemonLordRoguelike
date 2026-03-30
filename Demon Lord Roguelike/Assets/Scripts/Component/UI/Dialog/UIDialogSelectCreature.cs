

using System.Collections.Generic;
using UnityEngine.UI;

public partial class UIDialogSelectCreature : DialogView
{
    //当前选中的生物
    public List<CreatureBean> listSelect;
    public DialogSelectCreatureBean dialogSelectCreatureData;
    
    public override void SetData(DialogBean dialogData)
    {
        base.SetData(dialogData);
        if (listSelect == null)
        {
            listSelect = new List<CreatureBean>();
        }
        listSelect.Clear();

        dialogSelectCreatureData = dialogData as DialogSelectCreatureBean;

        var userData = GameDataHandler.Instance.manager.GetUserData();
        var listBackpackCreature = userData.listBackpackCreature;
        ui_UIViewCreatureCardList.SetData(listBackpackCreature, CardUseStateEnum.SelectCreature,OnCellChangeForSelectCreature);
        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        RefreshUI();
    }

    public void RefreshUI()
    {
        SetSelectNum(listSelect.Count, dialogSelectCreatureData.selectNumMax);
        ui_UIViewCreatureCardList.RefreshAllCard();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_UIViewExit)
        {
            CancelOnClick();
        }
    }
    

    /// <summary>
    /// 生物列表
    /// </summary>
    public void OnCellChangeForSelectCreature(int index, UIViewCreatureCardItem uiViewCreature, CreatureBean creatureData)
    {
        if (listSelect.Contains(creatureData))
        {
            uiViewCreature.SetCardState(CardStateEnum.SelectCreatureSelect);
        }
        else
        {
            uiViewCreature.SetCardState(CardStateEnum.SelectCreatureNoSelect);
        }
    }

    /// <summary>
    /// 生物选择
    /// </summary>
    /// <param name="uiViewCreatureCardItem"></param>
    public void EventForCardClickSelect(UIViewCreatureCardItem uiViewCreatureCardItem)
    {
        //反选取消
        if (listSelect.Contains(uiViewCreatureCardItem.cardData.creatureData))
        {
            listSelect.Remove(uiViewCreatureCardItem.cardData.creatureData);
        }
        //选择
        else
        {
            //如果已经超过最大选择数量
            if (listSelect.Count >= dialogSelectCreatureData.selectNumMax)
            {
                UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(1005003));
            }
            else
            {
                listSelect.Add(uiViewCreatureCardItem.cardData.creatureData);
            }
        }
        RefreshUI();
    }

    /// <summary>
    /// 设置选择数量
    /// </summary>
    public void SetSelectNum(int selectNum, int maxNum)
    {
        ui_NumText.text = string.Format(TextHandler.Instance.GetTextById(1005002), selectNum, maxNum);
    }

    public override void SubmitOnClick()
    {
        if (listSelect.IsNull())
        {
            UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(3000004));
            return;            
        }
        base.SubmitOnClick();
    }

}