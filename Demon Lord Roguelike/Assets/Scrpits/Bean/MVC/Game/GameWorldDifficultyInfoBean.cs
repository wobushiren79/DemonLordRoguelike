using System;
using System.Collections.Generic;
[Serializable]
public partial class GameWorldDifficultyInfoBean : BaseBean
{
	/// <summary>
	///图标资源
	/// </summary>
	public string icon_res;
	/// <summary>
	///背景颜色
	/// </summary>
	public string color_bg;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class GameWorldDifficultyInfoCfg : BaseCfg<long, GameWorldDifficultyInfoBean>
{
	public static string fileName = "GameWorldDifficultyInfo";
	protected static Dictionary<long, GameWorldDifficultyInfoBean> dicData = null;
	public static Dictionary<long, GameWorldDifficultyInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			GameWorldDifficultyInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static GameWorldDifficultyInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			GameWorldDifficultyInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(GameWorldDifficultyInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, GameWorldDifficultyInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			GameWorldDifficultyInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
