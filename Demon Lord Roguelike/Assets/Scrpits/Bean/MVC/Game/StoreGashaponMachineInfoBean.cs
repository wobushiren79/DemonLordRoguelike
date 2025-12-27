using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class StoreGashaponMachineInfoBean : BaseBean
{
	/// <summary>
	///包含的生物ID（,分割 -表示范围）
	/// </summary>
	public string creature_ids;
	/// <summary>
	///购买数量
	/// </summary>
	public int buy_num;
	/// <summary>
	///消耗的魔晶
	/// </summary>
	public int pay_crystal;
	/// <summary>
	///内容
	/// </summary>
	public string icon_res;
	/// <summary>
	///解锁ID
	/// </summary>
	public string pre_unlock_ids;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(StoreGashaponMachineInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class StoreGashaponMachineInfoCfg : BaseCfg<long, StoreGashaponMachineInfoBean>
{
	public static string fileName = "StoreGashaponMachineInfo";
	protected static Dictionary<long, StoreGashaponMachineInfoBean> dicData = null;
	public static Dictionary<long, StoreGashaponMachineInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static StoreGashaponMachineInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static StoreGashaponMachineInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			StoreGashaponMachineInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(StoreGashaponMachineInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, StoreGashaponMachineInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			StoreGashaponMachineInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
