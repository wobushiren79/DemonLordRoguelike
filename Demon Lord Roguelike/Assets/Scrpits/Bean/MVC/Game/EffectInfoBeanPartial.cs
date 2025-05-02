using System;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
public partial class EffectInfoBean
{
    List<EffectInfoItemDataBean> listEffectData;
    EffectBean effectData;

    public EffectBean GetEffectData()
    {
        if (effectData == null)
        {
            effectData = new EffectBean();
            effectData.effectType = EffectTypeEnum.Visual;
            effectData.effectName = res_name;
            effectData.isPlayInShow = false;
            effectData.timeForShow = show_time;
            effectData.effectShowType = EffectShowTypeEnum.Once;
        }
        return effectData;
    }

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
                    if (value.Contains("*"))
                    {
                        string[] valueArray = value.Split('*');
                        targetData = float.Parse(valueArray[0]);
                        effectInfoItemData.isSize = true;
                    }
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
            //处理Int数据
            SplitStringData(int_data, (key,value)=>
            {
                if(int_data.IsNull())
                    return;
                EffectInfoItemDataBean effectInfoItemData = new EffectInfoItemDataBean();
                int targetData = 0;
                if (value.Contains("{Direction}"))
                {
                    effectInfoItemData.isDirection = true;
                }
                else
                {
                    targetData = int.Parse(value);
                }          
                effectInfoItemData.dataType = 2;          
                effectInfoItemData.dataInt = targetData;
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
                    if(value.Contains("+"))
                    {
                        string[] valueArray = value.Split('+');
                        targetData = valueArray[0].SplitForVector3(',');
                    }
                    effectInfoItemData.isStartPosition = true;
                }
                else
                {
                    targetData = value.SplitForVector3(',');
                }
                effectInfoItemData.dataType = 5;
                effectInfoItemData.dataVector3 = targetData;
                effectInfoItemData.dataName = key;
                listEffectData.Add(effectInfoItemData);
            });

            //处理vector4数据
            SplitStringData(vector4_data, (key, value) =>
            {
                if(vector4_data.IsNull())
                    return;
                EffectInfoItemDataBean effectInfoItemData = new EffectInfoItemDataBean();
                Vector4  targetData =  value.SplitForVector4(',');

                effectInfoItemData.dataType = 6;
                effectInfoItemData.dataVector4 = targetData;
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
        if(strData.IsNull())
        {
            return;
        }
        try
        {
            string[] arrayData = strData.Split('&');
            for (int i = 0; i < arrayData.Length; i++)
            {
                string[] itemData = arrayData[i].Split(':');
                actionForItem?.Invoke(itemData[0], itemData[1]);
            }
        }
        catch (Exception e)
        {
            LogUtil.LogError($"拆分出错：{strData} {e.ToString()}");
        }
    }

    public EffectShowTypeEnum GetShowType()
    {
        return (EffectShowTypeEnum)show_type;
    }
}

public struct EffectInfoItemDataBean
{
    public string dataName;
    //1float 2int 3long 4string 5Vector3
    public int dataType;
    public bool isDirection;
    public bool isSize;
    public bool isStartPosition;
    public float dataFloat;
    public int dataInt;
    public Vector3 dataVector3;
    public Vector4 dataVector4;
}

public partial class EffectInfoCfg
{
}
