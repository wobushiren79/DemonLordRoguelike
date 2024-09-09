using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ExcelUtil;

public partial class UITestCard : BaseUIComponent
{
    public FightCreatureBean fightCreatureData;
    public override void Awake()
    {
        base.Awake();
        ui_InputText_Obj_Size.onValueChanged.AddListener(ListenerForObjSize);

        ui_InputText_S_Size.onValueChanged.AddListener(ListenerForSSize);
        ui_InputText_S_X.onValueChanged.AddListener(ListenerForSX);
        ui_InputText_S_Y.onValueChanged.AddListener(ListenerForSY);

        ui_InputText_B_Size.onValueChanged.AddListener(ListenerForBSize);
        ui_InputText_B_X.onValueChanged.AddListener(ListenerForBX);
        ui_InputText_B_Y.onValueChanged.AddListener(ListenerForBY);

        //���Ա�׼ģ��
        CreatureBean creatureNormalTest = new CreatureBean(1);
        creatureNormalTest.AddAllSkin();
        SpineHandler.Instance.SetSkeletonDataAsset(ui_NormalModel, creatureNormalTest.creatureModel.res_name);
        string[] skinArray = creatureNormalTest.GetSkinArray();
        SpineHandler.Instance.ChangeSkeletonSkin(ui_NormalModel.skeleton, skinArray);
        ui_NormalModel.transform.localScale = Vector3.one * creatureNormalTest.creatureModel.size_spine;
        SpineHandler.Instance.PlayAnim(ui_NormalModel, SpineAnimationStateEnum.Idle, true);
    }

    public override void OpenUI()
    {
        base.OpenUI();
        GameControlHandler.Instance.manager.EnableAllControl(false);
        CameraHandler.Instance.SetCardTestCamera();
    }

    public override void OnClickForButton(Button viewButton)
    {
        base.OnClickForButton(viewButton);
        if (viewButton == ui_CreateData)
        {
            OnClickForCreateData();
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    /// <param name="fightCreatureData"></param>
    public void SetData(FightCreatureBean fightCreatureData)
    {
        this.fightCreatureData = fightCreatureData;
        var creatureData = fightCreatureData.creatureData;
        ui_CreatureCardItem.SetData(creatureData, CardUseState.Show);
        ui_ViewCreatureCardDetails.SetData(creatureData);

        //����Ŀ��ģ��
        SpineHandler.Instance.SetSkeletonDataAsset(ui_TargetModel, creatureData.creatureModel.res_name);
        SpineHandler.Instance.ChangeSkeletonSkin(ui_TargetModel.skeleton, creatureData.GetSkinArray());
        SpineHandler.Instance.PlayAnim(ui_TargetModel, SpineAnimationStateEnum.Idle, true);

        ui_TargetModel.transform.localScale = Vector3.one * creatureData.creatureModel.size_spine;

        ui_InputText_S_Size.text = $"{ui_CreatureCardItem.ui_Icon.transform.localScale.x}";
        ui_InputText_S_X.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_S_Y.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.y}";

        ui_InputText_B_Size.text = $"{ui_ViewCreatureCardDetails.ui_Icon.transform.localScale.x}";
        ui_InputText_B_X.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_B_Y.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.y}";

        ui_InputText_Obj_Size.text = $"{ui_TargetModel.transform.localScale.x}";
    }

    /// <summary>
    /// �����������
    /// </summary>
    public void OnClickForCreateData()
    {
        float ssize = ui_CreatureCardItem.ui_Icon.transform.localScale.x;
        float sposX = ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.x;
        float sposY = ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.y;

        float bsize = ui_ViewCreatureCardDetails.ui_Icon.transform.localScale.x;
        float bposX = ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.x;
        float bposY = ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.y;

        float objSize = ui_TargetModel.transform.localScale.x;
        string sData = $"{ssize};{sposX},{sposY}";
        string bData = $"{bsize};{bposX},{bposY}";

        LogUtil.LogError($"С����{sData}");
        LogUtil.LogError($"�󿨣�{bData}");
        LogUtil.LogError($"ʵ���С��{objSize}");
#if UNITY_EDITOR
        List<ExcelChangeData> listData = new List<ExcelChangeData>() 
        {
            new ExcelChangeData(fightCreatureData.creatureData.id,"ui_data_s",sData),
            new ExcelChangeData(fightCreatureData.creatureData.id,"ui_data_b",bData),
            new ExcelChangeData(fightCreatureData.creatureData.id,"size_spine",$"{objSize}")
        };
        ExcelUtil.SetExcelData("Assets/Data/Excel/excel_creature_model[����ģ����Ϣ].xlsx", "CreatureModel", listData);
#endif
    }

    public void ListenerForObjSize(string data)
    {
        ui_TargetModel.transform.localScale = Vector3.one * float.Parse(data);
    }

    public void ListenerForSSize(string data)
    {
        ui_CreatureCardItem.ui_Icon.transform.localScale = Vector3.one * float.Parse(data);
    }

    public void ListenerForSX(string data)
    {
        ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition = new Vector2(float.Parse(data), ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.y);
    }

    public void ListenerForSY(string data)
    {
        ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition = new Vector2(ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.x, float.Parse(data));
    }

    public void ListenerForBSize(string data)
    {
        ui_ViewCreatureCardDetails.ui_Icon.transform.localScale = Vector3.one * float.Parse(data);
    }

    public void ListenerForBX(string data)
    {
        ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition = new Vector2(float.Parse(data), ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.y);
    }

    public void ListenerForBY(string data)
    {
        ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition = new Vector2(ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.x, float.Parse(data));
    }
}
