using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class LevelInfoBean : BaseBean
{
	/// <summary>
	///所需经验
	/// </summary>
	public string level_exp;
}
public partial class LevelInfoCfg : BaseCfg<long, LevelInfoBean>
{
	public static string fileName = "LevelInfo";
	protected static Dictionary<long, LevelInfoBean> dicData = null;
	public static Dictionary<long, LevelInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static LevelInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static LevelInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			LevelInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(LevelInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, LevelInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			LevelInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
