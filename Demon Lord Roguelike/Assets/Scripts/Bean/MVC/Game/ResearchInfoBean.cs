using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class ResearchInfoBean : BaseBean
{
	/// <summary>
	///类型 1设施 2强化 3魔物
	/// </summary>
	public int research_type;
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///升级上限
	/// </summary>
	public int level_max;
	/// <summary>
	///位置x
	/// </summary>
	public float position_x;
	/// <summary>
	///位置y
	/// </summary>
	public float position_y;
	/// <summary>
	///解锁的ID
	/// </summary>
	public long unlock_id;
	/// <summary>
	///前置条件
	/// </summary>
	public string pre_unlock_ids;
	/// <summary>
	///需要支付的水晶
	/// </summary>
	public string pay_crystal;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(ResearchInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ResearchInfoCfg : BaseCfg<long, ResearchInfoBean>
{
	public static string fileName = "ResearchInfo";
	protected static Dictionary<long, ResearchInfoBean> dicData = null;
	public static Dictionary<long, ResearchInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static ResearchInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static ResearchInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ResearchInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ResearchInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, ResearchInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ResearchInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
