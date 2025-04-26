using System;
using System.Collections.Generic;
[Serializable]
public partial class ResearchInfoBean : BaseBean
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
	public int research_type;
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
	///前置条件
	/// </summary>
	public string pre_research_ids;
	/// <summary>
	///解锁的ID
	/// </summary>
	public long unlock_id;
	/// <summary>
	///需要支付的水晶
	/// </summary>
	public int pay_crystal;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ResearchInfoCfg : BaseCfg<long, ResearchInfoBean>
{
	public static string fileName = "ResearchInfo";
	protected static Dictionary<long, ResearchInfoBean> dicData = null;
	public static Dictionary<long, ResearchInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			ResearchInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static ResearchInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ResearchInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ResearchInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, ResearchInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ResearchInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
