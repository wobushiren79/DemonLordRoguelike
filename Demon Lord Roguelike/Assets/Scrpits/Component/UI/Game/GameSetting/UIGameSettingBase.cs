using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameSettingBase
{
    public GameObject objListContainer;
    //游戏设置
    public GameConfigBean gameConfig;

    public UIGameSettingBase(GameObject objListContainer)
    {
        this.objListContainer = objListContainer;
    }

    public virtual void Open()
    {
        CptUtil.RemoveChildsByActive(objListContainer);
        gameConfig = GameDataHandler.Instance.manager.GetGameConfig();
    }

    public UIViewGameSettingSelect CreatureItemForSelect(string titleName, List<string> listSelect)
    {
        var targetObj = LoadItem("UIViewGameSettingSelect");
        var targetView = targetObj.GetComponent<UIViewGameSettingSelect>();
        targetView.SetCallBack(ActionForSelectValueChange);
        targetView.SetTitle(titleName);
        targetView.SetListSelect(listSelect);
        return targetView;
    }

    public UIViewGameSettingCheckBox CreatureItemForCheckBox(string titleName, string selectStr = null, string unselectStr = null)
    {
        var targetObj = LoadItem("UIViewGameSettingCheckBox");
        var targetView = targetObj.GetComponent<UIViewGameSettingCheckBox>();
        targetView.SetCallBack(ActionForCheckBoxValueChange);
        targetView.SetTitle(titleName);
        if (selectStr == null)
        {
            selectStr = TextHandler.Instance.GetTextById(40998);
        }
        if (unselectStr == null)
        {
            unselectStr = TextHandler.Instance.GetTextById(40999);
        }
        targetView.SetCheckStr(selectStr, unselectStr);
        return targetView;
    }

    public UIViewGameSettingRange CreatureItemForRange(string titleName, float rangeMin, float rangeMax)
    {
        var targetObj = LoadItem("UIViewGameSettingRange");
        var targetView = targetObj.GetComponent<UIViewGameSettingRange>();
        targetView.SetCallBack(ActionForRangeValueChange);
        targetView.SetTitle(titleName);
        targetView.SetMinAndMax(rangeMin, rangeMax);
        return targetView;
    }

    /// <summary>
    ///  读取控件
    /// </summary>
    protected GameObject LoadItem(string itemName)
    {
        GameObject objItemModel = LoadResourcesUtil.SyncLoadData<GameObject>($"UI/GameSetting/{itemName}");
        GameObject objItem = UIHandler.Instance.Instantiate(objListContainer, objItemModel);
        return objItem;
    }


    /// <summary>
    /// 回调
    /// </summary>
    public virtual void ActionForRangeValueChange(UIViewGameSettingRange targetView, float progress)
    {

    }

    public virtual void ActionForSelectValueChange(UIViewGameSettingSelect targetView, int index)
    {

    }

    public virtual void ActionForCheckBoxValueChange(UIViewGameSettingCheckBox targetView, bool isCheck)
    {

    }
}
