using System;
using System.Collections.Generic;
using UnityEngine;
public partial class ItemsInfoBean
{
    public Dictionary<ItemsInfoAttackModeDataEnum,string> dicAttackModeData;

    /// <summary>
    /// 获取道具类型
    /// </summary>
    public ItemTypeEnum GetItemType()
    {
        return (ItemTypeEnum)item_type;
    }

    /// <summary>
    /// 处理攻击模块数据
    /// </summary>
    public void HandleItemsInfoAttackModeData(SpriteRenderer spriteRenderer)
    {
        if (dicAttackModeData == null)
        {
            dicAttackModeData = attack_mode_data.SplitForDictionary<ItemsInfoAttackModeDataEnum>();
        }
        foreach (var item in dicAttackModeData)
        {
            switch (item.Key)
            {
                case ItemsInfoAttackModeDataEnum.VertexRotateAxis:
                    var itemVertexRotateAxis = item.Value.SplitForArrayFloat(',');
                    spriteRenderer.material.SetVector("_VertexRotateAxis", new Vector3(itemVertexRotateAxis[0],itemVertexRotateAxis[1],itemVertexRotateAxis[2]));
                    break;
                case ItemsInfoAttackModeDataEnum.VertexRotateSpeed:
                    var itemVertexRotateSpeed = float.Parse(item.Value);
                    spriteRenderer.material.SetFloat("_VertexRotateSpeed", itemVertexRotateSpeed);
                    break;
            }
        }
    }
}
public partial class ItemsInfoCfg
{
}
