

using System;
using UnityEngine;
using UnityEngine.UI;

public partial class UIGameConversation : BaseUIComponent
{
    public GameObject creatureObj;
    public CreatureBean creatureData;
    public Action acionForEnd;

    public override void OpenUI()
    {
        base.OpenUI();

    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(GameObject creatureObj, CreatureBean creatureData, string content, Action acionForEnd)
    {
        this.creatureObj = creatureObj;
        this.creatureData = creatureData;
        this.acionForEnd = acionForEnd;
        SetCardIcon(creatureData);
        SetName(creatureData.creatureName);
        SetContent(content);
        ui_IconContent.SetData(creatureData, PopupEnum.CreatureCardDetails);
    }

    /// <summary>
    /// 设置名字
    /// </summary>
    /// <param name="name"></param>
    public void SetName(string name)
    {
        ui_Name.text = $"{name}";
    }

    /// <summary>
    /// 设置内容
    /// </summary>
    public void SetContent(string content)
    {
        ui_TalkText.text = $"{content}";
    }

    /// <summary>
    /// 设置卡片图像
    /// </summary>
    public void SetCardIcon(CreatureBean creatureData)
    {
        //比原始大小放大2倍
        GameUIUtil.SetCreatureUIForSimple(ui_Icon, creatureData, scale: 2);
    }

    #region 点击事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_BG)
        {
            OnClickForEnd();
        }
        else if (viewButton == ui_Gift)
        {
            OnClickForGift();
        }
    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd()
    {
        acionForEnd?.Invoke();
    }

    /// <summary>
    /// 点击贿赂
    /// </summary>
    public void OnClickForGift()
    {
        DialogSelectItemBean dialogData = new DialogSelectItemBean();
        dialogData.actionForSelectGift = ActionForItemSelectGift;
        UIHandler.Instance.ShowDialogItemSelect(dialogData);
    }
    #endregion
    
    #region 道具使用回调
    public void ActionForItemSelectGift(UIDialogSelectItem dialogView, ItemBean itemData)
    {
        dialogView.DestroyDialog();
        //从背包里删除这个道具
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.RemoveItem(itemData);
        //添加好感
        var rarityInfo = RarityInfoCfg.GetItemData(itemData.rarity);
        if (rarityInfo != null)
        {
            creatureData.AddRelationship(rarityInfo.item_add_relationship);
        }
        //播放增加好感的粒子
        EffectBean effectData = new EffectBean();
        effectData.effectName = "EffectAddRelationship_1";
        effectData.timeForShow = 1f;
        effectData.effectPosition = creatureObj.transform.position;
        EffectHandler.Instance.ShowEffect(effectData);
    }
    #endregion
}