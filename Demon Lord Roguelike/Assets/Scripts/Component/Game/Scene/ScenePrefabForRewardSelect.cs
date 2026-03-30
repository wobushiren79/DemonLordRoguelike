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
    /// 初始化宝箱
    /// </summary>
    public async Task InitRewardBox(List<ItemBean> listReward)
    {
        float totalTimeShowDelay = 0;
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
            //随机等待一段时间出现
            float timeShowDelay = UnityEngine.Random.Range(0f, 0.2f);
            totalTimeShowDelay += timeShowDelay;
            //初始化箱子
            var taskInitData = itemBox.InitData(itemData, timeShowDelay);
            //添加箱子到列表
            listRewardSelectBox.Add(itemBox);
        }
        await new WaitForSeconds(totalTimeShowDelay);    
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
        float showTimeMax = 0;
        for (int i = 0; i < listRewardSelectBox.Count; i++)
        {
            var itemView = listRewardSelectBox[i];
            showTimeMax = await itemView.OpenBoxForPreview();
        }
        //再额外等2秒
        showTimeMax += 2;

        await new WaitForSeconds(showTimeMax);
    }

}