using System;
using System.Collections.Generic;
[Serializable]
public partial class ItemsTypeBean : BaseBean
{
	/// <summary>
	///图标
	/// </summary>
	public string icon_res;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ItemsTypeCfg : BaseCfg<long, ItemsTypeBean>
{
	public static string fileName = "ItemsType";
	protected static Dictionary<long, ItemsTypeBean> dicData = null;
	public static Dictionary<long, ItemsTypeBean> GetAllData()
	{
		if (dicData == null)
		{
			ItemsTypeBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static ItemsTypeBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ItemsTypeBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ItemsTypeBean[] arrayData)
	{
		dicData = new Dictionary<long, ItemsTypeBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ItemsTypeBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
