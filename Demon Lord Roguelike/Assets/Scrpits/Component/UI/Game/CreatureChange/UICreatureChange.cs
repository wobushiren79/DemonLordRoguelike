

using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UICreatureChange : BaseUIComponent
{
    //预览预制
    public GameObject previewObj;
    public SkeletonAnimation previewSpine;

    //新的生物数据
    protected CreatureBean currentCreatureData;

    //选中控件
    protected List<UIViewMainCreateSelectItem> listSelectView = new List<UIViewMainCreateSelectItem>();
    protected Dictionary<CreatureSkinTypeEnum, UIViewColorShow> dicSelectColorShow = new Dictionary<CreatureSkinTypeEnum, UIViewColorShow>();
    //物种数据
    protected List<long> listSelectForCreature = new List<long>();

    //物种数据
    protected Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<long>>> dicSelectData = new Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<long>>>();
    //选中的物种
    protected int selectCreatureIndex = 0;
    protected Action<CreatureBean> actionForSubmit;
    protected Action actionForCancel;
    public override void OpenUI()
    {
        base.OpenUI();
        ShowPreviewCreate(true);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ShowPreviewCreate(false);
        currentCreatureData = null;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(List<long> listSelectForCreature,
        Action<CreatureBean> actionForSubmit, Action actionForCancel,
        string contentStr = null)
    {
        this.listSelectForCreature = listSelectForCreature;
        this.actionForSubmit = actionForSubmit;
        this.actionForCancel = actionForCancel;
        InitData();
        SetContentText(contentStr);
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        //设置选项
        List<string> listCreatureStr = new List<string>();
        dicSelectData.Clear();
        for (int i = 0; i < listSelectForCreature.Count; i++)
        {
            var targetCreatureId = listSelectForCreature[i];
            var creatureInfo = CreatureInfoCfg.GetItemData(targetCreatureId);
            //添加随机数据
            var randomCreatureData = CreatureRandomInfoCfg.GetItemData(creatureInfo.creature_random_id);
            var randomSkinData = randomCreatureData.GetAllRandomData();
            dicSelectData.Add(targetCreatureId, randomSkinData);
            //设置选项名字
            var targetCreatureInfo = CreatureInfoCfg.GetItemData(targetCreatureId);
            listCreatureStr.Add($"{targetCreatureInfo.name_language}");
        } 
        ui_UIViewMainCreateSelectItem_Species.SetData(listCreatureStr, ActionForSelect);
    }

    /// <summary>
    /// 展示预览生物
    /// </summary>
    public void ShowPreviewCreate(bool isShow)
    {
        if (isShow)
        {
            //场景实例
            var baseSceneObj = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.BaseGaming);
            previewObj = baseSceneObj.transform.Find("PreviewCreate").gameObject;
            previewSpine = previewObj.transform.Find("Renderer").GetComponent<SkeletonAnimation>();

            previewObj.gameObject.SetActive(true);
            CameraHandler.Instance.SetPreviewCreateCamera(int.MaxValue, true);
        }
        else
        {
            previewObj.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 设置提示文本
    /// </summary>
    /// <param name="contentStr"></param>
    public void SetContentText(string contentStr)
    {
        if (contentStr.IsNull())
        {
            ui_ContentShow.gameObject.SetActive(false);
        }
        else
        {
            ui_ContentShow.gameObject.SetActive(true);
            ui_ContentPro.text = contentStr;
        }
    }

    /// <summary>
    /// 设置预览生物数据
    /// </summary>
    public void SetPreviewCreate(CreatureBean currentCreatureData)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(previewSpine, currentCreatureData);
        //播放spine动画
        SpineHandler.Instance.PlayAnim(previewSpine, SpineAnimationStateEnum.Idle, currentCreatureData, true);
    }

    #region 按钮事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnSubmit)
        {
            OnClickForSubmit();
        }
        else if (viewButton == ui_BtnRandom)
        {
            OnClickForRandom();
        }
    }

    public override void OnInputActionForStarted(InputActionUIEnum inputType, InputAction.CallbackContext callback)
    {
        base.OnInputActionForStarted(inputType, callback);
        if (inputType == InputActionUIEnum.ESC)
        {
            OnClickForExit();
        }
    }

    /// <summary>
    /// 点击退出
    /// </summary>
    public void OnClickForExit()
    {
        actionForCancel?.Invoke();
    }

    /// <summary>
    /// 创建
    /// </summary>
    public void OnClickForSubmit()
    {
        DialogBean dialogData = new DialogBean();
        dialogData.content = TextHandler.Instance.GetTextById(63003);
        dialogData.actionSubmit = ((view, data) =>
        {
            actionForSubmit?.Invoke(currentCreatureData);
        });
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 随机
    /// </summary>
    public void OnClickForRandom()
    {
        int randomSelect = UnityEngine.Random.Range(0, listSelectForCreature.Count);
        HandleForSelectCreature(randomSelect, true);
    }
    #endregion

    #region 回调处理
    public void ActionForSelectColor(UIViewColorShow viewColorShow, Color targetColor)
    {
        foreach (var item in dicSelectColorShow)
        {
            var skinType = item.Key;
            var colorShow = item.Value;
            if (colorShow == viewColorShow)
            {
                currentCreatureData.ChangeSkinColor(skinType, targetColor);
                SetPreviewCreate(currentCreatureData);
            }
        }
    }

    /// <summary>
    /// 选择回调
    /// </summary>
    public void ActionForSelect(UIViewMainCreateSelectItem targetView, int select, bool isInit)
    {
        if (targetView == ui_UIViewMainCreateSelectItem_Species)
        {
            HandleForSelectCreature(select);
        }
        else
        {
            HandleForSelectSkin(targetView, select, isInit);
        }
    }

    /// <summary>
    /// 处理选择其他
    /// </summary>
    /// <param name="targetView"></param>
    /// <param name="select"></param>
    public void HandleForSelectSkin(UIViewMainCreateSelectItem targetView, int select, bool isInit)
    {
        Color colorForSkin = Color.white;
        bool hasColorForSkin = false;
        //清理颜色选择
        if (dicSelectColorShow.TryGetValue(targetView.creatureSkinType, out UIViewColorShow oldColorShow))
        {
            colorForSkin = oldColorShow.showColor;
            DestroyImmediate(oldColorShow.gameObject);
            dicSelectColorShow.Remove(targetView.creatureSkinType);
        }

        //获取当前选择的数据
        dicSelectData.TryGetValue(targetView.creatureId, out Dictionary<CreatureSkinTypeEnum, List<long>> dicSkinData);
        dicSkinData.TryGetValue(targetView.creatureSkinType, out List<long> listSkin);
        var selectSkin = listSkin[select];
        var creatureModelInfo = CreatureModelInfoCfg.GetItemData(selectSkin);

        //如果当前选择的皮肤包含颜色选择
        if (creatureModelInfo.color_state != 0)
        {
            GameObject targetObj = Instantiate(ui_SelectContent.gameObject, ui_UIViewColorShow.gameObject);
            targetObj.transform.SetSiblingIndex(targetView.transform.GetSiblingIndex() + 1);
            UIViewColorShow colorShow = targetObj.GetComponent<UIViewColorShow>();
            dicSelectColorShow.Add(targetView.creatureSkinType, colorShow);

            string skinName = CreatureEnum.GetCreatureSkinTypeEnumName(targetView.creatureSkinType);
            colorShow.SetData(skinName, colorForSkin, ActionForSelectColor);
            hasColorForSkin = true;
        }
        else
        {
            colorForSkin = Color.white;
            hasColorForSkin = false;
        }

        SpineSkinBean spineSkin = new SpineSkinBean(selectSkin, hasColorForSkin, colorForSkin);
        currentCreatureData.AddSkin(spineSkin);
        if (!isInit)
        {
            SetPreviewCreate(currentCreatureData);
        }
    }

    /// <summary>
    /// 处理选择物种
    /// </summary>
    public void HandleForSelectCreature(int select, bool isRandom = false)
    {
        this.selectCreatureIndex = select;
        long creatureId = listSelectForCreature[select];
        currentCreatureData = new CreatureBean(creatureId);
        currentCreatureData.creatureId = creatureId;
        currentCreatureData.ClearSkin();
        currentCreatureData.AddSkinForBase();

        //隐藏所有选项
        for (int i = 0; i < listSelectView.Count; i++)
        {
            listSelectView[i].ShowObj(false);
        }
        //删除所有颜色选择
        foreach (var item in dicSelectColorShow)
        {
            DestroyImmediate(item.Value.gameObject);
        }
        dicSelectColorShow.Clear();

        //获取皮肤数据
        dicSelectData.TryGetValue(creatureId, out Dictionary<CreatureSkinTypeEnum, List<long>> dicSkinData);
        int index = 0;

        foreach (var item in dicSkinData)
        {
            List<long> listSkin = item.Value;
            List<string> listSkinName = new List<string>();
            //设置皮肤选择列表名字
            string skinName = CreatureEnum.GetCreatureSkinTypeEnumName(item.Key);
            for (int i = 0; i < listSkin.Count; i++)
            {
                var skinId = listSkin[i];
                listSkinName.Add($"{skinName} {i + 1}");
            }
            //获取控件
            UIViewMainCreateSelectItem targetView;
            if (index >= listSelectView.Count)
            {
                GameObject targetObj = Instantiate(ui_SelectContent.gameObject, ui_UIViewMainCreateSelectItem.gameObject);
                targetView = targetObj.GetComponent<UIViewMainCreateSelectItem>();
                listSelectView.Add(targetView);
            }
            else
            {
                targetView = listSelectView[index];
            }
            targetView.ShowObj(true);
            targetView.creatureId = creatureId;
            targetView.creatureSkinType = item.Key;
            int startRandomIndex = 0;
            if (isRandom)
            {
                startRandomIndex = UnityEngine.Random.Range(0, listSkinName.Count);
            }
            targetView.SetData(listSkinName, ActionForSelect, startRandomIndex);
            index++;
        }
        SetPreviewCreate(currentCreatureData);
    }
    #endregion
}