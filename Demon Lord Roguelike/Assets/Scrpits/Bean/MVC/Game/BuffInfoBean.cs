using System;
using System.Collections.Generic;
[Serializable]
public partial class BuffInfoBean : BaseBean
{
	/// <summary>
	///图标名字
	/// </summary>
	public string icon_res;
	/// <summary>
	///类型1:攻击模块 2：生物自带 
	/// </summary>
	public int buff_type;
	/// <summary>
	///引用实例
	/// </summary>
	public string class_entity;
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
	///触发改变的值百分比
	/// </summary>
	public float trigger_value_rate;
	/// <summary>
	///触发几率
	/// </summary>
	public float trigger_chance;
	/// <summary>
	///触发次数
	/// </summary>
	public int trigger_num;
	/// <summary>
	///触发时间
	/// </summary>
	public float trigger_time;
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
			BuffInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
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
