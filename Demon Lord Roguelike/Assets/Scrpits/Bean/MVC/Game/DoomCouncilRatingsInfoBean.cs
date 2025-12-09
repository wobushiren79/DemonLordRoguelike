using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class DoomCouncilRatingsInfoBean : BaseBean
{
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///投票票数
	/// </summary>
	public int vote;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(DoomCouncilRatingsInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class DoomCouncilRatingsInfoCfg : BaseCfg<long, DoomCouncilRatingsInfoBean>
{
	public static string fileName = "DoomCouncilRatingsInfo";
	protected static Dictionary<long, DoomCouncilRatingsInfoBean> dicData = null;
	public static Dictionary<long, DoomCouncilRatingsInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static DoomCouncilRatingsInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static DoomCouncilRatingsInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			DoomCouncilRatingsInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(DoomCouncilRatingsInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, DoomCouncilRatingsInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			DoomCouncilRatingsInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
