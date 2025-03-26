using System;
using System.Collections.Generic;
[Serializable]
public partial class ItemsInfoBean : BaseBean
{
	/// <summary>
	///道具类型1:帽子 2衣服 3裤子 4鞋子 10武器
	/// </summary>
	public int item_type;
	/// <summary>
	///道具上限
	/// </summary>
	public int num_max;
	/// <summary>
	///专属生物模组ID
	/// </summary>
	public long belong_creature_model;
	/// <summary>
	///图标
	/// </summary>
	public string icon_res;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ItemsInfoCfg : BaseCfg<long, ItemsInfoBean>
{
	public static string fileName = "ItemsInfo";
	protected static Dictionary<long, ItemsInfoBean> dicData = null;
	public static Dictionary<long, ItemsInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			ItemsInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static ItemsInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ItemsInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ItemsInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, ItemsInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ItemsInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
