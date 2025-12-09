using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class RarityInfoBean : BaseBean
{
	/// <summary>
	///ui外边框颜色
	/// </summary>
	public string ui_board_color;
	/// <summary>
	///ui外边框颜色(其他)
	/// </summary>
	public string ui_board_other_color;
	/// <summary>
	///ui外边框颜色(道具)
	/// </summary>
	public string ui_board_color_item;
	/// <summary>
	///不同品质道具关系加成
	/// </summary>
	public int item_add_relationship;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(RarityInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class RarityInfoCfg : BaseCfg<long, RarityInfoBean>
{
	public static string fileName = "RarityInfo";
	protected static Dictionary<long, RarityInfoBean> dicData = null;
	public static Dictionary<long, RarityInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static RarityInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static RarityInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			RarityInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(RarityInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, RarityInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			RarityInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
