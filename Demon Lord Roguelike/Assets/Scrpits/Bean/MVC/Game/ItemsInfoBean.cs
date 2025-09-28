using System;
using System.Collections.Generic;
[Serializable]
public partial class ItemsInfoBean : BaseBean
{
	/// <summary>
	///道具类型1:帽子 2衣服 3裤子 4鞋子 5鼻环 10武器
	/// </summary>
	public int item_type;
	/// <summary>
	///道具上限
	/// </summary>
	public int num_max;
	/// <summary>
	///生物模组信息ID
	/// </summary>
	public long creature_model_info_id;
	/// <summary>
	///图标
	/// </summary>
	public string icon_res;
	/// <summary>
	///图标旋转
	/// </summary>
	public float icon_rotate_z;
	/// <summary>
	///攻击模式相关数据(ShowSprite,VertexRotateAxis,VertexRotateSpeed,UVRotateSpeed,StartPosition)
	/// </summary>
	public string attack_mode_data;
	/// <summary>
	///名字
	/// </summary>
	public long name;
	/// <summary>
	///备注
	/// </summary>
	public string remark;
}
public partial class ItemsInfoCfg : BaseCfg<long, ItemsInfoBean>
{
	public static string fileName = "ItemsInfo";
	protected static Dictionary<long, ItemsInfoBean> dicData = null;
	public static Dictionary<long, ItemsInfoBean> GetAllData()
	{
		if (dicData == null)
		{
			ItemsInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return dicData;
	}
	public static ItemsInfoBean GetItemData(long key)
	{
		if (dicData == null)
		{
			ItemsInfoBean[] arrayData = GetInitData(fileName);
			InitData(arrayData);
		}
		return GetItemData(key, dicData);
	}
	public static void InitData(ItemsInfoBean[] arrayData)
	{
		dicData = new Dictionary<long, ItemsInfoBean>();
		for (int i = 0; i < arrayData.Length; i++)
		{
			ItemsInfoBean itemData = arrayData[i];
			dicData.Add(itemData.id, itemData);
		}
	}
}
