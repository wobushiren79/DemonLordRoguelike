using System;
using System.Collections.Generic;
[Serializable]
public partial class BuffPreInfoBean : BaseBean
{
	/// <summary>
	///引用实例
	/// </summary>
	public string class_entity;
	/// <summary>
	///名字-中文
	/// </summary>
	public string name_cn;
	/// <summary>
	///名字-英文
	/// </summary>
	public string name_en;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class BuffPreInfoCfg : BaseCfg<long, BuffPreInfoBean>
{
	public static string fileName = "BuffPreInfo";
	protected static Dictionary<long, BuffPreInfoBean> dicData = null;
	public static Dictionary<long, BuffPreInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			BuffPreInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static BuffPreInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			BuffPreInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(BuffPreInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, BuffPreInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			BuffPreInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
