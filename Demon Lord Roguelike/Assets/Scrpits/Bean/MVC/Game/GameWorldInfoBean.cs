using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class GameWorldInfoBean : BaseBean
{
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///地图坐标
	/// </summary>
	public string map_pos;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(name); } }
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
