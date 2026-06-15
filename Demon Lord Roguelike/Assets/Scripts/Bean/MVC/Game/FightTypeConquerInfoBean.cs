using System;
using System.Collections.Generic;
using Newtonsoft.Json;
[Serializable]
public partial class FightTypeConquerInfoBean : BaseBean
{
	/// <summary>
	///世界ID
	/// </summary>
	public long world_id;
	/// <summary>
	///战斗场景列表（用&分割）
	/// </summary>
	public string fight_scene_ids;
	/// <summary>
	///boss战斗场景列表（用&分割）
	/// </summary>
	public string fight_scene_boss_ids;
	/// <summary>
	///敌人列表（npcInfoId用&分割）
	/// </summary>
	public string enemy_ids;
	/// <summary>
	///boss列表（npcInfoId用&分割）
	/// </summary>
	public string enemy_boss_ids;
	/// <summary>
	///boss数量(单个数x或范围x-y)
	/// </summary>
	public string attack_boss_num;
	/// <summary>
	///第一关敌人数量
	/// </summary>
	public int attack_start_num;
	/// <summary>
	///进攻时间(秒)
	/// </summary>
	public float attack_show_time;
	/// <summary>
	///每关敌人倍数
	/// </summary>
	public float attack_num_addrate;
	/// <summary>
	///每关增加敌人数量
	/// </summary>
	public int attack_num_add;
	/// <summary>
	///普通敌人每关强度倍率(默认1,如1.1则每关HP/护甲/攻击力×1.1)
	/// </summary>
	public float attack_intensity_addrate;
	/// <summary>
	///关卡次数(单个数x或范围x-y)
	/// </summary>
	public string fight_num;
	/// <summary>
	///道路数量(单个数x或范围x-y)
	/// </summary>
	public string road_num;
	/// <summary>
	///道路长度(单个数x或范围x-y)
	/// </summary>
	public string road_length;
	/// <summary>
	///难度
	/// </summary>
	public int level;
	/// <summary>
	///难度数值加成
	/// </summary>
	public float level_add;
	/// <summary>
	///掉落魔晶
	/// </summary>
	public int drop_crystal;
	/// <summary>
	///奖励-魔晶
	/// </summary>
	public int reward_crystal;
	/// <summary>
	///奖励-装备稀有度
	/// </summary>
	public int reward_equip_rarity;
	/// <summary>
	///奖励-装备属性加成
	/// </summary>
	public int reward_equip_attribute_add;
	/// <summary>
	///奖励-普通关卡经验
	/// </summary>
	public int reward_exp;
	/// <summary>
	///奖励-BOSS关卡经验
	/// </summary>
	public int reward_exp_boss;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
	/// <summary>
	///背景色(由易到难)
	/// </summary>
	public string bg_color;
}
public partial class FightTypeConquerInfoCfg : BaseCfg<long, FightTypeConquerInfoBean>
{
	public static string fileName = "FightTypeConquerInfo";
	protected static Dictionary<long, FightTypeConquerInfoBean> dicData = null;
	public static Dictionary<long, FightTypeConquerInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			var arrayData = GetAllArrayData();
			InitData(arrayData);
		}
		return dicData;
	}
	public static FightTypeConquerInfoBean[] GetAllArrayData()
	{
		if (arrayData == null)
		{
			arrayData = GetInitData(fileName);
		}
		return arrayData;
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
