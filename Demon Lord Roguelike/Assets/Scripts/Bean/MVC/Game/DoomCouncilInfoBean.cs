using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class DoomCouncilInfoBean : BaseBean
{
	/// <summary>
	///通过率
	/// </summary>
	public float success_rate;
	/// <summary>
	///消耗的声望
	/// </summary>
	public long cost_reputation;
	/// <summary>
	///消耗的魔晶
	/// </summary>
	public long cost_crystal;
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///实例名字
	/// </summary>
	public string class_entity_name;
	/// <summary>
	///实例参数
	/// </summary>
	public string class_entity_data;
	/// <summary>
	///解锁ID
	/// </summary>
	public long unlock_id;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(DoomCouncilInfoCfg.fileName, name); } }
	/// <summary>
	///描述中文
	/// </summary>
	public long details;
	[JsonIgnore]
	public string details_language { get { return TextHandler.Instance.GetTextById(DoomCouncilInfoCfg.fileName, details); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class DoomCouncilInfoCfg : BaseCfg<long, DoomCouncilInfoBean>
{
	public static string fileName = "DoomCouncilInfo";
	protected static Dictionary<long, DoomCouncilInfoBean> dicData = null;
	public static Dictionary<long, DoomCouncilInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static DoomCouncilInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static DoomCouncilInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			DoomCouncilInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(DoomCouncilInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, DoomCouncilInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			DoomCouncilInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
