using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class UIMainCreate : BaseUIComponent
{
    protected int userDataIndex;

    //预览预制
    public GameObject previewObj;
    public SkeletonAnimation previewSpine;

    //创建的生物数据
    protected CreatureBean createCreatureData;

    //选中控件
    protected List<UIViewMainCreateSelectItem> listSelectView = new List<UIViewMainCreateSelectItem>();
    protected Dictionary<CreatureSkinTypeEnum,UIViewColorShow> dicSelectColorShow = new Dictionary<CreatureSkinTypeEnum, UIViewColorShow>();
    //物种数据
    protected List<int> listSelectForSpecies = new List<int>()
    {
        1,2
    };

    //物种数据
    protected Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<long>>> dicSelectData = new Dictionary<long, Dictionary<CreatureSkinTypeEnum, List<long>>>();
    //选中的物种
    protected int selectSpeciesIndex = 0;

    public override void OpenUI()
    {
        base.OpenUI();
        ShowPreviewCreate(true);
        InitData();
    }

    public override void CloseUI()
    {
        base.CloseUI();
        ShowPreviewCreate(false);
        createCreatureData = null;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        //设置选项
        List<string> listSpeciesStr = new List<string>();
        dicSelectData.Clear();
        for (int i = 0; i < listSelectForSpecies.Count; i++)
        {
            var targetCreatureId = listSelectForSpecies[i];
            //添加随机数据
            var randomCreatureData = CreatureInfoRandomCfg.GetItemData(targetCreatureId);
            var randomSkinData = randomCreatureData.GetAllRandomData();
            dicSelectData.Add(targetCreatureId, randomSkinData);
            //设置选项名字
            var targetCreatureInfo = CreatureInfoCfg.GetItemData(targetCreatureId);
            listSpeciesStr.Add($"{targetCreatureInfo.name_language}");
        }
        string speciesStr = TextHandler.Instance.GetTextById(303);
        ui_UIViewMainCreateSelectItem_Species.SetData(listSpeciesStr, ActionForSelect);
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
    /// 设置预览生物数据
    /// </summary>
    public void SetPreviewCreate(CreatureBean createCreatureData)
    {
        //设置spine
        CreatureHandler.Instance.SetCreatureData(previewSpine, createCreatureData);
        //播放spine动画
        SpineHandler.Instance.PlayAnim(previewSpine, SpineAnimationStateEnum.Idle, createCreatureData, true);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(int userDataIndex)
    {
        this.userDataIndex = userDataIndex;
    }
    #region 按钮事件
    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_ViewExit)
        {
            OnClickForExit();
        }
        else if (viewButton == ui_BtnCreate)
        {
            OnClickForCreate();
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
        UIHandler.Instance.OpenUIAndCloseOther<UIMainLoad>();
    }

    /// <summary>
    /// 创建
    /// </summary>
    public void OnClickForCreate()
    {
        if (ui_NameET.text.IsNull())
        {
            UIHandler.Instance.ToastHint<ToastView>(TextHandler.Instance.GetTextById(305));
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = string.Format(TextHandler.Instance.GetTextById(304), ui_NameET.text);
        dialogData.actionSubmit = ((view, data) =>
        {
            UserDataBean userData = new UserDataBean();
            userData.saveIndex = userDataIndex;
            //设置名字
            userData.userName = ui_NameET.text;
            //设置魔王
            createCreatureData.creatureUUId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
            createCreatureData.creatureName = ui_NameET.text;
            createCreatureData.level = 0;
            createCreatureData.rarity = 0;

            userData.selfCreature = createCreatureData;
            //添加3个初始魔物 2个近战 1个远程
            for (int i = 0; i < 3; i++)
            {
                long targetCreatureId;
                if (i < 2)
                {
                    targetCreatureId = 2001;
                }
                else
                {
                    targetCreatureId = 2002;
                }
                CreatureBean creatureData = new CreatureBean(targetCreatureId);
                var randomData = CreatureInfoRandomCfg.GetItemData(targetCreatureId);
                var listRandomSkin = randomData.GetRandomData();
                listRandomSkin.ForEach((index, itemData) =>
                {
                    creatureData.AddSkin(itemData);
                });
                creatureData.AddSkinForBase();
                //添加到背包
                userData.AddBackpackCreature(creatureData);
                //添加到阵容1
                userData.AddLineupCreature(1, creatureData.creatureUUId);
            }

            GameDataHandler.Instance.manager.SaveUserData(userData);
            GameDataHandler.Instance.manager.SetUserData(userData);
            WorldHandler.Instance.EnterGameForBaseScene(userData, false);
        });
        UIHandler.Instance.ShowDialogNormal(dialogData);

    }

    /// <summary>
    /// 随机
    /// </summary>
    public void OnClickForRandom()
    {
        int randomSelect = Random.Range(0, listSelectForSpecies.Count);
        HandleForSelectSpecies(randomSelect, true);
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
                createCreatureData.ChangeSkinColor(skinType, targetColor);  
                SetPreviewCreate(createCreatureData);
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
            HandleForSelectSpecies(select);
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
        var creatureModelInfo  =  CreatureModelInfoCfg.GetItemData(selectSkin);
        
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
        createCreatureData.AddSkin(spineSkin);
        if (!isInit)
        {
            SetPreviewCreate(createCreatureData);
        }
    }

    /// <summary>
    /// 处理选择物种
    /// </summary>
    public void HandleForSelectSpecies(int select, bool isRandom = false)
    {
        this.selectSpeciesIndex = select;
        int creatureId = listSelectForSpecies[select];
        createCreatureData = new CreatureBean(creatureId);
        createCreatureData.creatureId = creatureId;
        createCreatureData.ClearSkin();
        createCreatureData.AddSkinForBase();

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
                startRandomIndex = Random.Range(0, listSkinName.Count);
            }
            targetView.SetData(listSkinName, ActionForSelect, startRandomIndex);
            index++;
        }
        SetPreviewCreate(createCreatureData);
    }
    #endregion
}
