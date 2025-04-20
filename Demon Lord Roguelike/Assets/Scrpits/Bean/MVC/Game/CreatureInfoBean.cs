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
	///生物位置优先级
	/// </summary>
	public string creature_layer;
	/// <summary>
	///生物优先级搜寻
	/// </summary>
	public string creature_layer_find;
	/// <summary>
	///创建cd(秒)
	/// </summary>
	public float create_cd;
	/// <summary>
	///创建魔力
	/// </summary>
	public int create_magic;
	/// <summary>
	///身体的基础模块
	/// </summary>
	public string spine_base;
	/// <summary>
	///身体的基础武器模块
	/// </summary>
	public long equip_item_base_weapon;
	/// <summary>
	///可装备类型
	/// </summary>
	public string equip_items_type;
	/// <summary>
	///等待动画名字
	/// </summary>
	public string anim_idle;
	/// <summary>
	///死亡动画名字
	/// </summary>
	public string anim_dead;
	/// <summary>
	///走路动画名字
	/// </summary>
	public string anim_walk;
	/// <summary>
	///攻击动画名字
	/// </summary>
	public string anim_attack;
	/// <summary>
	///攻击动画循环状态
	/// </summary>
	public int anim_attack_loop;
	/// <summary>
	///攻击模式
	/// </summary>
	public int attack_mode;
	/// <summary>
	///攻击搜索敌人类型0直线 1范围 2遍历路线
	/// </summary>
	public int attack_search_type;
	/// <summary>
	///攻击搜索范围
	/// </summary>
	public float attack_search_range;
	/// <summary>
	///攻击间隔
	/// </summary>
	public float attack_cd;
	/// <summary>
	///死亡动画时间
	/// </summary>
	public float anim_dead_time;
	/// <summary>
	///攻击动画出手时间
	/// </summary>
	public float anim_attack_time;
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
