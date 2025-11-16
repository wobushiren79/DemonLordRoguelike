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

        //测试标准模型
        CreatureBean creatureNormalTest = new CreatureBean(2001);
        creatureNormalTest.AddSkinForBase();

        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_NormalModel, creatureNormalTest);

        SpineHandler.Instance.PlayAnim(ui_NormalModel, SpineAnimationStateEnum.Idle, creatureNormalTest, true);
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
    /// 设置数据
    /// </summary>
    /// <param name="fightCreatureData"></param>
    public void SetData(FightCreatureBean fightCreatureData)
    {
        this.fightCreatureData = fightCreatureData;
        var creatureData = fightCreatureData.creatureData;
        ui_CreatureCardItem.SetData(creatureData, CardUseState.Show);
        ui_ViewCreatureCardDetails.SetData(creatureData);

        //测试目标模型
        //设置spine
        CreatureHandler.Instance.SetCreatureData(ui_TargetModel, creatureData);
        //播放待机动画
        SpineHandler.Instance.PlayAnim(ui_TargetModel, SpineAnimationStateEnum.Idle, creatureData, true);

        ui_InputText_S_Size.text = $"{ui_CreatureCardItem.ui_Icon.transform.localScale.x}";
        ui_InputText_S_X.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_S_Y.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.y}";

        ui_InputText_B_Size.text = $"{ui_ViewCreatureCardDetails.ui_Icon.transform.localScale.x}";
        ui_InputText_B_X.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_B_Y.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.y}";

        ui_InputText_Obj_Size.text = $"{ui_TargetModel.transform.localScale.x}";
    }

    /// <summary>
    /// 点击生成数据
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

        LogUtil.LogError($"小卡：{sData}");
        LogUtil.LogError($"大卡：{bData}");
        LogUtil.LogError($"实体大小：{objSize}");
#if UNITY_EDITOR
        List<ExcelChangeData> listData = new List<ExcelChangeData>() 
        {
            new ExcelChangeData(fightCreatureData.creatureData.creatureModel.id,"ui_data_s",sData),
            new ExcelChangeData(fightCreatureData.creatureData.creatureModel.id,"ui_data_b",bData),
            new ExcelChangeData(fightCreatureData.creatureData.creatureModel.id,"size_spine",$"{objSize}")
        };
        ExcelUtil.SetExcelData("Assets/Data/Excel/excel_creature_model[生物模型信息].xlsx", "CreatureModel", listData);
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
