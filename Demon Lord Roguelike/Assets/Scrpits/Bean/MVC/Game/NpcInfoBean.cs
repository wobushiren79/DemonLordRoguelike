using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class NpcInfoBean : BaseBean
{
	/// <summary>
	///生物id
	/// </summary>
	public long creature_id;
	/// <summary>
	///NPC类型
	/// </summary>
	public int npc_type;
	/// <summary>
	///随机皮肤数据
	/// </summary>
	public long skin_random_id;
	/// <summary>
	///装备道具Ids
	/// </summary>
	public string equip_item_ids;
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
public partial class NpcInfoCfg : BaseCfg<long, NpcInfoBean>
{
	public static string fileName = "NpcInfo";
	protected static Dictionary<long, NpcInfoBean> dicData = null;
	public static Dictionary<long, NpcInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			NpcInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static NpcInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			NpcInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(NpcInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, NpcInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			NpcInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
