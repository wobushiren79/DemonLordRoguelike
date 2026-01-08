

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIRewardSelect : BaseUIComponent
{
    public Action actionForEnd = null;
    public RewardSelectBean rewardSelectData;
    public ScenePrefabForRewardSelect scenePrefab;

    public override void RefreshUI(bool isOpenInit = false)
    {
        base.RefreshUI(isOpenInit);
        if (!isOpenInit)
        {
            SetSelectNumText(rewardSelectData.selectNum, rewardSelectData.selectNumMax); 
        }
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public async void SetData(RewardSelectBean rewardSelectData, Action actionForEnd = null)
    {
        this.rewardSelectData = rewardSelectData;
        gameObject.SetActive(false);
        this.actionForEnd = actionForEnd;

        await WorldHandler.Instance.EnterRewardSelectScene();
        //场景实例
        var scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForRewardSelect>(GameSceneTypeEnum.RewardSelect);
        //初始化宝箱
        await scenePrefab.InitRewardBox(rewardSelectData.listReward);

        gameObject.SetActive(true);
        //刷新UI显示
        RefreshUI();
    }

    /// <summary>
    /// 设置剩余选择次数
    /// </summary>
    public void SetSelectNumText(int selectNum, int selectNumMax)
    {
        ui_TitleTextNum.text = string.Format(TextHandler.Instance.GetTextById(52003), selectNumMax - selectNum, selectNumMax);
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_SkipBtn)
        {
            OnClickForSkip();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        switch (inputType)
        {
            case InputActionUIEnum.Click:
                OnClickForSelectBox();
                break;
        }
    }

    /// <summary>
    /// 点击选择宝箱
    /// </summary>
    public void OnClickForSelectBox()
    {
        if (gameObject.activeSelf == false) return;
        
        ShowItemDetails(false, null);
        LogUtil.Log("OnClickForSelectBox");
        RayUtil.RayToScreenPointForMousePosition(100, 1 << LayerInfo.Other, out bool isCollider, out RaycastHit hit);
        if (isCollider)
        {
            Collider targetCollider = hit.collider;
            int boxIndex = int.Parse(targetCollider.gameObject.name);
            ItemBean itemData = rewardSelectData.listReward[boxIndex];
            //设置是否能选择 如果已经超过选择次数 则不能选择
            bool isCanSelect = rewardSelectData.selectNum >= rewardSelectData.selectNumMax ? false : true;
            int boxOpenState = scenePrefab.OpenRewardBox(targetCollider.gameObject, isCanSelect);
            switch (boxOpenState)
            {
                case 0://打开失败 没有次数
                    UIHandler.Instance.ToastHintText(TextHandler.Instance.GetTextById(52004));
                    break;
                case 1://打开宝箱
                    UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
                    //添加道具到背包里
                    userData.AddBackpackItem(itemData);
                    //数量+1
                    rewardSelectData.selectNum++;
                    //刷新UI
                    RefreshUI();
                    //展示道具详情
                    ShowItemDetails(true, itemData);
                    break;
                case 2://展示道具详情
                    ShowItemDetails(true, itemData);
                    break;
            }
        }
    }

    /// <summary>
    /// 展示道具详情
    /// </summary>
    public void ShowItemDetails(bool isShowDetails, ItemBean itemData)
    {
        if (isShowDetails)
        {
            ui_UIPopupItemInfo.gameObject.SetActive(true);
            ui_UIPopupItemInfo.SetData(itemData);
        }
        else
        {
            ui_UIPopupItemInfo.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 点击跳过
    /// </summary>
    public void OnClickForSkip()
    {
        LogUtil.Log("OnClickForSkip");
        ShowItemDetails(false, null);
        //如果还有未选择次数 提示一下
        if (rewardSelectData.selectNum < rewardSelectData.selectNumMax)
        {
            DialogBean dialogData = new DialogBean();
            dialogData.content = TextHandler.Instance.GetTextById(52005);
            dialogData.actionSubmit = (view, data) =>
            {
                OpenAllRewardBoxPreview();
            };
            UIHandler.Instance.ShowDialogNormal(dialogData);
            return;
        }
        //展示其他未选择的宝箱物品并且结束
        OpenAllRewardBoxPreview();
    }

    /// <summary>
    /// 展示其他未选择的宝箱物品并且结束
    /// </summary>
    public async void OpenAllRewardBoxPreview()
    {
        gameObject.SetActive(false);
        //展示所有宝箱
        await scenePrefab.OpenAllRewardBoxPreview();
        //结束回调
        actionForEnd?.Invoke();
    }
}