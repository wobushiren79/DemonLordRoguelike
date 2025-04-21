using System;
using System.Collections.Generic;
[Serializable]
public partial class StoreInfoBean : BaseBean
{
	/// <summary>
	///生物类型1：防御 2进攻
	/// </summary>
	public int creature_type;
	/// <summary>
	///创建魔力
	/// </summary>
	public int create_magic;
	/// <summary>
	///内容
	/// </summary>
	public string name_res;
	/// <summary>
	///移动速度
	/// </summary>
	public float speed_move;
	/// <summary>
	///类型 1设施 2强化 3魔物
	/// </summary>
	public int store_type;
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///位置x
	/// </summary>
	public float position_x;
	/// <summary>
	///位置y
	/// </summary>
	public float position_y;
	/// <summary>
	///解锁前置条件
	/// </summary>
	public string unlock_ids_pre;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class StoreInfoCfg : BaseCfg<long, StoreInfoBean>
{
	public static string fileName = "StoreInfo";
	protected static Dictionary<long, StoreInfoBean> dicData = null;
	public static Dictionary<long, StoreInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			StoreInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static StoreInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			StoreInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(StoreInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, StoreInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			StoreInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
