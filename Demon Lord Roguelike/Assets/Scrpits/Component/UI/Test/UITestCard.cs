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
        ui_InputText_S_Size.onValueChanged.AddListener(ListenerForSSize);
        ui_InputText_S_X.onValueChanged.AddListener(ListenerForSX);
        ui_InputText_S_Y.onValueChanged.AddListener(ListenerForSY);

        ui_InputText_B_Size.onValueChanged.AddListener(ListenerForBSize);
        ui_InputText_B_X.onValueChanged.AddListener(ListenerForBX);
        ui_InputText_B_Y.onValueChanged.AddListener(ListenerForBY);
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

        ui_CreatureCardItem.SetData(fightCreatureData, new Vector2(-200, 0));
        ui_ViewCreatureCardDetails.SetData(fightCreatureData);

        ui_InputText_S_Size.text = $"{ui_CreatureCardItem.ui_Icon.transform.localScale.x}";
        ui_InputText_S_X.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_S_Y.text = $"{ui_CreatureCardItem.ui_Icon.rectTransform.anchoredPosition.y}";

        ui_InputText_B_Size.text = $"{ui_ViewCreatureCardDetails.ui_Icon.transform.localScale.x}";
        ui_InputText_B_X.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.x}";
        ui_InputText_B_Y.text = $"{ui_ViewCreatureCardDetails.ui_Icon.rectTransform.anchoredPosition.y}";
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

        string sData = $"{ssize};{sposX},{sposY}";
        string bData = $"{bsize};{bposX},{bposY}";

        LogUtil.LogError($"С����{sData}");
        LogUtil.LogError($"�󿨣�{bData}");

#if UNITY_EDITOR
        List<ExcelChangeData> listData = new List<ExcelChangeData>() 
        {
            new ExcelChangeData(fightCreatureData.creatureData.id,"ui_data_s",sData),
            new ExcelChangeData(fightCreatureData.creatureData.id,"ui_data_b",bData)
        };
        ExcelUtil.SetExcelData("Assets/Data/Excel/excel_creature_model[����ģ����Ϣ].xlsx", "CreatureModel", listData);
#endif
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
