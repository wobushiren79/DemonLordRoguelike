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

    /// <summary>
    /// 初始化场景
    /// </summary>
    public override void InitSceneData()
    {
        base.InitSceneData();
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public override void RefreshScene()
    {
        base.RefreshScene();
    }

    /// <summary>
    /// 初始化宝箱
    /// </summary>
    public void InitBox(List<ItemBean> listReward)
    {
        for (int i = 0; i < listReward.Count; i++)
        {
            ItemBean itemData = listReward[i];
            GameObject objItemBox = Instantiate(objBoxContainer, objBoxModel);
            float offsetX = VectorUtil.GetCenterToTwoSide(0, 2.5f, listReward.Count, i);
            objItemBox.transform.position = new Vector3(offsetX, 0, 0);
            objItemBox.name = $"{i}";
            InitBoxItem(objItemBox, itemData);
        }
    }

    /// <summary>
    /// 初始化宝箱道具
    /// </summary>
    public void InitBoxItem(GameObject objBox, ItemBean itemData)
    {
        Transform tfItem = objBox.transform.Find("RewardSelectBoxItem");
        Transform tfItemRenderer = tfItem.Find("Renderer");
        SpriteRenderer srItemRenderer = tfItemRenderer.GetComponent<SpriteRenderer>();
        IconHandler.Instance.SetItemIcon(itemData.itemsInfo.icon_res, itemData.itemsInfo.icon_rotate_z, srItemRenderer);
        //先隐藏道具 点选之后再显示
        tfItem.gameObject.SetActive(false);
    }

    /// <summary>
    /// 选择宝箱
    /// </summary>
    /// <param name="objBox"></param>
    public int SelectBox(GameObject objBox, bool isCanSelect)
    {
        Transform tfItem = objBox.transform.Find("RewardSelectBoxItem");
        Transform tfBox = objBox.transform.Find("Box");
        //如果宝箱已经打开 则展示道具详情
        if (tfBox.gameObject.activeSelf == false)
        {
            return 2;
        }
        //如果宝箱没有打开 则打开宝箱
        else
        {
            //能否打开
            if (isCanSelect)
            {
                tfBox.gameObject.SetActive(false);
                tfItem.gameObject.SetActive(true);

                var taskShowBoxItem = ShowBoxItem(objBox);
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
    
    /// <summary>
    /// 打开所有宝箱
    /// </summary>
    /// <returns></returns>
    public async Task ShowAllBoxItem()
    {
        for (int i = 0; i < objBoxContainer.transform.childCount; i++)
        {
            var itemObjBox = objBoxContainer.transform.GetChild(i);
            await ShowBoxItem(itemObjBox.gameObject);
        }
    }

    /// <summary>
    /// 打开宝箱展示物品
    /// </summary>
    /// <returns></returns>
    public async Task ShowBoxItem(GameObject objBox)
    {
        
    }
}