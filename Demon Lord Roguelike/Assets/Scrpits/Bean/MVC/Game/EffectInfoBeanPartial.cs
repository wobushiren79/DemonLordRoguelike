using System;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
public partial class EffectInfoBean
{
    List<EffectInfoItemDataBean> listEffectData;

    public List<EffectInfoItemDataBean> GetEffectItemData()
    {
        if (listEffectData == null)
        {
            listEffectData = new List<EffectInfoItemDataBean>();
            //处理float数据
            SplitStringData(float_data, (key,value)=>
            {
                if(float_data.IsNull())
                    return;
                EffectInfoItemDataBean effectInfoItemData = new EffectInfoItemDataBean();
                float targetData = 0;
                if (value.Contains("{Size}"))
                {
                    string[] valueArray = value.Split('x');
                    targetData = float.Parse(valueArray[1]);
                    effectInfoItemData.isSize = true;
                }
                else
                {
                    targetData = float.Parse(value);
                }          
                effectInfoItemData.dataType = 1;          
                effectInfoItemData.dataFloat = targetData;
                effectInfoItemData.dataName = key;
                listEffectData.Add(effectInfoItemData);
            });
            //处理vector3数据
            SplitStringData(vector3_data, (key, value) =>
            {
                if(vector3_data.IsNull())
                    return;
                EffectInfoItemDataBean effectInfoItemData = new EffectInfoItemDataBean();
                Vector3 targetData = Vector3.zero;
                if (value.Contains("{StartPosition}"))
                {
                    effectInfoItemData.isStartPosition = true;
                }
                else
                {
                    float[] floatData = value.SplitForArrayFloat(',');
                    targetData = new Vector3(floatData[0], floatData[1], floatData[2]);
                }
                effectInfoItemData.dataType = 5;
                effectInfoItemData.dataVector3 = targetData;
                effectInfoItemData.dataName = key;
                listEffectData.Add(effectInfoItemData);
            });
        }
        return listEffectData;
    }

    /// <summary>
    /// 拆分数据
    /// </summary>
    public void SplitStringData(string strData, Action<string, string> actionForItem)
    {
        string[] arrayData = strData.Split('&');
        for (int i = 0; i < arrayData.Length; i++)
        {
            string[] itemData = arrayData[i].Split(':');
            actionForItem?.Invoke(itemData[0], itemData[1]);
        }
    }
}

public struct EffectInfoItemDataBean
{
    public string dataName;
    //1float 2int 3long 4string 5Vector3
    public int dataType;
    public bool isSize;
    public bool isStartPosition;
    public float dataFloat;
    public Vector3 dataVector3;
}

public partial class EffectInfoCfg
{
}
