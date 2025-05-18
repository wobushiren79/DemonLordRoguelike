
using System.Collections.Generic;
using Cinemachine;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;

public partial class UICreatureVat : BaseUIComponent
{
    //当前容器序号
    public int currentIndexVat;
    //场景预制
    public ScenePrefabForBase scenePrefab;
    //摄像头
    public CinemachineVirtualCamera vatCamera;

    //当前目标生物
    public List<CreatureBean> listTargetCreatureShow = new List<CreatureBean>();
    public List<CreatureBean> listMaterialCreatureShow = new List<CreatureBean>();

    //当前选中的生物
    public CreatureBean targetCreatureSelect;
     public List<CreatureBean> listMaterialCreatureSelect = new List<CreatureBean>();

    public override void OpenUI()
    {
        base.OpenUI();

        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);

        //场景实例
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        scenePrefab = baseSceneObj.GetComponent<ScenePrefabForBase>();
        //获取摄像头
        GameControlHandler.Instance.SetBaseControl(false);
        vatCamera = CameraHandler.Instance.SetCreatureVatCamera(int.MaxValue, true);
        //设置数据
        SetCurrentVat(0);
        RefreshVatState();
    }

    /// <summary>
    /// 设置容器状态
    /// </summary>
    public void RefreshVatState(bool hasAnim = false)
    {
        ui_BtnStart.gameObject.SetActive(false);
        ui_BtnEnd.gameObject.SetActive(false);
        ui_BtnComplete.gameObject.SetActive(false);

        AnimForListShow(ui_UIViewCreatureCardList_Target.transform, false, false);
        AnimForListShow(ui_UIViewCreatureCardList_Material.transform, false, false);

        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        //检测是否有数据
        var ascendData = userAscend.GetAscendData(currentIndexVat);
        if (ascendData != null)
        {
            //是否已经完成
            if (ascendData.progress == 1)
            {
                ui_BtnComplete.gameObject.SetActive(true);
            }
            else
            {
                ui_BtnEnd.gameObject.SetActive(true);
            }
        }
        //如果没有数据
        else
        {
            ui_BtnStart.gameObject.SetActive(true);
            ui_UIViewCreatureCardList_Material.gameObject.SetActive(true);
            AnimForListShow(ui_UIViewCreatureCardList_Target.transform, true, false);
        }
    }

    /// <summary>
    /// 设置当前容器数据
    /// </summary>
    public void SetCurrentVat(int indexVat)
    {
        targetCreatureSelect = null;
        listMaterialCreatureSelect.Clear();

        currentIndexVat = indexVat;
        var targetTFVat = scenePrefab.objBuildingVat.transform.GetChild(indexVat);

        vatCamera.Follow = targetTFVat;
        vatCamera.LookAt = targetTFVat;

        InitCreaturekDataForTarget();
    }

    /// <summary>
    /// 初始化生物数据
    /// </summary>
    public void InitCreaturekDataForTarget()
    {
        listTargetCreatureShow.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            listTargetCreatureShow.Add(creatureData);
        });
        ui_UIViewCreatureCardList_Target.SetData(listTargetCreatureShow, CardUseState.CreatureAscendTarget, OnCellChangeForBackpackCreatureTarget);
    }

    /// <summary>
    /// 初始化生物数据
    /// </summary>
    public void InitCreaturekDataForMaterial()
    {
        listMaterialCreatureShow.Clear();
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        userData.listBackpackCreature.ForEach((int index, CreatureBean creatureData) =>
        {
            listMaterialCreatureShow.Add(creatureData);
        });
        ui_UIViewCreatureCardList_Material.SetData(listMaterialCreatureShow, CardUseState.CreatureAscendMaterial, OnCellChangeForBackpackCreatureMaterial);
    }

    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreatureTarget(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (targetCreatureSelect == itemData)
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }


    /// <summary>
    /// 背包生物列表变化
    /// </summary>
    public void OnCellChangeForBackpackCreatureMaterial(int index, UIViewCreatureCardItem itemView, CreatureBean itemData)
    {
        if (listMaterialCreatureSelect.Contains(itemData))
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendSelect);
        }
        else
        {
            itemView.SetCardState(CardStateEnum.CreatureAscendNoSelect);
        }
    }


    #region  点击相关
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnLeft)
        {
            OnClickForChangeVat(-1);
        }
        else if (viewButton == ui_BtnRight)
        {
            OnClickForChangeVat(1);
        }
        else if (viewButton == ui_BtnStart)
        {

        }
        else if (viewButton == ui_BtnEnd)
        {

        }
        else if (viewButton == ui_BtnComplete)
        {

        }
    }

    /// <summary>
    /// 点击开始
    /// </summary>
    public void OnClickForStart()
    {

    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd()
    {

    }

    /// <summary>
    /// 点击完成
    /// </summary>
    public void OnClickForComplete()
    {

    }

    /// <summary>
    /// 点击离开
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIBaseCore>();
    }

    /// <summary>
    /// 点击切换容器
    /// </summary>
    public void OnClickForChangeVat(int changeType)
    {
        Transform targetVatOld = scenePrefab.objBuildingVat.transform.GetChild(currentIndexVat);
        float distance = float.MaxValue;
        int targetIndex = -1;
        //查询最近的容器
        for (int i = 0; i < scenePrefab.objBuildingVat.transform.childCount; i++)
        {
            var itemVat = scenePrefab.objBuildingVat.transform.GetChild(i);
            //容器可以使用 并且不是自身
            if (itemVat.gameObject.activeSelf && itemVat != targetVatOld)
            {
                //判断左右
                //如果是左选 但是目标在右边 则不处理
                if (changeType == -1 && itemVat.position.x > targetVatOld.position.x)
                {
                    continue;
                }

                //如果是右选 但是目标在左边 则不处理
                if (changeType == 1 && itemVat.position.x < targetVatOld.position.x)
                {
                    continue;
                }

                float tempDis = Vector3.Distance(itemVat.position, targetVatOld.position);
                if (tempDis < distance)
                {
                    distance = tempDis;
                    targetIndex = i;
                }
            }
        }
        if (targetIndex != -1)
        {
            SetCurrentVat(targetIndex);
        }
        RefreshVatState(false);
    }
    #endregion

    #region 事件
    /// <summary>
    /// 选择
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        //目标选择
        if (selectItemView.cardData.cardUseState == CardUseState.CreatureAscendTarget)
        {
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {
                if (targetCreatureSelect != null && targetCreatureSelect == selectCreatureData)
                {
                    targetCreatureSelect = null;
                }
            }
            else
            {
                targetCreatureSelect = selectCreatureData;
            }
            ui_UIViewCreatureCardList_Target.RefreshAllCard();
        }
        //材料选择
        else if (selectItemView.cardData.cardUseState == CardUseState.CreatureAscendMaterial)
        {
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {

            }
            else
            {

            }
            ui_UIViewCreatureCardList_Material.RefreshAllCard();
        }
    }
    #endregion

    #region 动画相关
    public void AnimForListShow(Transform targetList, bool isShow, bool isAnim)
    {
        RectTransform tragetRTF = (RectTransform)targetList;
        if (isShow)
        {
            tragetRTF.anchoredPosition = Vector2.zero;
        }
        else
        {
            if (tragetRTF.pivot.x == 1)
            {
                tragetRTF.anchoredPosition = new Vector2(600, 0);
            }
            else
            {
                tragetRTF.anchoredPosition = new Vector2(-600, 0);
            }
        }
    }
    #endregion
}