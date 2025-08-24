using System;
using System.Collections.Generic;
[Serializable]
public partial class FightTypeConquerInfoBean : BaseBean
{
	/// <summary>
	///世界ID
	/// </summary>
	public long world_id;
	/// <summary>
	///战斗场景列表
	/// </summary>
	public string fight_scene_ids;
	/// <summary>
	///boss战斗场景列表
	/// </summary>
	public string fight_scene_boss_ids;
	/// <summary>
	///敌人列表
	/// </summary>
	public string enemy_ids;
	/// <summary>
	///boss列表
	/// </summary>
	public string enemy_boss_ids;
	/// <summary>
	///敌人数量
	/// </summary>
	public int enemy_num;
	/// <summary>
	///起始敌人数量
	/// </summary>
	public int attack_start_num;
	/// <summary>
	///最小进攻次数
	/// </summary>
	public int attack_wave_min;
	/// <summary>
	///最大进攻次数
	/// </summary>
	public int attack_wave_max;
	/// <summary>
	///关卡次数-最小
	/// </summary>
	public int fight_num_min;
	/// <summary>
	///关卡次数-最大
	/// </summary>
	public int fight_num_max;
	/// <summary>
	///道路数量-最小
	/// </summary>
	public int road_num_min;
	/// <summary>
	///道路数量-最大
	/// </summary>
	public int road_num_max;
	/// <summary>
	///道路长度-最小
	/// </summary>
	public int road_length_min;
	/// <summary>
	///道路长度-最大
	/// </summary>
	public int road_length_max;
	/// <summary>
	///难度
	/// </summary>
	public int level;
	/// <summary>
	///难度数值加成
	/// </summary>
	public float level_add;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class FightTypeConquerInfoCfg : BaseCfg<long, FightTypeConquerInfoBean>
{
	public static string fileName = "FightTypeConquerInfo";
	protected static Dictionary<long, FightTypeConquerInfoBean> dicData = null;
	public static Dictionary<long, FightTypeConquerInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			FightTypeConquerInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static FightTypeConquerInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			FightTypeConquerInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(FightTypeConquerInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, FightTypeConquerInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			FightTypeConquerInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
