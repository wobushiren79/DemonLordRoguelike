

using System;
using System.Threading.Tasks;
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
    public void SetData(RewardSelectBean rewardSelectData, Action actionForEnd = null, bool isClearLastGame = false)
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
            //再等1帧 确保场景在遮罩下完成首帧渲染(UI保持隐藏 等首箱打开后再显示)
            await new WaitForEndOfFrame();
            //揭开遮罩
            maskUI.EndMask(TimeMaskFadeOut, null, null);
            //进入奖励选择场景时播放奖励音效
            AudioHandler.Instance.PlaySound(AudioEnum.sound_reward_6);
            //宝箱落地动画随遮罩淡出同步开始
            await scenePrefab.PlayAllBoxShowAnim();
            //全部宝箱落地后 自动打开第一个宝箱(首箱保底奖励直接入账 不消耗选择次数) 并等开箱动画播完
            await AutoOpenFirstRewardBox();
            //首箱打开后再显示UI(此期间UI隐藏 点击/跳过均被屏蔽 玩家只能看完首箱开启演出)
            gameObject.SetActive(true);
            RefreshUI();
        });
    }

    /// <summary>
    /// 自动打开第一个宝箱（首箱保底：已解锁装备=装备位/未解锁装备时为魔晶），等开箱动画播完；奖励直接入账且不消耗选择次数
    /// </summary>
    public async Task AutoOpenFirstRewardBox()
    {
        //防御：无奖励数据或场景未生成宝箱时不处理
        if (rewardSelectData.listReward == null || rewardSelectData.listReward.Count == 0) return;
        if (scenePrefab.listRewardSelectBox == null || scenePrefab.listRewardSelectBox.Count == 0) return;
        var firstBox = scenePrefab.listRewardSelectBox[0];
        //仅 Idle 状态才自动开(理论上玩家抢先点开则不重复发奖)
        if (firstBox.rewardSelectBoxState != RewardSelectBoxStateEnum.Idle) return;
        //首箱为保底赠送 不占用选择次数；等开箱动画播完(道具升起落定)再返回
        await firstBox.OpenBox();
        ItemBean itemData = rewardSelectData.listReward[0];
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        //添加道具到背包里
        userData.AddBackpackItem(itemData);
        //刷新UI
        RefreshUI();
        //展示道具详情
        ShowItemDetails(true, itemData);
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