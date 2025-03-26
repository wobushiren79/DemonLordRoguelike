using System;
using System.Collections.Generic;
[Serializable]
public partial class CreatureModelInfoBean : BaseBean
{
	/// <summary>
	///模组ID
	/// </summary>
	public int model_id;
	/// <summary>
	///展示类型0默认 1立绘
	/// </summary>
	public int show_type;
	/// <summary>
	///部件类型(0:基础类型 1:头 3：发型 4:身体 5:眼睛 6:嘴巴 7:角 8:翅膀 50:帽子 51:衣服 52:裤子 53:鞋子 54:腰带 55:鞋子 90：武器左 91：武器右)
	/// </summary>
	public int part_type;
	/// <summary>
	///资源名字
	/// </summary>
	public string res_name;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class CreatureModelInfoCfg : BaseCfg<long, CreatureModelInfoBean>
{
	public static string fileName = "CreatureModelInfo";
	protected static Dictionary<long, CreatureModelInfoBean> dicData = null;
	public static Dictionary<long, CreatureModelInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			CreatureModelInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static CreatureModelInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			CreatureModelInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(CreatureModelInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, CreatureModelInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			CreatureModelInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
