using System;
using System.Collections.Generic;
[Serializable]
public partial class CreatureInfoRandomBean : BaseBean
{
	/// <summary>
	///随机数据
	/// </summary>
	public string random_data;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureInfoRandomCfg : BaseCfg<long, CreatureInfoRandomBean>
{
	public static string fileName = "CreatureInfoRandom";
	protected static Dictionary<long, CreatureInfoRandomBean> dicData = null;
	public static Dictionary<long, CreatureInfoRandomBean> GetAllData()
	{
		if (dicData == null)
		{
			CreatureInfoRandomBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureInfoRandomBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureInfoRandomBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureInfoRandomBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureInfoRandomBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureInfoRandomBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
