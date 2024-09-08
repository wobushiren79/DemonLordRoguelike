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

    //物种数据
    protected List<int> listSelectForSpecies = new List<int>()
    {
        1,2
    };
    //物种数据
    protected Dictionary<int, Dictionary<CreatureSkinTypeEnum, List<int>>> dicSelectData = new Dictionary<int, Dictionary<CreatureSkinTypeEnum, List<int>>>();


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
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public void InitData()
    {
        Dictionary<CreatureSkinTypeEnum, List<int>> dicSkin1 = new Dictionary<CreatureSkinTypeEnum, List<int>>()
        {
            {CreatureSkinTypeEnum.Head, new List<int>(){ 1010010, 1010011,1010012}},
        };
        Dictionary<CreatureSkinTypeEnum, List<int>> dicSkin2 = new Dictionary<CreatureSkinTypeEnum, List<int>>()
        {
            {CreatureSkinTypeEnum.Hair, new List<int>(){ 2030001, 2030002, 2030003, 2030004}},
            {CreatureSkinTypeEnum.Body, new List<int>(){ 2040001, 2040002}},
            {CreatureSkinTypeEnum.Eye, new List<int>(){ 2050001, 2050002, 2050003, 2050004, 2050005, 2050006, 2050007, 2050008}},
            {CreatureSkinTypeEnum.Mouth, new List<int>(){ 2060001, 2060002}},
        };
        dicSelectData.Add(1, dicSkin1);
        dicSelectData.Add(2, dicSkin2);

        //设置选项
        List<string> listSpeciesStr = new List<string>();
        for (int i = 0; i < listSelectForSpecies.Count; i++)
        {
            var targetCreatureId = listSelectForSpecies[i];
            var targetCreatureInfo = CreatureInfoCfg.GetItemData(targetCreatureId);
            listSpeciesStr.Add($"{targetCreatureInfo.GetName()}");
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
            var baseSceneObj = WorldHandler.Instance.currentBaseScene;
            previewObj = baseSceneObj.transform.Find("PreviewCreate").gameObject;
            previewSpine = previewObj.transform.Find("Renderer").GetComponent<SkeletonAnimation>();

            previewObj.gameObject.SetActive(true);
            CameraHandler.Instance.SetPreviewCreateCamera(int.MaxValue, true);
        }
        else
        {
            previewObj.gameObject.SetActive(false);
            CameraHandler.Instance.SetGameStartCamera(int.MaxValue, true);
        }
    }

    /// <summary>
    /// 设置预览生物数据
    /// </summary>
    public void SetPreviewCreate(CreatureBean createCreatureData)
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(createCreatureData.id);
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        SpineHandler.Instance.SetSkeletonDataAsset(previewSpine, creatureModel.res_name);
        string[] skinArray = createCreatureData.GetSkinArray();
        //修改皮肤
        SpineHandler.Instance.ChangeSkeletonSkin(previewSpine.skeleton, skinArray);
        //播放spine动画
        SpineHandler.Instance.PlayAnim(previewSpine, SpineAnimationStateEnum.Idle, true);
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void SetData(int userDataIndex)
    {
        this.userDataIndex = userDataIndex;
    }

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
        createCreatureData.creatureId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        createCreatureData.creatureName = ui_NameET.text;
        createCreatureData.level = 0;
        createCreatureData.rarity = 0;
    }

    /// <summary>
    /// 随机
    /// </summary>
    public void OnClickForRandom()
    {

    }

    /// <summary>
    /// 选择回调
    /// </summary>
    public void ActionForSelect(UIViewMainCreateSelectItem targetView, int select)
    {
        if (targetView == ui_UIViewMainCreateSelectItem_Species)
        {
            HandleForSelectSpecies(select);
        }
    }

    /// <summary>
    /// 处理选择物种
    /// </summary>
    public void HandleForSelectSpecies(int select)
    {
        int creatureId = listSelectForSpecies[select];
        if (createCreatureData == null)
        {
            createCreatureData = new CreatureBean(creatureId);
        }
        createCreatureData.id = creatureId;
        createCreatureData.ClearSkin();
        //隐藏所有选项
        for (int i = 0; i < listSelectView.Count; i++)
        {
            listSelectView[i].ShowObj(false);
        }
        //获取皮肤数据
        dicSelectData.TryGetValue(creatureId, out Dictionary<CreatureSkinTypeEnum, List<int>> dicSkinData);
        int index = 0;

        foreach (var item in dicSkinData)
        {
            List<int> listSkin = item.Value;
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
            targetView.SetData(listSkinName, ActionForSelect);
            index++;
        }

        SetPreviewCreate(createCreatureData);
    }
}
