using System;
using System.Collections.Generic;
[Serializable]
public partial class AbyssalBlessingInfoBean : BaseBean
{
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///馈赠类型
	/// </summary>
	public int blessing_type;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	/// <summary>
	///描述中文
	/// </summary>
	public long details;
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
