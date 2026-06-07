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
	///目标值(类型1=击杀数量;类型2=秒数;类型3=通关次数)
	/// </summary>
	public long target_value;
	/// <summary>
	///附加参数(类型3=征服难度等级)
	/// </summary>
	public int target_extra;
	/// <summary>
	///奖励-魔晶
	/// </summary>
	public long reward_crystal;
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///排序
	/// </summary>
	public int sort;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get => _name_language.Get(() => TextHandler.Instance.GetTextById(AchievementInfoCfg.fileName, name)); set => _name_language.Set(value); }
	private LanguageCache _name_language;
	/// <summary>
	///描述
	/// </summary>
	public long description;
	[JsonIgnore]
	public string description_language { get => _description_language.Get(() => TextHandler.Instance.GetTextById(AchievementInfoCfg.fileName, description, 1)); set => _description_language.Set(value); }
	private LanguageCache _description_language;
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
