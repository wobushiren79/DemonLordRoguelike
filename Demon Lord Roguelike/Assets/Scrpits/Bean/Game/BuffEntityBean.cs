using NUnit.Framework;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Spine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BuffEntityBean
{
    public BuffInfoBean buffInfo;
    //是否有效
    public bool isValid;
    //buff施加者
    public string applierCreatureId;
    //buff触发者
    public string targetCreatureId;

    public float timeUpdate = 0;
    public int triggerNumLeft;//剩下的触发次数

    public BuffEntityBean(long buffId, string applierCreatureId, string targetCreatureId)
    {
        SetData(buffId, applierCreatureId, targetCreatureId);
    }

    public void SetData(long buffId, string applierCreatureId, string targetCreatureId)
    {
        buffInfo = BuffInfoCfg.GetItemData(buffId);
        if (buffInfo == null)
        {
            LogUtil.LogError($"buff初始化失败 没有找到applierCreatureId_{applierCreatureId} targetCreatureId_{targetCreatureId}  buffId_{buffId}");
        }
        else
        {
            this.triggerNumLeft = buffInfo.trigger_num;
        }
        this.applierCreatureId = applierCreatureId;
        this.targetCreatureId = targetCreatureId;
        isValid = true;
    }
}