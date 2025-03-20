using System;
using System.Collections.Generic;
[Serializable]
public partial class FightSceneBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string name_res;
	/// <summary>
	///道路方格颜色1
	/// </summary>
	public string road_color_a;
	/// <summary>
	///道路方格颜色2
	/// </summary>
	public string road_color_b;
	/// <summary>
	///天空盒子
	/// </summary>
	public string skybox_mat;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class FightSceneCfg : BaseCfg<long, FightSceneBean>
{
	public static string fileName = "FightScene";
	protected static Dictionary<long, FightSceneBean> dicData = null;
	public static Dictionary<long, FightSceneBean> GetAllData()
	{
		if (dicData == null)
		{
			FightSceneBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static FightSceneBean GetItemData(long key)
	{
		if (dicData == null)
		{
			FightSceneBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(FightSceneBean[] arrayData)
	{
		dicData = new Dictionary<long, FightSceneBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			FightSceneBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
