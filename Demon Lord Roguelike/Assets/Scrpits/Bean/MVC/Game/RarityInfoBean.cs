using System;
using System.Collections.Generic;
[Serializable]
public partial class RarityInfoBean : BaseBean
{
	/// <summary>
	///ui外边框颜色
	/// </summary>
	public string ui_board_color;
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
public partial class RarityInfoCfg : BaseCfg<long, RarityInfoBean>
{
	public static string fileName = "RarityInfo";
	protected static Dictionary<long, RarityInfoBean> dicData = null;
	public static Dictionary<long, RarityInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			RarityInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
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
