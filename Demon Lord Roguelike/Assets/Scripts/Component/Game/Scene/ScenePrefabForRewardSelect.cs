using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;
using Unity.Burst.Intrinsics;
using System.Threading.Tasks;

public class ScenePrefabForRewardSelect : ScenePrefabBase
{
    //箱子预制
    public GameObject objBoxModel;
    //箱子容积
    public GameObject objBoxContainer;
    //箱子列表
    public List<RewardSelectBoxComponent> listRewardSelectBox = new List<RewardSelectBoxComponent>();

    /// <summary>
    /// 初始化场景
    /// </summary>
    public override async Task InitSceneData()
    {
        await base.InitSceneData();
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public override async Task RefreshScene()
    {
        await base.RefreshScene();
    }

    /// <summary>
    /// 初始化宝箱(实例化+数据初始化+预热渲染 不播落地动画;落地动画由 PlayAllBoxShowAnim 统一触发)
    /// 调用时机应在遮罩盖住屏幕期间:预热渲染的shader编译/灯光/粒子激活开销都被遮罩盖住
    /// </summary>
    public async Task InitRewardBox(List<ItemBean> listReward)
    {
        for (int i = 0; i < listReward.Count; i++)
        {
            ItemBean itemData = listReward[i];
            GameObject objItemBox = Instantiate(objBoxContainer, objBoxModel);
            var itemBox = objItemBox.GetComponent<RewardSelectBoxComponent>();

            //设置箱子名字和位置
            float offsetX = VectorUtil.GetCenterToTwoSide(0, 2.5f, listReward.Count, i);
            objItemBox.transform.position = new Vector3(offsetX, 0, 0);
            objItemBox.transform.eulerAngles = new Vector3(0, 180, 0);
            objItemBox.name = $"{i}";
            //初始化箱子数据
            itemBox.InitData(itemData);
            //添加箱子到列表
            listRewardSelectBox.Add(itemBox);
        }
        //预热渲染:短暂激活所有宝箱与道具渲染2帧 把shader编译/实时灯/粒子的首次激活开销在此消化掉(遮罩盖着 玩家不可见)
        for (int i = 0; i < listRewardSelectBox.Count; i++)
        {
            listRewardSelectBox[i].SetPrewarmActive(true);
        }
        await new WaitForEndOfFrame();
        await new WaitForEndOfFrame();
        for (int i = 0; i < listRewardSelectBox.Count; i++)
        {
            listRewardSelectBox[i].SetPrewarmActive(false);
        }
    }

    /// <summary>
    /// 播放所有宝箱的落地(Show)动画 并等待全部落地完成
    /// </summary>
    public async Task PlayAllBoxShowAnim()
    {
        List<Task> listTaskShowAnim = new List<Task>();
        for (int i = 0; i < listRewardSelectBox.Count; i++)
        {
            //每个箱子随机延迟出现 错开落地节奏
            float timeShowDelay = UnityEngine.Random.Range(0f, 0.2f);
            listTaskShowAnim.Add(listRewardSelectBox[i].PlayShowAnim(timeShowDelay));
        }
        //等待最后一个箱子落地动画播完(等所有任务=取最大延迟+动画时长 而非延迟累加 箱数多时不再成倍拉长)
        await Task.WhenAll(listTaskShowAnim);
    }

    /// <summary>
    /// 选择宝箱
    /// </summary>
    /// <param name="objBox"></param>
    public int OpenRewardBox(GameObject objBox, bool isCanOpen)
    {
        var targetBoxView = objBox.GetComponent<RewardSelectBoxComponent>();
        //如果宝箱还未打开
        if (targetBoxView.rewardSelectBoxState == RewardSelectBoxStateEnum.Idle)
        {
            //能否打开
            if (isCanOpen)
            {
                var openTask = targetBoxView.OpenBox();
                return 1;
            }
            //不能打开 次数已经使用完
            else
            {
                return 0;
            }
        }
        //如果宝箱已经打开
        else
        {
            return 2;
        }
    }
    
    /// <summary>
    /// 打开所有宝箱预览
    /// </summary>
    /// <returns></returns>
    public async Task OpenAllRewardBoxPreview()
    {
        //每个箱子的开箱间隔
        float timeOpenInterval = 0.5f;
        bool isFirstOpen = true;
        for (int i = 0; i < listRewardSelectBox.Count; i++)
        {
            var itemView = listRewardSelectBox[i];
            //已打开的箱子跳过 不触发开箱也不占间隔
            if (itemView.rewardSelectBoxState == RewardSelectBoxStateEnum.Open)
                continue;
            //第一个箱子立即打开 之后每个间隔0.5秒
            if (!isFirstOpen)
            {
                await new WaitForSeconds(timeOpenInterval);
            }
            isFirstOpen = false;
            //开火即忘 不等单个开箱动画播完 让箱子连续打开
            _ = itemView.OpenBoxForPreview();
        }
        //最后固定等1秒后结束
        await new WaitForSeconds(1f);
    }

}