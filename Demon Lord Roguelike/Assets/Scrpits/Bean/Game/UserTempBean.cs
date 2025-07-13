using NUnit.Framework;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserTempBean
{
    //传送门随机数据
    public List<GameWorldInfoRandomBean> listPortalWorldInfoRandomData = new List<GameWorldInfoRandomBean>();

    /// <summary>
    /// 增加传送门随机数据
    /// </summary>
    public void AddPortalWorldInfoRandomData(GameWorldInfoRandomBean addData)
    {
        listPortalWorldInfoRandomData.Add(addData);
    }

    /// <summary>
    /// 清理传送门随机数据
    /// </summary>
    public void ClearPortalWorldInfoRandomData()
    {
        listPortalWorldInfoRandomData.Clear();
    }
}