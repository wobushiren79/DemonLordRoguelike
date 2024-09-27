using System;
using System.Collections.Generic;
[Serializable]
public partial class GameWorldMapTypeInfoBean : BaseBean
{
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///背景颜色
	/// </summary>
	public string color_bg;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class GameWorldMapTypeInfoCfg : BaseCfg<long, GameWorldMapTypeInfoBean>
{
	public static string fileName = "GameWorldMapTypeInfo";
	protected static Dictionary<long, GameWorldMapTypeInfoBean> dicData = null;
	public static Dictionary<long, GameWorldMapTypeInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			GameWorldMapTypeInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static GameWorldMapTypeInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			GameWorldMapTypeInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(GameWorldMapTypeInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, GameWorldMapTypeInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			GameWorldMapTypeInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
