using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class AbyssalBlessingInfoBean : BaseBean
{
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///buff_ids
	/// </summary>
	public string buff_ids;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(AbyssalBlessingInfoCfg.fileName, name); } }
	/// <summary>
	///描述中文
	/// </summary>
	public long details;
	[JsonIgnore]
	public string details_language { get { return TextHandler.Instance.GetTextById(AbyssalBlessingInfoCfg.fileName, details); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class AbyssalBlessingInfoCfg : BaseCfg<long, AbyssalBlessingInfoBean>
{
	public static string fileName = "AbyssalBlessingInfo";
	protected static Dictionary<long, AbyssalBlessingInfoBean> dicData = null;
	public static Dictionary<long, AbyssalBlessingInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			AbyssalBlessingInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static AbyssalBlessingInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			AbyssalBlessingInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(AbyssalBlessingInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, AbyssalBlessingInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			AbyssalBlessingInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
