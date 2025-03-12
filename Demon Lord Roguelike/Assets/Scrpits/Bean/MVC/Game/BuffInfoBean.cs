using System;
using System.Collections.Generic;
[Serializable]
public partial class BuffInfoBean : BaseBean
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
	///类型1：天赋 2：固定加成 3：条件触发
	/// </summary>
	public int buff_type;
	/// <summary>
	///模组ID
	/// </summary>
	public int model_id;
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
