using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class ConversationCouncilorInfoBean : BaseBean
{
	/// <summary>
	///好感
	/// </summary>
	public int relationship;
	/// <summary>
	///内容
	/// </summary>
	public long content;
	[JsonIgnore]
	public string content_language { get { return TextHandler.Instance.GetTextById(ConversationCouncilorInfoCfg.fileName, content); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ConversationCouncilorInfoCfg : BaseCfg<long, ConversationCouncilorInfoBean>
{
	public static string fileName = "ConversationCouncilorInfo";
	protected static Dictionary<long, ConversationCouncilorInfoBean> dicData = null;
	public static Dictionary<long, ConversationCouncilorInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static ConversationCouncilorInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static ConversationCouncilorInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ConversationCouncilorInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ConversationCouncilorInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, ConversationCouncilorInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ConversationCouncilorInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
