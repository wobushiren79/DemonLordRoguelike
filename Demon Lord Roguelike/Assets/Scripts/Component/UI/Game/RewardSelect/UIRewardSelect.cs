

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

    //遮罩淡入/淡出时长(秒) 用于遮盖领奖场景加载与宝箱预热渲染的卡顿帧
    public const float TimeMaskFadeIn = 0.3f;
    public const float TimeMaskFadeOut = 0.4f;

    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="rewardSelectData">领奖数据</param>
    /// <param name="actionForEnd">领奖结束回调</param>
    /// <param name="isClearLastGame">是否先清理上一场战斗(卸载战斗场景+清理战斗实体)。征服模式通关BOSS后进入领奖需传 true，避免BOSS战斗场景残留叠加</param>
    public async void SetData(RewardSelectBean rewardSelectData, Action actionForEnd = null, bool isClearLastGame = false)
    {
        this.rewardSelectData = rewardSelectData;
        gameObject.SetActive(false);
        this.actionForEnd = actionForEnd;

        //先淡入遮罩盖住屏幕 把清场/场景加载/预热渲染等重活藏在遮罩后执行 避免卡顿帧直接暴露给玩家
        UICommonMask maskUI = UIHandler.Instance.OpenUI<UICommonMask>(layer: 99);
        maskUI.StartMask(TimeMaskFadeIn, null, async () =>
        {
            //遮罩完全盖住后开始重活
            await WorldHandler.Instance.EnterRewardSelectScene(isClearLastGame);
            //场景实例
            scenePrefab = WorldHandler.Instance.GetCurrentScenePrefab<ScenePrefabForRewardSelect>(GameSceneTypeEnum.RewardSelect);
            //初始化宝箱(实例化后先预热渲染2帧 完成shader编译/灯光/粒子预热 此时尚有遮罩 玩家不可见)
            await scenePrefab.InitRewardBox(rewardSelectData.listReward);
            //UI显活并刷新(Canvas重建尖峰同样藏在遮罩后)
            gameObject.SetActive(true);
            RefreshUI();
            //再等1帧 确保UI与场景在遮罩下都完成了首帧渲染
            await new WaitForEndOfFrame();
            //揭开遮罩
            maskUI.EndMask(TimeMaskFadeOut, null, null);
            //进入奖励选择场景时播放奖励音效
            AudioHandler.Instance.PlaySound(AudioEnum.sound_reward_6);
            //宝箱落地动画随遮罩淡出同步开始
            await scenePrefab.PlayAllBoxShowAnim();
        });
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