using System;
using System.Collections.Generic;
[Serializable]
public partial class CreatureInfoBean : BaseBean
{
	/// <summary>
	///生物类型1：防御 2进攻
	/// </summary>
	public int creature_type;
	/// <summary>
	///创建cd(秒)
	/// </summary>
	public float create_cd;
	/// <summary>
	///创建魔力
	/// </summary>
	public int create_magic;
	/// <summary>
	///
	/// </summary>
	public int att_mode;
	/// <summary>
	///攻击范围
	/// </summary>
	public float att_range;
	/// <summary>
	///攻击间隔
	/// </summary>
	public float att_cd;
	/// <summary>
	///攻击动画出手时间
	/// </summary>
	public float att_anim_cast_time;
	/// <summary>
	///生命值
	/// </summary>
	public int HP;
	/// <summary>
	///护甲
	/// </summary>
	public int DR;
	/// <summary>
	///攻击力
	/// </summary>
	public int ATK;
	/// <summary>
	///攻击速度
	/// </summary>
	public int ASPD;
	/// <summary>
	///移动速度
	/// </summary>
	public float MSPD;
	/// <summary>
	///模组ID
	/// </summary>
	public long model_id;
	/// <summary>
	///卡片背景
	/// </summary>
	public string card_scene;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureInfoCfg : BaseCfg<long, CreatureInfoBean>
{
	public static string fileName = "CreatureInfo";
	protected static Dictionary<long, CreatureInfoBean> dicData = null;
	public static Dictionary<long, CreatureInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			CreatureInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
