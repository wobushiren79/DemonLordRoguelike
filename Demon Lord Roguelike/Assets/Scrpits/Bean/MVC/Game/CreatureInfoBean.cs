using System;
using System.Collections.Generic;
[Serializable]
public partial class CreatureInfoBean : BaseBean
{
	/// <summary>
	///生物类型1：防御 2进攻
	/// </summary>
	public int creature_type;
	/// <summary>
	///创建魔力
	/// </summary>
	public int create_magic;
	/// <summary>
	///内容
	/// </summary>
	public string name_res;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureInfoCfg : BaseCfg<long, CreatureInfoBean>
{
	public static string fileName = "CreatureInfo";
	protected static Dictionary<long, CreatureInfoBean> dicData = null;
	public static Dictionary<long, CreatureInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			CreatureInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
