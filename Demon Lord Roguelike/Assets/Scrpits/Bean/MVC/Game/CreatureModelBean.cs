using System;
using System.Collections.Generic;
[Serializable]
public partial class CreatureModelBean : BaseBean
{
	/// <summary>
	///资源名字
	/// </summary>
	public string res_name;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureModelCfg : BaseCfg<long, CreatureModelBean>
{
	public static string fileName = "CreatureModel";
	protected static Dictionary<long, CreatureModelBean> dicData = null;
	public static Dictionary<long, CreatureModelBean> GetAllData()
	{
		if (dicData == null)
		{
			CreatureModelBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureModelBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureModelBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureModelBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureModelBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureModelBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
