using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;

public class ScenePrefabForDoomCouncil : ScenePrefabBase
{
    //议员位置
    public GameObject councilorPosition;
    //讲台
    public GameObject podium;

    protected List<GameObject> listCouncilorObj = new List<GameObject>();

    /// <summary>
    /// 初始化所有议员
    /// </summary>
    public async Task InitCouncilor(List<CreatureBean> listCouncilor)
    {
        //生成议会议员
        for (int i = 0; i < councilorPosition.transform.childCount; i++)
        {
            if (i >= listCouncilor.Count)
            {
                break;
            }
            CreatureBean creatureData = listCouncilor[i];
            var itemPosition = councilorPosition.transform.GetChild(i);
            var targetCreatureObj = await CreatureHandler.Instance.CreateDoomCouncilCreature(creatureData, itemPosition.position);
            targetCreatureObj.name = creatureData.creatureUUId;
            listCouncilorObj.Add(targetCreatureObj);
        }
    }

    /// <summary>
    /// 场景删除
    /// </summary>
    /// <returns></returns>
    public override async Task DestoryScene()
    {
        await base.DestoryScene();
        DestoryAllCoucilor();
    }

    /// <summary>
    /// 删除所有议员
    /// </summary>
    public void DestoryAllCoucilor()
    {
        for (int i = 0; i < listCouncilorObj.Count; i++)
        {
            var itemObj = listCouncilorObj[i];
            Destroy(itemObj);
        }
        listCouncilorObj.Clear();
    }
}