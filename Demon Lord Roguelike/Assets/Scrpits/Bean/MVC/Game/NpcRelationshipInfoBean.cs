using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class NpcRelationshipInfoBean : BaseBean
{
	/// <summary>
	///范围最小值
	/// </summary>
	public int relationship_min;
	/// <summary>
	///范围最大值
	/// </summary>
	public int relationship_max;
	/// <summary>
	///关系类型
	/// </summary>
	public int relationship_type;
}
public partial class NpcRelationshipInfoCfg : BaseCfg<long, NpcRelationshipInfoBean>
{
	public static string fileName = "NpcRelationshipInfo";
	protected static Dictionary<long, NpcRelationshipInfoBean> dicData = null;
	public static Dictionary<long, NpcRelationshipInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static NpcRelationshipInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static NpcRelationshipInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			NpcRelationshipInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(NpcRelationshipInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, NpcRelationshipInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			NpcRelationshipInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
