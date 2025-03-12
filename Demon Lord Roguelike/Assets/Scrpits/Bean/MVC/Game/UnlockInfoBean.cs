using System;
using System.Collections.Generic;
[Serializable]
public partial class UnlockInfoBean : BaseBean
{
	/// <summary>
	///解锁类型 1扭蛋机 0生物
	/// </summary>
	public int unlock_type;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class UnlockInfoCfg : BaseCfg<long, UnlockInfoBean>
{
	public static string fileName = "UnlockInfo";
	protected static Dictionary<long, UnlockInfoBean> dicData = null;
	public static Dictionary<long, UnlockInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			UnlockInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static UnlockInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			UnlockInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(UnlockInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, UnlockInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			UnlockInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
