using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class AchievementInfoBean : BaseBean
{
	/// <summary>
	///成就类型 1击杀生物 2游玩时间 3征服模式通关
	/// </summary>
	public int achievement_type;
	/// <summary>
	///附加参数(类型3=征服难度等级)
	/// </summary>
	public int target_extra;
	/// <summary>
	///附加参数2(类型3=征服世界id 0=无)
	/// </summary>
	public int target_world;
	/// <summary>
	///各级目标值(逗号分隔,顺序即等级1..N)
	/// </summary>
	public string target_values;
	/// <summary>
	///各级奖励魔晶(逗号分隔,与target_values一一对应)
	/// </summary>
	public string reward_crystals;
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///排序
	/// </summary>
	public int sort;
	/// <summary>
	///名字(单条通用文本id)
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get => _name_language.Get(() => TextHandler.Instance.GetTextById(AchievementInfoCfg.fileName, name)); set => _name_language.Set(value); }
	private LanguageCache _name_language;
	/// <summary>
	///描述模板文本id(取content_1,含{Name}占位符;通常与name同id)
	/// </summary>
	public long details;
	[JsonIgnore]
	public string details_language { get => _details_language.Get(() => TextHandler.Instance.GetTextById(AchievementInfoCfg.fileName, details, 1)); set => _details_language.Set(value); }
	private LanguageCache _details_language;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class AchievementInfoCfg : BaseCfg<long, AchievementInfoBean>
{
	public static string fileName = "AchievementInfo";
	protected static Dictionary<long, AchievementInfoBean> dicData = null;
	public static Dictionary<long, AchievementInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static AchievementInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static AchievementInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			AchievementInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(AchievementInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, AchievementInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			AchievementInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
