using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIViewGameSettingSelect : BaseUIView
{
    protected Action<UIViewGameSettingSelect, int> actionForCallBack;

    public override void Awake()
    {
        base.Awake();
        ui_Dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    /// <summary>
    /// �ص�
    /// </summary>
    public void OnValueChanged(int index)
    {
        actionForCallBack?.Invoke(this, index);
    }

    /// <summary>
    /// ����ѡ��
    /// </summary>
    public void SetSelcet(int index)
    {
        ui_Dropdown.value = index;
    }

    /// <summary>
    /// ���ûص�
    /// </summary>
    public void SetCallBack(Action<UIViewGameSettingSelect, int> actionForCallBack)
    {
        this.actionForCallBack = actionForCallBack;
    }

    /// <summary>
    /// ���ñ���
    /// </summary>
    public void SetTitle(string title)
    {
        ui_SelectText.text = title;
    }

    /// <summary>
    /// ����ѡ��
    /// </summary>
    public void SetListSelect(List<string> listSelect)
    {
        ui_Dropdown.options.Clear();
        for (int i = 0; i < listSelect.Count; i++)
        {
            var itemSelectText = listSelect[i];
            TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData();
            optionData.text = itemSelectText;
            ui_Dropdown.options.Add(optionData);
        }
    }

}