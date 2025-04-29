using System;
using System.Collections.Generic;
[Serializable]
public partial class EffectInfoBean : BaseBean
{
	/// <summary>
	///内容
	/// </summary>
	public string res_name;
	/// <summary>
	///float数据
	/// </summary>
	public string float_data;
	/// <summary>
	///vector3数据
	/// </summary>
	public string vector3_data;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class EffectInfoCfg : BaseCfg<long, EffectInfoBean>
{
	public static string fileName = "EffectInfo";
	protected static Dictionary<long, EffectInfoBean> dicData = null;
	public static Dictionary<long, EffectInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			EffectInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static EffectInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			EffectInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(EffectInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, EffectInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			EffectInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
