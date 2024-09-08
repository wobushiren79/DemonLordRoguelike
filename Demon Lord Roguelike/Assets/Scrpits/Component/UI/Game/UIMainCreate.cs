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

    //Ԥ��Ԥ��
    public GameObject previewObj;
    public SkeletonAnimation previewSpine;

    //��������������
    protected CreatureBean createCreatureData;

    //ѡ�пؼ�
    protected List<UIViewMainCreateSelectItem> listSelectView = new List<UIViewMainCreateSelectItem>();

    //��������
    protected List<int> listSelectForSpecies = new List<int>()
    {
        1,2
    };
    //��������
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
    /// ��ʼ������
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

        //����ѡ��
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
    /// չʾԤ������
    /// </summary>
    public void ShowPreviewCreate(bool isShow)
    {
        if (isShow)
        {
            //����ʵ��
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
    /// ����Ԥ����������
    /// </summary>
    public void SetPreviewCreate(CreatureBean createCreatureData)
    {
        var creatureInfo = CreatureInfoCfg.GetItemData(createCreatureData.id);
        var creatureModel = CreatureModelCfg.GetItemData(creatureInfo.model_id);
        SpineHandler.Instance.SetSkeletonDataAsset(previewSpine, creatureModel.res_name);
        string[] skinArray = createCreatureData.GetSkinArray();
        //�޸�Ƥ��
        SpineHandler.Instance.ChangeSkeletonSkin(previewSpine.skeleton, skinArray);
        //����spine����
        SpineHandler.Instance.PlayAnim(previewSpine, SpineAnimationStateEnum.Idle, true);
    }

    /// <summary>
    /// ��������
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
    /// ����˳�
    /// </summary>
    public void OnClickForExit()
    {
        UIHandler.Instance.OpenUIAndCloseOther<UIMainLoad>();
    }

    /// <summary>
    /// ����
    /// </summary>
    public void OnClickForCreate()
    {
        createCreatureData.creatureId = SystemUtil.GetUUID(SystemUtil.UUIDTypeEnum.N);
        createCreatureData.creatureName = ui_NameET.text;
        createCreatureData.level = 0;
        createCreatureData.rarity = 0;
    }

    /// <summary>
    /// ���
    /// </summary>
    public void OnClickForRandom()
    {

    }

    /// <summary>
    /// ѡ��ص�
    /// </summary>
    public void ActionForSelect(UIViewMainCreateSelectItem targetView, int select)
    {
        if (targetView == ui_UIViewMainCreateSelectItem_Species)
        {
            HandleForSelectSpecies(select);
        }
    }

    /// <summary>
    /// ����ѡ������
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
        //��������ѡ��
        for (int i = 0; i < listSelectView.Count; i++)
        {
            listSelectView[i].ShowObj(false);
        }
        //��ȡƤ������
        dicSelectData.TryGetValue(creatureId, out Dictionary<CreatureSkinTypeEnum, List<int>> dicSkinData);
        int index = 0;

        foreach (var item in dicSkinData)
        {
            List<int> listSkin = item.Value;
            List<string> listSkinName = new List<string>();
            //����Ƥ��ѡ���б�����
            string skinName = CreatureEnum.GetCreatureSkinTypeEnumName(item.Key);
            for (int i = 0; i < listSkin.Count; i++)
            {
                var skinId = listSkin[i];
                listSkinName.Add($"{skinName} {i + 1}");
            }
            //��ȡ�ؼ�
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
