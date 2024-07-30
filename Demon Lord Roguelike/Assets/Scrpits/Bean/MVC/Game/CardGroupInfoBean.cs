using System;
using System.Collections.Generic;
[Serializable]
public partial class CardGroupInfoBean : BaseBean
{
	/// <summary>
	///包含的卡片ID
	/// </summary>
	public string card_ids;
	/// <summary>
	///消耗的魔晶
	/// </summary>
	public int pay_coin;
	/// <summary>
	///内容
	/// </summary>
	public string icon;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CardGroupInfoCfg : BaseCfg<long, CardGroupInfoBean>
{
	public static string fileName = "CardGroupInfo";
	protected static Dictionary<long, CardGroupInfoBean> dicData = null;
	public static Dictionary<long, CardGroupInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			CardGroupInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CardGroupInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CardGroupInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CardGroupInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CardGroupInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CardGroupInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
