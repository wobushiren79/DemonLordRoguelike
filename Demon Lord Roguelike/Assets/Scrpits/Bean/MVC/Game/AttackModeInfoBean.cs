using System;
using System.Collections.Generic;
[Serializable]
public partial class AttackModeInfoBean : BaseBean
{
	/// <summary>
	///类引用
	/// </summary>
	public string class_name;
	/// <summary>
	///预制名字
	/// </summary>
	public string prefab_name;
	/// <summary>
	///攻击buff
	/// </summary>
	public string buff;
	/// <summary>
	///碰撞检测大小（用于点到点）
	/// </summary>
	public float collider_size;
	/// <summary>
	///碰撞范围类型11中心圆形 12前方圆形 21中心box 22前方box
	/// </summary>
	public int collider_area_type;
	/// <summary>
	///碰撞范围检测（用于范围）
	/// </summary>
	public string collider_area_size;
	/// <summary>
	///打击之后的特效
	/// </summary>
	public long effect_hit;
	/// <summary>
	///受伤特效（默认不填，0为关闭）
	/// </summary>
	public string effect_damage;
	/// <summary>
	///移动速度
	/// </summary>
	public float speed_move;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class AttackModeInfoCfg : BaseCfg<long, AttackModeInfoBean>
{
	public static string fileName = "AttackModeInfo";
	protected static Dictionary<long, AttackModeInfoBean> dicData = null;
	public static Dictionary<long, AttackModeInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			AttackModeInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static AttackModeInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			AttackModeInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(AttackModeInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, AttackModeInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			AttackModeInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
