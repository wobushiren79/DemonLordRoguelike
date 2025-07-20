using System;
using System.Collections.Generic;
[Serializable]
public partial class FightTypeConquerInfoBean : BaseBean
{
	/// <summary>
	///敌人列表
	/// </summary>
	public string enemy_ids;
	/// <summary>
	///boss列表
	/// </summary>
	public string enemy_boss_ids;
	/// <summary>
	///难度
	/// </summary>
	public int level;
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
