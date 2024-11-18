using System;
using System.Collections.Generic;
[Serializable]
public partial class FightBuffInfoBean : BaseBean
{
	/// <summary>
	///引用实例
	/// </summary>
	public string class_entity;
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class FightBuffInfoCfg : BaseCfg<long, FightBuffInfoBean>
{
	public static string fileName = "FightBuffInfo";
	protected static Dictionary<long, FightBuffInfoBean> dicData = null;
	public static Dictionary<long, FightBuffInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			FightBuffInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static FightBuffInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			FightBuffInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(FightBuffInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, FightBuffInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			FightBuffInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
