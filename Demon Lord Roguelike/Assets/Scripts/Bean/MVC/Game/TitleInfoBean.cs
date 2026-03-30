using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class TitleInfoBean : BaseBean
{
	/// <summary>
	///所需经验
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(TitleInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class TitleInfoCfg : BaseCfg<long, TitleInfoBean>
{
	public static string fileName = "TitleInfo";
	protected static Dictionary<long, TitleInfoBean> dicData = null;
	public static Dictionary<long, TitleInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static TitleInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static TitleInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			TitleInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(TitleInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, TitleInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			TitleInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
