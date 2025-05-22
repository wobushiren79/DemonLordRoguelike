
using System;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
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

    //用户进阶数据
    protected UserAscendDetailsBean userAscendDetails;
    protected Transform targetVat;
    public override void OpenUI()
    {
        base.OpenUI();

        this.RegisterEvent<UIViewCreatureCardItem>(EventsInfo.UIViewCreatureCardItem_OnClickSelect, EventForCardClickSelect);
        this.RegisterEvent(EventsInfo.CreatureAscend_AddProgress, EventForRefreshVatProgress);

        //场景实例
        var baseSceneObj = WorldHandler.Instance.currentBaseScene;
        scenePrefab = baseSceneObj.GetComponent<ScenePrefabForBase>();
        //获取摄像头
        GameControlHandler.Instance.SetBaseControl(false);
        vatCamera = CameraHandler.Instance.SetCreatureVatCamera(int.MaxValue, true);
        //设置数据
        SetCurrentVat(0);
        RefreshVatState();
        RefreshVatProgress();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        listTargetCreatureShow.Clear();
        listMaterialCreatureShow.Clear();
        ui_UIViewCreatureCardList_Target.CloseUI();
        ui_UIViewCreatureCardList_Material.CloseUI();
        //设置展示vat
        scenePrefab.BuildingVatShow(-1);
    }

    /// <summary>
    /// 设置容器状态
    /// </summary>
    public void RefreshVatState(bool hasAnim = false)
    {
        ui_BtnStart.gameObject.SetActive(false);
        ui_BtnEnd.gameObject.SetActive(false);
        ui_BtnComplete.gameObject.SetActive(false);
        ui_BtnAddProgress.gameObject.SetActive(false);
        AnimForListShow(ui_UIViewCreatureCardList_Target.transform, false, hasAnim);
        AnimForListShow(ui_UIViewCreatureCardList_Material.transform, false, hasAnim);

        if (userAscendDetails != null)
        {
            //是否已经完成
            if (userAscendDetails.progress >= 1)
            {
                ui_BtnComplete.gameObject.SetActive(true);
            }
            else
            {
                ui_BtnEnd.gameObject.SetActive(true);
                ui_BtnAddProgress.gameObject.SetActive(true);
                ui_BtnAddProgressText.text = string.Format(TextHandler.Instance.GetTextById(80009), 1);
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
    /// 刷新进度
    /// </summary>
    public void RefreshVatProgress()
    {
        float progress = 0;
        if (userAscendDetails != null)
        {
            progress = userAscendDetails.progress;
        }
        if (progress > 1)
        {
            progress = 1;
            RefreshVatState();
        }
        ui_ProgressText.text = $"{MathUtil.GetPercentage(progress, 2)}%";
        ui_Progress.fillAmount = progress;
    }

    /// <summary>
    /// 设置当前容器数据
    /// </summary>
    public void SetCurrentVat(int indexVat)
    {
        targetCreatureSelect = null;
        listMaterialCreatureSelect.Clear();

        currentIndexVat = indexVat;

        //检测是否有数据
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        userAscendDetails = userAscend.GetAscendData(currentIndexVat);
        //获取容器模型
        targetVat = scenePrefab.objBuildingVat.transform.GetChild(indexVat);

        vatCamera.Follow = targetVat;
        vatCamera.LookAt = targetVat;

        InitCreaturekDataForTarget();

        //设置展示vat
        scenePrefab.BuildingVatShow(currentIndexVat);
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
            if (creatureData != targetCreatureSelect)
            {
                listMaterialCreatureShow.Add(creatureData);
            }
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
            OnClickForStart();
        }
        else if (viewButton == ui_BtnEnd)
        {
            OnClickForEnd();
        }
        else if (viewButton == ui_BtnComplete)
        {
            OnClickForComplete();
        }
        else if(viewButton == ui_BtnAddProgress)
        {
            OnClickForAddProgress();
        }
    }

    /// <summary>
    /// 点击增加进度
    /// </summary>
    public void OnClickForAddProgress()
    {

    }

    /// <summary>
    /// 点击开始
    /// </summary>
    public void OnClickForStart()
    {
        if (targetCreatureSelect == null)
        {
            string hintStr = TextHandler.Instance.GetTextById(80008);
            UIHandler.Instance.ToastHint<ToastView>(hintStr);
            return;
        }
        //先关闭UI
        UIHandler.Instance.ShowScreenLock();
        gameObject.SetActive(false);
        Action actionForAnimEnd = () =>
        {
            UIHandler.Instance.HideScreenLock();
            gameObject.SetActive(true);
            //设置数据
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            UserAscendBean userAscend = userData.GetUserAscendData();
            userAscendDetails = userAscend.AddAscendData(currentIndexVat, targetCreatureSelect);

            //刷新状态
            RefreshVatState();
            RefreshVatProgress();
        };
        scenePrefab.BuildingVatAnimForStart(targetVat, targetCreatureSelect,listMaterialCreatureSelect, actionForAnimEnd);
    }

    /// <summary>
    /// 点击结束
    /// </summary>
    public void OnClickForEnd()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(80004);
        dialogData.actionSubmit = (voew, data) =>
        {
            //设置数据
            UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
            UserAscendBean userAscend = userData.GetUserAscendData();
            userAscend.RemoveAscendData(currentIndexVat);
            userAscendDetails = null;
            //设置状态
            scenePrefab.BuildingVatSetState(targetVat, 1, targetCreatureSelect);
            //刷新状态
            RefreshVatState();
            RefreshVatProgress();
        };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 点击完成
    /// </summary>
    public void OnClickForComplete()
    {
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        UserAscendBean userAscend = userData.GetUserAscendData();
        userAscend.RemoveAscendData(currentIndexVat);
        userAscendDetails = null;
        //设置状态
        scenePrefab.BuildingVatSetState(targetVat,0, null);
        RefreshVatState();
        RefreshVatProgress();
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
        RefreshVatState();
        RefreshVatProgress();
    }
    #endregion

    #region 事件
    /// <summary>
    /// 刷新进度
    /// </summary>
    public void EventForRefreshVatProgress()
    {
        if (userAscendDetails != null)
        {
            RefreshVatProgress();
        }
    }

    /// <summary>
    /// 选择
    /// </summary>
    public void EventForCardClickSelect(UIViewCreatureCardItem selectItemView)
    {
        var selectCreatureData = selectItemView.cardData.creatureData;
        //目标选择
        if (selectItemView.cardData.cardUseState == CardUseState.CreatureAscendTarget)
        {
            listMaterialCreatureSelect.Clear();
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {
                //取消选择
                if (targetCreatureSelect != null && targetCreatureSelect == selectCreatureData)
                {
                    targetCreatureSelect = null;
                    AnimForListShow(ui_UIViewCreatureCardList_Material.transform, false, true);
                    scenePrefab.BuildingVatSetState(targetVat, 1, null);
                }
            }
            else
            {
                //切换目标魔物
                AnimForListShow(ui_UIViewCreatureCardList_Material.transform, true, true);
                targetCreatureSelect = selectCreatureData;
                scenePrefab.BuildingVatSetState(targetVat, 2, selectCreatureData);
                //初始化材料
                InitCreaturekDataForMaterial();
            }
            ui_UIViewCreatureCardList_Target.RefreshAllCard();
        }
        //材料选择
        else if (selectItemView.cardData.cardUseState == CardUseState.CreatureAscendMaterial)
        {
            if (selectItemView.cardData.cardState == CardStateEnum.CreatureAscendSelect)
            {
                if (listMaterialCreatureSelect.Contains(selectCreatureData))
                {
                    listMaterialCreatureSelect.Remove(selectCreatureData);
                }
            }
            else
            {
                if (!listMaterialCreatureSelect.Contains(selectCreatureData))
                {
                    listMaterialCreatureSelect.Add(selectCreatureData);
                }
            }
            ui_UIViewCreatureCardList_Material.RefreshAllCard();
        }
    }
    #endregion

    #region 动画相关
    public void AnimForListShow(Transform targetList, bool isShow, bool isAnim,float animTime = 0.2f)
    {
        RectTransform tragetRTF = (RectTransform)targetList;
        tragetRTF.DOKill();
        Vector2 targetPos;
        if (isShow)
        {
            targetPos = Vector2.zero;
        }
        else
        {
            if (tragetRTF.pivot.x == 1)
            {
                targetPos = new Vector2(600, 0);
            }
            else
            {
                targetPos = new Vector2(-600, 0);
            }
        }
        if (isAnim)
        {
            tragetRTF.DOAnchorPos(targetPos, animTime);
        }
        else
        {
            tragetRTF.anchoredPosition = targetPos;
        }
    }
    #endregion
}