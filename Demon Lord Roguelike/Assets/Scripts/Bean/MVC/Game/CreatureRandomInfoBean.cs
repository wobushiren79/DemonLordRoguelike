using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class CreatureRandomInfoBean : BaseBean
{
	/// <summary>
	///随机数据
	/// </summary>
	public string skin_random_data;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureRandomInfoCfg : BaseCfg<long, CreatureRandomInfoBean>
{
	public static string fileName = "CreatureRandomInfo";
	protected static Dictionary<long, CreatureRandomInfoBean> dicData = null;
	public static Dictionary<long, CreatureRandomInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureRandomInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static CreatureRandomInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureRandomInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureRandomInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureRandomInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureRandomInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
