using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class CreatureAttributeTypeInfoBean : BaseBean
{
	/// <summary>
	///标记名字
	/// </summary>
	public string mark_name;
	/// <summary>
	///资源名字
	/// </summary>
	public string res_name;
	/// <summary>
	///文本颜色
	/// </summary>
	public string color_text;
	/// <summary>
	///名字-中文
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(CreatureAttributeTypeInfoCfg.fileName, name); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureAttributeTypeInfoCfg : BaseCfg<long, CreatureAttributeTypeInfoBean>
{
	public static string fileName = "CreatureAttributeTypeInfo";
	protected static Dictionary<long, CreatureAttributeTypeInfoBean> dicData = null;
	public static Dictionary<long, CreatureAttributeTypeInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureAttributeTypeInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static CreatureAttributeTypeInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureAttributeTypeInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureAttributeTypeInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureAttributeTypeInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureAttributeTypeInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
