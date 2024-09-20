using System;
using System.Collections.Generic;
[Serializable]
public partial class GameWorldInfoBean : BaseBean
{
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///难度数据-无尽模式
	/// </summary>
	public string difficulty_0;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_1;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_2;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_3;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_4;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_5;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_6;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_7;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_8;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_9;
	/// <summary>
	///难度数据
	/// </summary>
	public string difficulty_10;
	/// <summary>
	///地图坐标
	/// </summary>
	public string map_pos;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class GameWorldInfoCfg : BaseCfg<long, GameWorldInfoBean>
{
	public static string fileName = "GameWorldInfo";
	protected static Dictionary<long, GameWorldInfoBean> dicData = null;
	public static Dictionary<long, GameWorldInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			GameWorldInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static GameWorldInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			GameWorldInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(GameWorldInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, GameWorldInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			GameWorldInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
