using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class BuffInfoBean : BaseBean
{
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///类型1:攻击模块 2：生物自带 3:深渊馈赠 11 12 13:生物稀有度BUFF
	/// </summary>
	public int buff_type;
	/// <summary>
	///稀有度
	/// </summary>
	public int rarity;
	/// <summary>
	///buff触发对象类型0所有 1防御 2进攻 99防守核心
	/// </summary>
	public int trigger_creature_type;
	/// <summary>
	///引用实例
	/// </summary>
	public string class_entity;
	/// <summary>
	///实例数据
	/// </summary>
	public string class_entity_data;
	/// <summary>
	///身体变色
	/// </summary>
	public string color_body;
	/// <summary>
	///前置条件
	/// </summary>
	public string pre_info;
	/// <summary>
	///触发改变的值
	/// </summary>
	public float trigger_value;
	/// <summary>
	///触发改变的值(最小值)
	/// </summary>
	public float trigger_value_min;
	/// <summary>
	///触发改变的值百分比
	/// </summary>
	public float trigger_value_rate;
	/// <summary>
	///触发改变的值百分比(最小值)
	/// </summary>
	public float trigger_value_rate_min;
	/// <summary>
	///触发几率
	/// </summary>
	public float trigger_chance;
	/// <summary>
	///触发几率(最小值)
	/// </summary>
	public float trigger_chance_min;
	/// <summary>
	///触发次数
	/// </summary>
	public int trigger_num;
	/// <summary>
	///触发时间
	/// </summary>
	public float trigger_time;
	/// <summary>
	///触发时的粒子
	/// </summary>
	public long trigger_effect;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	[JsonIgnore]
	public string name_language { get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, name); } }
	/// <summary>
	///描述
	/// </summary>
	public long content;
	[JsonIgnore]
	public string content_language { get { return TextHandler.Instance.GetTextById(BuffInfoCfg.fileName, content); } }
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class BuffInfoCfg : BaseCfg<long, BuffInfoBean>
{
	public static string fileName = "BuffInfo";
	protected static Dictionary<long, BuffInfoBean> dicData = null;
	public static Dictionary<long, BuffInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static BuffInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
	}
	public static BuffInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			BuffInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(BuffInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, BuffInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			BuffInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
